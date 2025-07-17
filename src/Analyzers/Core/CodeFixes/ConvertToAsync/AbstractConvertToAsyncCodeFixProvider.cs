// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ConvertToAsync;

internal abstract partial class AbstractConvertToAsyncCodeFixProvider : CodeFixProvider
{
    protected abstract Task<string> GetDescriptionAsync(Diagnostic diagnostic, SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken);
    protected abstract Task<(SyntaxTree syntaxTree, SyntaxNode root)?> GetRootInOtherSyntaxTreeAsync(SyntaxNode node, SemanticModel semanticModel, Diagnostic diagnostic, CancellationToken cancellationToken);

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (!TryGetNode(root, context.Span, out var node))
        {
            return;
        }

        var codeAction = await GetCodeActionAsync(
            node, context.Document, context.Diagnostics[0], context.CancellationToken).ConfigureAwait(false);
        if (codeAction != null)
        {
            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static bool TryGetNode(
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

    private async Task<CodeAction?> GetCodeActionAsync(
        SyntaxNode node, Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var result = await GetRootInOtherSyntaxTreeAsync(node, semanticModel, diagnostic, cancellationToken).ConfigureAwait(false);
        if (result is not (var syntaxTree, var newRoot))
            return null;

        var otherDocument = document.Project.Solution.GetRequiredDocument(syntaxTree);
        var title = await GetDescriptionAsync(diagnostic, node, semanticModel, cancellationToken).ConfigureAwait(false);
        return CodeAction.Create(
            title,
            token => Task.FromResult(otherDocument.WithSyntaxRoot(newRoot)),
            title);
    }
}
