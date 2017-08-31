// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class StrongNameProviderTests
    {
        [Fact]
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

            // using base directory; base path ignored
            var path = provider.ResolveStrongNameKeyFile(fileName);
            Assert.Equal(subFilePath, path, StringComparer.OrdinalIgnoreCase);

            // search paths
            var providerSP = new VirtualizedStrongNameProvider(
                existingFullPaths: fs,
                searchPaths: ImmutableArray.Create(@"C:\goo", dir, subdir));

            path = providerSP.ResolveStrongNameKeyFile(fileName);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            // null base dir, no search paths
            var providerNullBase = new VirtualizedStrongNameProvider(
                existingFullPaths: fs,
                searchPaths: ImmutableArray.Create<string>());

            // relative path
            path = providerNullBase.ResolveStrongNameKeyFile(fileName);
            Assert.Null(path);

            // full path
            path = providerNullBase.ResolveStrongNameKeyFile(filePath);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            // null base dir
            var providerNullBaseSP = new VirtualizedStrongNameProvider(
                existingFullPaths: fs,
                searchPaths: ImmutableArray.Create(dir, subdir));

            // relative path
            path = providerNullBaseSP.ResolveStrongNameKeyFile(fileName);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            // full path
            path = providerNullBaseSP.ResolveStrongNameKeyFile(filePath);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);
        }

        public class VirtualizedStrongNameProvider : DesktopStrongNameProvider
        {
            private readonly HashSet<string> _existingFullPaths;

            public VirtualizedStrongNameProvider(
                IEnumerable<string> existingFullPaths = null,
                ImmutableArray<string> searchPaths = default(ImmutableArray<string>))
                : base(searchPaths.NullToEmpty())
            {
                _existingFullPaths = new HashSet<string>(existingFullPaths, StringComparer.OrdinalIgnoreCase);
            }

            internal override bool FileExists(string fullPath)
            {
                return fullPath != null && _existingFullPaths != null && _existingFullPaths.Contains(FileUtilities.NormalizeAbsolutePath(fullPath));
            }
        }
    }
}
