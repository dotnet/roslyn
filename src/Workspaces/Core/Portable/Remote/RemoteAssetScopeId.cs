// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Remote
{
    [DataContract]
    internal readonly struct RemoteAssetScopeId : IEquatable<RemoteAssetScopeId>
    {
        [DataMember(Order = 0)]
        public readonly int Id;

        public RemoteAssetScopeId(int id)
            => Id = id;

        public override bool Equals(object? obj)
            => obj is RemoteAssetScopeId id && Equals(id);

        public bool Equals(RemoteAssetScopeId other)
            => Id == other.Id;

        public override int GetHashCode()
            => Id;

        public static bool operator ==(RemoteAssetScopeId left, RemoteAssetScopeId right)
            => left.Equals(right);

        public static bool operator !=(RemoteAssetScopeId left, RemoteAssetScopeId right)
            => !(left == right);
    }
}
