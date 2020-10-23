// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal readonly struct RemoteServiceCallbackDispatcherRegistry

    {
        public sealed class ExportMetadata
        {
            public Type ServiceInterface { get; }

            public ExportMetadata(IDictionary<string, object> data)
            {
                var serviceInterface = data.GetValueOrDefault(nameof(ExportRemoteServiceCallbackDispatcherAttribute.ServiceInterface));
                Contract.ThrowIfNull(serviceInterface);
                ServiceInterface = (Type)serviceInterface;
            }
        }

        private readonly ImmutableDictionary<Type, Lazy<IRemoteServiceCallbackDispatcher, ExportMetadata>> _callbackDispatchers;

        public RemoteServiceCallbackDispatcherRegistry(IEnumerable<Lazy<IRemoteServiceCallbackDispatcher, ExportMetadata>> dispatchers)
            => _callbackDispatchers = dispatchers.ToImmutableDictionary(d => d.Metadata.ServiceInterface);

        public IRemoteServiceCallbackDispatcher GetDispatcher(Type serviceType)
            => _callbackDispatchers[serviceType].Value;
    }
}
