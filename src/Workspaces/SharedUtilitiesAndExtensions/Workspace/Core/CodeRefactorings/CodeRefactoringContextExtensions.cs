// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

internal static class CodeRefactoringContextExtensions
{
    /// <summary>
    /// Use this helper to register multiple refactorings (<paramref name="actions"/>).
    /// </summary>
    public static void RegisterRefactorings<TCodeAction>(
        this CodeRefactoringContext context, ImmutableArray<TCodeAction> actions, TextSpan? applicableToSpan = null)
        where TCodeAction : CodeAction
    {
        if (!actions.IsDefault)
        {
            foreach (var action in actions)
            {
#if WORKSPACE
                if (applicableToSpan != null)
                {
                    context.RegisterRefactoring(action, applicableToSpan.Value);
                    continue;
                }
#endif

                context.RegisterRefactoring(action);
            }
        }
    }

    public static Task<TSyntaxNode?> TryGetRelevantNodeAsync<TSyntaxNode>(this CodeRefactoringContext context) where TSyntaxNode : SyntaxNode
        => TryGetRelevantNodeAsync<TSyntaxNode>(context, allowEmptyNode: false);

    public static async Task<TSyntaxNode?> TryGetRelevantNodeAsync<TSyntaxNode>(this CodeRefactoringContext context, bool allowEmptyNode) where TSyntaxNode : SyntaxNode
    {
        var parsedDocument = await ParsedDocument.CreateAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
        return TryGetRelevantNode<TSyntaxNode>(parsedDocument, context.Span, allowEmptyNode, context.CancellationToken);
    }

    public static Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(this CodeRefactoringContext context) where TSyntaxNode : SyntaxNode
        => GetRelevantNodesAsync<TSyntaxNode>(context, allowEmptyNodes: false);

    public static async Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(this CodeRefactoringContext context, bool allowEmptyNodes) where TSyntaxNode : SyntaxNode
    {
        var parsedDocument = await ParsedDocument.CreateAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
        return GetRelevantNodes<TSyntaxNode>(parsedDocument, context.Span, allowEmptyNodes, context.CancellationToken);
    }

    public static async Task<TSyntaxNode?> TryGetRelevantNodeAsync<TSyntaxNode>(this Document document, TextSpan span, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        return TryGetRelevantNode<TSyntaxNode>(parsedDocument, span, cancellationToken);
    }

    public static TSyntaxNode? TryGetRelevantNode<TSyntaxNode>(this ParsedDocument document, TextSpan span, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        => TryGetRelevantNode<TSyntaxNode>(document, span, allowEmptyNode: false, cancellationToken);

    public static TSyntaxNode? TryGetRelevantNode<TSyntaxNode>(this ParsedDocument document, TextSpan span, bool allowEmptyNode, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        using var result = TemporaryArray<TSyntaxNode>.Empty;
        AddRelevantNodes(document, span, allowEmptyNode, maxCount: 1, ref result.AsRef(), cancellationToken);
        return result.FirstOrDefault();
    }

    public static Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(
        this Document document, TextSpan span, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        return GetRelevantNodesAsync<TSyntaxNode>(document, span, allowEmptyNodes: false, cancellationToken);
    }

    public static async Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(
        this Document document, TextSpan span, bool allowEmptyNodes, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        return GetRelevantNodes<TSyntaxNode>(parsedDocument, span, allowEmptyNodes, cancellationToken);
    }

    public static ImmutableArray<TSyntaxNode> GetRelevantNodes<TSyntaxNode>(
        this ParsedDocument document, TextSpan span, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        => GetRelevantNodes<TSyntaxNode>(document, span, allowEmptyNodes: false, cancellationToken);

    public static ImmutableArray<TSyntaxNode> GetRelevantNodes<TSyntaxNode>(
        this ParsedDocument document, TextSpan span, bool allowEmptyNodes, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        using var result = TemporaryArray<TSyntaxNode>.Empty;
        AddRelevantNodes(document, span, allowEmptyNodes, maxCount: int.MaxValue, ref result.AsRef(), cancellationToken);

        return result.ToImmutableAndClear();
    }

    private static void AddRelevantNodes<TSyntaxNode>(
        this ParsedDocument document, TextSpan span, bool allowEmptyNodes, int maxCount, ref TemporaryArray<TSyntaxNode> result, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        var helpers = document.LanguageServices.GetRequiredService<IRefactoringHelpersService>();
        helpers.AddRelevantNodes(document, span, allowEmptyNodes, maxCount, ref result, cancellationToken);
    }
}
