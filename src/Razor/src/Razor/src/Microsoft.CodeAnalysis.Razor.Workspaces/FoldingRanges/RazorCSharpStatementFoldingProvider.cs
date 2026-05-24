// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.CodeAnalysis.Razor.FoldingRanges;

internal class RazorCSharpStatementFoldingProvider : AbstractSyntaxNodeFoldingProvider<CSharpStatementSyntax>
{
    protected override string GetCollapsedText(CSharpStatementSyntax node)
    {
        return "@{...}";
    }

    protected override ImmutableArray<CSharpStatementSyntax> GetFoldableNodes(RazorSyntaxTree syntaxTree)
    {
        return syntaxTree.Root
            .DescendantNodes(static node => node is RazorDocumentSyntax or MarkupBlockSyntax or MarkupElementSyntax or CSharpCodeBlockSyntax)
            .OfType<CSharpStatementSyntax>()
            .SelectAsArray(d => d);
    }
}
