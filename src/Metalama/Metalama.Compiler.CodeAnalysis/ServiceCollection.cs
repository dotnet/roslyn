// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Metalama.Compiler
{
    /// <summary>
    /// Collection of services exposing an <see cref="IServiceProvider"/>.
    /// </summary>
    internal sealed class ServiceCollection : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        /// <summary>
        /// Adds a service instance to the collection.
        /// </summary>
        /// <param name="type">Service type.</param>
        /// <param name="instance">Service instance.</param>
        public void AddService(Type type, object instance)
        {
            _services[type] = instance;
        }

        /// <summary>
        /// Gets a service instance of type <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">Service type.</param>
        /// <returns>
        /// The service instance of type <paramref name="serviceType"/>
        /// or <c>null</c> if the service is not contained int the collection.
        /// </returns>
        public object? GetService(Type serviceType) => this._services.TryGetValue(serviceType, out var service) ? service : null;
    }
}
