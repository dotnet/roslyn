// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Remote
{
    [DataContract]
    internal readonly struct RemoteServiceCallbackId(int id) : IEquatable<RemoteServiceCallbackId>
    {
        [DataMember(Order = 0)]
        public readonly int Id = id;

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
}
