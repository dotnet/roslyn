// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal readonly static StrongNameFileSystem Instance = new StrongNameFileSystem();
        internal readonly string _tempPath;

        internal StrongNameFileSystem(string tempPath = null)
        {
            _tempPath = tempPath;
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

        internal virtual bool FileExists(string fullPath)
        {
            Debug.Assert(fullPath == null || PathUtilities.IsAbsolute(fullPath));
            return File.Exists(fullPath);
        }

        internal virtual string GetTempPath() => _tempPath ?? Path.GetTempPath();
    }
}
