// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorProjectFileSystemTest
{
    [Fact]
    public void NormalizeAndEnsureValidPath_DoesNotModifyPath()
    {
        // Arrange
        var project = new TestRazorProjectFileSystem();

        // Act
        var path = project.NormalizeAndEnsureValidPath("/Views/Home/Index.cshtml");

        // Assert
        Assert.Equal("/Views/Home/Index.cshtml", path);
    }

    [Fact]
    public void NormalizeAndEnsureValidPath_ThrowsIfPathIsNull()
    {
        // Arrange
        var project = new TestRazorProjectFileSystem();

        // Act and Assert
        Assert.Throws<ArgumentNullException>(paramName: "path", () => project.NormalizeAndEnsureValidPath(null!));
    }

    [Fact]
    public void NormalizeAndEnsureValidPath_ThrowsIfPathIsEmpty()
    {
        // Arrange
        var project = new TestRazorProjectFileSystem();

        // Act and Assert
        Assert.Throws<ArgumentException>(paramName: "path", () => project.NormalizeAndEnsureValidPath(""));
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("~/foo")]
    [InlineData("\\foo")]
    public void NormalizeAndEnsureValidPath_ThrowsIfPathDoesNotStartWithForwardSlash(string path)
    {
        // Arrange
        var project = new TestRazorProjectFileSystem();

        // Act and Assert
        Assert.Throws<ArgumentException>(paramName: "path", () => project.NormalizeAndEnsureValidPath(path));
    }

    [Fact]
    public void FindHierarchicalItems_ReturnsEmptySequenceIfPathIsAtRoot()
    {
        // Arrange
        var project = new TestRazorProjectFileSystem();

        // Act
        var result = project.FindHierarchicalItems("/", "File.cshtml");

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("_ViewStart.cshtml")]
    [InlineData("_ViewImports.cshtml")]
    public void FindHierarchicalItems_ReturnsItemsForPath(string fileName)
    {
        // Arrange
        var path = "/Views/Home/Index.cshtml";
        var project = new TestRazorProjectFileSystem(
            CreateProjectItem($"/{fileName}"),
            CreateProjectItem($"/Views/{fileName}"),
            CreateProjectItem($"/Views/Home/{fileName}"));

        // Act
        var result = project.FindHierarchicalItems(path, $"{fileName}");

        // Assert
        Assert.Collection(
            result,
            item => Assert.Equal($"/{fileName}", item.FilePath),
            item => Assert.Equal($"/Views/{fileName}", item.FilePath),
            item => Assert.Equal($"/Views/Home/{fileName}", item.FilePath));
    }

    [Fact]
    public void FindHierarchicalItems_ReturnsItemsForPathAtRoot()
    {
        // Arrange
        var path = "/Index.cshtml";
        var project = new TestRazorProjectFileSystem(
            CreateProjectItem("/File.cshtml"));

        // Act
        var result = project.FindHierarchicalItems(path, "File.cshtml");

        // Assert
        Assert.Collection(
            result,
            item => Assert.Equal("/File.cshtml", item.FilePath));
    }

    [Fact]
    public void FindHierarchicalItems_DoesNotIncludePassedInItem()
    {
        // Arrange
        var path = "/Areas/MyArea/Views/Home/File.cshtml";
        var project = new TestRazorProjectFileSystem(
            CreateProjectItem("/Areas/MyArea/Views/Home/File.cshtml"),
            CreateProjectItem("/Areas/MyArea/Views/File.cshtml"),
            CreateProjectItem("/Areas/MyArea/File.cshtml"),
            CreateProjectItem("/Areas/File.cshtml"),
            CreateProjectItem("/File.cshtml"));

        // Act
        var result = project.FindHierarchicalItems(path, "File.cshtml");

        // Assert
        Assert.Collection(
            result,
            item => Assert.Equal("/File.cshtml", item.FilePath),
            item => Assert.Equal("/Areas/File.cshtml", item.FilePath),
            item => Assert.Equal("/Areas/MyArea/File.cshtml", item.FilePath),
            item => Assert.Equal("/Areas/MyArea/Views/File.cshtml", item.FilePath));
    }

    [Fact]
    public void FindHierarchicalItems_ReturnsEmptySequenceIfPassedInItemWithFileNameIsAtRoot()
    {
        // Arrange
        var path = "/File.cshtml";
        var project = new TestRazorProjectFileSystem(
            CreateProjectItem("/File.cshtml"));

        // Act
        var result = project.FindHierarchicalItems(path, "File.cshtml");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindHierarchicalItems_IncludesNonExistentFiles()
    {
        // Arrange
        var path = "/Areas/MyArea/Views/Home/Test.cshtml";
        var project = new TestRazorProjectFileSystem(
            CreateProjectItem("/Areas/MyArea/File.cshtml"),
            CreateProjectItem("/File.cshtml"));

        // Act
        var result = project.FindHierarchicalItems(path, "File.cshtml");

        // Assert
        Assert.Collection(
            result,
            item =>
            {
                Assert.Equal("/File.cshtml", item.FilePath);
                Assert.True(item.Exists);
            },
            item =>
            {
                Assert.Equal("/Areas/File.cshtml", item.FilePath);
                Assert.False(item.Exists);
            },
            item =>
            {
                Assert.Equal("/Areas/MyArea/File.cshtml", item.FilePath);
                Assert.True(item.Exists);
            },
            item =>
            {
                Assert.Equal("/Areas/MyArea/Views/File.cshtml", item.FilePath);
                Assert.False(item.Exists);
            },
            item =>
            {
                Assert.Equal("/Areas/MyArea/Views/Home/File.cshtml", item.FilePath);
                Assert.False(item.Exists);
            });
    }

    [Theory]
    [InlineData("/Areas")]
    [InlineData("/Areas/")]
    public void FindHierarchicalItems_WithBasePath(string basePath)
    {
        // Arrange
        var path = "/Areas/MyArea/Views/Home/Test.cshtml";
        var project = new TestRazorProjectFileSystem(
            CreateProjectItem("/Areas/MyArea/File.cshtml"),
            CreateProjectItem("/File.cshtml"));

        // Act
        var result = project.FindHierarchicalItems(basePath, path, "File.cshtml");

        // Assert
        Assert.Collection(
            result,
            item =>
            {
                Assert.Equal("/Areas/File.cshtml", item.FilePath);
                Assert.False(item.Exists);
            },
            item =>
            {
                Assert.Equal("/Areas/MyArea/File.cshtml", item.FilePath);
                Assert.True(item.Exists);
            },
            item =>
            {
                Assert.Equal("/Areas/MyArea/Views/File.cshtml", item.FilePath);
                Assert.False(item.Exists);
            },
            item =>
            {
                Assert.Equal("/Areas/MyArea/Views/Home/File.cshtml", item.FilePath);
                Assert.False(item.Exists);
            });
    }

    [Theory]
    [InlineData("/Areas/MyArea/Views")]
    [InlineData("/Areas/MyArea/Views/")]
    public void FindHierarchicalItems_WithNestedBasePath(string basePath)
    {
        // Arrange
        var path = "/Areas/MyArea/Views/Home/Test.cshtml";
        var project = new TestRazorProjectFileSystem(
            CreateProjectItem("/Areas/MyArea/File.cshtml"),
            CreateProjectItem("/File.cshtml"));

        // Act
        var result = project.FindHierarchicalItems(basePath, path, "File.cshtml");

        // Assert
        Assert.Collection(
            result,
            item =>
            {
                Assert.Equal("/Areas/MyArea/Views/File.cshtml", item.FilePath);
                Assert.False(item.Exists);
            },
            item =>
            {
                Assert.Equal("/Areas/MyArea/Views/Home/File.cshtml", item.FilePath);
                Assert.False(item.Exists);
            });
    }

    [Theory]
    [InlineData("/Areas/MyArea/Views/Home")]
    [InlineData("/Areas/MyArea/Views/Home/")]
    public void FindHierarchicalItems_WithFileAtBasePath(string basePath)
    {
        // Arrange
        var path = "/Areas/MyArea/Views/Home/Test.cshtml";
        var project = new TestRazorProjectFileSystem(
            CreateProjectItem("/Areas/MyArea/File.cshtml"),
            CreateProjectItem("/File.cshtml"));

        // Act
        var result = project.FindHierarchicalItems(basePath, path, "File.cshtml");

        // Assert
        Assert.Collection(
            result,
            item =>
            {
                Assert.Equal("/Areas/MyArea/Views/Home/File.cshtml", item.FilePath);
                Assert.False(item.Exists);
            });
    }

    [Fact]
    public void FindHierarchicalItems_ReturnsEmptySequenceIfPathIsNotASubPathOfBasePath()
    {
        // Arrange
        var basePath = "/Pages";
        var path = "/Areas/MyArea/Views/Home/Test.cshtml";
        var project = new TestRazorProjectFileSystem(
            CreateProjectItem("/Areas/MyArea/File.cshtml"),
            CreateProjectItem("/File.cshtml"));

        // Act
        var result = project.FindHierarchicalItems(basePath, path, "File.cshtml");

        // Assert
        Assert.Empty(result);
    }

    private static RazorProjectItem CreateProjectItem(string path)
    {
        var projectItem = new Mock<RazorProjectItem>();
        projectItem.SetupGet(f => f.FilePath).Returns(path);
        projectItem.SetupGet(f => f.Exists).Returns(true);
        return projectItem.Object;
    }

    public sealed class TestRazorProjectFileSystem(params IEnumerable<RazorProjectItem> items) : RazorProjectFileSystem
    {
        private readonly Dictionary<string, RazorProjectItem> _lookup = items.ToDictionary(item => item.FilePath);

        public TestRazorProjectFileSystem()
            : this([])
        {
        }

        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            throw new NotImplementedException();
        }

        public override RazorProjectItem GetItem(string path, RazorFileKind? fileKind)
        {
            if (!_lookup.TryGetValue(path, out var value))
            {
                value = new NotFoundProjectItem(path, fileKind);
            }

            return value;
        }

        public new string NormalizeAndEnsureValidPath(string path)
            => base.NormalizeAndEnsureValidPath(path);
    }
}
