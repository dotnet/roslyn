// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
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

        internal static Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(
            this Document document,
            IRefactoringHelpers helpers,
            TextSpan span,
            CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            return helpers.GetRelevantNodesAsync<TSyntaxNode>(document, span, cancellationToken);
        }
    }
}
