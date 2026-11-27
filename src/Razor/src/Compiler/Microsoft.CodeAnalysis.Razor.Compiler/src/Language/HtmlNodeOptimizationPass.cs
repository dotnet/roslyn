// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language;

internal class HtmlNodeOptimizationPass : RazorEngineFeatureBase, IRazorSyntaxTreePass
{
    public int Order => 100;

    public RazorSyntaxTree Execute(
        RazorCodeDocument codeDocument,
        RazorSyntaxTree syntaxTree,
        CancellationToken cancellationToken = default)
    {
        var whitespaceRewriter = new WhitespaceRewriter(cancellationToken);
        var rewritten = whitespaceRewriter.Visit(syntaxTree.Root);

        return new RazorSyntaxTree(rewritten, syntaxTree.Source, syntaxTree.Diagnostics, syntaxTree.Options);
    }
}
