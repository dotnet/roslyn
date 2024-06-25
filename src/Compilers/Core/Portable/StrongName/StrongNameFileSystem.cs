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
        => StringComparer.Ordinal.GetHashCode(_signingTempPath);

        public override bool Equals(object? obj)
        {
            if (Object.ReferenceEquals(obj, this))
            {
                return true;
            }

            if (GetType() != obj!.GetType())
            {
                return false;
            }

            var other = (StrongNameFileSystem)obj;
            return string.Equals(_signingTempPath, other._signingTempPath, StringComparison.Ordinal);
        }
    }
}
