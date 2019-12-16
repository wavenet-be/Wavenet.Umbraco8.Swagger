// <copyright file="OAuth2ClientSettings.cs" company="Wavenet">
// Copyright (c) Wavenet. All rights reserved.
// </copyright>

namespace Wavenet.Umbraco8.Swagger
{
    using System.Collections.Generic;

    /// <summary>The OAuth client settings.</summary>
    internal class OAuth2ClientSettings
    {
        /// <summary>Gets or sets the client identifier.</summary>
        public string ClientId { get; set; } = "client_id";

        /// <summary>Gets or sets the client secret.</summary>
        public string ClientSecret { get; set; } = "client_secret";

        /// <summary>Gets or sets the realm.</summary>
        public string Realm { get; set; } = "realm";

        /// <summary>Gets or sets the name of the application.</summary>
        public string AppName { get; set; } = "app_name";

        /// <summary>Gets or sets the scope separator.</summary>
        public string ScopeSeparator { get; set; } = " ";

        /// <summary>Gets the additional query string parameters.</summary>
        public IDictionary<string, string> AdditionalQueryStringParameters { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets a value indicating whether proof Key for Code Exchange. Only applies to `accessCode` flow. Supported in SwaggerUI 3.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use pkce with authorization code grant]; otherwise, <c>false</c>.
        /// </value>
        public bool UsePkceWithAuthorizationCodeGrant { get; set; }
    }
}