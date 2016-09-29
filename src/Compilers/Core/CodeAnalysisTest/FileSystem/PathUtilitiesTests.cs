using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.FileSystem
{
    public class PathUtilitiesTests
    {
        private void TestGetDirectoryNameAndCompareToDotnet(string expectedDirectoryName, string fullPath)
        {
            var roslynName = PathUtilities.GetDirectoryName(fullPath, isUnixLike: false);
            Assert.Equal(expectedDirectoryName, roslynName);

            var dotnetName = Path.GetDirectoryName(fullPath);
            Assert.Equal(dotnetName, roslynName);
        }

        [Fact]
        public void TestGetDirectoryName_WindowsPaths_Absolute()
        {
            TestGetDirectoryNameAndCompareToDotnet(@"C:\temp", @"C:\temp\foo.txt");
            TestGetDirectoryNameAndCompareToDotnet(@"C:\temp", @"C:\temp\foo");
            TestGetDirectoryNameAndCompareToDotnet(@"C:\temp", @"C:\temp\");
            TestGetDirectoryNameAndCompareToDotnet(@"C:\", @"C:\temp");
            TestGetDirectoryNameAndCompareToDotnet(null, @"C:\");
            TestGetDirectoryNameAndCompareToDotnet(null, @"C:");

            // dotnet throws on empty argument.  But not on null... go figure.
            Assert.Equal(
                null,
                PathUtilities.GetDirectoryName(@"", isUnixLike: false));
            
            TestGetDirectoryNameAndCompareToDotnet(null, null);
        }

        [Fact]
        public void TestGetDirectoryName_WindowsPaths_Relative()
        {
            TestGetDirectoryNameAndCompareToDotnet(@"foo\temp", @"foo\temp\foo.txt");
            TestGetDirectoryNameAndCompareToDotnet(@"foo\temp", @"foo\temp\foo");
            TestGetDirectoryNameAndCompareToDotnet(@"foo\temp", @"foo\temp\");
            TestGetDirectoryNameAndCompareToDotnet(@"foo", @"foo\temp");
            TestGetDirectoryNameAndCompareToDotnet(@"foo", @"foo\");
            TestGetDirectoryNameAndCompareToDotnet("", @"foo");
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
            TestGetDirectoryNameAndCompareToDotnet(@"\\server\temp", @"\\server\temp\foo.txt");
            TestGetDirectoryNameAndCompareToDotnet(@"\\server\temp", @"\\server\temp\foo");
            TestGetDirectoryNameAndCompareToDotnet(@"\\server\temp", @"\\server\temp\");
            TestGetDirectoryNameAndCompareToDotnet(null, @"\\server\temp");
            TestGetDirectoryNameAndCompareToDotnet(null, @"\\server\");
            TestGetDirectoryNameAndCompareToDotnet(null, @"\\server");
            TestGetDirectoryNameAndCompareToDotnet(null, @"\\");
            TestGetDirectoryNameAndCompareToDotnet(null, @"\");
        }

        [Fact]
        public void TestGetDirectoryName_EsotericCases()
        {
            TestGetDirectoryNameAndCompareToDotnet(@"C:\temp", @"C:\temp\\foo.txt");
            TestGetDirectoryNameAndCompareToDotnet(@"C:\temp", @"C:\temp\\\foo.txt");

            // Dotnet does normalization of dots, so we can't compare against it here.
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

            TestGetDirectoryNameAndCompareToDotnet(@"C:temp", @"C:temp\\foo.txt");
            TestGetDirectoryNameAndCompareToDotnet(@"C:temp", @"C:temp\\\foo.txt");

            Assert.Equal(
                @"C:temp\..",
                PathUtilities.GetDirectoryName(@"C:temp\..\foo.txt", isUnixLike: false));

            Assert.Equal(
                @"C:temp",
                PathUtilities.GetDirectoryName(@"C:temp\..", isUnixLike: false));

            Assert.Equal(
                @"C:temp\.",
                PathUtilities.GetDirectoryName(@"C:temp\.\foo.txt", isUnixLike: false));

            Assert.Equal(
                @"C:temp",
                PathUtilities.GetDirectoryName(@"C:temp\.", isUnixLike: false));

            TestGetDirectoryNameAndCompareToDotnet(@"C:temp", @"C:temp\");
            TestGetDirectoryNameAndCompareToDotnet(@"C:", @"C:temp");
            TestGetDirectoryNameAndCompareToDotnet(null, @"C:");
        }

        [Fact]
        public void TestContainsPathComponent()
        {
            Assert.True(
                PathUtilities.ContainsPathComponent(@"c:\packages\temp", "packages", ignoreCase: true));
            Assert.True(
                PathUtilities.ContainsPathComponent(@"\\server\packages\temp", "packages", ignoreCase: true));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"\\packages\temp", "packages", ignoreCase: true));
            Assert.True(
                PathUtilities.ContainsPathComponent(@"c:\packages", "packages", ignoreCase: true));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\packages1\temp", "packages", ignoreCase: true));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\package\temp", "packages", ignoreCase: true));

            Assert.True(
                PathUtilities.ContainsPathComponent(@"c:\packages\temp", "packages", ignoreCase: false));
            Assert.True(
                PathUtilities.ContainsPathComponent(@"\\server\packages\temp", "packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"\\packages\temp", "packages", ignoreCase: false));
            Assert.True(
                PathUtilities.ContainsPathComponent(@"c:\packages", "packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\packages1\temp", "packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\package\temp", "packages", ignoreCase: false));

            Assert.True(
                PathUtilities.ContainsPathComponent(@"c:\packages\temp", "Packages", ignoreCase: true));
            Assert.True(
                PathUtilities.ContainsPathComponent(@"\\server\packages\temp", "Packages", ignoreCase: true));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"\\packages\temp", "Packages", ignoreCase: true));
            Assert.True(
                PathUtilities.ContainsPathComponent(@"c:\packages", "Packages", ignoreCase: true));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\packages1\temp", "Packages", ignoreCase: true));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\package\temp", "Packages", ignoreCase: true));

            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\packages\temp", "Packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"\\server\packages\temp", "Packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"\\packages\temp", "Packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\packages", "Packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\packages1\temp", "Packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\package\temp", "Packages", ignoreCase: false));

            Assert.True(
                PathUtilities.ContainsPathComponent(@"c:\Packages\temp", "packages", ignoreCase: true));
            Assert.True(
                PathUtilities.ContainsPathComponent(@"\\server\Packages\temp", "packages", ignoreCase: true));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"\\Packages\temp", "packages", ignoreCase: true));
            Assert.True(
                PathUtilities.ContainsPathComponent(@"c:\Packages", "packages", ignoreCase: true));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\Packages1\temp", "packages", ignoreCase: true));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\Package\temp", "packages", ignoreCase: true));

            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\Packages\temp", "packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"\\server\Packages\temp", "packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"\\Packages\temp", "packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\Packages", "packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\Packages1\temp", "packages", ignoreCase: false));
            Assert.False(
                PathUtilities.ContainsPathComponent(@"c:\Package\temp", "packages", ignoreCase: false));
        }
    }
}
