// <copyright file="SwaggerUiSettingsBase.cs" company="Wavenet">
// Copyright (c) Wavenet. All rights reserved.
// </copyright>

namespace Wavenet.Umbraco8.Swagger.Migration
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Owin;

    using Newtonsoft.Json;

    using NSwag.Generation;

    /// <summary>
    /// The base settings for all Swagger UIs.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="OpenApiDocumentGeneratorSettings"/>.</typeparam>
    /// <seealso cref="SwaggerSettings{T}" />
    public abstract class SwaggerUiSettingsBase<T> : SwaggerSettings<T>
        where T : OpenApiDocumentGeneratorSettings, new()
    {
        /// <summary>Initializes a new instance of the <see cref="SwaggerUiSettingsBase{T}"/> class.</summary>
        public SwaggerUiSettingsBase()
        {
        }

        /// <summary>Gets or sets a URI to load a custom JavaScript file into the index.html.</summary>
        public string CustomJavaScriptPath { get; set; }

        /// <summary>Gets or sets a URI to load a custom CSS Stylesheet into the index.html.</summary>
        public string CustomStylesheetPath { get; set; }

        /// <summary>Gets or sets the internal swagger UI route (must start with '/').</summary>
        public string Path { get; set; } = "/swagger";

        /// <summary>Gets or sets the external route base path (must start with '/', default: null = use SwaggerUiRoute).</summary>
        public Func<string, IOwinRequest, string> TransformToExternalPath { get; set; }

        /// <summary>Generates the additional objects JavaScript code.</summary>
        /// <param name="additionalSettings">The additional settings.</param>
        /// <returns>The code.</returns>
        protected string GenerateAdditionalSettings(IDictionary<string, object> additionalSettings)
        {
            var code = string.Empty;
            foreach (var pair in additionalSettings)
            {
                code += pair.Key + ": " + JsonConvert.SerializeObject(pair.Value) + ", \n    ";
            }

            return code;
        }

        /// <summary>
        /// Gets an HTML snippet for including custom JavaScript in swagger UI.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The custome script HTML.</returns>
        protected string GetCustomScriptHtml(IOwinRequest request)
        {
            if (this.CustomJavaScriptPath == null)
            {
                return string.Empty;
            }

            var uriString = System.Net.WebUtility.HtmlEncode(this.TransformToExternalPath(this.CustomJavaScriptPath, request));
            return $"<script src=\"{uriString}\"></script>";
        }

        /// <summary>
        /// Gets an HTML snippet for including custom StyleSheet in swagger UI.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The custom style HTML.</returns>
        protected string GetCustomStyleHtml(IOwinRequest request)
        {
            if (this.CustomStylesheetPath == null)
            {
                return string.Empty;
            }

            var uriString = System.Net.WebUtility.HtmlEncode(this.TransformToExternalPath(this.CustomStylesheetPath, request));
            return $"<link rel=\"stylesheet\" href=\"{uriString}\">";
        }
    }
}