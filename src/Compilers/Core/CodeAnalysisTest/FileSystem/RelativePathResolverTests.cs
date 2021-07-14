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

            var resolver = new VirtualizedRelativePathResolver(
                existingFullPaths: fs,
                searchPaths: ImmutableArray.Create<string>(),
                baseDirectory: subdir);

            // unqualified file name:
            var path = resolver.ResolvePath(fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path);

            // prefer the base file over base directory:
            path = resolver.ResolvePath(fileName, baseFilePath: PathUtilities.CombineAbsoluteAndRelativePaths(dir, "goo.csx"));
            Assert.Equal(filePath, path);

            path = resolver.ResolvePath(@"\" + fileName, baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(@"/" + fileName, baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(@".", baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(@".\" + fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path);

            path = resolver.ResolvePath(@"./" + fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path);

            path = resolver.ResolvePath(@".x.dll", baseFilePath: null);
            Assert.Equal(dotted, path);

            path = resolver.ResolvePath(@"..", baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(@"..\" + fileName, baseFilePath: null);
            Assert.Equal(filePath, path);

            path = resolver.ResolvePath(@"../" + fileName, baseFilePath: null);
            Assert.Equal(filePath, path);

            path = resolver.ResolvePath(@"C:\" + fileName, baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(@"C:/" + fileName, baseFilePath: null);
            Assert.Null(path);

            path = resolver.ResolvePath(filePath, baseFilePath: null);
            Assert.Equal(filePath, path);

            // drive-relative paths not supported:
            path = resolver.ResolvePath(drive + ":" + fileName, baseFilePath: null);
            Assert.Null(path);

            // \abc\def
            string rooted = filePath.Substring(2);
            path = resolver.ResolvePath(rooted, null);
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

            // with no search paths
            var resolver = new VirtualizedRelativePathResolver(
                existingFullPaths: fs,
                baseDirectory: subdir);

            // using base path
            var path = resolver.ResolvePath(fileName, baseFilePath: PathUtilities.CombineAbsoluteAndRelativePaths(dir, "goo.csx"));
            Assert.Equal(filePath, path);

            // using base dir
            path = resolver.ResolvePath(fileName, baseFilePath: null);
            Assert.Equal(subFilePath, path);

            // search paths
            var resolverSP = new VirtualizedRelativePathResolver(
                existingFullPaths: fs,
                searchPaths: new[] { dir, subdir }.AsImmutableOrNull(),
                baseDirectory: @"C:\goo");

            path = resolverSP.ResolvePath(fileName, baseFilePath: null);
            Assert.Equal(filePath, path);

            // null base dir, no search paths
            var resolverNullBase = new VirtualizedRelativePathResolver(
                existingFullPaths: fs,
                baseDirectory: null);

            // relative path
            path = resolverNullBase.ResolvePath(fileName, baseFilePath: null);
            Assert.Null(path);

            // full path
            path = resolverNullBase.ResolvePath(filePath, baseFilePath: null);
            Assert.Equal(filePath, path);

            // null base dir
            var resolverNullBaseSP = new VirtualizedRelativePathResolver(
                existingFullPaths: fs,
                searchPaths: new[] { dir, subdir }.AsImmutableOrNull(),
                baseDirectory: null);

            // relative path
            path = resolverNullBaseSP.ResolvePath(fileName, baseFilePath: null);
            Assert.Equal(filePath, path);

            // full path
            path = resolverNullBaseSP.ResolvePath(filePath, baseFilePath: null);
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

            var path = resolver.ResolvePath("f.dll", null);
            Assert.Equal(f1, path);
        }
    }
}
