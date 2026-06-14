// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.CodeAnalysis.Remote.Razor.FoldingRanges;

[Shared]
[Export(typeof(IRazorFoldingRangeProvider))]
internal sealed class RazorCSharpStatementKeywordFoldingProvider : AbstractSyntaxNodeFoldingProvider<CSharpCodeBlockSyntax>
{
    protected override string GetCollapsedText(CSharpCodeBlockSyntax node)
    {
        if (node.Children is [_, CSharpStatementLiteralSyntax literal, ..] &&
            literal.LiteralTokens is [var keyword, ..])
        {
            return $"@{keyword.Content}";
        }

        return "@{...}";
    }

    protected override ImmutableArray<CSharpCodeBlockSyntax> GetFoldableNodes(RazorSyntaxTree syntaxTree)
    {
        return syntaxTree.Root
            .EnumerateDescendantNodes(static node => node is RazorDocumentSyntax or MarkupBlockSyntax or MarkupElementSyntax or CSharpCodeBlockSyntax)
            .Where(static n => n is CSharpStatementLiteralSyntax
            {
                Parent: CSharpCodeBlockSyntax,
                LiteralTokens: [{ Kind: SyntaxKind.Keyword }, ..]
            })
            .Select(static n => (CSharpCodeBlockSyntax)n.Parent)
            .ToImmutableArray();
    }
}
