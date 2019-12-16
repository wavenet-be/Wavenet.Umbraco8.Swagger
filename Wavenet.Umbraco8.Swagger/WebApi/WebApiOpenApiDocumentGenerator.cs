// <copyright file="WebApiOpenApiDocumentGenerator.cs" company="Wavenet">
// Copyright (c) Wavenet. All rights reserved.
// </copyright>

namespace Wavenet.Umbraco8.Swagger.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Description;

    using Namotion.Reflection;

    using NJsonSchema;

    using NSwag;
    using NSwag.Generation;
    using NSwag.Generation.Processors;
    using NSwag.Generation.Processors.Contexts;

    using Umbraco.Web.WebApi;

    /// <summary>Generates a <see cref="OpenApiDocument"/> object for the given Web API class type. </summary>
    internal class WebApiOpenApiDocumentGenerator
    {
        private Lazy<Collection<ApiDescription>> apiDescriptions = new Lazy<Collection<ApiDescription>>(() => GlobalConfiguration.Configuration.Services.GetApiExplorer().ApiDescriptions);

        /// <summary>Initializes a new instance of the <see cref="WebApiOpenApiDocumentGenerator" /> class.</summary>
        /// <param name="settings">The settings.</param>
        public WebApiOpenApiDocumentGenerator(WebApiOpenApiDocumentGeneratorSettings settings)
        {
            this.Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public WebApiOpenApiDocumentGeneratorSettings Settings { get; }

        /// <summary>Gets all controller class types of the given assembly.</summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The controller classes.</returns>
        public static IEnumerable<Type> GetControllerClasses(Assembly assembly)
        {
            // TODO: Move to IControllerClassLoader interface
            return assembly.ExportedTypes
                .Where(t => t.GetTypeInfo().IsAbstract == false)
                .Where(t => t.Name.EndsWith("Controller") ||
                            t.InheritsFromTypeName("ApiController", TypeNameStyle.Name) ||
                            t.InheritsFromTypeName("ControllerBase", TypeNameStyle.Name)) // in ASP.NET Core, a Web API controller inherits from Controller
                .Where(t => t.GetTypeInfo().ImplementedInterfaces.All(i => i.FullName != "System.Web.Mvc.IController")) // no MVC controllers (legacy ASP.NET)
                .Where(t =>
                {
                    return t.GetTypeInfo().GetCustomAttributes()
                        .SingleOrDefault(a => a.GetType().Name == "ApiExplorerSettingsAttribute")?
                        .TryGetPropertyValue("IgnoreApi", false) != true;
                });
        }

        /// <summary>Generates a Swagger specification for the given controller type.</summary>
        /// <typeparam name="TController">The type of the controller.</typeparam>
        /// <returns>The <see cref="OpenApiDocument" />.</returns>
        /// <exception cref="InvalidOperationException">The operation has more than one body parameter.</exception>
        public Task<OpenApiDocument> GenerateForControllerAsync<TController>()
        {
            return this.GenerateForControllersAsync(new[] { typeof(TController) });
        }

        /// <summary>Generates a Swagger specification for the given controller type.</summary>
        /// <param name="controllerType">The type of the controller.</param>
        /// <returns>The <see cref="OpenApiDocument" />.</returns>
        /// <exception cref="InvalidOperationException">The operation has more than one body parameter.</exception>
        public Task<OpenApiDocument> GenerateForControllerAsync(Type controllerType)
        {
            return this.GenerateForControllersAsync(new[] { controllerType });
        }

        /// <summary>Generates a Swagger specification for the given controller types.</summary>
        /// <param name="controllerTypes">The types of the controller.</param>
        /// <returns>The <see cref="OpenApiDocument" />.</returns>
        /// <exception cref="InvalidOperationException">The operation has more than one body parameter.</exception>
        public async Task<OpenApiDocument> GenerateForControllersAsync(IEnumerable<Type> controllerTypes)
        {
            var document = await this.CreateDocumentAsync().ConfigureAwait(false);
            var schemaResolver = new OpenApiSchemaResolver(document, this.Settings);

            var usedControllerTypes = new List<Type>();
            foreach (var controllerType in controllerTypes)
            {
                var generator = new OpenApiDocumentGenerator(this.Settings, schemaResolver);
                var isIncluded = this.GenerateForController(document, controllerType, generator, schemaResolver);
                if (isIncluded)
                {
                    usedControllerTypes.Add(controllerType);
                }
            }

            document.GenerateOperationIds();

            foreach (var processor in this.Settings.DocumentProcessors)
            {
                processor.Process(
                    new DocumentProcessorContext(
                        document,
                        controllerTypes,
                        usedControllerTypes,
                        schemaResolver,
                        this.Settings.SchemaGenerator,
                        this.Settings));
            }

            return document;
        }

        private static IEnumerable<MethodInfo> GetActionMethods(Type controllerType)
        {
            var methods = controllerType.GetRuntimeMethods().Where(m => m.IsPublic);
            return methods.Where(m =>
                m.IsSpecialName == false && // avoid property methods
                m.DeclaringType == controllerType && // no inherited methods (handled in GenerateForControllerAsync)
                m.DeclaringType != typeof(object) &&
                m.IsStatic == false &&
                m.GetCustomAttributes().Select(a => a.GetType()).All(a =>
                    !a.IsAssignableToTypeName("SwaggerIgnoreAttribute", TypeNameStyle.Name) &&
                    !a.IsAssignableToTypeName("NonActionAttribute", TypeNameStyle.Name)) &&
                m.DeclaringType.FullName.StartsWith("Microsoft.AspNet") == false && // .NET Core (Web API & MVC)
                m.DeclaringType.FullName != "System.Web.Http.ApiController" &&
                m.DeclaringType.FullName != "System.Web.Mvc.Controller")
                .Where(m =>
                {
                    return m.GetCustomAttributes()
                        .SingleOrDefault(a => a.GetType().Name == "ApiExplorerSettingsAttribute")?
                        .TryGetPropertyValue("IgnoreApi", false) != true;
                });
        }

        private bool AddOperationDescriptionsToDocument(OpenApiDocument document, Type controllerType, List<Tuple<OpenApiOperationDescription, MethodInfo>> operations, OpenApiDocumentGenerator swaggerGenerator, OpenApiSchemaResolver schemaResolver)
        {
            var addedOperations = 0;
            var allOperation = operations.Select(t => t.Item1).ToList();
            foreach (var tuple in operations)
            {
                var operation = tuple.Item1;
                var method = tuple.Item2;

                var addOperation = this.RunOperationProcessors(document, controllerType, method, operation, allOperation, swaggerGenerator, schemaResolver);
                if (addOperation)
                {
                    var path = operation.Path.Replace("//", "/");

                    if (!document.Paths.ContainsKey(path))
                    {
                        document.Paths[path] = new OpenApiPathItem();
                    }

                    if (document.Paths[path].ContainsKey(operation.Method))
                    {
                        throw new InvalidOperationException("The method '" + operation.Method + "' on path '" + path + "' is registered multiple times " +
                            "(check the DefaultUrlTemplate setting [default for Web API: 'api/{controller}/{id}'; for MVC projects: '{controller}/{action}/{id?}']).");
                    }

                    document.Paths[path][operation.Method] = operation.Operation;
                    addedOperations++;
                }
            }

            return addedOperations > 0;
        }

        private async Task<OpenApiDocument> CreateDocumentAsync()
        {
            var document = !string.IsNullOrEmpty(this.Settings.DocumentTemplate) ?
                await OpenApiDocument.FromJsonAsync(this.Settings.DocumentTemplate).ConfigureAwait(false) :
                new OpenApiDocument();

            document.Generator = "NSwag v" + OpenApiDocument.ToolchainVersion + " (NJsonSchema v" + JsonSchema.ToolchainVersion + ")";
            document.SchemaType = this.Settings.SchemaType;

            document.Consumes = new List<string> { "application/json" };
            document.Produces = new List<string> { "application/json" };

            if (document.Info == null)
            {
                document.Info = new OpenApiInfo();
            }

            if (string.IsNullOrEmpty(this.Settings.DocumentTemplate))
            {
                if (!string.IsNullOrEmpty(this.Settings.Title))
                {
                    document.Info.Title = this.Settings.Title;
                }

                if (!string.IsNullOrEmpty(this.Settings.Description))
                {
                    document.Info.Description = this.Settings.Description;
                }

                if (!string.IsNullOrEmpty(this.Settings.Version))
                {
                    document.Info.Version = this.Settings.Version;
                }
            }

            return document;
        }

        private IEnumerable<string> ExpandOptionalHttpPathParameters(string path, MethodInfo method)
        {
            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (segment.EndsWith("?}"))
                {
                    // Only expand if optional parameter is available in action method
                    if (method.GetParameters().Any(p => segment.StartsWith("{" + p.Name + ":") || segment.StartsWith("{" + p.Name + "?")))
                    {
                        foreach (var p in this.ExpandOptionalHttpPathParameters(string.Join("/", segments.Take(i).Concat(new[] { segment.Replace("?", string.Empty) }).Concat(segments.Skip(i + 1))), method))
                        {
                            yield return p;
                        }
                    }
                    else
                    {
                        foreach (var p in this.ExpandOptionalHttpPathParameters(string.Join("/", segments.Take(i).Concat(segments.Skip(i + 1))), method))
                        {
                            yield return p;
                        }
                    }

                    yield break;
                }
            }

            yield return path;
        }

        /// <exception cref="InvalidOperationException">The operation has more than one body parameter.</exception>
        private bool GenerateForController(OpenApiDocument document, Type controllerType, OpenApiDocumentGenerator swaggerGenerator, OpenApiSchemaResolver schemaResolver)
        {
            var hasIgnoreAttribute = controllerType.GetTypeInfo()
                .GetCustomAttributes()
                .GetAssignableToTypeName("SwaggerIgnoreAttribute", TypeNameStyle.Name)
                .Any();

            if (hasIgnoreAttribute)
            {
                return false;
            }

            var operations = new List<Tuple<OpenApiOperationDescription, MethodInfo>>();

            var currentControllerType = controllerType;
            while (currentControllerType != null)
            {
                foreach (var method in GetActionMethods(currentControllerType))
                {
                    var httpPaths = this.GetHttpPaths(controllerType, method).ToList();
                    var httpMethods = this.GetSupportedHttpMethods(method).ToList();

                    foreach (var httpPath in httpPaths)
                    {
                        foreach (var httpMethod in httpMethods)
                        {
                            var isPathAlreadyDefinedInInheritanceHierarchy =
                                operations.Any(o => o.Item1.Path == httpPath &&
                                                    o.Item1.Method == httpMethod &&
                                                    o.Item2.DeclaringType != currentControllerType &&
                                                    o.Item2.DeclaringType.IsAssignableToTypeName(currentControllerType.FullName, TypeNameStyle.FullName));

                            if (isPathAlreadyDefinedInInheritanceHierarchy == false)
                            {
                                var operationDescription = new OpenApiOperationDescription
                                {
                                    Path = httpPath,
                                    Method = httpMethod,
                                    Operation = new OpenApiOperation
                                    {
                                        IsDeprecated = method.GetCustomAttribute<ObsoleteAttribute>() != null,
                                        OperationId = this.GetOperationId(document, controllerType.Name, method),
                                    },
                                };

                                operations.Add(new Tuple<OpenApiOperationDescription, MethodInfo>(operationDescription, method));
                            }
                        }
                    }
                }

                currentControllerType = currentControllerType.GetTypeInfo().BaseType;
            }

            return this.AddOperationDescriptionsToDocument(document, controllerType, operations, swaggerGenerator, schemaResolver);
        }

        private string GetActionName(MethodInfo method)
        {
            var actionName = method.GetCustomAttribute<ActionNameAttribute>()?.Name;
            if (actionName == null)
            {
                actionName = method.Name;
                if (actionName.EndsWith("Async"))
                {
                    actionName = actionName.Substring(0, actionName.Length - 5);
                }
            }

            return actionName;
        }

        private IEnumerable<string> GetHttpPaths(Type controllerType, MethodInfo method)
        {
            var httpPaths = new List<string>();
            var controllerName = controllerType.Name.Replace("Controller", string.Empty);
            var routeAttributes = this.GetRouteAttributes(method.GetCustomAttributes()).ToList();
            var routeAttributeOnClass = this.GetRouteAttribute(controllerType);
            var routePrefixAttribute = this.GetRoutePrefixAttribute(controllerType);

            if (routeAttributes.Any())
            {
                foreach (var attribute in routeAttributes)
                {
                    if (attribute.Template.StartsWith("~/"))
                    {
                        httpPaths.Add(attribute.Template.Substring(1));
                    }
                    else if (routePrefixAttribute != null)
                    {
                        httpPaths.Add(routePrefixAttribute.Prefix + "/" + attribute.Template);
                    }
                    else if (routeAttributeOnClass != null)
                    {
                        if (attribute.Template.StartsWith("/"))
                        {
                            httpPaths.Add(attribute.Template);
                        }
                        else
                        {
                            httpPaths.Add(routeAttributeOnClass.Template + "/" + attribute.Template);
                        }
                    }
                    else
                    {
                        httpPaths.Add(attribute.Template);
                    }
                }
            }
            else if (routePrefixAttribute != null && routeAttributeOnClass != null)
            {
                httpPaths.Add(routePrefixAttribute.Prefix + "/" + routeAttributeOnClass.Template);
            }
            else if (routePrefixAttribute != null)
            {
                httpPaths.Add(routePrefixAttribute.Prefix);
            }
            else if (routeAttributeOnClass != null)
            {
                httpPaths.Add(routeAttributeOnClass.Template);
            }
            else
            {
                var umbracoApiControllerType = typeof(UmbracoApiController);
                var apiDescriptor = this.apiDescriptions.Value.FirstOrDefault(a => umbracoApiControllerType.IsAssignableFrom(a.ActionDescriptor.ControllerDescriptor.ControllerType) && a.ActionDescriptor is ReflectedHttpActionDescriptor descriptor && descriptor.MethodInfo == method);
                if (apiDescriptor != null)
                {
                    var parts = apiDescriptor.RelativePath.Split('?').ToList();
                    if (parts.Count == 2)
                    {
                        var query = HttpUtility.ParseQueryString(parts[1]);
                        var parameters = method.GetParameters().ToDictionary(keySelector: p => p.Name, StringComparer.OrdinalIgnoreCase);
                        for (int i = query.Count - 1; i >= 0; i--)
                        {
                            var key = query.GetKey(i);
                            if (parameters.TryGetValue(query.GetKey(i), out var parameter) &&
                                (parameter.IsOptional || parameter.HasDefaultValue))
                            {
                                query.Remove(key);
                            }
                        }

                        if (query.Count > 0)
                        {
                            parts[1] = Regex.Replace(query.ToString(), "%7[bd]", m => HttpUtility.UrlDecode(m.Value), RegexOptions.IgnoreCase);
                        }
                        else
                        {
                            parts.RemoveAt(1);
                        }
                    }

                    httpPaths.Add(string.Join("?", parts));
                }
                else
                {
                    httpPaths.Add(this.Settings.DefaultUrlTemplate ?? string.Empty);
                }
            }

            var actionName = this.GetActionName(method);
            return httpPaths
                .SelectMany(p => this.ExpandOptionalHttpPathParameters(p, method))
                .Select(p =>
                    "/" + p
                        .Replace("[", "{")
                        .Replace("]", "}")
                        .Replace("{controller}", controllerName)
                        .Replace("{action}", actionName)
                        .Replace("{*", "{") // wildcard path parameters are not supported in Swagger
                        .Trim('/'))
                .Distinct()
                .ToList();
        }

        private string GetOperationId(OpenApiDocument document, string controllerName, MethodInfo method)
        {
            string operationId;

            dynamic swaggerOperationAttribute = method
                .GetCustomAttributes()
                .FirstAssignableToTypeNameOrDefault("SwaggerOperationAttribute", TypeNameStyle.Name);

            if (swaggerOperationAttribute != null && !string.IsNullOrEmpty(swaggerOperationAttribute.OperationId))
            {
                operationId = swaggerOperationAttribute.OperationId;
            }
            else
            {
                if (controllerName.EndsWith("Controller"))
                {
                    controllerName = controllerName.Substring(0, controllerName.Length - 10);
                }

                operationId = controllerName + "_" + this.GetActionName(method);
            }

            var number = 1;
            while (document.Operations.Any(o => o.Operation.OperationId == operationId + (number > 1 ? "_" + number : string.Empty)))
            {
                number++;
            }

            return operationId + (number > 1 ? number.ToString() : string.Empty);
        }

        private RouteAttribute GetRouteAttribute(Type type)
            => type.GetCustomAttribute<RouteAttribute>(true);

        private IEnumerable<RouteAttribute> GetRouteAttributes(IEnumerable<Attribute> attributes)
            => attributes.OfType<RouteAttribute>().Where(r => !string.IsNullOrEmpty(r.Template));

        private RoutePrefixAttribute GetRoutePrefixAttribute(Type type)
            => type.GetCustomAttribute<RoutePrefixAttribute>(true);

        private IEnumerable<RoutePrefixAttribute> GetRoutePrefixAttributes(IEnumerable<Attribute> attributes)
            => attributes.OfType<RoutePrefixAttribute>().Where(r => !string.IsNullOrEmpty(r.Prefix));

        private IEnumerable<string> GetSupportedHttpMethods(MethodInfo method)
        {
            // See http://www.asp.net/web-api/overview/web-api-routing-and-actions/routing-in-aspnet-web-api
            var actionName = this.GetActionName(method);
            var httpMethods = this.GetSupportedHttpMethodsFromAttributes(method).ToList();
            foreach (var httpMethod in httpMethods)
            {
                yield return httpMethod;
            }

            if (!httpMethods.Any())
            {
                if (actionName.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
                {
                    yield return OpenApiOperationMethod.Get;
                }
                else if (actionName.StartsWith("Post", StringComparison.OrdinalIgnoreCase))
                {
                    yield return OpenApiOperationMethod.Post;
                }
                else if (actionName.StartsWith("Put", StringComparison.OrdinalIgnoreCase))
                {
                    yield return OpenApiOperationMethod.Put;
                }
                else if (actionName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
                {
                    yield return OpenApiOperationMethod.Delete;
                }
                else if (actionName.StartsWith("Patch", StringComparison.OrdinalIgnoreCase))
                {
                    yield return OpenApiOperationMethod.Patch;
                }
                else if (actionName.StartsWith("Options", StringComparison.OrdinalIgnoreCase))
                {
                    yield return OpenApiOperationMethod.Options;
                }
                else if (actionName.StartsWith("Head", StringComparison.OrdinalIgnoreCase))
                {
                    yield return OpenApiOperationMethod.Head;
                }
                else
                {
                    yield return OpenApiOperationMethod.Post;
                }
            }
        }

        private IEnumerable<string> GetSupportedHttpMethodsFromAttributes(MethodInfo method)
        {
            return method.GetCustomAttributes(true)
                         .OfType<IActionHttpMethodProvider>()
                         .SelectMany(a => a.HttpMethods)
                         .Concat(method.GetCustomAttribute<AcceptVerbsAttribute>()?.HttpMethods ?? Enumerable.Empty<HttpMethod>())
                         .Select(m => m.Method.ToLowerInvariant())
                         .Distinct();
        }

        private bool RunOperationProcessors(OpenApiDocument document, Type controllerType, MethodInfo methodInfo, OpenApiOperationDescription operationDescription, List<OpenApiOperationDescription> allOperations, OpenApiDocumentGenerator swaggerGenerator, OpenApiSchemaResolver schemaResolver)
        {
            var context = new OperationProcessorContext(
                                document,
                                operationDescription,
                                controllerType,
                                methodInfo,
                                swaggerGenerator,
                                this.Settings.SchemaGenerator,
                                schemaResolver,
                                this.Settings,
                                allOperations);

            // 1. Run from settings
            foreach (var operationProcessor in this.Settings.OperationProcessors)
            {
                if (operationProcessor.Process(context) == false)
                {
                    return false;
                }
            }

            var operationProcessorAttribute = methodInfo.DeclaringType.GetTypeInfo()
                .GetCustomAttributes() // 2. Run from class attributes
                .Concat(methodInfo.GetCustomAttributes()) // 3. Run from method attributes
                .Where(a => a.GetType().IsAssignableToTypeName("SwaggerOperationProcessorAttribute", TypeNameStyle.Name));

            foreach (dynamic attribute in operationProcessorAttribute)
            {
                var operationProcessor = ObjectExtensions.HasProperty(attribute, "Parameters") ?
                    (IOperationProcessor)Activator.CreateInstance(attribute.Type, attribute.Parameters) :
                    (IOperationProcessor)Activator.CreateInstance(attribute.Type);

                if (!operationProcessor.Process(context))
                {
                    return false;
                }
            }

            return true;
        }
    }
}