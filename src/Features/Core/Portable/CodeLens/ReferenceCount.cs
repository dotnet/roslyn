// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CodeLens
{
    /// <summary>
    /// Represents the result of a FindReferences Count operation.
    /// </summary>
    [DataContract]
    internal readonly struct ReferenceCount
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

        public ReferenceCount(int count, bool isCapped)
        {
            Count = count;
            IsCapped = isCapped;
        }
    }
}
