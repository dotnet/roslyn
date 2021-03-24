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
    public class RelativePathResolverTests : TestBase
    {
        [ConditionalFact(typeof(WindowsOnly))]
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

            var resolver = new RelativePathResolver(
                searchPaths: ImmutableArray.Create<string>(),
                baseDirectory: subdir);

            // unqualified file name:
            var fileSystem = TestableFileSystem.CreateForExistingPaths(fs);
            var path = resolver.ResolvePath(fileSystem, fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path);

            // prefer the base file over base directory:
            path = resolver.ResolvePath(fileSystem, fileName, baseFilePath: PathUtilities.CombineAbsoluteAndRelativePaths(dir, "goo.csx"));
            Assert.Equal(filePath, path);

            path = resolver.ResolvePath(fileSystem, @"\" + fileName, baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(fileSystem, @"/" + fileName, baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(fileSystem, @".", baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(fileSystem, @".\" + fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path);

            path = resolver.ResolvePath(fileSystem, @"./" + fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path);

            path = resolver.ResolvePath(fileSystem, @".x.dll", baseFilePath: null);
            Assert.Equal(dotted, path);

            path = resolver.ResolvePath(fileSystem, @"..", baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(fileSystem, @"..\" + fileName, baseFilePath: null);
            Assert.Equal(filePath, path);

            path = resolver.ResolvePath(fileSystem, @"../" + fileName, baseFilePath: null);
            Assert.Equal(filePath, path);

            path = resolver.ResolvePath(fileSystem, @"C:\" + fileName, baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(fileSystem, @"C:/" + fileName, baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(fileSystem, filePath, baseFilePath: null);
            Assert.Equal(filePath, path);

            // drive-relative paths not supported:
            path = resolver.ResolvePath(fileSystem, drive + ":" + fileName, baseFilePath: null);
            Assert.Null(path);

            // \abc\def
            string rooted = filePath.Substring(2);
            path = resolver.ResolvePath(fileSystem, rooted, null);
            Assert.Equal(filePath, path);
        }

        [ConditionalFact(typeof(WindowsOnly))]
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
            var fileSystem = TestableFileSystem.CreateForExistingPaths(fs);

            // with no search paths
            var resolver = new RelativePathResolver(
                searchPaths: ImmutableArray<string>.Empty,
                baseDirectory: subdir);

            // using base path
            var path = resolver.ResolvePath(fileSystem, fileName, baseFilePath: PathUtilities.CombineAbsoluteAndRelativePaths(dir, "goo.csx"));
            Assert.Equal(filePath, path);

            // using base dir
            path = resolver.ResolvePath(fileSystem, fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path);

            // search paths
            var resolverSP = new RelativePathResolver(
                searchPaths: new[] { dir, subdir }.AsImmutableOrNull(),
                baseDirectory: @"C:\goo");

            path = resolverSP.ResolvePath(fileSystem, fileName, baseFilePath: null);
            Assert.Equal(filePath, path);

            // null base dir, no search paths
            var resolverNullBase = new RelativePathResolver(
                searchPaths: ImmutableArray<string>.Empty,
                baseDirectory: null);

            // relative path
            path = resolverNullBase.ResolvePath(fileSystem, fileName, baseFilePath: null);
            Assert.Null(path);

            // full path
            path = resolverNullBase.ResolvePath(fileSystem, filePath, baseFilePath: null);
            Assert.Equal(filePath, path);

            // null base dir
            var resolverNullBaseSP = new RelativePathResolver(
                searchPaths: new[] { dir, subdir }.AsImmutableOrNull(),
                baseDirectory: null);

            // relative path
            path = resolverNullBaseSP.ResolvePath(fileSystem, fileName, baseFilePath: null);
            Assert.Equal(filePath, path);

            // full path
            path = resolverNullBaseSP.ResolvePath(fileSystem, filePath, baseFilePath: null);
            Assert.Equal(filePath, path);
        }

        [Fact]
        public void ResolvePath_Order()
        {
            var dir = Temp.CreateDirectory();
            var dir1 = dir.CreateDirectory("dir1");
            var dir2 = dir.CreateDirectory("dir2");

            var f1 = dir1.CreateFile("f.dll").Path;
            var f2 = dir2.CreateFile("f.dll").Path;

            var resolver = new RelativePathResolver(
                ImmutableArray.Create(dir1.Path, dir2.Path),
                baseDirectory: null);

            var path = resolver.ResolvePath(StandardFileSystem.Instance, "f.dll", null);
            Assert.Equal(f1, path);
        }
    }
}
