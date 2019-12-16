// <copyright file="BaseSwaggerComponent.cs" company="Wavenet">
// Copyright (c) Wavenet. All rights reserved.
// </copyright>

namespace Wavenet.Umbraco8.Swagger.Components
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Web.Http;
    using System.Web.Mvc;
    using System.Web.Routing;

    using Umbraco.Core.Composing;

    /// <summary>
    /// The base Swagger Component for Umbraco.
    /// </summary>
    /// <seealso cref="Umbraco.Core.Composing.IUserComposer" />
    public abstract class BaseSwaggerComponent : IUserComposer
    {
        /// <summary>
        /// Gets the apis.
        /// </summary>
        /// <value>
        /// The apis.
        /// </value>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Dictionary<(string version, string api), (string name, IEnumerable<Type> controllers)> Apis { get; } = new Dictionary<(string version, string api), (string name, IEnumerable<Type> controllers)>();

        /// <summary>
        /// Gets a value indicating whether the UI is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the UI is enabled; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsUiEnabled { get; } = true;

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public virtual string Path { get; } = "/swagger";

        /// <summary>
        /// Composes the specified composition.
        /// </summary>
        /// <param name="composition">The composition.</param>
        public void Compose(Composition composition)
        {
            var path = this.Path.Trim('/');
            if (this.IsUiEnabled)
            {
                RouteTable.Routes.MapRoute("WavenetSwaggerUI", path, new
                {
                    controller = "Swagger",
                    action = "Index",
                });
            }

            RouteTable.Routes.MapRoute("WavenetSwagger", path + "/{version}/{api}", new
            {
                controller = "Swagger",
                action = "Document",
            });

            this.Configure();
        }

        /// <summary>
        /// Configures this instance.
        /// </summary>
        public abstract void Configure();

        /// <summary>
        /// Registers all <see cref="ApiController"/> from the specified <paramref name="assemblies"/>.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="api">The API.</param>
        /// <param name="name">The name.</param>
        /// <param name="assemblies">The assemblies.</param>
        protected void Register(string version, string api, string name, params Assembly[] assemblies)
            => this.Register(version, api, name, assemblies.AsEnumerable());

        /// <summary>
        /// Registers all <see cref="ApiController"/> from the specified <paramref name="assemblies"/>.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="api">The API.</param>
        /// <param name="name">The name.</param>
        /// <param name="assemblies">The assemblies.</param>
        protected void Register(string version, string api, string name, IEnumerable<Assembly> assemblies)
        {
            var type = typeof(ApiController);
            this.Register(version, api, name, assemblies.SelectMany(a => a.GetTypes().Where(t => type.IsAssignableFrom(t))));
        }

        /// <summary>
        /// Registers the specified <paramref name="controllerTypes"/>.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="api">The API.</param>
        /// <param name="name">The name.</param>
        /// <param name="controllerTypes">The controller types.</param>
        protected void Register(string version, string api, string name, IEnumerable<Type> controllerTypes)
        {
            Apis.Add((version.ToLowerInvariant(), api.ToLowerInvariant()), (name, controllerTypes));
        }
    }
}