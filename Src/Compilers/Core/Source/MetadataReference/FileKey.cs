using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal struct FileKey : IEquatable<FileKey>
    {
        /// <summary>
        /// Full case-insensitive path.
        /// </summary>
        public readonly string FullPath;

        /// <summary>
        /// Last write time (Utc).
        /// </summary>
        public readonly DateTime Timestamp;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fullPath">Full path.</param>
        /// <param name="timestamp">Last write time (Utc).</param>
        public FileKey(string fullPath, DateTime timestamp)
        {
            Debug.Assert(FileUtilities.IsAbsolute(fullPath));

            FullPath = fullPath;
            Timestamp = timestamp;
        }

        public FileKey(string fullPath)
            : this(fullPath, GetTimeStamp(fullPath))
        {
        }

        public static DateTime GetTimeStamp(string fullPath)
        {
            Debug.Assert(FileUtilities.IsAbsolute(fullPath));
            return File.GetLastWriteTimeUtc(fullPath);
        }

        public FileKey Create(string fullPath)
        {
            return new FileKey(fullPath, GetTimeStamp(fullPath));
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(this.FullPath),
                this.Timestamp.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return obj is FileKey && Equals((FileKey)obj);
        }

        public override string ToString()
        {
            return string.Format("'{0}'@{1}", FullPath, Timestamp);
        }

        public bool Equals(FileKey other)
        {
            return
                this.Timestamp == other.Timestamp &&
                StringComparer.OrdinalIgnoreCase.Equals(this.FullPath, other.FullPath);
        }
    }
}