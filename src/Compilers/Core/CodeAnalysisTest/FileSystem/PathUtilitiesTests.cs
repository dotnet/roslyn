using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.FileSystem
{
    public class PathUtilitiesTests
    {
        [Fact]
        public void TestGetDirectoryName_WindowsPaths_Absolute()
        {
            Assert.Equal(
                @"C:\temp",
                PathUtilities.GetDirectoryName(@"C:\temp\foo.txt", isUnixLike: false));

            Assert.Equal(
                @"C:\temp",
                PathUtilities.GetDirectoryName(@"C:\temp\foo", isUnixLike: false));

            Assert.Equal(
                @"C:\temp",
                PathUtilities.GetDirectoryName(@"C:\temp\", isUnixLike: false));

            Assert.Equal(
                @"C:\",
                PathUtilities.GetDirectoryName(@"C:\temp", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"C:\", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"C:", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(null, isUnixLike: false));
        }

        [Fact]
        public void TestGetDirectoryName_WindowsPaths_Relative()
        {
            Assert.Equal(
                @"foo\temp",
                PathUtilities.GetDirectoryName(@"foo\temp\foo.txt", isUnixLike: false));

            Assert.Equal(
                @"foo\temp",
                PathUtilities.GetDirectoryName(@"foo\temp\foo", isUnixLike: false));

            Assert.Equal(
                @"foo\temp",
                PathUtilities.GetDirectoryName(@"foo\temp\", isUnixLike: false));

            Assert.Equal(
                @"foo",
                PathUtilities.GetDirectoryName(@"foo\temp", isUnixLike: false));

            Assert.Equal(
                @"foo",
                PathUtilities.GetDirectoryName(@"foo\", isUnixLike: false));

            Assert.Equal(
                "",
                PathUtilities.GetDirectoryName(@"foo", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(null, isUnixLike: false));
        }

        [Fact]
        public void TestGetDirectoryName_UnixPaths_Absolute()
        {
            Assert.Equal(
                @"/temp",
                PathUtilities.GetDirectoryName(@"/temp/foo.txt", isUnixLike: true));

            Assert.Equal(
                @"/temp",
                PathUtilities.GetDirectoryName(@"/temp/foo", isUnixLike: true));

            Assert.Equal(
                @"/temp",
                PathUtilities.GetDirectoryName(@"/temp/", isUnixLike: true));

            Assert.Equal(
                @"/",
                PathUtilities.GetDirectoryName(@"/temp", isUnixLike: true));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"/", isUnixLike: true));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"", isUnixLike: true));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(null, isUnixLike: true));
        }

        [Fact]
        public void TestGetDirectoryName_UnixPaths_Relative()
        {
            Assert.Equal(
                @"foo/temp",
                PathUtilities.GetDirectoryName(@"foo/temp/foo.txt", isUnixLike: true));

            Assert.Equal(
                @"foo/temp",
                PathUtilities.GetDirectoryName(@"foo/temp/foo", isUnixLike: true));

            Assert.Equal(
                @"foo/temp",
                PathUtilities.GetDirectoryName(@"foo/temp/", isUnixLike: true));

            Assert.Equal(
                @"foo",
                PathUtilities.GetDirectoryName(@"foo/temp", isUnixLike: true));

            Assert.Equal(
                @"foo",
                PathUtilities.GetDirectoryName(@"foo/", isUnixLike: true));

            Assert.Equal(
                "",
                PathUtilities.GetDirectoryName(@"foo", isUnixLike: true));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"", isUnixLike: true));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(null, isUnixLike: true));
        }

        [Fact]
        public void TestGetDirectoryName_WindowsSharePaths()
        {
            Assert.Equal(
                @"\\server\temp",
                PathUtilities.GetDirectoryName(@"\\server\temp\foo.txt", isUnixLike: false));

            Assert.Equal(
                @"\\server\temp",
                PathUtilities.GetDirectoryName(@"\\server\temp\foo", isUnixLike: false));

            Assert.Equal(
                @"\\server\temp",
                PathUtilities.GetDirectoryName(@"\\server\temp\", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"\\server\temp", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"\\server\", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"\\server", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"\\", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"\", isUnixLike: false));

            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(null, isUnixLike: false));
        }

        [Fact]
        public void TestGetDirectoryName_EsotericCases()
        {
            Assert.Equal(
                @"C:\temp",
                PathUtilities.GetDirectoryName(@"C:\temp\\foo.txt", isUnixLike: false));

            Assert.Equal(
                @"C:\temp",
                PathUtilities.GetDirectoryName(@"C:\temp\\\foo.txt", isUnixLike: false));

            Assert.Equal(
                @"C:\temp\..",
                PathUtilities.GetDirectoryName(@"C:\temp\..\foo.txt", isUnixLike: false));

            Assert.Equal(
                @"C:\temp",
                PathUtilities.GetDirectoryName(@"C:\temp\..", isUnixLike: false));

            Assert.Equal(
                @"C:\temp\.",
                PathUtilities.GetDirectoryName(@"C:\temp\.\foo.txt", isUnixLike: false));

            Assert.Equal(
                @"C:\temp",
                PathUtilities.GetDirectoryName(@"C:\temp\.", isUnixLike: false));
        }

        [Fact]
        public void TestContainsPathComponent()
        {
            Assert.True(
                PathUtilities.ContainsPathComponent(@"c:\packages\temp", "packages"));
            Assert.True(
                PathUtilities.ContainsPathComponent(@"\\server\packages\temp", "packages"));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"\\packages\temp", "packages"));
            Assert.True(
                PathUtilities.ContainsPathComponent(@"c:\packages", "packages"));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\packages1\temp", "packages"));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\package\temp", "packages"));
        }
    }
}
