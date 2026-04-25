// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorCodeDocumentExtensionsTest
{
    [Fact]
    public void GetAndSetImportSyntaxTrees_ReturnsSyntaxTrees()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        var importSyntaxTree = RazorSyntaxTree.Parse(codeDocument.Source);
        codeDocument = codeDocument.WithImportSyntaxTrees([importSyntaxTree]);

        // Act
        var actual = codeDocument.GetImportSyntaxTrees();

        // Assert
        Assert.False(actual.IsEmpty);
        Assert.Equal<RazorSyntaxTree>([importSyntaxTree], actual);
    }

    [Fact]
    public void GetAndSetTagHelpers_ReturnsTagHelpers()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        TagHelperCollection expected =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly").Build()
        ];

        codeDocument = codeDocument.WithTagHelpers(expected);

        // Act
        var actual = codeDocument.GetTagHelpers();

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetAndSetTagHelperContext_ReturnsTagHelperContext()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        var expected = TagHelperDocumentContext.GetOrCreate(tagHelpers: []);
        codeDocument = codeDocument.WithTagHelperContext(expected);

        // Act
        var actual = codeDocument.GetTagHelperContext();

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetAndSetDirectiveTagHelperContributions_ReturnsContributions()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.Create("@using A");
        var usingDirective = GetUsingDirectives(codeDocument).Single();
        var contribution = new DirectiveTagHelperContribution(usingDirective.SpanStart, TagHelperCollection.Empty);

        // Act
        codeDocument = codeDocument.WithDirectiveTagHelperContributions([contribution]);
        var actual = codeDocument.GetDirectiveTagHelperContributions();

        // Assert
        var stored = Assert.Single(actual);
        Assert.Equal(usingDirective.SpanStart, stored.DirectiveSpanStart);
    }

    [Fact]
    public void IsDirectiveUsed_NoReferencedTagHelpers_ReturnsFalse()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.Create("@using A\r\n@using B");
        var directives = GetUsingDirectives(codeDocument);
        codeDocument = codeDocument.WithDirectiveTagHelperContributions(
        [
            new(directives[0].SpanStart, TagHelperCollection.Empty),
            new(directives[1].SpanStart, TagHelperCollection.Empty),
        ]);

        // Act
        var isFirstUsed = codeDocument.IsDirectiveUsed(directives[0]);
        var isSecondUsed = codeDocument.IsDirectiveUsed(directives[1]);

        // Assert
        Assert.False(isFirstUsed);
        Assert.False(isSecondUsed);
    }

    [Fact]
    public void IsDirectiveUsed_MixOfUsedAndUnused_ReturnsExpectedValues()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.Create("@using A\r\n@using B");
        var directives = GetUsingDirectives(codeDocument);
        var usedTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("T", "A").Build();

        codeDocument = codeDocument
            .WithDirectiveTagHelperContributions(
            [
                new(directives[0].SpanStart, TagHelperCollection.Create([usedTagHelper])),
                new(directives[1].SpanStart, TagHelperCollection.Empty),
            ])
            .WithReferencedTagHelpers(TagHelperCollection.Create([usedTagHelper]));

        // Act
        var isFirstUsed = codeDocument.IsDirectiveUsed(directives[0]);
        var isSecondUsed = codeDocument.IsDirectiveUsed(directives[1]);

        // Assert
        Assert.True(isFirstUsed);
        Assert.False(isSecondUsed);
    }

    [Theory]
    [InlineData("_Imports.razor")]
    [InlineData("_ViewImports.cshtml")]
    public void IsDirectiveUsed_ImportDocument_ReturnsTrue(string filePath)
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("@using A", filePath: filePath, relativePath: filePath);
        var codeDocument = RazorCodeDocument.Create(
            source,
            parserOptions: RazorParserOptions.Create(RazorLanguageVersion.Latest, FileKinds.GetFileKindFromPath(filePath)));

        var directive = GetUsingDirectives(codeDocument).Single();
        codeDocument = codeDocument.WithDirectiveTagHelperContributions([new(directive.SpanStart, TagHelperCollection.Empty)]);

        // Act
        var isDirectiveUsed = codeDocument.IsDirectiveUsed(directive);

        // Assert
        Assert.True(isDirectiveUsed);
    }

    [Fact]
    public void TryGetNamespace_RootNamespaceNotSet_ReturnsNull()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Test.cshtml", relativePath: "Test.cshtml");
        var codeDocument = RazorCodeDocument.Create(
            source,
            codeGenerationOptions: RazorCodeGenerationOptions.Default);

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Null(@namespace);
    }

    [Fact]
    public void TryGetNamespace_RelativePathNull_ReturnsNull()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Test.cshtml", relativePath: null);
        var codeDocument = RazorCodeDocument.Create(
            source,
            codeGenerationOptions: RazorCodeGenerationOptions.Default.WithRootNamespace("Hello"));

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Null(@namespace);
    }

    [Fact]
    public void TryGetNamespace_FilePathNull_ReturnsNull()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(filePath: null, relativePath: "Test.cshtml");
        var codeDocument = RazorCodeDocument.Create(
            source,
            codeGenerationOptions: RazorCodeGenerationOptions.Default.WithRootNamespace("Hello"));

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Null(@namespace);
    }

    [Fact]
    public void TryGetNamespace_RelativePathLongerThanFilePath_ReturnsNull()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            filePath: "C:\\Hello\\Test.cshtml",
            relativePath: "Some\\invalid\\relative\\path\\Test.cshtml");

        var codeDocument = RazorCodeDocument.Create(
            source,
            codeGenerationOptions: RazorCodeGenerationOptions.Default.WithRootNamespace("Hello"));

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Null(@namespace);
    }

    [Fact]
    public void TryGetNamespace_ComputesNamespace()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            filePath: "C:\\Hello\\Components\\Test.cshtml",
            relativePath: "\\Components\\Test.cshtml");

        var codeDocument = RazorCodeDocument.Create(
            source,
            codeGenerationOptions: RazorCodeGenerationOptions.Default.WithRootNamespace("Hello"));

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Equal("Hello.Components", @namespace);
    }

    [Fact]
    public void TryGetNamespace_NoRootNamespaceFallback_ReturnsNull()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            filePath: "C:\\Hello\\Components\\Test.cshtml",
            relativePath: "\\Components\\Test.cshtml");

        var codeDocument = RazorCodeDocument.Create(
            source,
            codeGenerationOptions: RazorCodeGenerationOptions.Default.WithRootNamespace("Hello"));

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: false, out var @namespace);

        // Assert
        Assert.Null(@namespace);
    }

    [Fact]
    public void TryGetNamespace_SanitizesNamespaceName()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            filePath: "C:\\Hello\\Components with space\\Test$name.cshtml",
            relativePath: "\\Components with space\\Test$name.cshtml");

        var codeDocument = RazorCodeDocument.Create(
            source,
            codeGenerationOptions: RazorCodeGenerationOptions.Default.WithRootNamespace("Hel?o.World"));

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Equal("Hel_o.World.Components_with_space", @namespace);
    }

    [Fact]
    public void TryGetNamespace_RespectsNamespaceDirective()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            content: "@namespace My.Custom.NS",
            filePath: "C:\\Hello\\Components\\Test.cshtml",
            relativePath: "\\Components\\Test.cshtml");

        var codeDocument = RazorCodeDocument.Create(
            source,
            parserOptions: RazorParserOptions.Create(RazorLanguageVersion.Latest, RazorFileKind.Component, builder =>
            {
                builder.Directives = [NamespaceDirective.Directive];
            }),
            codeGenerationOptions: RazorCodeGenerationOptions.Default.WithRootNamespace("Hello.World"));

        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(source, codeDocument.ParserOptions));

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Equal("My.Custom.NS", @namespace);
    }

    [Fact]
    public void TryGetNamespace_RespectsImportsNamespaceDirective()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            filePath: "C:\\Hello\\Components\\Test.cshtml",
            relativePath: "\\Components\\Test.cshtml");

        var codeDocument = RazorCodeDocument.Create(
            source,
            parserOptions: RazorParserOptions.Create(RazorLanguageVersion.Latest, RazorFileKind.Component, builder =>
            {
                builder.Directives = [NamespaceDirective.Directive];
            }),
            codeGenerationOptions: RazorCodeGenerationOptions.Default.WithRootNamespace("Hello.World"));

        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(source, codeDocument.ParserOptions));

        var importSource = TestRazorSourceDocument.Create(
            content: "@namespace My.Custom.NS",
            filePath: "C:\\Hello\\_Imports.razor",
            relativePath: "\\_Imports.razor");

        var importSyntaxTree = RazorSyntaxTree.Parse(importSource, codeDocument.ParserOptions);
        codeDocument = codeDocument.WithImportSyntaxTrees([importSyntaxTree]);

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Equal("My.Custom.NS.Components", @namespace);
    }

    [Fact]
    public void TryGetNamespace_IgnoresImportsNamespaceDirectiveWhenAsked()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            filePath: "C:\\Hello\\Components\\Test.cshtml",
            relativePath: "\\Components\\Test.cshtml");
        var codeDocument = RazorCodeDocument.Create(
            source,
            parserOptions: RazorParserOptions.Create(RazorLanguageVersion.Latest, RazorFileKind.Component, builder =>
            {
                builder.Directives = [NamespaceDirective.Directive];
            }),
            codeGenerationOptions: RazorCodeGenerationOptions.Default.WithRootNamespace("Hello.World"));

        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(source, codeDocument.ParserOptions));

        var importSource = TestRazorSourceDocument.Create(
            content: "@namespace My.Custom.NS",
            filePath: "C:\\Hello\\_Imports.razor",
            relativePath: "\\_Imports.razor");

        var importSyntaxTree = RazorSyntaxTree.Parse(importSource, codeDocument.ParserOptions);
        codeDocument = codeDocument.WithImportSyntaxTrees([importSyntaxTree]);

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, considerImports: false, out var @namespace, out _);

        // Assert
        Assert.Equal("Hello.World.Components", @namespace);
    }

    [Fact]
    public void TryGetNamespace_RespectsImportsNamespaceDirective_SameFolder()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            filePath: "C:\\Hello\\Components\\Test.cshtml",
            relativePath: "\\Components\\Test.cshtml");

        var codeDocument = RazorCodeDocument.Create(
            source,
            parserOptions: RazorParserOptions.Create(RazorLanguageVersion.Latest, RazorFileKind.Component, builder =>
            {
                builder.Directives = [NamespaceDirective.Directive];
            }),
            codeGenerationOptions: RazorCodeGenerationOptions.Default.WithRootNamespace("Hello.World"));

        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(source, codeDocument.ParserOptions));

        var importSource = TestRazorSourceDocument.Create(
            content: "@namespace My.Custom.NS",
            filePath: "C:\\Hello\\Components\\_Imports.razor",
            relativePath: "\\Components\\_Imports.razor");

        var importSyntaxTree = RazorSyntaxTree.Parse(importSource, codeDocument.ParserOptions);
        codeDocument = codeDocument.WithImportSyntaxTrees([importSyntaxTree]);

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Equal("My.Custom.NS", @namespace);
    }

    [Fact]
    public void TryGetNamespace_OverrideImportsNamespaceDirective()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            content: "@namespace My.Custom.OverrideNS",
            filePath: "C:\\Hello\\Components\\Test.cshtml",
            relativePath: "\\Components\\Test.cshtml");

        var codeDocument = RazorCodeDocument.Create(
            source,
            parserOptions: RazorParserOptions.Create(RazorLanguageVersion.Latest, RazorFileKind.Component, builder =>
            {
                builder.Directives = [NamespaceDirective.Directive];
            }));

        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(source, codeDocument.ParserOptions));

        var importSource = TestRazorSourceDocument.Create(
            content: "@namespace My.Custom.NS",
            filePath: "C:\\Hello\\_Imports.razor",
            relativePath: "\\_Imports.razor");

        var importSyntaxTree = RazorSyntaxTree.Parse(importSource, codeDocument.ParserOptions);
        codeDocument = codeDocument.WithImportSyntaxTrees([importSyntaxTree]);

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Equal("My.Custom.OverrideNS", @namespace);
    }

    [Fact]
    public void TryGetNamespace_PicksNearestImportsNamespaceDirective()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            filePath: "C:\\RazorPagesWebPage\\Pages\\Namespace\\Nested\\Folder\\Index.cshtml",
            relativePath: "\\Pages\\Namespace\\Nested\\Folder\\Index.cshtml");

        var codeDocument = RazorCodeDocument.Create(
            source,
            parserOptions: RazorParserOptions.Create(RazorLanguageVersion.Latest, RazorFileKind.Legacy, builder =>
            {
                builder.Directives = [NamespaceDirective.Directive];
            }));

        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(source, codeDocument.ParserOptions));

        var importSource1 = TestRazorSourceDocument.Create(
            content: "@namespace RazorPagesWebSite.Pages",
            filePath: "C:\\RazorPagesWebPage\\Pages\\_ViewImports.cshtml",
            relativePath: "\\Pages\\_ViewImports.cshtml");

        var importSyntaxTree1 = RazorSyntaxTree.Parse(importSource1, codeDocument.ParserOptions);

        var importSource2 = TestRazorSourceDocument.Create(
            content: "@namespace CustomNamespace",
            filePath: "C:\\RazorPagesWebPage\\Pages\\Namespace\\_ViewImports.cshtml",
            relativePath: "\\Pages\\Namespace\\_ViewImports.cshtml");

        var importSyntaxTree2 = RazorSyntaxTree.Parse(importSource2, codeDocument.ParserOptions);

        codeDocument = codeDocument.WithImportSyntaxTrees([importSyntaxTree1, importSyntaxTree2]);

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Equal("CustomNamespace.Nested.Folder", @namespace);
    }

    [Theory]
    [InlineData("/", "foo.cshtml", "Base")]
    [InlineData("/", "foo/bar.cshtml", "Base.foo")]
    [InlineData("/", "foo/bar/baz.cshtml", "Base.foo.bar")]
    [InlineData("/foo/", "bar/baz.cshtml", "Base.bar")]
    [InlineData("/Foo/", "bar/baz.cshtml", "Base.bar")]
    [InlineData("c:\\", "foo.cshtml", "Base")]
    [InlineData("c:\\", "foo\\bar.cshtml", "Base.foo")]
    [InlineData("c:\\", "foo\\bar\\baz.cshtml", "Base.foo.bar")]
    [InlineData("c:\\foo\\", "bar\\baz.cshtml", "Base.bar")]
    [InlineData("c:\\Foo\\", "bar\\baz.cshtml", "Base.bar")]
    public void TryGetNamespace_ComputesNamespaceWithSuffix(string basePath, string relativePath, string expectedNamespace)
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            filePath: Path.Combine(basePath, relativePath),
            relativePath: relativePath);

        var codeDocument = RazorCodeDocument.Create(
            source,
            parserOptions: RazorParserOptions.Default.WithDirectives(NamespaceDirective.Directive));

        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(source, codeDocument.ParserOptions));

        var importRelativePath = "_ViewImports.cshtml";
        var importSource = TestRazorSourceDocument.Create(
            content: "@namespace Base",
            filePath: Path.Combine(basePath, importRelativePath),
            relativePath: importRelativePath);

        var importSyntaxTree = RazorSyntaxTree.Parse(importSource, codeDocument.ParserOptions);
        codeDocument = codeDocument.WithImportSyntaxTrees([importSyntaxTree]);

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Equal(expectedNamespace, @namespace);
    }

    [Fact]
    public void TryGetNamespace_ForNonRelatedFiles_UsesNamespaceVerbatim()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(
            filePath: "c:\\foo\\bar\\bleh.cshtml",
            relativePath: "bar\\bleh.cshtml");

        var codeDocument = RazorCodeDocument.Create(
            source,
            parserOptions: RazorParserOptions.Default.WithDirectives(NamespaceDirective.Directive));

        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(source, codeDocument.ParserOptions));

        var importSource = TestRazorSourceDocument.Create(
            content: "@namespace Base",
            filePath: "c:\\foo\\baz\\bleh.cshtml",
            relativePath: "baz\\bleh.cshtml");

        var importSyntaxTree = RazorSyntaxTree.Parse(importSource, codeDocument.ParserOptions);
        codeDocument = codeDocument.WithImportSyntaxTrees([importSyntaxTree]);

        // Act
        codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace);

        // Assert
        Assert.Equal("Base", @namespace);
    }

    private static RazorUsingDirectiveSyntax[] GetUsingDirectives(RazorCodeDocument codeDocument)
    {
        var syntaxTree = RazorSyntaxTree.Parse(codeDocument.Source);
        return [.. syntaxTree.Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>()];
    }
}
