// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a shadow copy of a single file.
    /// </summary>
    public sealed class ShadowCopy
    {
        private readonly string _originalPath;
        private readonly string _fullPath;

        // Keeps the file locked until it's closed.
        // We need to hold on the stream to prevent it from closing the underlying file handle. The stream should not be used for reading the file content.
        // If we only held on the handle the stream could be GC'd and its finalizer could close the handle.
        private readonly IDisposable _stream;

        internal ShadowCopy(IDisposable stream, string originalPath, string fullPath)
        {
            Debug.Assert(stream != null);
            Debug.Assert(originalPath != null);
            Debug.Assert(fullPath != null);

            _stream = stream;
            _originalPath = originalPath;
            _fullPath = fullPath;
        }

        public string FullPath
        {
            get { return _fullPath; }
        }

        public string OriginalPath
        {
            get { return _originalPath; }
        }

        // keep this internal so that users can't delete files that the provider manages
        internal void DisposeFileStream()
        {
            _stream.Dispose();
        }
    }
}
