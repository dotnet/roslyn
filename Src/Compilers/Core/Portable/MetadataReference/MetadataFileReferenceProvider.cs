// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A factory for <see cref="PortableExecutableReference"/> based on a path.
    /// </summary>
    internal class MetadataFileReferenceProvider
    {
        public static readonly MetadataFileReferenceProvider Default = new MetadataFileReferenceProvider();

        /// <summary>
        /// Maps a path to <see cref="PortableExecutableReference"/>. 
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="properties">Metadata reference properties.</param>
        /// <returns>A <see cref="PortableExecutableReference"/> corresponding to the <paramref name="path"/> and
        /// <paramref name="properties"/> parameters.</returns>
        public virtual PortableExecutableReference GetReference(string path, MetadataReferenceProperties properties)
        {
            return MetadataReference.CreateFromFile(path, properties);
        }
    }
}
