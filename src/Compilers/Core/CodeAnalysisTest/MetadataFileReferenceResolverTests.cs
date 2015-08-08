// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class MetadataFileReferenceResolverTests : TestBase
    {
        [Fact]
        public void ResolveMetadataFile1()
        {
            string fileName = "f.dll";
            string drive = "C";
            string dir = @"C:\dir";
            string subdir = @"C:\dir\subdir";
            string filePath = dir + @"\" + fileName;
            string subFilePath = subdir + @"\" + fileName;
            string dotted = subdir + @"\" + ".x.dll";

            var fs = new HashSet<string>
            {
                filePath,
                subFilePath,
                dotted
            };

            var resolver = new VirtualizedFileReferenceResolver(
                existingFullPaths: fs,
                searchPaths: ImmutableArray.Create<string>(),
                baseDirectory: subdir);

            // unqualified file name:
            var path = resolver.ResolveReference(fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path, StringComparer.OrdinalIgnoreCase);

            // prefer the base file over base directory:
            path = resolver.ResolveReference(fileName, baseFilePath: PathUtilities.CombineAbsoluteAndRelativePaths(dir, "foo.csx"));
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(@"\" + fileName, baseFilePath: null);
            Assert.Equal(null, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(@"/" + fileName, baseFilePath: null);
            Assert.Equal(null, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(@".", baseFilePath: null);
            Assert.Equal(null, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(@".\" + fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(@"./" + fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(@".x.dll", baseFilePath: null);
            Assert.Equal(dotted, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(@"..", baseFilePath: null);
            Assert.Equal(null, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(@"..\" + fileName, baseFilePath: null);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(@"../" + fileName, baseFilePath: null);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(@"C:\" + fileName, baseFilePath: null);
            Assert.Equal(null, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(@"C:/" + fileName, baseFilePath: null);
            Assert.Equal(null, path, StringComparer.OrdinalIgnoreCase);

            path = resolver.ResolveReference(filePath, baseFilePath: null);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            // drive-relative paths not supported:
            path = resolver.ResolveReference(drive + ":" + fileName, baseFilePath: null);
            Assert.Equal(null, path, StringComparer.OrdinalIgnoreCase);

            // \abc\def
            string rooted = filePath.Substring(2);
            path = resolver.ResolveReference(rooted, null);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ResolveMetadataFile2()
        {
            string fileName = "f.dll";
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
            var resolver = new VirtualizedFileReferenceResolver(
                existingFullPaths: fs,
                baseDirectory: subdir);

            // using base path
            var path = resolver.ResolveReference(fileName, baseFilePath: PathUtilities.CombineAbsoluteAndRelativePaths(dir, "foo.csx"));
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            // using base dir
            path = resolver.ResolveReference(fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path, StringComparer.OrdinalIgnoreCase);

            // search paths
            var resolverSP = new VirtualizedFileReferenceResolver(
                existingFullPaths: fs,
                searchPaths: new[] { dir, subdir }.AsImmutableOrNull(),
                baseDirectory: @"C:\foo");

            path = resolverSP.ResolveReference(fileName, baseFilePath: null);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            // null base dir, no search paths
            var resolverNullBase = new VirtualizedFileReferenceResolver(
                existingFullPaths: fs,
                baseDirectory: null);

            // relative path
            path = resolverNullBase.ResolveReference(fileName, baseFilePath: null);
            Assert.Null(path);

            // full path
            path = resolverNullBase.ResolveReference(filePath, baseFilePath: null);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            // null base dir
            var resolverNullBaseSP = new VirtualizedFileReferenceResolver(
                existingFullPaths: fs,
                searchPaths: new[] { dir, subdir }.AsImmutableOrNull(),
                baseDirectory: null);

            // relative path
            path = resolverNullBaseSP.ResolveReference(fileName, baseFilePath: null);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);

            // full path
            path = resolverNullBaseSP.ResolveReference(filePath, baseFilePath: null);
            Assert.Equal(filePath, path, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ResolvePath_Order()
        {
            var dir = Temp.CreateDirectory();
            var dir1 = dir.CreateDirectory("dir1");
            var dir2 = dir.CreateDirectory("dir2");

            var f1 = dir1.CreateFile("f.dll").Path;
            var f2 = dir2.CreateFile("f.dll").Path;

            var resolver = new RelativePathReferenceResolver(
                ImmutableArray.Create(dir1.Path, dir2.Path),
                baseDirectory: null);

            var path = resolver.ResolveReference("f.dll", null);
            Assert.Equal(f1, path, StringComparer.OrdinalIgnoreCase);
        }
    }
}
