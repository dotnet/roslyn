// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class StrongNameProviderTests
    {
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        public void ResolveStrongNameKeyFile()
        {
            string fileName = "f.snk";
            string dir = @"C:\dir";
            string subdir = @"C:\dir\subdir";
            string filePath = dir + @"\" + fileName;
            string subFilePath = subdir + @"\" + fileName;

            var fs = new HashSet<string>
            {
                filePath,
                subFilePath
            };

            // with no search paths
            var provider = new VirtualizedStrongNameProvider(
                existingFullPaths: fs,
                searchPaths: ImmutableArray.Create(subdir));
            var subdirSearchPath = ImmutableArray.Create(subdir);

            // using base directory; base path ignored
            var path = resolve(fileName, subdirSearchPath);
            Assert.Equal(subFilePath, path, StringComparer.OrdinalIgnoreCase);

            // search paths
            var searchPathsSP = ImmutableArray.Create(@"C:\goo", dir, subdir);

            path = resolve(fileName, searchPathsSP);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            // null base dir, no search paths
            var searchPathsEmpty = ImmutableArray<string>.Empty;

            // relative path
            path = resolve(fileName, searchPathsEmpty);
            Assert.Null(path);

            // full path
            path = resolve(filePath, searchPathsEmpty);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            // null base dir
            var searchPathsNullBaseSP = ImmutableArray.Create(dir, subdir);

            // relative path
            path = resolve(fileName, searchPathsNullBaseSP);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            // full path
            path = resolve(filePath, searchPathsNullBaseSP);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            string resolve(string keyFilePath, ImmutableArray<string> searchPaths) => DesktopStrongNameProvider.ResolveStrongNameKeyFile(keyFilePath, provider.FileSystem, searchPaths);
        }

        public class VirtualizedStrongNameProvider : DesktopStrongNameProvider
        {
            private class VirtualStrongNameFileSystem : StrongNameFileSystem
            {
                private readonly HashSet<string> _existingFullPaths;
                public VirtualStrongNameFileSystem(HashSet<string> existingFullPaths)
                {
                    _existingFullPaths = existingFullPaths;
                }

                internal override bool FileExists(string fullPath)
                {
                    return fullPath != null && _existingFullPaths != null && _existingFullPaths.Contains(FileUtilities.NormalizeAbsolutePath(fullPath));
                }
            }

            public VirtualizedStrongNameProvider(
                IEnumerable<string> existingFullPaths = null,
                ImmutableArray<string> searchPaths = default(ImmutableArray<string>))
                : base(searchPaths.NullToEmpty(), new VirtualStrongNameFileSystem(new HashSet<string>(existingFullPaths, StringComparer.OrdinalIgnoreCase)))
            {
            }
        }
    }
}
