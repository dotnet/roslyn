// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Resolves references to source documents specified in the source.
    /// </summary>
    public abstract class SourceReferenceResolver
    {
        protected SourceReferenceResolver()
        {
        }

        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();

        /// <summary>
        /// Normalizes specified source path with respect to base file path.
        /// </summary>
        /// <param name="path">The source path to normalize. May be absolute or relative.</param>
        /// <param name="baseFilePath">Path of the source file that contains the <paramref name="path"/> (may also be relative), or null if not available.</param>
        /// <returns>Normalized path, or null if <paramref name="path"/> can't be normalized. The resulting path doesn't need to exist.</returns>
        public abstract string NormalizePath(string path, string baseFilePath);

        /// <summary>
        /// Resolves specified path with respect to base file path.
        /// </summary>
        /// <param name="path">The path to resolve. May be absolute or relative.</param>
        /// <param name="baseFilePath">Path of the source file that contains the <paramref name="path"/> (may also be relative), or null if not available.</param>
        /// <returns>Normalized path, or null if the file can't be resolved.</returns>
        public abstract string ResolveReference(string path, string baseFilePath);

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

        /// <summary>
        /// Reads the contents of <paramref name="resolvedPath"/> and returns a <see cref="SourceText"/>.
        /// </summary>
        /// <param name="resolvedPath">Path returned by <see cref="ResolveReference(string, string)"/>.</param>
        public virtual SourceText ReadText(string resolvedPath)
        {
            using (var stream = OpenRead(resolvedPath))
            {
                return EncodedStringText.Create(stream);
            }
        }
    }
}
