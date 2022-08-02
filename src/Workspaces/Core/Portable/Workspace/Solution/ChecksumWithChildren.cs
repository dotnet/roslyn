// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Serialization
{
    /// <summary>
    /// this is a collection that has its own checksum and contains only checksum or checksum collection as its children.
    /// </summary>
    internal abstract class ChecksumWithChildren : IChecksummedObject
    {
        public ChecksumWithChildren(ImmutableArray<object> children)
        {
            Checksum = CreateChecksum(children);
            Children = children;
        }

        public Checksum Checksum { get; }

        public ImmutableArray<object> Children { get; }

        private static Checksum CreateChecksum(ImmutableArray<object> children)
        {
            // given children must be either Checksum or Checksums (collection of a checksum)
            return Checksum.Create(children.Select(c => c as Checksum ?? ((ChecksumCollection)c).Checksum));
        }
    }
}
