// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class DefaultRazorTagHelperRewritePhase : RazorEnginePhaseBase
{
    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        if (!codeDocument.TryGetSyntaxTree(out var syntaxTree))
        {
            return codeDocument.WithReferencedTagHelpers([]);
        }

        if (!codeDocument.TryGetTagHelperContext(out var context) ||
            context.TagHelpers is [])
        {
            // No tag helpers to rewrite. The rewritten tree is the same as the canonical tree.
            // Tooling in the workspaces layer always expects GetRequiredTagHelperRewrittenSyntaxTree()
            // to return a non-null value after the full pipeline has run.
            return codeDocument
                .WithReferencedTagHelpers([])
                .WithTagHelperRewrittenSyntaxTree(syntaxTree);
        }

        var binder = context.GetBinder();
        using var usedHelpers = new TagHelperCollection.Builder();
        var rewrittenSyntaxTree = TagHelperParseTreeRewriter.Rewrite(syntaxTree, binder, usedHelpers, cancellationToken);

        return codeDocument
            .WithReferencedTagHelpers(usedHelpers.ToCollection())
            .WithTagHelperRewrittenSyntaxTree(rewrittenSyntaxTree);
    }
}
