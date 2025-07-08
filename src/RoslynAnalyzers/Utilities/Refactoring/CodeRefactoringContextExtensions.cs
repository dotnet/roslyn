// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities
{
    internal static class CodeRefactoringContextExtensions
    {
        internal static Task<TSyntaxNode?> TryGetRelevantNodeAsync<TSyntaxNode>(this CodeRefactoringContext context, IRefactoringHelpers helpers)
            where TSyntaxNode : SyntaxNode
            => TryGetRelevantNodeAsync<TSyntaxNode>(context.Document, helpers, context.Span, context.CancellationToken);

        internal static Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(this CodeRefactoringContext context, IRefactoringHelpers helpers)
            where TSyntaxNode : SyntaxNode
            => GetRelevantNodesAsync<TSyntaxNode>(context.Document, helpers, context.Span, context.CancellationToken);

        internal static async Task<TSyntaxNode?> TryGetRelevantNodeAsync<TSyntaxNode>(
            this Document document,
            IRefactoringHelpers helpers,
            TextSpan span,
            CancellationToken cancellationToken)
            where TSyntaxNode : SyntaxNode
        {
            var potentialNodes = await GetRelevantNodesAsync<TSyntaxNode>(document, helpers, span, cancellationToken).ConfigureAwait(false);
            return potentialNodes.FirstOrDefault();
        }

        internal static async Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(
            this Document document,
            IRefactoringHelpers helpers,
            TextSpan span,
            CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            return GetRelevantNodes<TSyntaxNode>(helpers, text, root, span, cancellationToken);
        }

        private static ImmutableArray<TSyntaxNode> GetRelevantNodes<TSyntaxNode>(
            IRefactoringHelpers helpers,
            SourceText text,
            SyntaxNode root,
            TextSpan span,
            CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            using var result = TemporaryArray<TSyntaxNode>.Empty;
            helpers.AddRelevantNodes<TSyntaxNode>(
                text, root, span, allowEmptyNodes: false, maxCount: int.MaxValue, ref result.AsRef(), cancellationToken);
            return result.ToImmutableAndClear();
        }
    }
}
