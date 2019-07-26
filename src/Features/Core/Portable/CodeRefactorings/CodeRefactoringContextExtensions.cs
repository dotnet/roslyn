// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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

        internal static async Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(this CodeRefactoringContext context)
            where TSyntaxNode : SyntaxNode
        {
            (var document, var span, var cancellationToken) = context;
            var potentialNodes = await GetRelevantNodes<TSyntaxNode>(document, span, cancellationToken).ConfigureAwait(false);
            return potentialNodes.FirstOrDefault();
        }

        internal static async Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(this Document document, TextSpan span, Func<TSyntaxNode, bool> predicate, CancellationToken cancellationToken)
where TSyntaxNode : SyntaxNode
        {
            var potentialNodes = await GetRelevantNodes<TSyntaxNode>(document, span, cancellationToken).ConfigureAwait(false);
            return potentialNodes.FirstOrDefault(predicate);
        }

        internal static async Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(this Document document, TextSpan span, CancellationToken cancellationToken)
where TSyntaxNode : SyntaxNode
        {
            var potentialNodes = await GetRelevantNodes<TSyntaxNode>(document, span, cancellationToken).ConfigureAwait(false);
            return potentialNodes.FirstOrDefault();
        }

        private static async Task<ImmutableArray<TSyntaxNode>> GetRelevantNodes<TSyntaxNode>(Document document, TextSpan span, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            var helpers = document.GetLanguageService<IRefactoringHelpersService>();
            var potentialNodes = await helpers.GetRelevantNodesAsync<TSyntaxNode>(document, span, cancellationToken).ConfigureAwait(false);
            return potentialNodes;
        }
    }
}
