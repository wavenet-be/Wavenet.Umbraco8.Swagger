// <copyright file="SwaggerUi3Route.cs" company="Wavenet">
// Copyright (c) Wavenet. All rights reserved.
// </copyright>

namespace Wavenet.Umbraco8.Swagger.Migration
{
    using System;

    using Newtonsoft.Json;

    /// <summary>Specifies a route in the Swagger dropdown.</summary>
    internal class SwaggerUi3Route
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerUi3Route" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="url">The URL.</param>
        public SwaggerUi3Route(string name, string url)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            this.Name = name;
            this.Url = url;
        }

        /// <summary>
        /// Gets or sets the route name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the route URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}