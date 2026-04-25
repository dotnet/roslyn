// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    public class SourceGeneratorProjectItemTest
    {
        [Fact]
        public void PhysicalPath_ReturnsSourceTextPath()
        {
            // Arrange
            var emptyBasePath = "/";
            var path = "/foo/bar.cshtml";
            var projectItem = new SourceGeneratorProjectItem(
                filePath: path,
                basePath: emptyBasePath,
                relativePhysicalPath: "/foo",
                fileKind: RazorFileKind.Legacy,
                additionalText: new TestAdditionalText(string.Empty),
                cssScope: null);

            // Act
            var physicalPath = projectItem.PhysicalPath;

            // Assert
            Assert.Equal("dummy", physicalPath);
        }

        [Theory]
        [InlineData("/Home/Index")]
        [InlineData("EditUser")]
        public void Extension_ReturnsNullIfFileDoesNotHaveExtension(string path)
        {
            // Arrange
            var projectItem = new SourceGeneratorProjectItem(
                filePath: path,
                basePath: "/views",
                relativePhysicalPath: "/foo",
                fileKind: RazorFileKind.Legacy,
                additionalText: new TestAdditionalText(string.Empty),
                cssScope: null);

            // Act
            var extension = projectItem.Extension;

            // Assert
            Assert.Null(extension);
        }

        [Theory]
        [InlineData("/Home/Index.cshtml", ".cshtml")]
        [InlineData("/Home/Index.en-gb.cshtml", ".cshtml")]
        [InlineData("EditUser.razor", ".razor")]
        public void Extension_ReturnsFileExtension(string path, string expected)
        {
            // Arrange
            var projectItem = new SourceGeneratorProjectItem(
                filePath: path,
                basePath: "/views",
                relativePhysicalPath: "/foo",
                fileKind: RazorFileKind.Legacy,
                additionalText: new TestAdditionalText(string.Empty),
                cssScope: null);

            // Act
            var extension = projectItem.Extension;

            // Assert
            Assert.Equal(expected, extension);
        }

        [Theory]
        [InlineData("Home/Index.cshtml", "Index.cshtml")]
        [InlineData("/Accounts/Customers/Manage-en-us.razor", "Manage-en-us.razor")]
        public void FileName_ReturnsFileNameWithExtension(string path, string expected)
        {
            // Arrange
            var projectItem = new SourceGeneratorProjectItem(
                filePath: path,
                basePath: "/",
                relativePhysicalPath: "/foo",
                fileKind: RazorFileKind.Legacy,
                additionalText: new TestAdditionalText(string.Empty),
                cssScope: null);

            // Act
            var fileName = projectItem.FileName;

            // Assert
            Assert.Equal(expected, fileName);
        }

        [Theory]
        [InlineData("Home/Index", "Home/Index")]
        [InlineData("Home/Index.cshtml", "Home/Index")]
        [InlineData("/Accounts/Customers/Manage.en-us.razor", "/Accounts/Customers/Manage.en-us")]
        [InlineData("/Accounts/Customers/Manage-en-us.razor", "/Accounts/Customers/Manage-en-us")]
        public void PathWithoutExtension_ExcludesExtension(string path, string expected)
        {
            // Arrange
            var projectItem = new SourceGeneratorProjectItem(
                filePath: path,
                basePath: "/",
                relativePhysicalPath: "/foo",
                fileKind: RazorFileKind.Legacy,
                additionalText: new TestAdditionalText(string.Empty),
                cssScope: null);

            // Act
            var fileName = projectItem.FilePathWithoutExtension;

            // Assert
            Assert.Equal(expected, fileName);
        }

        [Fact]
        public void ProjectItems_WithDifferentPaths_SameContent_AreNotEqual()
        {
            // Two additional texts with same contents, but different paths
            var content = "<h1>Hello World</h1>";
            var additionalText1 = new TestAdditionalText(content, Encoding.UTF8, "File1.cshtml");
            var additionalText2 = new TestAdditionalText(content, Encoding.UTF8, "File2.cshtml");
            
            var projectItem1 = new SourceGeneratorProjectItem(
                filePath: "/Views/Home/Index.cshtml",
                basePath: "/",
                relativePhysicalPath: "/Views/Home",
                fileKind: RazorFileKind.Legacy,
                additionalText: additionalText1,
                cssScope: null);

            var projectItem2 = new SourceGeneratorProjectItem(
                filePath: "/Views/About/Index.cshtml",
                basePath: "/",
                relativePhysicalPath: "/Views/About",
                fileKind: RazorFileKind.Legacy,
                additionalText: additionalText2,
                cssScope: null);

            // Act & Assert
            Assert.NotEqual(projectItem1, projectItem2);
            Assert.False(projectItem1.Equals(projectItem2));
        }
    }
}
