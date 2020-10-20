// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    [DataContract]
    internal readonly struct RemoteServiceCallbackId : IEquatable<RemoteServiceCallbackId>
    {
        [DataMember(Order = 0)]
        public readonly int Id;

        public RemoteServiceCallbackId(int id)
            => Id = id;

        public override bool Equals(object? obj)
            => obj is RemoteServiceCallbackId id && Equals(id);

        public bool Equals(RemoteServiceCallbackId other)
            => Id == other.Id;

        public override int GetHashCode()
            => Id;

        public static bool operator ==(RemoteServiceCallbackId left, RemoteServiceCallbackId right)
            => left.Equals(right);

        public static bool operator !=(RemoteServiceCallbackId left, RemoteServiceCallbackId right)
            => !(left == right);
    }

    internal abstract class RemoteServiceCallbackDispatcher
    {
        internal readonly struct Handle : IDisposable
        {
            private readonly ConcurrentDictionary<RemoteServiceCallbackId, object> _callbackInstances;
            public readonly RemoteServiceCallbackId Id;

            public Handle(ConcurrentDictionary<RemoteServiceCallbackId, object> callbackInstances, RemoteServiceCallbackId callbackId)
            {
                _callbackInstances = callbackInstances;
                Id = callbackId;
            }

            public void Dispose()
            {
                Contract.ThrowIfTrue(_callbackInstances?.TryRemove(Id, out _) == false);
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
