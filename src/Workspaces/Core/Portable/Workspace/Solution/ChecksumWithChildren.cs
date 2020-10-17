﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Serialization
{
    /// <summary>
    /// this is a collection that has its own checksum and contains only checksum or checksum collection as its children.
    /// </summary>
    internal abstract class ChecksumWithChildren : IChecksummedObject
    {
        public ChecksumWithChildren(WellKnownSynchronizationKind kind, params object[] children)
        {
            Checksum = CreateChecksum(kind, children);
            Children = children;
        }

        public Checksum Checksum { get; }

        public IReadOnlyList<object> Children { get; }

        private static Checksum CreateChecksum(WellKnownSynchronizationKind kind, object[] children)
        {
            // given children must be either Checksum or Checksums (collection of a checksum)
            return Checksum.Create(kind, children.Select(c => c as Checksum ?? ((ChecksumCollection)c).Checksum));
        }
    }
}
