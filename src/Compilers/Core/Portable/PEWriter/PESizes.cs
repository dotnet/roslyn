// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Cci
{
    internal sealed class PESizes
    {
        /// <summary>
        /// Total size of metadata (header and all streams).
        /// </summary>
        public readonly int MetadataSize;

        /// <summary>
        /// The size of IL stream.
        /// </summary>
        public readonly int ILStreamSize;

        /// <summary>
        /// The size of mapped field data stream.
        /// Aligned to <see cref="MetadataWriter.MappedFieldDataAlignment"/>.
        /// </summary>
        public readonly int MappedFieldDataSize;

        /// <summary>
        /// The size of managed resource data stream.
        /// Aligned to <see cref="MetadataWriter.ManagedResourcesDataAlignment"/>.
        /// </summary>
        public readonly int ResourceDataSize;

        /// <summary>
        /// Size of strong name hash.
        /// </summary>
        public readonly int StrongNameSignatureSize;

        public PESizes(
            int metadataSize,
            int ilStreamSize,
            int mappedFieldDataSize,
            int resourceDataSize,
            int strongNameSignatureSize)
        {
            MetadataSize = metadataSize;
            ResourceDataSize = resourceDataSize;
            ILStreamSize = ilStreamSize;
            MappedFieldDataSize = mappedFieldDataSize;
            StrongNameSignatureSize = strongNameSignatureSize;
        }
    }
}
