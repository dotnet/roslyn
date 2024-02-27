// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;

namespace Roslyn.Utilities
{
    internal readonly struct FileKey : IEquatable<FileKey>
    {
        /// <summary>
        /// Full case-insensitive path.
        /// </summary>
        public readonly string FullPath;

        /// <summary>
        /// Last write time (UTC).
        /// </summary>
        public readonly DateTime Timestamp;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fullPath">Full path.</param>
        /// <param name="timestamp">Last write time (UTC).</param>
        public FileKey(string fullPath, DateTime timestamp)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            Debug.Assert(timestamp.Kind == DateTimeKind.Utc);

            FullPath = fullPath;
            Timestamp = timestamp;
        }

        /// <exception cref="IOException"/>
        public static FileKey Create(string fullPath)
        {
            return new FileKey(fullPath, FileUtilities.GetFileTimeStamp(fullPath));
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(this.FullPath),
                this.Timestamp.GetHashCode());
        }

        public override bool Equals(object? obj)
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
                string.Equals(this.FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
