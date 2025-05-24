// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorRemoteServiceCallbackDispatcherRegistry : IRemoteServiceCallbackDispatcherProvider
    {
        public static readonly RazorRemoteServiceCallbackDispatcherRegistry Empty = new(Array.Empty<(Type, RazorRemoteServiceCallbackDispatcher)>());

        private readonly ImmutableDictionary<Type, RazorRemoteServiceCallbackDispatcher> _lazyDispatchers;

        public RazorRemoteServiceCallbackDispatcherRegistry(IEnumerable<(Type serviceType, RazorRemoteServiceCallbackDispatcher dispatcher)> lazyDispatchers)
        {
            _lazyDispatchers = lazyDispatchers.ToImmutableDictionary(e => e.serviceType, e => e.dispatcher);
        }

        IRemoteServiceCallbackDispatcher IRemoteServiceCallbackDispatcherProvider.GetDispatcher(Type serviceType)
            => _lazyDispatchers[serviceType];
    }
}
