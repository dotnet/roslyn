// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Caravela.Compiler
{
    internal sealed class ServiceCollection
    {
        private readonly Dictionary<Type, object> _services = new();

        public void AddService(Type type, object instance)
        {
            _services[type] = instance;
        }

        public IServiceProvider GetServiceProvider()
        {
            return new ServiceProvider(this);
        }

        public sealed class ServiceProvider : IServiceProvider
        {
            private readonly ServiceCollection _parent;

            public ServiceProvider(ServiceCollection parent)
            {
                _parent = parent;
            }

            public object? GetService(Type serviceType) => this._parent._services[serviceType];
        }
    }
}
