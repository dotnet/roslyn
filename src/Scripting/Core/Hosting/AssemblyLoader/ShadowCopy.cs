// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Represents a shadow copy of a single file.
    /// </summary>
    public sealed class FileShadowCopy
    {
        public string OriginalPath { get; }
        public string FullPath { get; }

        // Keeps the file locked until it's closed.
        // We need to hold on the stream to prevent it from closing the underlying file handle. The stream should not be used for reading the file content.
        // If we only held on the handle the stream could be GC'd and its finalizer could close the handle.
        private readonly IDisposable _stream;

        internal FileShadowCopy(IDisposable stream, string originalPath, string fullPath)
        {
            Debug.Assert(stream != null);
            Debug.Assert(originalPath != null);
            Debug.Assert(fullPath != null);

            _stream = stream;
            OriginalPath = originalPath;
            FullPath = fullPath;
        }

        // keep this internal so that users can't delete files that the provider manages
        internal void DisposeFileStream()
        {
            _stream.Dispose();
        }
    }
}
