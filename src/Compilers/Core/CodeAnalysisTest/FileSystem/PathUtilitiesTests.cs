// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.FileSystem
{
    public class PathUtilitiesTests
    {
        private void TestGetDirectoryNameAndCompareToDotnet(string expectedDirectoryName, string fullPath)
        {
            var roslynName = PathUtilities.TestAccessor.GetDirectoryName(fullPath, isUnixLike: false);
            Assert.Equal(expectedDirectoryName, roslynName);

            var dotnetName = Path.GetDirectoryName(fullPath);
            Assert.Equal(dotnetName, roslynName);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void TestGetDirectoryName_WindowsPaths_Absolute()
        {
            TestGetDirectoryNameAndCompareToDotnet(@"C:\temp", @"C:\temp\goo.txt");
            TestGetDirectoryNameAndCompareToDotnet(@"C:\temp", @"C:\temp\goo");
            TestGetDirectoryNameAndCompareToDotnet(@"C:\temp", @"C:\temp\");
            TestGetDirectoryNameAndCompareToDotnet(@"C:\", @"C:\temp");
            TestGetDirectoryNameAndCompareToDotnet(null, @"C:\");
            TestGetDirectoryNameAndCompareToDotnet(null, @"C:");

            // dotnet throws on empty argument.  But not on null... go figure.
            Assert.Null(PathUtilities.TestAccessor.GetDirectoryName(@"", isUnixLike: false));

            TestGetDirectoryNameAndCompareToDotnet(null, null);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void TestGetDirectoryName_WindowsPaths_Relative()
        {
            TestGetDirectoryNameAndCompareToDotnet(@"goo\temp", @"goo\temp\goo.txt");
            TestGetDirectoryNameAndCompareToDotnet(@"goo\temp", @"goo\temp\goo");
            TestGetDirectoryNameAndCompareToDotnet(@"goo\temp", @"goo\temp\");
            TestGetDirectoryNameAndCompareToDotnet(@"goo", @"goo\temp");
            TestGetDirectoryNameAndCompareToDotnet(@"goo", @"goo\");
            TestGetDirectoryNameAndCompareToDotnet("", @"goo");
        }

        [Fact]
        public void TestGetDirectoryName_UnixPaths_Absolute()
        {
            Assert.Equal(
                @"/temp",
                PathUtilities.TestAccessor.GetDirectoryName(@"/temp/goo.txt", isUnixLike: true));

            Assert.Equal(
                @"/temp",
                PathUtilities.TestAccessor.GetDirectoryName(@"/temp/goo", isUnixLike: true));

            Assert.Equal(
                @"/temp",
                PathUtilities.TestAccessor.GetDirectoryName(@"/temp/", isUnixLike: true));

            Assert.Equal(
                @"/",
                PathUtilities.TestAccessor.GetDirectoryName(@"/temp", isUnixLike: true));

            Assert.Null(
                PathUtilities.TestAccessor.GetDirectoryName(@"/", isUnixLike: true));

            Assert.Null(
                PathUtilities.TestAccessor.GetDirectoryName(@"", isUnixLike: true));

            Assert.Null(
                PathUtilities.TestAccessor.GetDirectoryName(null, isUnixLike: true));
        }

        [Fact]
        public void TestGetDirectoryName_UnixPaths_Relative()
        {
            Assert.Equal(
                @"goo/temp",
                PathUtilities.TestAccessor.GetDirectoryName(@"goo/temp/goo.txt", isUnixLike: true));

            Assert.Equal(
                @"goo/temp",
                PathUtilities.TestAccessor.GetDirectoryName(@"goo/temp/goo", isUnixLike: true));

            Assert.Equal(
                @"goo/temp",
                PathUtilities.TestAccessor.GetDirectoryName(@"goo/temp/", isUnixLike: true));

            Assert.Equal(
                @"goo",
                PathUtilities.TestAccessor.GetDirectoryName(@"goo/temp", isUnixLike: true));

            Assert.Equal(
                @"goo",
                PathUtilities.TestAccessor.GetDirectoryName(@"goo/", isUnixLike: true));

            Assert.Equal(
                "",
                PathUtilities.TestAccessor.GetDirectoryName(@"goo", isUnixLike: true));

            Assert.Null(
                PathUtilities.TestAccessor.GetDirectoryName(@"", isUnixLike: true));

            Assert.Null(
                PathUtilities.TestAccessor.GetDirectoryName(null, isUnixLike: true));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void TestGetDirectoryName_WindowsSharePaths()
        {
            TestGetDirectoryNameAndCompareToDotnet(@"\\server\temp", @"\\server\temp\goo.txt");
            TestGetDirectoryNameAndCompareToDotnet(@"\\server\temp", @"\\server\temp\goo");
            TestGetDirectoryNameAndCompareToDotnet(@"\\server\temp", @"\\server\temp\");
            TestGetDirectoryNameAndCompareToDotnet(null, @"\\server\temp");
            TestGetDirectoryNameAndCompareToDotnet(null, @"\\server\");
            TestGetDirectoryNameAndCompareToDotnet(null, @"\\server");
            TestGetDirectoryNameAndCompareToDotnet(null, @"\\");
            TestGetDirectoryNameAndCompareToDotnet(null, @"\");
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void TestGetDirectoryName_EsotericCases()
        {
            TestGetDirectoryNameAndCompareToDotnet(@"C:\temp", @"C:\temp\\goo.txt");
            TestGetDirectoryNameAndCompareToDotnet(@"C:\temp", @"C:\temp\\\goo.txt");

            // Dotnet does normalization of dots, so we can't compare against it here.
            Assert.Equal(
                @"C:\temp\..",
                PathUtilities.TestAccessor.GetDirectoryName(@"C:\temp\..\goo.txt", isUnixLike: false));

            Assert.Equal(
                @"C:\temp",
                PathUtilities.TestAccessor.GetDirectoryName(@"C:\temp\..", isUnixLike: false));

            Assert.Equal(
                @"C:\temp\.",
                PathUtilities.TestAccessor.GetDirectoryName(@"C:\temp\.\goo.txt", isUnixLike: false));

            Assert.Equal(
                @"C:\temp",
                PathUtilities.TestAccessor.GetDirectoryName(@"C:\temp\.", isUnixLike: false));

            TestGetDirectoryNameAndCompareToDotnet(@"C:temp", @"C:temp\\goo.txt");
            TestGetDirectoryNameAndCompareToDotnet(@"C:temp", @"C:temp\\\goo.txt");

            Assert.Equal(
                @"C:temp\..",
                PathUtilities.TestAccessor.GetDirectoryName(@"C:temp\..\goo.txt", isUnixLike: false));

            Assert.Equal(
                @"C:temp",
                PathUtilities.TestAccessor.GetDirectoryName(@"C:temp\..", isUnixLike: false));

            Assert.Equal(
                @"C:temp\.",
                PathUtilities.TestAccessor.GetDirectoryName(@"C:temp\.\goo.txt", isUnixLike: false));

            Assert.Equal(
                @"C:temp",
                PathUtilities.TestAccessor.GetDirectoryName(@"C:temp\.", isUnixLike: false));

            TestGetDirectoryNameAndCompareToDotnet(@"C:temp", @"C:temp\");
            TestGetDirectoryNameAndCompareToDotnet(@"C:", @"C:temp");
            TestGetDirectoryNameAndCompareToDotnet(null, @"C:");
        }

        [ConditionalFact(typeof(WindowsOnly))]
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

        [ConditionalFact(typeof(WindowsOnly))]
        public void IsSameDirectoryOrChildOfHandlesDifferentSlashes()
        {
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\", @"C:"));
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\", @"C:\"));
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:", @"C:"));
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:", @"C:\"));

            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH", @"C:"));
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH", @"C:\"));

            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH\", @"C:"));
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH\", @"C:\"));

            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH", @"C:\ABCD"));
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH", @"C:\ABCD\"));

            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH\", @"C:\ABCD"));
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH\", @"C:\ABCD\"));

            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH", @"C:\ABCD\EFGH"));
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH", @"C:\ABCD\EFGH\"));

            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH\", @"C:\ABCD\EFGH"));
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCD\EFGH\", @"C:\ABCD\EFGH\"));
        }

        [Fact]
        public void IsSameDirectoryOrChildOfNegativeTests()
        {
            Assert.False(PathUtilities.IsSameDirectoryOrChildOf(@"C:\", @"C:\ABCD"));
            Assert.False(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABC", @"C:\ABCD"));
            Assert.False(PathUtilities.IsSameDirectoryOrChildOf(@"C:\ABCDE", @"C:\ABCD"));

            Assert.False(PathUtilities.IsSameDirectoryOrChildOf(@"C:\A\B\C", @"C:\A\B\C\D"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void IsSameDirectoryOrChildOfSpecifyingCaseSensitivity_Windows()
        {
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\a\B\C", @"C:\A\B", StringComparison.OrdinalIgnoreCase));
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"C:\A\b\C", @"C:\A\B", StringComparison.OrdinalIgnoreCase));

            Assert.False(PathUtilities.IsSameDirectoryOrChildOf(@"C:\a\B\C", @"C:\A\B", StringComparison.Ordinal));
            Assert.False(PathUtilities.IsSameDirectoryOrChildOf(@"C:\A\b\C", @"C:\A\B", StringComparison.Ordinal));
        }

        [ConditionalFact(typeof(UnixLikeOnly))]
        public void IsSameDirectoryOrChildOfSpecifyingCaseSensitivity_Unix()
        {
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"/a/B/C", @"/A/B", StringComparison.OrdinalIgnoreCase));
            Assert.True(PathUtilities.IsSameDirectoryOrChildOf(@"/A/b/C", @"/A/B", StringComparison.OrdinalIgnoreCase));

            Assert.False(PathUtilities.IsSameDirectoryOrChildOf(@"/a/B/C", @"/A/B", StringComparison.Ordinal));
            Assert.False(PathUtilities.IsSameDirectoryOrChildOf(@"/A/b/C", @"/A/B", StringComparison.Ordinal));
        }

        [Fact]
        public void IsValidFilePath()
        {
            var cases = new[] {
                ("test/data1.txt", true),
                ("test\\data1.txt", true),
                ("data1.txt", true),
                ("data1", true),
                ("data1\\", PathUtilities.IsUnixLikePlatform),
                ("data1//", false),
                (null, false),
                ("", false),
                ("  ", ExecutionConditionUtil.IsCoreClrUnix),
                ("path/?.txt", !ExecutionConditionUtil.IsWindowsDesktop),
                ("path/*.txt", !ExecutionConditionUtil.IsWindowsDesktop),
                ("path/:.txt", !ExecutionConditionUtil.IsWindowsDesktop),
                ("path/\".txt", !ExecutionConditionUtil.IsWindowsDesktop),
                ("IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII" +
            "IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII" +
            "IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII" +
            "IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII" +
            "IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII.txt", !ExecutionConditionUtil.IsWindowsDesktop)
            };

            foreach (var (path, isValid) in cases)
            {
                Assert.True(isValid == PathUtilities.IsValidFilePath(path), $"Expected {isValid} for \"{path}\"");
            }
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData(@"C:\", "B")]
        [InlineData(@"C:\A", "B")]
        [InlineData(@"C:\A\", "B")]
        [InlineData(@"C:A\", "B")]
        [InlineData(@"\A", "B")]
        [InlineData(@"\\A\B\C", "B")]
        [InlineData(@"C", @"B:\")]
        [InlineData(@"C:", @"B:\")]
        [InlineData(@"C:\", @"B:\")]
        [InlineData(@"C:\A", @"B:\")]
        [InlineData(@"C:\A\", @"B:\")]
        [InlineData(@"C:A\", @"B:\")]
        [InlineData(@"\A", @"B:\")]
        [InlineData(@"\\A\B\C", @"B:\")]
        [InlineData("", @"B:\")]
        [InlineData(" ", @"B:\")]
        public void CombinePaths_SameAsPathCombine_Windows(string path1, string path2)
        {
            Assert.Equal(Path.Combine(path1, path2), PathUtilities.CombinePaths(path1, path2));
        }

        [ConditionalTheory(typeof(UnixLikeOnly))]
        [InlineData("C", "B")]
        [InlineData("C/", "\t")]
        [InlineData("C/", "B")]
        [InlineData("/C", "B")]
        [InlineData("/C/", "B")]
        [InlineData("C", "/B")]
        [InlineData("C/", "/B")]
        [InlineData("/C", "/B")]
        [InlineData("/C/", "/B")]
        [InlineData("", "/B/")]
        [InlineData(" ", "/B/")]
        public void CombinePaths_SameAsPathCombine_Linux(string path1, string path2)
        {
            Assert.Equal(Path.Combine(path1, path2), PathUtilities.CombinePaths(path1, path2));
        }

        [Theory]
        [InlineData("C", " ")]
        [InlineData("C", "B")]
        [InlineData("", "")]
        [InlineData(" ", " ")]
        [InlineData("", "B")]
        [InlineData(" ", "B")]
        public void CombinePaths_SameAsPathCombine_Common(string path1, string path2)
        {
            Assert.Equal(Path.Combine(path1, path2), PathUtilities.CombinePaths(path1, path2));
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData(@"C:\|\<>", @"C:\|", "<>")]
        [InlineData("C:\\\t", @"C:\", "\t")]
        [InlineData("C", "C", null)]
        [InlineData("C:B", "C:", "B")]
        [InlineData(null, null, null)]
        [InlineData("B", null, "B")]
        public void CombinePaths_DifferentFromPathCombine(string expected, string path1, string path2)
        {
            Assert.Equal(expected, PathUtilities.CombinePaths(path1, path2));
        }

        [ConditionalFact(typeof(WindowsOnly)), WorkItem(51602, @"https://github.com/dotnet/roslyn/issues/51602")]
        public void GetRelativePath_EnsureNo_IndexOutOfRangeException_Windows()
        {
            var expected = "";
            var result = PathUtilities.GetRelativePath(@"C:\A\B\", @"C:\A\B");
            Assert.Equal(expected, result);
        }

        [ConditionalFact(typeof(UnixLikeOnly)), WorkItem(51602, @"https://github.com/dotnet/roslyn/issues/51602")]
        public void GetRelativePath_EnsureNo_IndexOutOfRangeException_Unix()
        {
            var expected = "";
            var result = PathUtilities.GetRelativePath(@"/A/B/", @"/A/B");
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(@"//a/b/c", @"//a/b/c")]
        [InlineData(@"/a\b/c/", @"/a/b/c/")]
        [InlineData(@"\a\b/c/", @"/a/b/c/")]
        [InlineData(@"C:\\a", @"C:/a")]
        [InlineData(@"C:\a\b\c\", @"C:/a/b/c/")]
        [InlineData(@"/\a", @"//a")]
        [InlineData(@"a\\\b", @"a/b")]
        [InlineData(@"\\\a\b\c", @"///a/b/c")]
        [InlineData(@"\\\\a\b\c", @"///a/b/c")]
        public void CollapseWithForwardSlash(string input, string output)
        {
            AssertEx.Equal(output, PathUtilities.CollapseWithForwardSlash(input.AsSpan()));
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData(@"//a/b/c", @"//a/b/c")]
        [InlineData(@"/a\b/c/", @"/a\b/c/")]
        [InlineData(@"C:B", @"C:B")]
        [InlineData(@"c:b", @"c:b")]
        [InlineData(@"c:\b", @"C:\b")]
        [InlineData(@"c:/b", @"C:/b")]
        public void NormalizeDriveLetter_Windows(string input, string output)
        {
            AssertEx.Equal(output, PathUtilities.NormalizeDriveLetter(input));
        }

        [ConditionalTheory(typeof(UnixLikeOnly))]
        [InlineData(@"//a/b/c")]
        [InlineData(@"/a\b/c/")]
        [InlineData(@"C:B")]
        [InlineData(@"c:b")]
        [InlineData(@"c:\b")]
        [InlineData(@"c:/b")]
        public void NormalizeDriveLetter_UnixLike(string input)
        {
            AssertEx.Equal(input, PathUtilities.NormalizeDriveLetter(input));
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData(@"c:\a\b\..\c", @"C:/a/c")]
        [InlineData(@"C:\a\.\b\c", @"C:/a/b/c")]
        [InlineData(@"c:\\a\\b\\c", @"C:/a/b/c")]
        [InlineData(@"c:/a/b/../c", @"C:/a/c")]
        [InlineData(@"C:/a/./b/c", @"C:/a/b/c")]
        [InlineData(@"c:\a\b\c\", @"C:/a/b/c/")]
        [InlineData(@"c:\", @"C:/")]
        [InlineData(@"c:/", @"C:/")]
        public void NormalizePathForEditorConfig_Windows(string input, string expected)
        {
            AssertEx.Equal(expected, PathUtilities.NormalizePathForEditorConfig(input));
        }

        [Theory]
        [InlineData(@"/a/b/../c", @"/a/c")]
        [InlineData(@"/a/./b/c", @"/a/b/c")]
        [InlineData(@"/a//b//c", @"/a/b/c")]
        [InlineData(@"/a\b/c", @"/a/b/c")]
        [InlineData(@"//a/b/c", @"//a/b/c")]
        [InlineData(@"/a/b/c/", @"/a/b/c/")]
        [InlineData(@"/", @"/")]
        public void NormalizePathForEditorConfig_Unix(string input, string expected)
        {
            AssertEx.Equal(expected, PathUtilities.NormalizePathForEditorConfig(input));
        }
    }
}
