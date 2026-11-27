// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class FilePathUtilitiesTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1579")]
    public void GetRelativePath_SameDirectory()
    {
        var baseDirectory = TestHelpers.GetRootedPath("Alpha", "Beta", "Gamma");
        var fullPath = TestHelpers.GetRootedPath("Alpha", "Beta", "Gamma", "Doc.txt");

        var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

        Assert.Equal(expected: "Doc.txt", actual: result);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1579")]
    public void GetRelativePath_NestedOneLevelDown()
    {
        var baseDirectory = TestHelpers.GetRootedPath("Alpha", "Beta", "Gamma");
        var fullPath = TestHelpers.GetRootedPath("Alpha", "Beta", "Gamma", "Delta", "Doc.txt");

        var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

        Assert.Equal(expected: Path.Combine("Delta", "Doc.txt"), actual: result);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1579")]
    public void GetRelativePath_NestedTwoLevelsDown()
    {
        var baseDirectory = TestHelpers.GetRootedPath("Alpha", "Beta", "Gamma");
        var fullPath = TestHelpers.GetRootedPath("Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Doc.txt");

        var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

        Assert.Equal(expected: Path.Combine("Delta", "Epsilon", "Doc.txt"), actual: result);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1579")]
    public void GetRelativePath_UpOneLevel()
    {
        var baseDirectory = TestHelpers.GetRootedPath("Alpha", "Beta", "Gamma");
        var fullPath = TestHelpers.GetRootedPath("Alpha", "Beta", "Doc.txt");

        var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

        Assert.Equal(expected: Path.Combine("..", "Doc.txt"), actual: result);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1579")]
    public void GetRelativePath_UpTwoLevels()
    {
        var baseDirectory = TestHelpers.GetRootedPath("Alpha", "Beta", "Gamma");
        var fullPath = TestHelpers.GetRootedPath("Alpha", "Doc.txt");

        var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

        Assert.Equal(expected: Path.Combine("..", "..", "Doc.txt"), actual: result);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1579")]
    public void GetRelativePath_UpTwoLevelsAndThenDown()
    {
        var baseDirectory = TestHelpers.GetRootedPath("Alpha", "Beta", "Gamma");
        var fullPath = TestHelpers.GetRootedPath("Alpha", "Phi", "Omega", "Doc.txt");

        var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

        Assert.Equal(expected: Path.Combine("..", "..", "Phi", "Omega", "Doc.txt"), actual: result);
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "Tests drive letter behavior, which is Windows-specific"), WorkItem("https://github.com/dotnet/roslyn/issues/1579")]
    public void GetRelativePath_OnADifferentDrive()
    {
        var baseDirectory = @"C:\Alpha\Beta\Gamma";
        var fullPath = @"D:\Alpha\Beta\Gamma\Doc.txt";

        var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

        Assert.Equal(expected: @"D:\Alpha\Beta\Gamma\Doc.txt", actual: result);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4660")]
    public void GetRelativePath_WithBaseDirectoryMatchingIncompletePortionOfFullPath()
    {
        var baseDirectory = TestHelpers.GetRootedPath("Alpha", "Beta");
        var fullPath = TestHelpers.GetRootedPath("Alpha", "Beta2", "Gamma");

        var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

        Assert.Equal(expected: Path.Combine("..", "Beta2", "Gamma"), actual: result);
    }

    [ConditionalTheory(typeof(WindowsOnly)), WorkItem(72043, "https://github.com/dotnet/roslyn/issues/72043")]
    [InlineData(@"C:\Alpha", @"C:\", @"..")]
    [InlineData(@"C:\Alpha\Beta", @"C:\", @"..\..")]
    [InlineData(@"C:\Alpha\Beta", @"C:\Gamma", @"..\..\Gamma")]
    public void GetRelativePath_WithFullPathShorterThanBasePath_Windows(string baseDirectory, string fullPath, string expected)
    {
        var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

        Assert.Equal(expected, result);
    }

    [ConditionalTheory(typeof(UnixLikeOnly)), WorkItem(72043, "https://github.com/dotnet/roslyn/issues/72043")]
    [InlineData("/Alpha", "/", "..")]
    [InlineData("/Alpha/Beta", "/", "../..")]
    [InlineData("/Alpha/Beta", "/Gamma", "../../Gamma")]
    public void GetRelativePath_WithFullPathShorterThanBasePath_Unix(string baseDirectory, string fullPath, string expected)
    {
        var result = PathUtilities.GetRelativePath(baseDirectory, fullPath);

        Assert.Equal(expected, result);
    }
}
