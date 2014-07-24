// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a non source code file.
    /// </summary>
    public sealed class AdditionalFileStream : AdditionalStream
    {
        private readonly string path;

        public AdditionalFileStream(string fullPath)
        {
            this.path = fullPath;
        }

        /// <summary>
        /// Resolved absolute path of the stream (does not contain wildcards).
        /// </summary>
        /// <remarks>
        /// Although this path is absolute it may not be normalized. That is, it may contain ".." and "." in the middle. 
        /// </remarks>
        public override string Path
        {
            get
            {
                return this.path;
            }
        }

        /// <summary>
        /// Opens a <see cref="Stream"/> that allows reading the content of this file.
        /// </summary>
        public override Stream OpenRead(CancellationToken cancellationToken = default(CancellationToken))
        {
            string resolvedPath = null;
            if (File.Exists(path))
            {
                resolvedPath = FileUtilities.TryNormalizeAbsolutePath(path);
            }
            
            if (resolvedPath == null)
            {
                throw new FileNotFoundException(CodeAnalysisResources.FileNotFound, path);
            }

            return FileUtilities.OpenRead(resolvedPath);
        }
    }
}
