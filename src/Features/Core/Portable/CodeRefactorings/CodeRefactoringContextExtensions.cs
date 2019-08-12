// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal static class CodeRefactoringContextExtensions
    {
        /// <summary>
        /// Use this helper to register multiple refactorings (<paramref name="actions"/>).
        /// </summary>
        internal static void RegisterRefactorings<TCodeAction>(
            this CodeRefactoringContext context, ImmutableArray<TCodeAction> actions)
            where TCodeAction : CodeAction
        {
            if (!actions.IsDefault)
            {
                foreach (var action in actions)
                {
                    context.RegisterRefactoring(action);
                }
            }
        }

        internal static Task<TSyntaxNode> TryGetRelevantNodeAsync<TSyntaxNode>(this CodeRefactoringContext context)
            where TSyntaxNode : SyntaxNode
            => TryGetRelevantNodeAsync<TSyntaxNode>(context.Document, context.Span, context.CancellationToken);

        internal static Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(this CodeRefactoringContext context)
            where TSyntaxNode : SyntaxNode
            => GetRelevantNodesAsync<TSyntaxNode>(context.Document, context.Span, context.CancellationToken);

        internal static async Task<TSyntaxNode> TryGetRelevantNodeAsync<TSyntaxNode>(
            this Document document,
            TextSpan span,
            CancellationToken cancellationToken)
            where TSyntaxNode : SyntaxNode
        {
            var potentialNodes = await GetRelevantNodesAsync<TSyntaxNode>(document, span, cancellationToken).ConfigureAwait(false);
            return potentialNodes.FirstOrDefault();
        }

        internal static async Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(
            this Document document,
            TextSpan span,
            CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            var helpers = document.GetLanguageService<IRefactoringHelpersService>();
            var potentialNodes = await helpers.GetRelevantNodesAsync<TSyntaxNode>(document, span, cancellationToken).ConfigureAwait(false);
            return potentialNodes;
        }
    }
}
