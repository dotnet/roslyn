// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes.Iterator;

internal abstract class AbstractIteratorCodeFixProvider : CodeFixProvider
{
    protected abstract Task<CodeAction?> GetCodeFixAsync(SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostics, CancellationToken cancellationToken);

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (!TryGetNode(root, context.Span, out var node))
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var codeAction = await GetCodeFixAsync(root, node, context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false);

        if (codeAction != null)
            context.RegisterCodeFix(codeAction, diagnostic);
    }

    protected virtual bool TryGetNode(
        SyntaxNode root, TextSpan span, [NotNullWhen(true)] out SyntaxNode? node)
    {
        node = null;
        var ancestors = root.FindToken(span.Start).GetAncestors<SyntaxNode>();
        if (!ancestors.Any())
        {
            return false;
        }

        node = ancestors.FirstOrDefault(n => n.Span.Contains(span) && n != root);
        return node != null;
    }
}
