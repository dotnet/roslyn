// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultRazorTagHelperContextDiscoveryPhaseTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    #region Legacy
    [Fact]
    public void Execute_CanHandleSingleLengthAddTagHelperDirective()
    {
        // Arrange
        var expectedDiagnostics = new[]
        {
            RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                new SourceSpan(new SourceLocation(14 + Environment.NewLine.Length, 1, 14), contentLength: 1)),
            RazorDiagnosticFactory.CreateParsing_InvalidTagHelperLookupText(
                new SourceSpan(new SourceLocation(14 + Environment.NewLine.Length, 1, 14), contentLength: 1), "\"")
        };

        var content =
        @"
@addTagHelper """;
        var source = TestRazorSourceDocument.Create(content, filePath: null);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);

        // Act
        ProjectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);

        // Assert
        var erroredNode = codeDocument.GetSyntaxTree().Root.DescendantNodes().First(n => n.GetChunkGenerator() is AddTagHelperChunkGenerator);
        var chunkGenerator = Assert.IsType<AddTagHelperChunkGenerator>(erroredNode.GetChunkGenerator());
        Assert.Equal(expectedDiagnostics, chunkGenerator.Diagnostics);
    }

    [Fact]
    public void Execute_CanHandleSingleLengthRemoveTagHelperDirective()
    {
        // Arrange
        var expectedDiagnostics = new[]
        {
            RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                new SourceSpan(new SourceLocation(17 + Environment.NewLine.Length, 1, 17), contentLength: 1)),
            RazorDiagnosticFactory.CreateParsing_InvalidTagHelperLookupText(
                new SourceSpan(new SourceLocation(17 + Environment.NewLine.Length, 1, 17), contentLength: 1), "\"")
        };

        var content =
        @"
@removeTagHelper """;
        var source = TestRazorSourceDocument.Create(content, filePath: null);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);

        // Act
        ProjectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);

        // Assert
        var erroredNode = codeDocument.GetSyntaxTree().Root.DescendantNodes().First(n => n.GetChunkGenerator() is RemoveTagHelperChunkGenerator);
        var chunkGenerator = Assert.IsType<RemoveTagHelperChunkGenerator>(erroredNode.GetChunkGenerator());
        Assert.Equal(expectedDiagnostics, chunkGenerator.Diagnostics);
    }

    [Fact]
    public void Execute_CanHandleSingleLengthTagHelperPrefix()
    {
        // Arrange
        var expectedDiagnostics = new[]
        {
            RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                new SourceSpan(new SourceLocation(17 + Environment.NewLine.Length, 1, 17), contentLength: 1)),
            RazorDiagnosticFactory.CreateParsing_InvalidTagHelperPrefixValue(
                new SourceSpan(new SourceLocation(17 + Environment.NewLine.Length, 1, 17), contentLength: 1), "tagHelperPrefix", '\"', "\""),
        };

        var content =
        @"
@tagHelperPrefix """;
        var source = TestRazorSourceDocument.Create(content, filePath: null);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);

        // Act
        ProjectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);

        // Assert
        var erroredNode = codeDocument.GetSyntaxTree().Root.DescendantNodes().First(n => n.GetChunkGenerator() is TagHelperPrefixDirectiveChunkGenerator);
        var chunkGenerator = Assert.IsType<TagHelperPrefixDirectiveChunkGenerator>(erroredNode.GetChunkGenerator());
        Assert.Equal(expectedDiagnostics, chunkGenerator.Diagnostics);
    }

    [Fact]
    public void Execute_RewritesTagHelpers()
    {
        // Arrange
        var tagHelper1 = CreateTagHelperDescriptor(
            tagName: "form",
            typeName: "TestFormTagHelper",
            assemblyName: "TestAssembly");

        var tagHelper2 = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "TestInputTagHelper",
            assemblyName: "TestAssembly");

        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.SetTagHelpers(tagHelper1, tagHelper2);
        });

        var source = CreateTestSourceDocument();
        var codeDocument = projectEngine.CreateCodeDocument(source);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);

        // Act
        codeDocument = projectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);
        codeDocument = projectEngine.ExecutePhase<DefaultRazorIntermediateNodeLoweringPhase>(codeDocument);
        codeDocument = projectEngine.ExecutePhase<DefaultTagHelperResolutionPhase>(codeDocument);

        // Assert
        var documentNode = codeDocument.GetDocumentNode();
        Assert.Empty(codeDocument.GetSyntaxTree().Diagnostics);

        var tagHelperNodes = FindTagHelperNodes(documentNode);
        Assert.Equal("form", tagHelperNodes[0].TagName);
        Assert.Equal("input", tagHelperNodes[1].TagName);
    }

    [Fact]
    public void Execute_WithTagHelperDescriptorsFromCodeDocument_RewritesTagHelpers()
    {
        // Arrange
        var tagHelper1 = CreateTagHelperDescriptor(
            tagName: "form",
            typeName: "TestFormTagHelper",
            assemblyName: "TestAssembly");

        var tagHelper2 = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "TestInputTagHelper",
            assemblyName: "TestAssembly");

        var sourceDocument = CreateTestSourceDocument();
        var codeDocument = ProjectEngine.CreateCodeDocument(sourceDocument);
        var originalTree = RazorSyntaxTree.Parse(sourceDocument);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);
        codeDocument = codeDocument.WithTagHelpers([tagHelper1, tagHelper2]);

        // Act
        codeDocument = ProjectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);
        codeDocument = ProjectEngine.ExecutePhase<DefaultRazorIntermediateNodeLoweringPhase>(codeDocument);
        codeDocument = ProjectEngine.ExecutePhase<DefaultTagHelperResolutionPhase>(codeDocument);

        // Assert
        var documentNode = codeDocument.GetDocumentNode();
        Assert.Empty(codeDocument.GetSyntaxTree().Diagnostics);

        var tagHelperNodes = FindTagHelperNodes(documentNode);
        Assert.Equal("form", tagHelperNodes[0].TagName);
        Assert.Equal("input", tagHelperNodes[1].TagName);
    }

    [Fact]
    public void Execute_NullTagHelperDescriptorsFromCodeDocument_FallsBackToTagHelperFeature()
    {
        // Arrange
        var tagHelper1 = CreateTagHelperDescriptor(
            tagName: "form",
            typeName: "TestFormTagHelper",
            assemblyName: "TestAssembly");

        var tagHelper2 = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "TestInputTagHelper",
            assemblyName: "TestAssembly");

        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.SetTagHelpers(tagHelper1, tagHelper2);
        });

        var source = CreateTestSourceDocument();
        var codeDocument = projectEngine.CreateCodeDocument(source);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);
        codeDocument = codeDocument.WithTagHelpers(value: null);

        // Act
        codeDocument = projectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);
        codeDocument = projectEngine.ExecutePhase<DefaultRazorIntermediateNodeLoweringPhase>(codeDocument);
        codeDocument = projectEngine.ExecutePhase<DefaultTagHelperResolutionPhase>(codeDocument);

        // Assert
        var documentNode = codeDocument.GetDocumentNode();
        Assert.Empty(codeDocument.GetSyntaxTree().Diagnostics);

        var tagHelperNodes = FindTagHelperNodes(documentNode);
        Assert.Equal("form", tagHelperNodes[0].TagName);
        Assert.Equal("input", tagHelperNodes[1].TagName);
    }

    [Fact]
    public void Execute_EmptyTagHelperDescriptorsFromCodeDocument_DoesNotFallbackToTagHelperFeature()
    {
        // Arrange
        var tagHelper1 = CreateTagHelperDescriptor(
            tagName: "form",
            typeName: "TestFormTagHelper",
            assemblyName: "TestAssembly");

        var tagHelper2 = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "TestInputTagHelper",
            assemblyName: "TestAssembly");

        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.SetTagHelpers(tagHelper1, tagHelper2);
        });

        var source = CreateTestSourceDocument();
        var codeDocument = projectEngine.CreateCodeDocument(source);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);
        codeDocument = codeDocument.WithTagHelpers(value: []);

        // Act
        codeDocument = projectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);
        codeDocument = projectEngine.ExecutePhase<DefaultRazorIntermediateNodeLoweringPhase>(codeDocument);
        codeDocument = projectEngine.ExecutePhase<DefaultTagHelperResolutionPhase>(codeDocument);

        // Assert
        var documentNode = codeDocument.GetDocumentNode();
        Assert.Empty(codeDocument.GetSyntaxTree().Diagnostics);

        var tagHelperNodes = FindTagHelperNodes(documentNode);
        Assert.Empty(tagHelperNodes);
    }

    [Fact]
    public void Execute_DirectiveWithoutQuotes_RewritesTagHelpers_TagHelperMatchesElementTwice()
    {
        // Arrange
        var tagHelper = CreateTagHelperDescriptor(
            tagName: "form",
            typeName: "TestFormTagHelper",
            assemblyName: "TestAssembly",
            ruleBuilders:
            [
                ruleBuilder => ruleBuilder
                    .RequireAttributeDescriptor(attribute => attribute
                        .Name("a", RequiredAttributeNameComparison.FullMatch)),
                ruleBuilder => ruleBuilder
                    .RequireAttributeDescriptor(attribute => attribute
                        .Name("b", RequiredAttributeNameComparison.FullMatch)),
            ]);

        var content = @"
@addTagHelper *, TestAssembly
<form a=""hi"" b=""there"">
</form>";

        var source = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(source, [tagHelper]);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);

        // Act
        codeDocument = ProjectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);
        codeDocument = ProjectEngine.ExecutePhase<DefaultRazorIntermediateNodeLoweringPhase>(codeDocument);
        codeDocument = ProjectEngine.ExecutePhase<DefaultTagHelperResolutionPhase>(codeDocument);

        // Assert
        var documentNode = codeDocument.GetDocumentNode();
        Assert.Empty(codeDocument.GetSyntaxTree().Diagnostics);

        var tagHelperNodes = FindTagHelperNodes(documentNode);
        var formTagHelper = Assert.Single(tagHelperNodes);
        Assert.Equal("form", formTagHelper.TagName);
        Assert.Contains(formTagHelper.TagHelpers, th => th.Name == tagHelper.Name && th.AssemblyName == tagHelper.AssemblyName);
    }

    [Fact]
    public void Execute_DirectiveWithQuotes_RewritesTagHelpers_TagHelperMatchesElementTwice()
    {
        // Arrange
        var tagHelper = CreateTagHelperDescriptor(
            tagName: "form",
            typeName: "TestFormTagHelper",
            assemblyName: "TestAssembly",
            ruleBuilders:
            [
                ruleBuilder => ruleBuilder
                    .RequireAttributeDescriptor(attribute => attribute
                        .Name("a", RequiredAttributeNameComparison.FullMatch)),
                ruleBuilder => ruleBuilder
                    .RequireAttributeDescriptor(attribute => attribute
                        .Name("b", RequiredAttributeNameComparison.FullMatch)),
            ]);

        var content = @"
@addTagHelper ""*, TestAssembly""
<form a=""hi"" b=""there"">
</form>";

        var source = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(source, [tagHelper]);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);

        // Act
        codeDocument = ProjectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);
        codeDocument = ProjectEngine.ExecutePhase<DefaultRazorIntermediateNodeLoweringPhase>(codeDocument);
        codeDocument = ProjectEngine.ExecutePhase<DefaultTagHelperResolutionPhase>(codeDocument);

        // Assert
        var documentNode = codeDocument.GetDocumentNode();
        Assert.Empty(codeDocument.GetSyntaxTree().Diagnostics);

        var tagHelperNodes = FindTagHelperNodes(documentNode);
        var formTagHelper = Assert.Single(tagHelperNodes);
        Assert.Equal("form", formTagHelper.TagName);
        Assert.Contains(formTagHelper.TagHelpers, th => th.Name == tagHelper.Name && th.AssemblyName == tagHelper.AssemblyName);
    }

    [Fact]
    public void Execute_TagHelpersFromCodeDocumentAndFeature_PrefersCodeDocument()
    {
        // Arrange
        var featureTagHelper = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "TestInputTagHelper",
            assemblyName: "TestAssembly");

        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.SetTagHelpers(featureTagHelper);
        });

        var source = CreateTestSourceDocument();
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);

        var codeDocumentTagHelper = CreateTagHelperDescriptor(
            tagName: "form",
            typeName: "TestFormTagHelper",
            assemblyName: "TestAssembly");

        codeDocument = codeDocument.WithTagHelpers([codeDocumentTagHelper]);

        // Act
        codeDocument = ProjectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);
        codeDocument = ProjectEngine.ExecutePhase<DefaultRazorIntermediateNodeLoweringPhase>(codeDocument);
        codeDocument = ProjectEngine.ExecutePhase<DefaultTagHelperResolutionPhase>(codeDocument);

        // Assert
        var documentNode = codeDocument.GetDocumentNode();
        Assert.Empty(codeDocument.GetSyntaxTree().Diagnostics);

        var tagHelperNodes = FindTagHelperNodes(documentNode);
        var formTagHelper = Assert.Single(tagHelperNodes);
        Assert.Equal("form", formTagHelper.TagName);
    }

    [Fact]
    public void Execute_NoopsWhenNoTagHelpersFromCodeDocumentOrFeature()
    {
        // Arrange
        var source = CreateTestSourceDocument();
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);

        // Act
        codeDocument = ProjectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);

        // Assert
        var outputTree = codeDocument.GetSyntaxTree();
        Assert.Empty(outputTree.Diagnostics);
        Assert.Same(originalTree, outputTree);
    }

    [Fact]
    public void Execute_NoopsWhenNoTagHelperDescriptorsAreResolved()
    {
        // Arrange

        // No taghelper directives here so nothing is resolved.
        var source = TestRazorSourceDocument.Create("Hello, world");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);

        // Act
        codeDocument = ProjectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);

        // Assert
        var outputTree = codeDocument.GetSyntaxTree();
        Assert.Empty(outputTree.Diagnostics);
        Assert.Same(originalTree, outputTree);
    }

    [Fact]
    public void Execute_SetsTagHelperDocumentContext()
    {
        // Arrange
        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.Features.Add(new TestTagHelperFeature());
        });

        // No taghelper directives here so nothing is resolved.
        var source = TestRazorSourceDocument.Create("Hello, world");
        var codeDocument = projectEngine.CreateCodeDocument(source);
        var originalTree = RazorSyntaxTree.Parse(source);
        codeDocument = codeDocument.WithSyntaxTree(originalTree);

        // Act
        codeDocument = projectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);

        // Assert
        var context = codeDocument.GetTagHelperContext();
        Assert.NotNull(context);
        Assert.Null(context.Prefix);
        Assert.Empty(context.TagHelpers);
    }

    [Fact]
    public void Execute_CombinesErrorsOnRewritingErrors()
    {
        // Arrange
        var tagHelper1 = CreateTagHelperDescriptor(
            tagName: "form",
            typeName: "TestFormTagHelper",
            assemblyName: "TestAssembly");

        var tagHelper2 = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "TestInputTagHelper",
            assemblyName: "TestAssembly");

        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.SetTagHelpers(tagHelper1, tagHelper2);
        });

        var content =
        @"
@addTagHelper *, TestAssembly
<form>
    <input value='Hello' type='text' />";
        var source = TestRazorSourceDocument.Create(content, filePath: null);
        var codeDocument = projectEngine.CreateCodeDocument(source);

        var originalTree = RazorSyntaxTree.Parse(source);

        var initialError = RazorDiagnostic.Create(
            new RazorDiagnosticDescriptor("RZ9999", "Initial test error", RazorDiagnosticSeverity.Error),
            new SourceSpan(SourceLocation.Zero, contentLength: 1));
        var expectedRewritingError = RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
            new SourceSpan(new SourceLocation((Environment.NewLine.Length * 2) + 30, 2, 1), contentLength: 4), "form");

        var erroredOriginalTree = new RazorSyntaxTree(originalTree.Root, originalTree.Source, [initialError], originalTree.Options);
        codeDocument = codeDocument.WithSyntaxTree(erroredOriginalTree);

        // Act
        codeDocument = projectEngine.ExecutePhase<DefaultRazorTagHelperContextDiscoveryPhase>(codeDocument);
        codeDocument = projectEngine.ExecutePhase<DefaultRazorIntermediateNodeLoweringPhase>(codeDocument);
        codeDocument = projectEngine.ExecutePhase<DefaultTagHelperResolutionPhase>(codeDocument);
        codeDocument = projectEngine.ExecutePhase<DefaultRazorTagHelperRewritePhase>(codeDocument);

        // Assert
        var outputTree = codeDocument.GetTagHelperRewrittenSyntaxTree();
        Assert.Empty(originalTree.Diagnostics);
        Assert.NotSame(erroredOriginalTree, outputTree);
        Assert.Equal<RazorDiagnostic>([initialError, expectedRewritingError], outputTree.Diagnostics);
    }

    private static string AssemblyA => "TestAssembly";

    private static string AssemblyB => "AnotherAssembly";

    private static TagHelperDescriptor Valid_PlainTagHelperDescriptor
    {
        get
        {
            return CreateTagHelperDescriptor(
                tagName: "valid_plain",
                typeName: "Microsoft.AspNetCore.Razor.TagHelpers.ValidPlainTagHelper",
                assemblyName: AssemblyA);
        }
    }

    private static TagHelperDescriptor Valid_InheritedTagHelperDescriptor
    {
        get
        {
            return CreateTagHelperDescriptor(
                tagName: "valid_inherited",
                typeName: "Microsoft.AspNetCore.Razor.TagHelpers.ValidInheritedTagHelper",
                assemblyName: AssemblyA);
        }
    }

    private static TagHelperDescriptor String_TagHelperDescriptor
    {
        get
        {
            // We're treating 'string' as a TagHelper so we can test TagHelpers in multiple assemblies without
            // building a separate assembly with a single TagHelper.
            return CreateTagHelperDescriptor(
                tagName: "string",
                typeName: "System.String",
                assemblyName: AssemblyB);
        }
    }

    public static TheoryData<string, string> ProcessTagHelperPrefixData
    {
        get
        {
            // source, expected prefix
            return new TheoryData<string, string>
            {
                {
                    $@"
@tagHelperPrefix """"
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidPlain*, TestAssembly",
                    null
                },
                {
                    $@"
@tagHelperPrefix th:
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidPlain*, {AssemblyA}",
                    "th:"
                },
                {
                    $@"
@addTagHelper *, {AssemblyA}
@tagHelperPrefix th:",
                    "th:"
                },
                {
                    $@"
@tagHelperPrefix th-
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidPlain*, {AssemblyA}
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidInherited*, {AssemblyA}",
                    "th-"
                },
                {
                    $@"
@tagHelperPrefix
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidPlain*, {AssemblyA}
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidInherited*, {AssemblyA}",
                    null
                },
                {
                    $@"
@tagHelperPrefix ""th""
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}",
                    "th"
                },
                {
                    $@"
@addTagHelper *, {AssemblyA}
@tagHelperPrefix th:-
@addTagHelper *, {AssemblyB}",
                    "th:-"
                },
            };
        }
    }

    [Theory]
    [MemberData(nameof(ProcessTagHelperPrefixData))]
    public void DirectiveVisitor_ExtractsPrefixFromSyntaxTree(
        string source,
        string expectedPrefix)
    {
        // Arrange
        var sourceDocument = TestRazorSourceDocument.Create(source, filePath: "TestFile");
        var parser = new RazorParser();
        var syntaxTree = parser.Parse(sourceDocument);
        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.TagHelperDirectiveVisitor();
        visitor.Initialize(tagHelpers: [], sourceDocument.FilePath);

        // Act
        visitor.Visit(syntaxTree.Root);

        // Assert
        Assert.Equal(expectedPrefix, visitor.TagHelperPrefix);
    }

    public static TheoryData<string, TagHelperCollection, TagHelperCollection> ProcessTagHelperMatchesData
        // source, taghelpers, expected descriptors
        => new()
        {
            {
                $@"
@addTagHelper *, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor],
                [Valid_PlainTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor],
                [Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper *, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor],
                [Valid_PlainTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper *, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor],
                [String_TagHelperDescriptor]
            },
            {
                $@"
@addTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}
@addTagHelper *, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor],
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor],
                [Valid_InheritedTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor],
                [Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor],
                [Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidPlain*, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor],
                [Valid_PlainTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.*, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor],
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidP*, {AssemblyA}
@addTagHelper *, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor],
                [Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper Str*, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor],
                [Valid_PlainTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper *, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor],
                [Valid_PlainTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@addTagHelper System.{String_TagHelperDescriptor.Name}, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor],
                [Valid_PlainTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper Microsoft.*, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor],
                [String_TagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper ?Microsoft*, {AssemblyA}
@removeTagHelper System.{String_TagHelperDescriptor.Name}, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor],
                [Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper TagHelper*, {AssemblyA}
@removeTagHelper System.{String_TagHelperDescriptor.Name}, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor],
                [Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor]
            }
        };

    [Theory]
    [MemberData(nameof(ProcessTagHelperMatchesData))]
    public void DirectiveVisitor_FiltersTagHelpersByDirectives(
        string source,
        TagHelperCollection tagHelpers,
        TagHelperCollection expectedTagHelpers)
    {
        // Arrange
        var sourceDocument = TestRazorSourceDocument.Create(source, filePath: "TestFile");
        var parser = new RazorParser();
        var syntaxTree = parser.Parse(sourceDocument);
        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.TagHelperDirectiveVisitor();
        visitor.Initialize(tagHelpers, sourceDocument.FilePath);

        // Act
        visitor.Visit(syntaxTree.Root);
        var results = visitor.GetResults();

        // Assert
        Assert.Equal(expectedTagHelpers.Count, results.Count);

        foreach (var expectedTagHelper in expectedTagHelpers)
        {
            Assert.Contains(expectedTagHelper, results);
        }
    }

    public static TheoryData<string, TagHelperCollection> ProcessTagHelperMatches_EmptyResultData
        // source, taghelpers
        => new()
        {
            {
                $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper *, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}
@removeTagHelper {Valid_InheritedTagHelperDescriptor.Name}, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper *, {AssemblyA}
@removeTagHelper *, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor]
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}
@removeTagHelper {Valid_InheritedTagHelperDescriptor.Name}, {AssemblyA}
@removeTagHelper {String_TagHelperDescriptor.Name}, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor]
            },
            {
                $@"
@removeTagHelper *, {AssemblyA}
@removeTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}",
                []
            },
            {
                $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper Mic*, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper Mic*, {AssemblyA}
@removeTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}
@removeTagHelper {Valid_InheritedTagHelperDescriptor.Name}, {AssemblyA}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor]
            },
            {
                $@"
@addTagHelper Microsoft.*, {AssemblyA}
@addTagHelper System.*, {AssemblyB}
@removeTagHelper Microsoft.AspNetCore.Razor.TagHelpers*, {AssemblyA}
@removeTagHelper System.*, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor]
            },
            {
                $@"
@addTagHelper ?icrosoft.*, {AssemblyA}
@addTagHelper ?ystem.*, {AssemblyB}
@removeTagHelper *?????r, {AssemblyA}
@removeTagHelper Sy??em.*, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor]
            },
            {
                $@"
@addTagHelper ?i?crosoft.*, {AssemblyA}
@addTagHelper ??ystem.*, {AssemblyB}",
                [Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor]
            }
        };

    [Theory]
    [MemberData(nameof(ProcessTagHelperMatches_EmptyResultData))]
    public void ProcessDirectives_CanReturnEmptyDescriptorsBasedOnDirectiveDescriptors(
        string source,
        TagHelperCollection tagHelpers)
    {
        // Arrange
        var sourceDocument = TestRazorSourceDocument.Create(source, filePath: "TestFile");
        var parser = new RazorParser();
        var syntaxTree = parser.Parse(sourceDocument);
        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.TagHelperDirectiveVisitor();
        visitor.Initialize(tagHelpers, sourceDocument.FilePath);

        // Act
        visitor.Visit(syntaxTree.Root);
        var results = visitor.GetResults();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void TagHelperDirectiveVisitor_DoesNotMatch_Components()
    {
        // Arrange
        var componentDescriptor = CreateComponentDescriptor("counter", "SomeProject.Counter", AssemblyA);
        var legacyDescriptor = Valid_PlainTagHelperDescriptor;
        TagHelperCollection tagHelpers =
        [
            legacyDescriptor,
            componentDescriptor
        ];

        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.TagHelperDirectiveVisitor();
        visitor.Initialize(tagHelpers, filePath: null);
        var sourceDocument = CreateTestSourceDocument();
        var tree = RazorSyntaxTree.Parse(sourceDocument);

        // Act
        visitor.Visit(tree);
        var results = visitor.GetResults();

        // Assert
        var result = Assert.Single(results);
        Assert.Same(legacyDescriptor, result);
    }

    private static RazorSourceDocument CreateTestSourceDocument()
    {
        var content =
        @"
@addTagHelper *, TestAssembly
<form>
    <input value='Hello' type='text' />
</form>";

        return TestRazorSourceDocument.Create(content, filePath: null);
    }

    private static TagHelperDescriptor CreateTagHelperDescriptor(
        string tagName,
        string typeName,
        string assemblyName,
        string typeNamespace = null,
        string typeNameIdentifier = null,
        IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null,
        IEnumerable<Action<TagMatchingRuleDescriptorBuilder>> ruleBuilders = null)
    {
        return CreateDescriptor(TagHelperKind.ITagHelper, tagName, typeName, assemblyName, typeNamespace, typeNameIdentifier, attributes, ruleBuilders);
    }
    #endregion

    #region Components
    [Fact]
    public void ComponentDirectiveVisitor_DoesNotMatch_LegacyTagHelpers()
    {
        // Arrange
        var currentNamespace = "SomeProject";
        var componentDescriptor = CreateComponentDescriptor("counter", "SomeProject.Counter", AssemblyA);
        var legacyDescriptor = Valid_PlainTagHelperDescriptor;
        TagHelperCollection tagHelpers =
        [
            legacyDescriptor,
            componentDescriptor
        ];
        var sourceDocument = CreateComponentTestSourceDocument(@"<Counter />", "C:\\SomeFolder\\SomeProject\\Counter.cshtml");
        var tree = RazorSyntaxTree.Parse(sourceDocument);
        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor();
        visitor.Initialize(tagHelpers, sourceDocument.FilePath, currentNamespace);

        // Act
        visitor.Visit(tree);

        // Assert
        Assert.Null(visitor.TagHelperPrefix);
        var result = Assert.Single(visitor.GetResults());
        Assert.Same(componentDescriptor, result);
    }

    [Fact]
    public void ComponentDirectiveVisitor_AddsErrorOnLegacyTagHelperDirectives()
    {
        // Arrange
        var currentNamespace = "SomeProject";
        var componentDescriptor = CreateComponentDescriptor("counter", "SomeProject.Counter", AssemblyA);
        var legacyDescriptor = Valid_PlainTagHelperDescriptor;
        TagHelperCollection tagHelpers =
        [
            legacyDescriptor,
            componentDescriptor
        ];
        var filePath = "C:\\SomeFolder\\SomeProject\\Counter.cshtml";
        var content = @"
@tagHelperPrefix th:

<Counter />
";
        var sourceDocument = CreateComponentTestSourceDocument(content, filePath);
        var tree = RazorSyntaxTree.Parse(sourceDocument);
        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor();
        visitor.Initialize(tagHelpers, sourceDocument.FilePath, currentNamespace);

        // Act
        visitor.Visit(tree);
        var results = visitor.GetResults();

        // Assert
        Assert.Null(visitor.TagHelperPrefix);
        var result = Assert.Single(results);
        Assert.Same(componentDescriptor, result);
        var directiveChunkGenerator = (TagHelperPrefixDirectiveChunkGenerator)tree.Root.DescendantNodes().First(n => n is CSharpStatementLiteralSyntax).GetChunkGenerator();
        var diagnostic = Assert.Single(directiveChunkGenerator.Diagnostics);
        Assert.Equal("RZ9978", diagnostic.Id);
    }

    [Fact]
    public void ComponentDirectiveVisitor_MatchesFullyQualifiedComponents()
    {
        // Arrange
        var currentNamespace = "SomeProject";
        var componentDescriptor = CreateComponentDescriptor(
            "SomeProject.SomeOtherFolder.Counter",
            "SomeProject.SomeOtherFolder.Counter",
            AssemblyA,
            fullyQualified: true);
        TagHelperCollection tagHelpers =
        [
            componentDescriptor
        ];
        var filePath = "C:\\SomeFolder\\SomeProject\\Counter.cshtml";
        var content = @"
";
        var sourceDocument = CreateComponentTestSourceDocument(content, filePath);
        var tree = RazorSyntaxTree.Parse(sourceDocument);
        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor();
        visitor.Initialize(tagHelpers, sourceDocument.FilePath, currentNamespace);

        // Act
        visitor.Visit(tree);
        var results = visitor.GetResults();

        // Assert
        var result = Assert.Single(results);
        Assert.Same(componentDescriptor, result);
    }

    [Fact]
    public void ComponentDirectiveVisitor_ComponentInScope_MatchesChildContent()
    {
        // Arrange
        var currentNamespace = "SomeProject";
        var componentDescriptor = CreateComponentDescriptor(
            "Counter",
            "SomeProject.Counter",
            AssemblyA);
        var childContentDescriptor = CreateComponentDescriptor(
            "ChildContent",
            "SomeProject.Counter.ChildContent",
            AssemblyA,
            "SomeProject",
            "Counter",
            childContent: true);
        TagHelperCollection tagHelpers =
        [
            componentDescriptor,
            childContentDescriptor
        ];
        var filePath = "C:\\SomeFolder\\SomeProject\\Counter.cshtml";
        var content = @"
";
        var sourceDocument = CreateComponentTestSourceDocument(content, filePath);
        var tree = RazorSyntaxTree.Parse(sourceDocument);
        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor();
        visitor.Initialize(tagHelpers, sourceDocument.FilePath, currentNamespace);

        // Act
        visitor.Visit(tree);
        var results = visitor.GetResults();

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ComponentDirectiveVisitor_NullCurrentNamespace_MatchesOnlyFullyQualifiedComponents()
    {
        // Arrange
        string currentNamespace = null;
        var componentDescriptor = CreateComponentDescriptor(
            "Counter",
            "SomeProject.Counter",
            AssemblyA);
        var fullyQualifiedComponent = CreateComponentDescriptor(
           "SomeProject.SomeOtherFolder.Counter",
           "SomeProject.SomeOtherFolder.Counter",
           AssemblyA,
           fullyQualified: true);
        TagHelperCollection tagHelpers =
        [
            componentDescriptor,
            fullyQualifiedComponent
        ];
        var filePath = "C:\\SomeFolder\\SomeProject\\Counter.cshtml";
        var content = @"
";
        var sourceDocument = CreateComponentTestSourceDocument(content, filePath);
        var tree = RazorSyntaxTree.Parse(sourceDocument);
        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor();
        visitor.Initialize(tagHelpers, sourceDocument.FilePath, currentNamespace);

        // Act
        visitor.Visit(tree);
        var results = visitor.GetResults();

        // Assert
        var result = Assert.Single(results);
        Assert.Same(fullyQualifiedComponent, result);
    }

    [Fact]
    public void ComponentDirectiveVisitor_MatchesIfNamespaceInUsing()
    {
        // Arrange
        var currentNamespace = "SomeProject";
        var componentDescriptor = CreateComponentDescriptor(
            "Counter",
            "SomeProject.Counter",
            AssemblyA);
        var anotherComponentDescriptor = CreateComponentDescriptor(
           "Foo",
           "SomeProject.SomeOtherFolder.Foo",
           AssemblyA);
        TagHelperCollection tagHelpers =
        [
            componentDescriptor,
            anotherComponentDescriptor
        ];
        var filePath = "C:\\SomeFolder\\SomeProject\\Counter.cshtml";
        var content = @"
@using SomeProject.SomeOtherFolder
";
        var sourceDocument = CreateComponentTestSourceDocument(content, filePath);
        var tree = RazorSyntaxTree.Parse(sourceDocument);
        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor();
        visitor.Initialize(tagHelpers, sourceDocument.FilePath, currentNamespace);

        // Act
        visitor.Visit(tree);
        var results = visitor.GetResults();

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ComponentDirectiveVisitor_MatchesIfNamespaceInUsing_GlobalPrefix()
    {
        // Arrange
        var currentNamespace = "SomeProject";
        var componentDescriptor = CreateComponentDescriptor(
            "Counter",
            "SomeProject.SomeOtherFolder.Counter",
            AssemblyA);
        TagHelperCollection tagHelpers =
        [
            componentDescriptor
        ];
        var filePath = "C:\\SomeFolder\\SomeProject\\Counter.cshtml";
        var content = """
            @using global::SomeProject.SomeOtherFolder
            """;
        var sourceDocument = CreateComponentTestSourceDocument(content, filePath);
        var tree = RazorSyntaxTree.Parse(sourceDocument);
        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor();
        visitor.Initialize(tagHelpers, sourceDocument.FilePath, currentNamespace);

        // Act
        visitor.Visit(tree);
        var results = visitor.GetResults();

        // Assert
        var result = Assert.Single(results);
        Assert.Same(componentDescriptor, result);
    }

    [Fact]
    public void ComponentDirectiveVisitor_DoesNotMatchForUsingAliasAndStaticUsings()
    {
        // Arrange
        var currentNamespace = "SomeProject";
        var componentDescriptor = CreateComponentDescriptor(
            "Counter",
            "SomeProject.Counter",
            AssemblyA);
        var anotherComponentDescriptor = CreateComponentDescriptor(
           "Foo",
           "SomeProject.SomeOtherFolder.Foo",
           AssemblyA);
        TagHelperCollection tagHelpers =
        [
            componentDescriptor,
            anotherComponentDescriptor
        ];
        var filePath = "C:\\SomeFolder\\SomeProject\\Counter.cshtml";
        var content = @"
@using Bar = SomeProject.SomeOtherFolder
@using static SomeProject.SomeOtherFolder.Foo
";
        var sourceDocument = CreateComponentTestSourceDocument(content, filePath);
        var tree = RazorSyntaxTree.Parse(sourceDocument);
        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor();
        visitor.Initialize(tagHelpers, sourceDocument.FilePath, currentNamespace);

        // Act
        visitor.Visit(tree);
        var results = visitor.GetResults();

        // Assert
        var result = Assert.Single(results);
        Assert.Same(componentDescriptor, result);
    }

    [Theory]
    [InlineData("", "", true)]
    [InlineData("Foo", "Project", true)]
    [InlineData("Project.Foo", "Project", true)]
    [InlineData("Project.Bar.Foo", "Project.Bar", true)]
    [InlineData("Project.Foo", "Project.Bar", true)]
    [InlineData("Project.Bar.Foo", "Project", false)]
    [InlineData("Bar.Foo", "Project", false)]
    public void IsTypeNamespaceInScope_WorksAsExpected(string typeName, string currentNamespace, bool expected)
    {
        // Arrange & Act
        var descriptor = CreateComponentDescriptor(typeName, typeName, "Test.dll");
        var tagHelperTypeNamespace = descriptor.TypeNamespace;

        var result = DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor.IsTypeNamespaceInScope(tagHelperTypeNamespace, currentNamespace);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsTagHelperFromMangledClass_WorksAsExpected()
    {
        // Arrange
        var className = "Counter";
        var typeName = $"SomeProject.SomeNamespace.{ComponentHelpers.MangleClassName(className)}";
        var descriptor = CreateComponentDescriptor(
            tagName: "Counter",
            typeName: typeName,
            assemblyName: AssemblyA);

        // Act
        var result = DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor.IsTagHelperFromMangledClass(descriptor);

        // Assert
        Assert.True(result);
    }

    private static RazorSourceDocument CreateComponentTestSourceDocument(string content, string filePath = null)
    {
        var sourceDocument = TestRazorSourceDocument.Create(content, filePath: filePath);
        return sourceDocument;
    }

    private static TagHelperDescriptor CreateComponentDescriptor(
        string tagName,
        string typeName,
        string assemblyName,
        string typeNamespace = null,
        string typeNameIdentifier = null,
        IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null,
        IEnumerable<Action<TagMatchingRuleDescriptorBuilder>> ruleBuilders = null,
        bool fullyQualified = false,
        bool childContent = false)
    {
        var kind = childContent ? TagHelperKind.ChildContent : TagHelperKind.Component;

        return CreateDescriptor(kind, tagName, typeName, assemblyName, typeNamespace, typeNameIdentifier, attributes, ruleBuilders, fullyQualified);
    }
    #endregion

    private static TagHelperDescriptor CreateDescriptor(
        TagHelperKind kind,
        string tagName,
        string typeName,
        string assemblyName,
        string typeNamespace,
        string typeNameIdentifier,
        IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null,
        IEnumerable<Action<TagMatchingRuleDescriptorBuilder>> ruleBuilders = null,
        bool componentFullyQualified = false)
    {
        var builder = TagHelperDescriptorBuilder.CreateTagHelper(kind, typeName, assemblyName);

        if (typeNamespace == null || typeNameIdentifier == null)
        {
            var lastDotIndex = typeName.LastIndexOf('.');
            typeNamespace ??= lastDotIndex >= 0 ? typeName[..lastDotIndex] : "";
            typeNameIdentifier ??= lastDotIndex >= 0 ? typeName[(lastDotIndex + 1)..] : typeName;
        }

        builder.SetTypeName(typeName, typeNamespace, typeNameIdentifier);

        if (attributes != null)
        {
            foreach (var attributeBuilder in attributes)
            {
                builder.BoundAttributeDescriptor(attributeBuilder);
            }
        }

        if (ruleBuilders != null)
        {
            foreach (var ruleBuilder in ruleBuilders)
            {
                builder.TagMatchingRuleDescriptor(innerRuleBuilder =>
                {
                    innerRuleBuilder.RequireTagName(tagName);
                    ruleBuilder(innerRuleBuilder);
                });
            }
        }
        else
        {
            builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));
        }

        if (componentFullyQualified)
        {
            builder.IsFullyQualifiedNameMatch = true;
        }

        var descriptor = builder.Build();

        return descriptor;
    }

    private static TagHelperIntermediateNode[] FindTagHelperNodes(DocumentIntermediateNode documentNode)
    {
        var results = new System.Collections.Generic.List<TagHelperIntermediateNode>();
        CollectTagHelperNodes(documentNode, results);
        return results.ToArray();
    }

    private static void CollectTagHelperNodes(IntermediateNode node, System.Collections.Generic.List<TagHelperIntermediateNode> results)
    {
        if (node is TagHelperIntermediateNode tagHelperNode)
        {
            results.Add(tagHelperNode);
        }

        foreach (var child in node.Children)
        {
            CollectTagHelperNodes(child, results);
        }
    }
}
