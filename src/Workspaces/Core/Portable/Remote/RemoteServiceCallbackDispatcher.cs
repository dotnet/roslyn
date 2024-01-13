// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IRemoteServiceCallbackDispatcher
    {
        RemoteServiceCallbackDispatcher.Handle CreateHandle(object? instance);
    }

    internal class RemoteServiceCallbackDispatcher : IRemoteServiceCallbackDispatcher
    {
        internal readonly struct Handle(ConcurrentDictionary<RemoteServiceCallbackId, object> callbackInstances, RemoteServiceCallbackId callbackId) : IDisposable
        {
            public readonly RemoteServiceCallbackId Id = callbackId;

            public void Dispose()
            {
                Contract.ThrowIfTrue(callbackInstances?.TryRemove(Id, out _) == false);
            }
        }

        private int _callbackId = 1;
        private readonly ConcurrentDictionary<RemoteServiceCallbackId, object> _callbackInstances = new(concurrencyLevel: 2, capacity: 10);

        public Handle CreateHandle(object? instance)
        {
            if (instance is null)
            {
                return default;
            }

            var callbackId = new RemoteServiceCallbackId(Interlocked.Increment(ref _callbackId));
            var handle = new Handle(_callbackInstances, callbackId);
            _callbackInstances.Add(callbackId, instance);
            return handle;
        }

        public object GetCallback(RemoteServiceCallbackId callbackId)
        {
            Contract.ThrowIfFalse(_callbackInstances.TryGetValue(callbackId, out var instance));
            return instance;
        }
    }
}
