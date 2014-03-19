// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// An extensible mechanism for providing <see cref="PortableExecutableReference"/>
    /// to services that require them.
    /// </summary>
    /// <remarks>Used to create <see cref="PortableExecutableReference"/> when 
    /// processing interactive code directives that load .NET metadata.
    /// </remarks>
    public abstract class MetadataReferenceProvider
    {
        protected MetadataReferenceProvider()
        {
        }
        
        /// <summary>
        /// Maps "metadata about .NET metadata" to <see cref="PortableExecutableReference"/>. 
        /// </summary>
        /// <param name="resolvedPath">Path returned by <see cref="MetadataReferenceResolver.ResolveReference(string, string)"/>.</param>
        /// <param name="properties">Metadata reference properties.</param>
        /// <returns>A <see cref="PortableExecutableReference"/> corresponding to the <paramref name="resolvedPath"/> and
        /// <paramref name="properties"/> parameters.</returns>
        public abstract PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties);
    }
}
