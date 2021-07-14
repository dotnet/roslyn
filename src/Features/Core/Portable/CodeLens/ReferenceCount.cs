// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CodeLens
{
    /// <summary>
    /// Represents the result of a FindReferences Count operation.
    /// </summary>
    [DataContract]
    internal readonly struct ReferenceCount : IEquatable<ReferenceCount>
    {
        /// <summary>
        /// Represents the number of references to a given symbol.
        /// </summary>
        [DataMember(Order = 0)]
        public int Count { get; }

        /// <summary>
        /// Represents if the count is capped by a certain maximum.
        /// </summary>
        [DataMember(Order = 1)]
        public bool IsCapped { get; }

        [DataMember(Order = 2)]
        public string Version { get; }

        public ReferenceCount(int count, bool isCapped, string version)
        {
            Count = count;
            IsCapped = isCapped;
            Version = version;
        }

        public static bool operator ==(ReferenceCount left, ReferenceCount right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ReferenceCount left, ReferenceCount right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            return obj is ReferenceCount count
                && Equals(count);
        }

        public bool Equals(ReferenceCount other)
        {
            return Count == other.Count
                && IsCapped == other.IsCapped
                && Version == other.Version;
        }

        public override int GetHashCode()
        {
            var hashCode = -24231741;
            hashCode = hashCode * -1521134295 + Count.GetHashCode();
            hashCode = hashCode * -1521134295 + IsCapped.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Version);
            return hashCode;
        }
    }
}
