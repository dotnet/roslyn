// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class PathUtilitiesTests
{
    // This test data and the tests that use it are derived from the .NET Runtime:
    // - https://github.com/dotnet/runtime/blob/4eff254880789bf59bab922763446161b1f80640/src/libraries/System.Runtime/tests/System.Runtime.Extensions.Tests/System/IO/PathTests.cs

    public static TheoryData<string, string> TestData_GetExtension => new()
    {
        { @"file.exe", ".exe" },
        { @"file", "" },
        { @"file.", "" },
        { @"file.s", ".s" },
        { @"test/file", "" },
        { @"test/file.extension", ".extension" },
        { @"test\file", "" },
        { @"test\file.extension", ".extension" },
        { "file.e xe", ".e xe"},
        { "file. ", ". "},
        { " file. ", ". "},
        { " file.extension", ".extension"}
    };

    public static TheoryData<string, string?> TestData_GetDirectoryName => new()
    {
        { ".", "" },
        { "..", "" },
        { "baz", "" },
        { Path.Combine("dir", "baz"), "dir" },
        { "dir.foo" + Path.AltDirectorySeparatorChar + "baz.txt", "dir.foo" },
        { Path.Combine("dir", "baz", "bar"), Path.Combine("dir", "baz") },
        { Path.Combine("..", "..", "files.txt"), Path.Combine("..", "..") },
        { Path.DirectorySeparatorChar + "foo", Path.DirectorySeparatorChar.ToString() },
        { Path.DirectorySeparatorChar.ToString(), null }
    };

    public static TheoryData<string, string?> TestData_GetDirectoryName_Windows => new()
    {
        { @"C:\", null },
        { @"C:/", null },
        { @"C:", null },
        { @"dir\\baz", "dir" },
        { @"dir//baz", "dir" },
        { @"C:\foo", @"C:\" },
        { @"C:foo", "C:" }
    };

    public static TheoryData<string> TestData_Spaces =>
    [
        " ",
        "   "
    ];

    public static TheoryData<string> TestData_EmbeddedNull =>
    [
        "a\0b"
    ];

    public static TheoryData<string> TestData_ControlChars =>
    [
        "\t",
        "\r\n",
        "\b",
        "\v",
        "\n"
    ];

    public static TheoryData<string> TestData_UnicodeWhiteSpace =>
    [
        "\u00A0", // Non-breaking Space
        "\u2028", // Line separator
        "\u2029", // Paragraph separator
    ];

    [Theory, MemberData(nameof(TestData_GetExtension))]
    public void GetExtension(string path, string expected)
    {
        Assert.Equal(expected, Path.GetExtension(path));
        Assert.Equal(!string.IsNullOrEmpty(expected), Path.HasExtension(path));
    }

    [Theory, MemberData(nameof(TestData_GetExtension))]
    public void GetExtension_Span(string path, string expected)
    {
        AssertEqual(expected, PathUtilities.GetExtension(path.AsSpan()));
        Assert.Equal(!string.IsNullOrEmpty(expected), PathUtilities.HasExtension(path.AsSpan()));
    }

    // The tests below are derived from the .NET Runtime:
    // - https://github.com/dotnet/runtime/blob/91195a7948a16c769ccaf7fd8ca84b1d210f6841/src/libraries/System.Runtime/tests/System.Runtime.Extensions.Tests/System/IO/Path.IsPathFullyQualified.cs

    [Fact]
    public static void IsPathFullyQualified_NullArgument()
    {
        Assert.Throws<ArgumentNullException>(() => PathUtilities.IsPathFullyQualified(null!));
    }

    [Fact]
    public static void IsPathFullyQualified_Empty()
    {
        Assert.False(PathUtilities.IsPathFullyQualified(""));
        Assert.False(PathUtilities.IsPathFullyQualified(ReadOnlySpan<char>.Empty));
    }

    [ConditionalTheory(Is.Windows)]
    [InlineData("/")]
    [InlineData(@"\")]
    [InlineData(".")]
    [InlineData("C:")]
    [InlineData("C:foo.txt")]
    public static void IsPathFullyQualified_Windows_Invalid(string path)
    {
        Assert.False(PathUtilities.IsPathFullyQualified(path));
        Assert.False(PathUtilities.IsPathFullyQualified(path.AsSpan()));
    }

    [ConditionalTheory(Is.Windows)]
    [InlineData(@"\\")]
    [InlineData(@"\\\")]
    [InlineData(@"\\Server")]
    [InlineData(@"\\Server\Foo.txt")]
    [InlineData(@"\\Server\Share\Foo.txt")]
    [InlineData(@"\\Server\Share\Test\Foo.txt")]
    [InlineData(@"C:\")]
    [InlineData(@"C:\foo1")]
    [InlineData(@"C:\\")]
    [InlineData(@"C:\\foo2")]
    [InlineData(@"C:/")]
    [InlineData(@"C:/foo1")]
    [InlineData(@"C://")]
    [InlineData(@"C://foo2")]
    public static void IsPathFullyQualified_Windows_Valid(string path)
    {
        Assert.True(PathUtilities.IsPathFullyQualified(path));
        Assert.True(PathUtilities.IsPathFullyQualified(path.AsSpan()));
    }

    [ConditionalTheory(Is.AnyUnix)]
    [InlineData(@"\")]
    [InlineData(@"\\")]
    [InlineData(".")]
    [InlineData("./foo.txt")]
    [InlineData("..")]
    [InlineData("../foo.txt")]
    [InlineData(@"C:")]
    [InlineData(@"C:/")]
    [InlineData(@"C://")]
    public static void IsPathFullyQualified_Unix_Invalid(string path)
    {
        Assert.False(PathUtilities.IsPathFullyQualified(path));
        Assert.False(PathUtilities.IsPathFullyQualified(path.AsSpan()));
    }

    [ConditionalTheory(Is.AnyUnix)]
    [InlineData("/")]
    [InlineData("/foo.txt")]
    [InlineData("/..")]
    [InlineData("//")]
    [InlineData("//foo.txt")]
    [InlineData("//..")]
    public static void IsPathFullyQualified_Unix_Valid(string path)
    {
        Assert.True(PathUtilities.IsPathFullyQualified(path));
        Assert.True(PathUtilities.IsPathFullyQualified(path.AsSpan()));
    }

    [Fact]
    public void GetDirectoryName_NullReturnsNull()
    {
        Assert.Null(PathUtilities.GetDirectoryName(null));
    }

    [Theory, MemberData(nameof(TestData_GetDirectoryName))]
    public void GetDirectoryName(string path, string? expected)
    {
        Assert.Equal(expected, PathUtilities.GetDirectoryName(path));
    }

    [Fact]
    public void GetDirectoryName_CurrentDirectory()
    {
        var curDir = Directory.GetCurrentDirectory();
        Assert.Equal(curDir, PathUtilities.GetDirectoryName(Path.Combine(curDir, "baz")));

        Assert.Null(PathUtilities.GetDirectoryName(Path.GetPathRoot(curDir)));
        Assert.True(PathUtilities.GetDirectoryName(Path.GetPathRoot(curDir).AsSpan()).IsEmpty);
    }

    [Fact]
    public void GetDirectoryName_EmptyReturnsNull()
    {
        // In .NET Framework this throws argument exception
        Assert.Null(PathUtilities.GetDirectoryName(string.Empty));
    }

    [ConditionalTheory(Is.Windows)]
    [MemberData(nameof(TestData_Spaces))]
    public void GetDirectoryName_Spaces_Windows(string path)
    {
        // In Windows spaces are eaten by Win32, making them effectively empty
        Assert.Null(PathUtilities.GetDirectoryName(path));
    }

    [ConditionalTheory(Is.AnyUnix)]
    [MemberData(nameof(TestData_Spaces))]
    public void GetDirectoryName_Spaces_Unix(string path)
    {
        Assert.Empty(PathUtilities.GetDirectoryName(path));
    }

    [Theory, MemberData(nameof(TestData_Spaces))]
    public void GetDirectoryName_Span_Spaces(string path)
    {
        Assert.True(PathUtilities.GetDirectoryName(path.AsSpan()).IsEmpty);
    }

    [Theory]
    [MemberData(nameof(TestData_EmbeddedNull))]
    [MemberData(nameof(TestData_ControlChars))]
    [MemberData(nameof(TestData_UnicodeWhiteSpace))]
    public void GetDirectoryName_NetFxInvalid(string path)
    {
        Assert.Empty(PathUtilities.GetDirectoryName(path));
        Assert.Equal(path, PathUtilities.GetDirectoryName(PathCombine(path, path)));
        Assert.True(PathUtilities.GetDirectoryName(path.AsSpan()).IsEmpty);
        AssertEqual(path, PathUtilities.GetDirectoryName(PathCombine(path, path).AsSpan()));

        // Path.Combine on net472 throws on invalid path characters, so have to do this manually.
        static string PathCombine(string path1, string path2)
        {
            return $"{path1}{Path.DirectorySeparatorChar}{path2}";
        }
    }

    [Theory, MemberData(nameof(TestData_GetDirectoryName))]
    public void GetDirectoryName_Span(string path, string? expected)
    {
        AssertEqual(expected ?? ReadOnlySpan<char>.Empty, PathUtilities.GetDirectoryName(path.AsSpan()));
    }

    [Fact]
    public void GetDirectoryName_Span_CurrentDirectory()
    {
        var curDir = Directory.GetCurrentDirectory();
        AssertEqual(curDir, PathUtilities.GetDirectoryName(Path.Combine(curDir, "baz").AsSpan()));
        Assert.True(PathUtilities.GetDirectoryName(Path.GetPathRoot(curDir).AsSpan()).IsEmpty);
    }

    [Theory]
    [InlineData(@" C:\dir/baz", @" C:\dir")]
    public void GetDirectoryName_SkipSpaces(string path, string expected)
    {
        // We no longer trim leading spaces for any path
        Assert.Equal(expected, PathUtilities.GetDirectoryName(path));
    }

    [ConditionalTheory(Is.Windows)]
    [MemberData(nameof(TestData_GetDirectoryName_Windows))]
    public void GetDirectoryName_Windows(string path, string? expected)
    {
        Assert.Equal(expected, Path.GetDirectoryName(path));
    }

    private static void AssertEqual(ReadOnlySpan<char> expected, ReadOnlySpan<char> actual)
    {
        if (!actual.SequenceEqual(expected))
        {
            throw Xunit.Sdk.EqualException.ForMismatchedValues(expected.ToString(), actual.ToString());
        }
    }
}
