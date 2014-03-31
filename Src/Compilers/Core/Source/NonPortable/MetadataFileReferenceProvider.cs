// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// An extensible mechanism for providing <see cref="PortableExecutableReference"/>
    /// to services that require them.
    /// </summary>
    /// <remarks>Used to create <see cref="PortableExecutableReference"/> when 
    /// processing interactive code directives that load .NET metadata.
    /// </remarks>
    public class MetadataFileReferenceProvider : MetadataReferenceProvider
    {
        public static readonly MetadataFileReferenceProvider Default = new MetadataFileReferenceProvider();

        /// <summary>
        /// Maps "metadata about .NET metadata" to <see cref="PortableExecutableReference"/>. 
        /// </summary>
        /// <param name="resolvedPath">Path returned by <see cref="MetadataReferenceResolver.ResolveReference(string, string)"/>.</param>
        /// <param name="properties">Metadata reference properties.</param>
        /// <returns>A <see cref="PortableExecutableReference"/> corresponding to the <paramref name="resolvedPath"/> and
        /// <paramref name="properties"/> parameters.</returns>
        public override PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
        {
            return new MetadataFileReference(resolvedPath, properties);
        }

        /// <summary>
        /// Removes any cached data or files created by the provider.
        /// </summary>
        /// <remarks>
        /// <see cref="GetReference"/> might be optimized to cache results and ignore changes to the files made after the file has been provided. 
        /// This behavior is useful when the compiler and services that use the provider work on a snapshot of the metadata files.
        /// Call this method to clear the cache and start over.
        /// </remarks>
        public virtual void ClearCache()
        {
        }
    }
}
