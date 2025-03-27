// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This is an abstraction over the file system which allows for us to do more thorough unit testing.
    /// </summary>
    internal class StrongNameFileSystem
    {
        internal static readonly StrongNameFileSystem Instance = new StrongNameFileSystem();
        internal readonly string? _signingTempPath;

        internal StrongNameFileSystem(string? signingTempPath = null)
        {
            _signingTempPath = signingTempPath;
        }

        internal virtual FileStream CreateFileStream(string filePath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return new FileStream(filePath, fileMode, fileAccess, fileShare);
        }

        internal virtual byte[] ReadAllBytes(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            return File.ReadAllBytes(fullPath);
        }

        internal virtual bool FileExists(string? fullPath)
        {
            Debug.Assert(fullPath == null || PathUtilities.IsAbsolute(fullPath));
            return File.Exists(fullPath);
        }

        internal string? GetSigningTempPath() => _signingTempPath;

        public override int GetHashCode()
            => _signingTempPath != null ? StringComparer.Ordinal.GetHashCode(_signingTempPath) : 0;

        public override bool Equals(object? obj)
            => Equals(obj as StrongNameFileSystem);

        private bool Equals(StrongNameFileSystem? other)
        {
            if (this == other)
                return true;

            return this.GetType() == other?.GetType() && StringComparer.Ordinal.Equals(_signingTempPath, other?._signingTempPath);
        }
    }
}
