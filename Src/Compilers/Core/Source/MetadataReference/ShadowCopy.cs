using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a shadow copy of a single file.
    /// </summary>
    public sealed class ShadowCopy
    {
        private readonly string originalPath;
        private readonly string fullPath;

        // Keeps the file locked until it's closed.
        // We need to hold on the stream to prevent it from closing the underlying file handle. The stream should not be used for reading the file content.
        // If we only held on the handle the stream could be GC'd and its finalizer could close the handle.
        private readonly FileStream stream;

        internal ShadowCopy(FileStream stream, string originalPath, string fullPath)
        {
            Debug.Assert(originalPath != null);
            Debug.Assert(fullPath != null);
            Debug.Assert(!stream.SafeFileHandle.IsInvalid && !stream.SafeFileHandle.IsClosed);

            this.stream = stream;
            this.originalPath = originalPath;
            this.fullPath = fullPath;
        }

        public string FullPath
        {
            get { return fullPath; }
        }

        public string OriginalPath
        {
            get { return originalPath; }
        }

        // keep this internal so that users can't delete files that the provider manages
        internal FileStream Stream
        {
            get { return stream; }
        }

        // keep this internal so that users can't delete files that the provider manages
        internal void DisposeFileStream()
        {
            stream.Dispose();
        }
    }
}
