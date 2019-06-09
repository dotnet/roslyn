// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;

namespace Microsoft.Cci
{
    /// <summary>
    /// Represents the portion of a <see cref="DebugSourceDocument"/> that are derived
    /// from the source document content, and which can be computed asynchronously.
    /// </summary>
    internal struct DebugSourceInfo
    {
        /// <summary>
        /// The ID of the hash algorithm used.
        /// </summary>
        public readonly Guid ChecksumAlgorithmId;

        /// <summary>
        /// The hash of the document content.
        /// </summary>
        public readonly ImmutableArray<byte> Checksum;

        /// <summary>
        /// The source text to embed in the PDB. (If any, otherwise default.)
        /// </summary>
        public readonly ImmutableArray<byte> EmbeddedTextBlob;

        public DebugSourceInfo(
            ImmutableArray<byte> checksum,
            SourceHashAlgorithm checksumAlgorithm,
            ImmutableArray<byte> embeddedTextBlob = default(ImmutableArray<byte>))
            : this(checksum, DebugSourceDocument.GetAlgorithmGuid(checksumAlgorithm), embeddedTextBlob)
        {
        }

        public DebugSourceInfo(
            ImmutableArray<byte> checksum,
            Guid checksumAlgorithmId,
            ImmutableArray<byte> embeddedTextBlob = default(ImmutableArray<byte>))
        {
            ChecksumAlgorithmId = checksumAlgorithmId;
            Checksum = checksum;
            EmbeddedTextBlob = embeddedTextBlob;
        }
    }
}
