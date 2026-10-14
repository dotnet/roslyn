// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.CodeAnalysis.Remote.Razor.FoldingRanges;

[Shared]
[Export(typeof(IRazorFoldingRangeProvider))]
internal sealed class RazorDocumentationFoldingProvider : AbstractSyntaxNodeFoldingProvider<RazorDocumentationDirectiveSyntax>
{
    protected override string GetCollapsedText(RazorDocumentationDirectiveSyntax node)
        => "@documentation";

    protected override ImmutableArray<RazorDocumentationDirectiveSyntax> GetFoldableNodes(RazorSyntaxTree syntaxTree)
        => syntaxTree.GetDirectives<RazorDocumentationDirectiveSyntax>();
}
