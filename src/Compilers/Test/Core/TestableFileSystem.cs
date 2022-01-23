// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    public delegate Stream OpenFileExFunc(string filePath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, out string normalizedFilePath);
    public delegate Stream OpenFileFunc(string filePath, FileMode mode, FileAccess access, FileShare share);

    public sealed class TestableFileSystem : ICommonCompilerFileSystem
    {
        public OpenFileFunc OpenFileFunc { get; set; } = delegate { throw new InvalidOperationException(); };
        public OpenFileExFunc OpenFileExFunc { get; set; } = (string _, FileMode _, FileAccess _, FileShare _, int _, FileOptions _, out string _) => throw new InvalidOperationException();
        public Func<string, bool> FileExistsFunc { get; set; } = delegate { throw new InvalidOperationException(); };

        public Stream OpenFile(string filePath, FileMode mode, FileAccess access, FileShare share)
            => OpenFileFunc(filePath, mode, access, share);

        public Stream OpenFileEx(string filePath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, out string normalizedFilePath)
            => OpenFileExFunc(filePath, mode, access, share, bufferSize, options, out normalizedFilePath);

        public bool FileExists(string filePath) => FileExistsFunc(filePath);

        public static TestableFileSystem CreateForStandard(
            OpenFileFunc? openFileFunc = null,
            OpenFileExFunc? openFileExFunc = null,
            Func<string, bool>? fileExistsFunc = null)
            => new TestableFileSystem()
            {
                OpenFileFunc = openFileFunc ?? StandardFileSystem.Instance.OpenFile,
                OpenFileExFunc = openFileExFunc ?? StandardFileSystem.Instance.OpenFileEx,
                FileExistsFunc = fileExistsFunc ?? StandardFileSystem.Instance.FileExists,
            };

        public static TestableFileSystem CreateForOpenFile(OpenFileFunc openFileFunc)
            => new TestableFileSystem() { OpenFileFunc = openFileFunc };

        public static TestableFileSystem CreateForExistingPaths(IEnumerable<string> existingPaths, StringComparer? comparer = null)
        {
            comparer ??= StringComparer.OrdinalIgnoreCase;
            var set = new HashSet<string>(existingPaths, comparer);
            return new TestableFileSystem()
            {
                FileExistsFunc = filePath => set.Contains(filePath)
            };
        }

        public static TestableFileSystem CreateForMap(Dictionary<string, TestableFile> map)
            => new TestableFileSystem()
            {
                OpenFileExFunc = (string filePath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, out string normalizedFilePath) =>
                {
                    normalizedFilePath = filePath;
                    return map[filePath].GetStream(access);
                },

                OpenFileFunc = (string filePath, FileMode mode, FileAccess access, FileShare share) => map[filePath].GetStream(access),
                FileExistsFunc = filePath => map.ContainsKey(filePath),
            };
    }
}
