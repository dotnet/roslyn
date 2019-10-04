// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.IO;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Resolves references to XML documents specified in source code.
    /// </summary>
    public abstract class XmlReferenceResolver
    {
        protected XmlReferenceResolver()
        {
        }

        public abstract override bool Equals(object? other);
        public abstract override int GetHashCode();

        /// <summary>
        /// Resolves specified XML reference with respect to base file path.
        /// </summary>
        /// <param name="path">The reference path to resolve. May be absolute or relative path.</param>
        /// <param name="baseFilePath">Path of the source file that contains the <paramref name="path"/> (may also be relative), or null if not available.</param>
        /// <returns>Path to the XML artifact, or null if the file can't be resolved.</returns>
        public abstract string? ResolveReference(string path, string? baseFilePath);

        /// <summary>
        /// Opens a <see cref="Stream"/> that allows reading the content of the specified file.
        /// </summary>
        /// <param name="resolvedPath">Path returned by <see cref="ResolveReference(string, string)"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="resolvedPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="resolvedPath"/> is not a valid absolute path.</exception>
        /// <exception cref="IOException">Error reading file <paramref name="resolvedPath"/>. See <see cref="Exception.InnerException"/> for details.</exception>
        public abstract Stream OpenRead(string resolvedPath);

        internal Stream OpenReadChecked(string fullPath)
        {
            var stream = OpenRead(fullPath);

            if (stream == null || !stream.CanRead)
            {
                throw new InvalidOperationException(CodeAnalysisResources.ReferenceResolverShouldReturnReadableNonNullStream);
            }

            return stream;
        }
    }
}
