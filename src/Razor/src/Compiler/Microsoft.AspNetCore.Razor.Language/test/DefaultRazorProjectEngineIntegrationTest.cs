// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultRazorProjectEngineIntegrationTest
{
    [Fact]
    public void Process_SetsOptions_Runtime()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var parserOptions = codeDocument.ParserOptions;
        Assert.False(parserOptions.DesignTime);

        var codeGenerationOptions = codeDocument.CodeGenerationOptions;
        Assert.NotNull(codeGenerationOptions);
        Assert.False(codeGenerationOptions.DesignTime);
        Assert.False(codeGenerationOptions.SuppressChecksum);
        Assert.False(codeGenerationOptions.SuppressMetadataAttributes);
    }

    [Fact]
    public void ProcessDesignTime_SetsOptions_DesignTime()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.ProcessDesignTime(projectItem);

        // Assert
        var parserOptions = codeDocument.ParserOptions;
        Assert.True(parserOptions.DesignTime);

        var codeGenerationOptions = codeDocument.CodeGenerationOptions;
        Assert.NotNull(codeGenerationOptions);
        Assert.True(codeGenerationOptions.DesignTime);
        Assert.True(codeGenerationOptions.SuppressChecksum);
        Assert.True(codeGenerationOptions.SuppressMetadataAttributes);
    }

    [Fact]
    public void Process_GetsImportsFromFeature()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var testImport = new TestRazorProjectItem("testvalue");
        var importFeature = new TestImportProjectFeature(testImport);

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty, builder =>
        {
            builder.SetImportFeature(importFeature);
        });

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var import = Assert.Single(codeDocument.Imports);
        Assert.Equal("testvalue", import.FilePath);
    }

    [Fact]
    public void Process_GetsImportsFromFeature_MultipleFeatures()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var testImport1 = new TestRazorProjectItem("testvalue1");
        var importFeature1 = new TestImportProjectFeature(testImport1);

        var testImport2 = new TestRazorProjectItem("testvalue2");
        var importFeature2 = new TestImportProjectFeature(testImport2);

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty, builder =>
        {
            builder.Features.Add(importFeature1);
            builder.Features.Add(importFeature2);
        });

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        Assert.Collection(codeDocument.Imports,
            i => Assert.Equal("testvalue1", i.FilePath),
            i => Assert.Equal("testvalue2", i.FilePath));
    }

    [Fact]
    public void Process_GeneratesCodeDocumentWithValidCSharpDocument()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");
        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var csharpDocument = codeDocument.GetRequiredCSharpDocument();
        Assert.NotNull(csharpDocument);
        Assert.Empty(csharpDocument.Diagnostics);
    }

    [Fact]
    public void Process_WithImportsAndTagHelpers_SetsOnCodeDocument()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");
        var importItem = new TestRazorProjectItem("_import.cshtml");
        var expectedImports = ImmutableArray.Create(RazorSourceDocument.ReadFrom(importItem));
        TagHelperCollection expectedTagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly").Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("Test2TagHelper", "TestAssembly").Build(),
        ];

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), RazorFileKind.Legacy, expectedImports, expectedTagHelpers);

        // Assert
        var tagHelpers = codeDocument.GetTagHelpers();
        Assert.Same(expectedTagHelpers, tagHelpers);
        Assert.Equal(expectedImports, codeDocument.Imports);
    }

    [Fact]
    public void Process_WithFileKind_SetsOnCodeDocument()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), RazorFileKind.Legacy, importSources: default, tagHelpers: null);

        // Assert
        var actual = codeDocument.FileKind;
        Assert.Equal(RazorFileKind.Legacy, actual);
    }

    [Fact]
    public void Process_WithNullTagHelpers_SetsOnCodeDocument()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), RazorFileKind.Legacy, importSources: default, tagHelpers: null);

        // Assert
        var tagHelpers = codeDocument.GetTagHelpers();
        Assert.Null(tagHelpers);
    }

    [Fact]
    public void Process_SetsNullTagHelpersOnCodeDocument()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var tagHelpers = codeDocument.GetTagHelpers();
        Assert.Null(tagHelpers);
    }

    [Fact]
    public void Process_SetsInferredFileKindOnCodeDocument_MvcFile()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var actual = codeDocument.FileKind;
        Assert.Equal(RazorFileKind.Legacy, actual);
    }

    [Fact]
    public void Process_SetsInferredFileKindOnCodeDocument_Component()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.razor");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var actual = codeDocument.FileKind;
        Assert.Equal(RazorFileKind.Component, actual);
    }

    [Fact]
    public void Process_WithNullImports_SetsEmptyListOnCodeDocument()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), RazorFileKind.Legacy, importSources: default, tagHelpers: null);

        // Assert
        Assert.Empty(codeDocument.Imports);
    }

    [Fact]
    public void ProcessDesignTime_WithImportsAndTagHelpers_SetsOnCodeDocument()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");
        var importItem = new TestRazorProjectItem("_import.cshtml");
        var expectedImports = ImmutableArray.Create(RazorSourceDocument.ReadFrom(importItem));
        TagHelperCollection expectedTagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly").Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("Test2TagHelper", "TestAssembly").Build(),
        ];

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.ProcessDesignTime(RazorSourceDocument.ReadFrom(projectItem), RazorFileKind.Legacy, expectedImports, expectedTagHelpers);

        // Assert
        var tagHelpers = codeDocument.GetTagHelpers();
        Assert.Same(expectedTagHelpers, tagHelpers);
        Assert.Equal(expectedImports, codeDocument.Imports);
    }

    [Fact]
    public void ProcessDesignTime_WithNullTagHelpers_SetsOnCodeDocument()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.ProcessDesignTime(RazorSourceDocument.ReadFrom(projectItem), RazorFileKind.Legacy, default, tagHelpers: null);

        // Assert
        var tagHelpers = codeDocument.GetTagHelpers();
        Assert.Null(tagHelpers);
    }

    [Fact]
    public void ProcessDesignTime_SetsInferredFileKindOnCodeDocument_MvcFile()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.ProcessDesignTime(projectItem);

        // Assert
        var actual = codeDocument.FileKind;
        Assert.Equal(RazorFileKind.Legacy, actual);
    }

    [Fact]
    public void ProcessDesignTime_SetsInferredFileKindOnCodeDocument_Component()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.razor");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.ProcessDesignTime(projectItem);

        // Assert
        var actual = codeDocument.FileKind;
        Assert.Equal(RazorFileKind.Component, actual);
    }

    [Fact]
    public void ProcessDesignTime_SetsNullTagHelpersOnCodeDocument()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.ProcessDesignTime(projectItem);

        // Assert
        var tagHelpers = codeDocument.GetTagHelpers();
        Assert.Null(tagHelpers);
    }

    [Fact]
    public void ProcessDesignTime_WithNullImports_SetsEmptyListOnCodeDocument()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("Index.cshtml");

        var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

        // Act
        var codeDocument = projectEngine.ProcessDesignTime(RazorSourceDocument.ReadFrom(projectItem), RazorFileKind.Legacy, importSources: default, tagHelpers: null);

        // Assert
        Assert.Empty(codeDocument.Imports);
    }
}
