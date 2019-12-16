// <copyright file="WebApiOpenApiDocumentGeneratorSettings.cs" company="Wavenet">
// Copyright (c) Wavenet. All rights reserved.
// </copyright>

namespace Wavenet.Umbraco8.Swagger.WebApi
{
    using NSwag.Generation;
    using NSwag.Generation.Processors;

    using Wavenet.Umbraco8.Swagger.WebApi.Processors;

    /// <summary>Settings for the <see cref="WebApiOpenApiDocumentGenerator"/>.</summary>
    internal class WebApiOpenApiDocumentGeneratorSettings : OpenApiDocumentGeneratorSettings
    {
        /// <summary>Initializes a new instance of the <see cref="WebApiOpenApiDocumentGeneratorSettings"/> class.</summary>
        public WebApiOpenApiDocumentGeneratorSettings()
        {
            this.OperationProcessors.Insert(0, new ApiVersionProcessor());
            this.OperationProcessors.Insert(3, new OperationParameterProcessor(this));
            this.OperationProcessors.Insert(3, new OperationResponseProcessor(this));
        }

        /// <summary>Gets or sets a value indicating whether to add path parameters which are missing in the action method.</summary>
        public bool AddMissingPathParameters { get; set; }

        /// <summary>Gets or sets the default Web API URL template (default for Web API: 'api/{controller}/{id}'; for MVC projects: '{controller}/{action}/{id?}').</summary>
        public string DefaultUrlTemplate { get; set; } = "api/{controller}/{id?}";

        /// <summary>Gets or sets a value indicating whether the controllers are hosted by ASP.NET Core.</summary>
        public bool IsAspNetCore { get; set; }
    }
}