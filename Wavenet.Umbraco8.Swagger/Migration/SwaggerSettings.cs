// <copyright file="SwaggerSettings.cs" company="Wavenet">
// Copyright (c) Wavenet. All rights reserved.
// </copyright>

namespace Wavenet.Umbraco8.Swagger.Migration
{
    using System;

    using Newtonsoft.Json;

    using NSwag;
    using NSwag.Generation;

    /// <summary>
    /// The settings for UseSwagger.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="OpenApiDocumentGeneratorSettings"/>.</typeparam>
    public class SwaggerSettings<T>
        where T : OpenApiDocumentGeneratorSettings, new()
    {
        /// <summary>Initializes a new instance of the <see cref="SwaggerSettings{T}"/> class.</summary>
        public SwaggerSettings()
        {
            this.GeneratorSettings = new T();
        }

        /// <summary>Gets or sets the Swagger document route (must start with '/', default: '/swagger/v1/swagger.json').</summary>
        public string DocumentPath { get; set; } = "/swagger/v1/swagger.json";

        /// <summary>Gets or sets for how long a <see cref="Exception"/> caught during schema generation is cached.</summary>
        public TimeSpan ExceptionCacheTime { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>Gets the generator settings.</summary>
        public T GeneratorSettings { get; }

        /// <summary>Gets or sets the OWIN base path (when mapped via app.MapOwinPath()) (must start with '/').</summary>
        public string MiddlewareBasePath { get; set; }

        /// <summary>Gets or sets the Swagger post process action.</summary>
        public Action<OpenApiDocument> PostProcess { get; set; }

        /// <summary>
        /// Creates the generator settings.
        /// </summary>
        /// <param name="serializerSettings">The serializer settings.</param>
        /// <param name="mvcOptions">The MVC options.</param>
        /// <returns>The generator settings.</returns>
        internal T CreateGeneratorSettings(JsonSerializerSettings serializerSettings, object mvcOptions)
        {
            this.GeneratorSettings.ApplySettings(serializerSettings, mvcOptions);
            return this.GeneratorSettings;
        }
    }
}