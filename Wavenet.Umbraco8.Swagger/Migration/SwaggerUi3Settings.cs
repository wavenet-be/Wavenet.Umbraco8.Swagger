// <copyright file="SwaggerUi3Settings.cs" company="Wavenet">
// Copyright (c) Wavenet. All rights reserved.
// </copyright>

namespace Wavenet.Umbraco8.Swagger.Migration
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.Owin;

    using Newtonsoft.Json;

    using NSwag.Generation;

    using Wavenet.Umbraco8.Swagger;

    /// <summary>
    /// The settings for UseSwaggerUi3.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="OpenApiDocumentGeneratorSettings"/>.</typeparam>
    /// <seealso cref="SwaggerUiSettingsBase{T}" />
    internal class SwaggerUi3Settings<T> : SwaggerUiSettingsBase<T>
        where T : OpenApiDocumentGeneratorSettings, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerUi3Settings{T}"/> class.
        /// </summary>
        public SwaggerUi3Settings()
        {
            this.DocExpansion = "none";
            this.OperationsSorter = "none";
            this.DefaultModelsExpandDepth = 1;
            this.DefaultModelExpandDepth = 1;
            this.TagsSorter = "none";
        }

        /// <summary>
        /// Gets the additional Swagger UI 3 settings.
        /// </summary>
        /// <value>
        /// The additional settings.
        /// </value>
        public IDictionary<string, object> AdditionalSettings { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the default expansion depth for the model on the model-example section in Swagger UI 3.
        /// </summary>
        /// <value>
        /// The default model expand depth.
        /// </value>
        public int DefaultModelExpandDepth
        {
            get => (int)this.AdditionalSettings["defaultModelExpandDepth"];
            set => this.AdditionalSettings["defaultModelExpandDepth"] = value;
        }

        /// <summary>
        /// Gets or sets the default expansion depth for models (set to -1 completely hide the models) in Swagger UI 3.
        /// </summary>
        /// <value>
        /// The default models expand depth.
        /// </value>
        public int DefaultModelsExpandDepth
        {
            get => (int)this.AdditionalSettings["defaultModelsExpandDepth"];
            set => this.AdditionalSettings["defaultModelsExpandDepth"] = value;
        }

        /// <summary>
        /// Gets or sets controls how the API listing is displayed. It can be set to 'none' (default), 'list' (shows operations for each resource), or 'full' (fully expanded: shows operations and their details).
        /// </summary>
        /// <value>
        /// The document expansion.
        /// </value>
        public string DocExpansion
        {
            get => (string)this.AdditionalSettings["docExpansion"];
            set => this.AdditionalSettings["docExpansion"] = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether specifies whether the "Try it out" option is enabled in Swagger UI 3.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable try it out]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableTryItOut { get; set; } = true;

        /// <summary>
        /// Gets or sets the Swagger UI OAuth2 client settings.
        /// </summary>
        /// <value>
        /// The o auth2 client.
        /// </value>
        public OAuth2ClientSettings OAuth2Client { get; set; }

        /// <summary>
        /// Gets or sets the operations sorter in Swagger UI 3.
        /// </summary>
        /// <value>
        /// The operations sorter.
        /// </value>
        public string OperationsSorter
        {
            get => (string)this.AdditionalSettings["operationsSorter"];
            set => this.AdditionalSettings["operationsSorter"] = value;
        }

        /// <summary>
        /// Gets or sets the server URL.
        /// </summary>
        /// <value>
        /// The server URL.
        /// </value>
        public string ServerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets the Swagger URL routes (must start with '/', hides SwaggerRoute).
        /// </summary>
        /// <value>
        /// The swagger routes.
        /// </value>
        public ICollection<SwaggerUi3Route> SwaggerRoutes { get; } = new List<SwaggerUi3Route>();

        /// <summary>
        /// Gets or sets the tags sorter in Swagger UI 3.
        /// </summary>
        /// <value>
        /// The tags sorter.
        /// </value>
        public string TagsSorter
        {
            get => (string)this.AdditionalSettings["tagsSorter"];
            set => this.AdditionalSettings["tagsSorter"] = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Swagger specification should be validated.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the Swagger specification should be validated; otherwise, <c>false</c>.
        /// </value>
        public bool ValidateSpecification { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to send credentials from the Swagger UI 3 to the backend.
        /// </summary>
        /// <value>
        ///   <c>true</c> if sent credentials; otherwise, <c>false</c>.
        /// </value>
        public bool WithCredentials
        {
            get => (bool)this.AdditionalSettings["withCredentials"];
            set => this.AdditionalSettings["withCredentials"] = value;
        }
    }
}