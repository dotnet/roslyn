// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
    protected abstract Task<Tuple<SyntaxTree, SyntaxNode>> GetRootInOtherSyntaxTreeAsync(SyntaxNode node, SemanticModel semanticModel, Diagnostic diagnostic, CancellationToken cancellationToken);

    public override FixAllProvider GetFixAllProvider()
    {
        // Fix All is not supported by this code fix
        // https://github.com/dotnet/roslyn/issues/34463
        return null;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (!TryGetNode(root, context.Span, out var node))
        {
            return;
        }

        var diagnostic = context.Diagnostics.FirstOrDefault();

        var codeAction = await GetCodeActionAsync(
            node, context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false);
        if (codeAction != null)
        {
            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static bool TryGetNode(SyntaxNode root, TextSpan span, out SyntaxNode node)
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

    private async Task<CodeAction> GetCodeActionAsync(
        SyntaxNode node, Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var result = await GetRootInOtherSyntaxTreeAsync(node, semanticModel, diagnostic, cancellationToken).ConfigureAwait(false);
        if (result == null)
            return null;

        var syntaxTree = result.Item1;
        var newRoot = result.Item2;
        var otherDocument = document.Project.Solution.GetDocument(syntaxTree);
        var title = await GetDescriptionAsync(diagnostic, node, semanticModel, cancellationToken).ConfigureAwait(false);
        return CodeAction.Create(
            title,
            token => Task.FromResult(otherDocument.WithSyntaxRoot(newRoot)),
            title);
    }
}
