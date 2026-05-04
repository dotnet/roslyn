// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultRazorIntermediateNodeLoweringPhaseTest
{
    [Fact]
    public void Execute_AutomaticallyImportsSingleLineSinglyOccurringDirective()
    {
        // Arrange
        var directive = DirectiveDescriptor.CreateSingleLineDirective(
            "custom",
            builder =>
            {
                builder.AddStringToken();
                builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            });
        var phase = new DefaultRazorIntermediateNodeLoweringPhase();
        var engine = RazorProjectEngine.CreateEmpty(b =>
        {
            b.Phases.Add(phase);
            b.AddDirective(directive);
        });

        var options = RazorParserOptions.Default
            .WithDirectives(directive)
            .WithFlags(useRoslynTokenizer: true);

        var importSource = TestRazorSourceDocument.Create("@custom \"hello\"", filePath: "import.cshtml");
        var codeDocument = TestRazorCodeDocument.Create("<p>NonDirective</p>");
        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
        codeDocument = codeDocument.WithImportSyntaxTrees([RazorSyntaxTree.Parse(importSource, options)]);

        // Act
        codeDocument = phase.Execute(codeDocument);

        // Assert
        var documentNode = codeDocument.GetRequiredDocumentNode();
        var customDirectives = documentNode.FindDirectiveReferences(directive);
        var customDirective = Assert.Single(customDirectives).Node;
        var stringToken = Assert.Single(customDirective.Tokens);
        Assert.Equal("\"hello\"", stringToken.Content);
    }

    [Fact]
    public void Execute_AutomaticallyOverridesImportedSingleLineSinglyOccurringDirective_MainDocument()
    {
        // Arrange
        var directive = DirectiveDescriptor.CreateSingleLineDirective(
            "custom",
            builder =>
            {
                builder.AddStringToken();
                builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            });
        var phase = new DefaultRazorIntermediateNodeLoweringPhase();
        var engine = RazorProjectEngine.CreateEmpty(b =>
        {
            b.Phases.Add(phase);
            b.AddDirective(directive);
        });

        var options = RazorParserOptions.Default
            .WithDirectives(directive)
            .WithFlags(useRoslynTokenizer: true);

        var importSource = TestRazorSourceDocument.Create("@custom \"hello\"", filePath: "import.cshtml");
        var codeDocument = TestRazorCodeDocument.Create("@custom \"world\"");
        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
        codeDocument = codeDocument.WithImportSyntaxTrees([RazorSyntaxTree.Parse(importSource, options)]);

        // Act
        codeDocument = phase.Execute(codeDocument);

        // Assert
        var documentNode = codeDocument.GetRequiredDocumentNode();
        var customDirectives = documentNode.FindDirectiveReferences(directive);
        var customDirective = Assert.Single(customDirectives).Node;
        var stringToken = Assert.Single(customDirective.Tokens);
        Assert.Equal("\"world\"", stringToken.Content);
    }

    [Fact]
    public void Execute_AutomaticallyOverridesImportedSingleLineSinglyOccurringDirective_MultipleImports()
    {
        // Arrange
        var directive = DirectiveDescriptor.CreateSingleLineDirective(
            "custom",
            builder =>
            {
                builder.AddStringToken();
                builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            });
        var phase = new DefaultRazorIntermediateNodeLoweringPhase();
        var engine = RazorProjectEngine.CreateEmpty(b =>
        {
            b.Phases.Add(phase);
            b.AddDirective(directive);
        });

        var options = RazorParserOptions.Default
            .WithDirectives(directive)
            .WithFlags(useRoslynTokenizer: true);

        var importSource1 = TestRazorSourceDocument.Create("@custom \"hello\"", filePath: "import1.cshtml");
        var importSource2 = TestRazorSourceDocument.Create("@custom \"world\"", filePath: "import2.cshtml");
        var codeDocument = TestRazorCodeDocument.Create("<p>NonDirective</p>");
        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
        codeDocument = codeDocument.WithImportSyntaxTrees([RazorSyntaxTree.Parse(importSource1, options), RazorSyntaxTree.Parse(importSource2, options)]);

        // Act
        codeDocument = phase.Execute(codeDocument);

        // Assert
        var documentNode = codeDocument.GetRequiredDocumentNode();
        var customDirectives = documentNode.FindDirectiveReferences(directive);
        var customDirective = Assert.Single(customDirectives).Node;
        var stringToken = Assert.Single(customDirective.Tokens);
        Assert.Equal("\"world\"", stringToken.Content);
    }

    [Fact]
    public void Execute_DoesNotImportNonFileScopedSinglyOccurringDirectives_Block()
    {
        // Arrange
        var codeBlockDirective = DirectiveDescriptor.CreateCodeBlockDirective("code", b => b.AddStringToken());
        var razorBlockDirective = DirectiveDescriptor.CreateRazorBlockDirective("razor", b => b.AddStringToken());
        var phase = new DefaultRazorIntermediateNodeLoweringPhase();
        var engine = RazorProjectEngine.CreateEmpty(b =>
        {
            b.Phases.Add(phase);
            b.AddDirective(codeBlockDirective);
            b.AddDirective(razorBlockDirective);
        });

        var options = RazorParserOptions.Default
            .WithDirectives(codeBlockDirective, razorBlockDirective)
            .WithFlags(useRoslynTokenizer: true);

        var importSource = TestRazorSourceDocument.Create(
@"@code ""code block"" { }
@razor ""razor block"" { }",
            filePath: "testImports.cshtml");
        var codeDocument = TestRazorCodeDocument.Create("<p>NonDirective</p>");
        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
        codeDocument = codeDocument.WithImportSyntaxTrees([RazorSyntaxTree.Parse(importSource, options)]);

        // Act
        codeDocument = phase.Execute(codeDocument);

        // Assert
        var documentNode = codeDocument.GetRequiredDocumentNode();
        var directives = documentNode.Children.OfType<DirectiveIntermediateNode>();
        Assert.Empty(directives);
    }

    [Fact]
    public void Execute_ErrorsForCodeBlockFileScopedSinglyOccurringDirectives()
    {
        // Arrange
        var directive = DirectiveDescriptor.CreateCodeBlockDirective("custom", b => b.Usage = DirectiveUsage.FileScopedSinglyOccurring);
        var phase = new DefaultRazorIntermediateNodeLoweringPhase();
        var engine = RazorProjectEngine.CreateEmpty(b =>
        {
            b.Phases.Add(phase);
            b.AddDirective(directive);
        });

        var options = RazorParserOptions.Default
            .WithDirectives(directive)
            .WithFlags(useRoslynTokenizer: true);

        var importSource = TestRazorSourceDocument.Create("@custom { }", filePath: "import.cshtml");
        var codeDocument = TestRazorCodeDocument.Create("<p>NonDirective</p>");
        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
        codeDocument = codeDocument.WithImportSyntaxTrees([RazorSyntaxTree.Parse(importSource, options)]);
        var expectedDiagnostic = RazorDiagnosticFactory.CreateDirective_BlockDirectiveCannotBeImported("custom");

        // Act
        codeDocument = phase.Execute(codeDocument);

        // Assert
        var documentNode = codeDocument.GetRequiredDocumentNode();
        var directives = documentNode.Children.OfType<DirectiveIntermediateNode>();
        Assert.Empty(directives);
        var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
        Assert.Equal(expectedDiagnostic, diagnostic);
    }

    [Fact]
    public void Execute_ErrorsForRazorBlockFileScopedSinglyOccurringDirectives()
    {
        // Arrange
        var directive = DirectiveDescriptor.CreateRazorBlockDirective("custom", b => b.Usage = DirectiveUsage.FileScopedSinglyOccurring);
        var phase = new DefaultRazorIntermediateNodeLoweringPhase();
        var engine = RazorProjectEngine.CreateEmpty(b =>
        {
            b.Phases.Add(phase);
            b.AddDirective(directive);
        });

        var options = RazorParserOptions.Default
            .WithDirectives(directive)
            .WithFlags(useRoslynTokenizer: true);

        var importSource = TestRazorSourceDocument.Create("@custom { }", filePath: "import.cshtml");
        var codeDocument = TestRazorCodeDocument.Create("<p>NonDirective</p>");
        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
        codeDocument = codeDocument.WithImportSyntaxTrees([RazorSyntaxTree.Parse(importSource, options)]);
        var expectedDiagnostic = RazorDiagnosticFactory.CreateDirective_BlockDirectiveCannotBeImported("custom");

        // Act
        codeDocument = phase.Execute(codeDocument);

        // Assert
        var documentNode = codeDocument.GetRequiredDocumentNode();
        var directives = documentNode.Children.OfType<DirectiveIntermediateNode>();
        Assert.Empty(directives);
        var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
        Assert.Equal(expectedDiagnostic, diagnostic);
    }

    [Fact]
    public void Execute_ThrowsForMissingDependency_SyntaxTree()
    {
        // Arrange
        var phase = new DefaultRazorIntermediateNodeLoweringPhase();

        var engine = RazorProjectEngine.CreateEmpty(b =>
        {
            b.Phases.Add(phase);
        });

        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => phase.Execute(codeDocument));
        Assert.Equal(
            $"The '{nameof(DefaultRazorIntermediateNodeLoweringPhase)}' phase requires a '{nameof(RazorSyntaxTree)}' " +
            $"provided by the '{nameof(RazorCodeDocument)}'.",
            exception.Message);
    }

    [Fact]
    public void Execute_CollatesSyntaxDiagnosticsFromSourceDocument()
    {
        // Arrange
        var phase = new DefaultRazorIntermediateNodeLoweringPhase();
        var engine = RazorProjectEngine.CreateEmpty(b =>
        {
            b.Phases.Add(phase);
        });

        var options = RazorParserOptions.Default
            .WithFlags(useRoslynTokenizer: true);

        var codeDocument = TestRazorCodeDocument.Create("<p class=@(");
        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));

        // Act
        codeDocument = phase.Execute(codeDocument);

        // Assert
        var documentNode = codeDocument.GetRequiredDocumentNode();
        var diagnostic = Assert.Single(documentNode.Diagnostics);
        Assert.Equal(@"The explicit expression block is missing a closing "")"" character.  Make sure you have a matching "")"" character for all the ""("" characters within this block, and that none of the "")"" characters are being interpreted as markup.",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Execute_CollatesSyntaxDiagnosticsFromImportDocuments()
    {
        // Arrange
        var phase = new DefaultRazorIntermediateNodeLoweringPhase();
        var engine = RazorProjectEngine.CreateEmpty(b =>
        {
            b.Phases.Add(phase);
        });

        var parseOptions = RazorParserOptions.Default
            .WithFlags(useRoslynTokenizer: true);

        var codeDocument = TestRazorCodeDocument.CreateEmpty();
        codeDocument = codeDocument.WithSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, parseOptions));
        codeDocument = codeDocument.WithImportSyntaxTrees(
        [
            RazorSyntaxTree.Parse(TestRazorSourceDocument.Create("@ "), parseOptions),
            RazorSyntaxTree.Parse(TestRazorSourceDocument.Create("<p @("), parseOptions),
        ]);
        var options = RazorCodeGenerationOptions.Default;

        // Act
        codeDocument = phase.Execute(codeDocument);

        // Assert
        var documentNode = codeDocument.GetRequiredDocumentNode();
        Assert.Collection(documentNode.Diagnostics,
            diagnostic =>
            {
                Assert.Equal(@"A space or line break was encountered after the ""@"" character.  Only valid identifiers, keywords, comments, ""("" and ""{"" are valid at the start of a code block and they must occur immediately following ""@"" with no space in between.",
                    diagnostic.GetMessage(CultureInfo.CurrentCulture));
            },
            diagnostic =>
            {
                Assert.Equal(@"The explicit expression block is missing a closing "")"" character.  Make sure you have a matching "")"" character for all the ""("" characters within this block, and that none of the "")"" characters are being interpreted as markup.",
                    diagnostic.GetMessage(CultureInfo.CurrentCulture));
            });
    }
}
