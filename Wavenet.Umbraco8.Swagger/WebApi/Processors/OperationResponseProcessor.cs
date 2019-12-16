// <copyright file="OperationResponseProcessor.cs" company="Wavenet">
// Copyright (c) Wavenet. All rights reserved.
// </copyright>

namespace Wavenet.Umbraco8.Swagger.WebApi.Processors
{
    using System.Linq;
    using System.Reflection;

    using Namotion.Reflection;

    using NSwag.Generation.Processors;
    using NSwag.Generation.Processors.Contexts;

    using Wavenet.Umbraco8.Swagger.WebApi;

    /// <summary>
    /// Generates the operation's response objects based on reflection and the ResponseTypeAttribute, SwaggerResponseAttribute and ProducesResponseTypeAttribute attributes.
    /// </summary>
    /// <seealso cref="OperationResponseProcessorBase" />
    /// <seealso cref="IOperationProcessor" />
    internal class OperationResponseProcessor : OperationResponseProcessorBase, IOperationProcessor
    {
        private readonly WebApiOpenApiDocumentGeneratorSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationResponseProcessor"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public OperationResponseProcessor(WebApiOpenApiDocumentGeneratorSettings settings)
            : base(settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Processes the specified method information.
        /// </summary>
        /// <param name="context">The processor context.</param>
        /// <returns>
        /// true if the operation should be added to the Swagger specification.
        /// </returns>
        public bool Process(OperationProcessorContext context)
        {
            var responseTypeAttributes = context.MethodInfo.GetCustomAttributes()
                .Where(a => a.GetType().IsAssignableToTypeName("ResponseTypeAttribute", TypeNameStyle.Name) ||
                            a.GetType().IsAssignableToTypeName("SwaggerResponseAttribute", TypeNameStyle.Name))
                .Concat(context.MethodInfo.DeclaringType.GetTypeInfo().GetCustomAttributes()
                    .Where(a => a.GetType().IsAssignableToTypeName("SwaggerResponseAttribute", TypeNameStyle.Name)))
                .ToList();

            var producesResponseTypeAttributes = context.MethodInfo.GetCustomAttributes()
                .Where(a => a.GetType().IsAssignableToTypeName("ProducesResponseTypeAttribute", TypeNameStyle.Name) ||
                            a.GetType().IsAssignableToTypeName("ProducesAttribute", TypeNameStyle.Name))
                .ToList();

            var attributes = responseTypeAttributes.Concat(producesResponseTypeAttributes);

            this.ProcessResponseTypeAttributes(context, attributes);
            this.UpdateResponseDescription(context);

            return true;
        }

        /// <summary>
        /// Gets the response HTTP status code for an empty/void response and the given generator.
        /// </summary>
        /// <returns>
        /// The status code.
        /// </returns>
        protected override string GetVoidResponseStatusCode()
        {
            return this.settings.IsAspNetCore ? "200" : "204";
        }
    }
}