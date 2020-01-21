// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class FilePathUtilitiesTests
    {
        [Fact]
        [WorkItem(1579, "https://github.com/dotnet/roslyn/issues/1579")]
        public void GetRelativePath_SameDirectory()
        {
            var baseDirectory = @"C:\Alpha\Beta\Gamma";
            var fullPath = @"C:\Alpha\Beta\Gamma\Doc.txt";

            var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

            Assert.Equal(expected: @"Doc.txt", actual: result);
        }

        [Fact]
        [WorkItem(1579, "https://github.com/dotnet/roslyn/issues/1579")]
        public void GetRelativePath_NestedOneLevelDown()
        {
            var baseDirectory = @"C:\Alpha\Beta\Gamma";
            var fullPath = @"C:\Alpha\Beta\Gamma\Delta\Doc.txt";

            var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

            Assert.Equal(expected: @"Delta\Doc.txt", actual: result);
        }

        [Fact]
        [WorkItem(1579, "https://github.com/dotnet/roslyn/issues/1579")]
        public void GetRelativePath_NestedTwoLevelsDown()
        {
            var baseDirectory = @"C:\Alpha\Beta\Gamma";
            var fullPath = @"C:\Alpha\Beta\Gamma\Delta\Epsilon\Doc.txt";

            var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

            Assert.Equal(expected: @"Delta\Epsilon\Doc.txt", actual: result);
        }

        [Fact]
        [WorkItem(1579, "https://github.com/dotnet/roslyn/issues/1579")]
        public void GetRelativePath_UpOneLevel()
        {
            var baseDirectory = @"C:\Alpha\Beta\Gamma";
            var fullPath = @"C:\Alpha\Beta\Doc.txt";

            var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

            Assert.Equal(expected: @"..\Doc.txt", actual: result);
        }

        [Fact]
        [WorkItem(1579, "https://github.com/dotnet/roslyn/issues/1579")]
        public void GetRelativePath_UpTwoLevels()
        {
            var baseDirectory = @"C:\Alpha\Beta\Gamma";
            var fullPath = @"C:\Alpha\Doc.txt";

            var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

            Assert.Equal(expected: @"..\..\Doc.txt", actual: result);
        }

        [Fact]
        [WorkItem(1579, "https://github.com/dotnet/roslyn/issues/1579")]
        public void GetRelativePath_UpTwoLevelsAndThenDown()
        {
            var baseDirectory = @"C:\Alpha\Beta\Gamma";
            var fullPath = @"C:\Alpha\Phi\Omega\Doc.txt";

            var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

            Assert.Equal(expected: @"..\..\Phi\Omega\Doc.txt", actual: result);
        }

        [Fact]
        [WorkItem(1579, "https://github.com/dotnet/roslyn/issues/1579")]
        public void GetRelativePath_OnADifferentDrive()
        {
            var baseDirectory = @"C:\Alpha\Beta\Gamma";
            var fullPath = @"D:\Alpha\Beta\Gamma\Doc.txt";

            var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

            Assert.Equal(expected: @"D:\Alpha\Beta\Gamma\Doc.txt", actual: result);
        }

        [Fact]
        [WorkItem(4660, "https://github.com/dotnet/roslyn/issues/4660")]
        public void GetRelativePath_WithBaseDirectoryMatchingIncompletePortionOfFullPath()
        {
            var baseDirectory = @"C:\Alpha\Beta";
            var fullPath = @"C:\Alpha\Beta2\Gamma";

            var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

            Assert.Equal(expected: @"..\Beta2\Gamma", actual: result);
        }
    }
}
