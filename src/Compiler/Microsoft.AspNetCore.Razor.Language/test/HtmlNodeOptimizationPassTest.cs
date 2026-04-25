// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class HtmlNodeOptimizationPassTest
{
    [Fact]
    public void Execute_RewritesWhitespace()
    {
        // Assert
        var content = """
            
                @true
            """;

        var projectEngine = RazorProjectEngine.CreateEmpty(builder =>
        {
            builder.Features.Add(new HtmlNodeOptimizationPass());
        });

        var source = TestRazorSourceDocument.Create(content);
        var originalTree = RazorSyntaxTree.Parse(source);
        var codeDocument = projectEngine.CreateCodeDocument(source);
        Assert.True(projectEngine.Engine.TryGetFeature<HtmlNodeOptimizationPass>(out var pass));

        // Act
        var outputTree = pass.Execute(codeDocument, originalTree);

        // Assert
        var document = Assert.IsType<RazorDocumentSyntax>(outputTree.Root);
        var block = Assert.IsType<MarkupBlockSyntax>(document.Document);
        Assert.Equal(4, block.Children.Count);
        var whitespace = Assert.IsType<MarkupTextLiteralSyntax>(block.Children[1]);
        Assert.True(whitespace.GetContent().All(char.IsWhiteSpace));
    }
}
