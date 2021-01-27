// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal sealed class UnitTestingRemoteServiceCallbackDispatcherRegistry : IRemoteServiceCallbackDispatcherProvider
    {
        public static readonly UnitTestingRemoteServiceCallbackDispatcherRegistry Empty = new(Array.Empty<(Type, UnitTestingRemoteServiceCallbackDispatcher)>());

        private readonly ImmutableDictionary<Type, UnitTestingRemoteServiceCallbackDispatcher> _lazyDispatchers;

        public UnitTestingRemoteServiceCallbackDispatcherRegistry(IEnumerable<(Type serviceType, UnitTestingRemoteServiceCallbackDispatcher dispatcher)> lazyDispatchers)
        {
            _lazyDispatchers = lazyDispatchers.ToImmutableDictionary(e => e.serviceType, e => e.dispatcher);
        }

        IRemoteServiceCallbackDispatcher IRemoteServiceCallbackDispatcherProvider.GetDispatcher(Type serviceType)
            => _lazyDispatchers[serviceType];
    }
}
