// <copyright file="SwaggerController.cs" company="Wavenet">
// Copyright (c) Wavenet. All rights reserved.
// </copyright>

namespace Wavenet.Umbraco8.Swagger.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Mvc;

    using NSwag;

    using Umbraco.Web.Mvc;
    using Umbraco.Web.WebApi;
    using Wavenet.Umbraco8.Swagger.Components;
    using Wavenet.Umbraco8.Swagger.Migration;
    using Wavenet.Umbraco8.Swagger.WebApi;

    /// <summary>
    /// Swagger Controller.
    /// </summary>
    /// <seealso cref="SurfaceController" />
    public class SwaggerController : SurfaceController
    {
        /// <summary>
        /// The settings.
        /// </summary>
        private readonly SwaggerSettings<WebApiOpenApiDocumentGeneratorSettings> settings = new SwaggerSettings<WebApiOpenApiDocumentGeneratorSettings>();

        /// <summary>
        /// Swagger UI.
        /// </summary>
        /// <returns>The Swagger UI.</returns>
        public ActionResult Index()
            => this.View("~/App_Plugins/WavenetSwagger/Index.cshtml");

        /// <summary>
        /// Gets the OpenApi document.
        /// </summary>
        /// <param name="version">The desired version.</param>
        /// <param name="api">The desired API.</param>
        /// <returns>The <see cref="OpenApiDocument"/>.</returns>
        [ActionName("Document")]
        public async Task<ActionResult> DocumentAsync(string version, string api)
        {
            if (BaseSwaggerComponent.Apis.TryGetValue((version.ToLowerInvariant(), api.ToLowerInvariant()), out var result))
            {
                return this.Content(await this.GenerateDocumentAsync(version, result.name, result.controllers), "application/json");
            }
            else
            {
                return this.HttpNotFound();
            }
        }

        /// <summary>
        /// Generates the Swagger specification.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="name">The name.</param>
        /// <param name="controllerTypes">The controller types.</param>
        /// <returns>
        /// The Swagger specification.
        /// </returns>
        protected virtual async Task<string> GenerateDocumentAsync(string version, string name, IEnumerable<Type> controllerTypes)
        {
            var settings = this.settings.CreateGeneratorSettings(null, null);
            settings.Version = version;
            settings.Title = name;
            var generator = new WebApiOpenApiDocumentGenerator(settings);
            var document = await generator.GenerateForControllersAsync(controllerTypes);

            if (this.settings.MiddlewareBasePath != null)
            {
                document.Host = this.Request.Url.Host ?? string.Empty;
                document.Schemes.Add(this.Request.Url.Scheme == "http" ? OpenApiSchema.Http : OpenApiSchema.Https);
                var basePath = this.Request.Url.AbsolutePath;
                document.BasePath = basePath.Substring(0, basePath.Length - (this.settings.MiddlewareBasePath?.Length ?? 0));
            }
            else
            {
                document.Servers.Clear();
                document.Servers.Add(new OpenApiServer
                {
                    Url = new Uri(this.Request.Url, "/").ToString(),
                });
            }

            this.settings.PostProcess?.Invoke(document);
            return document.ToJson();
        }
    }
}