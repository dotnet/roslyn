// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SyntaxEditorExtensions
    {
        /// <summary>
        /// Performs several edits to a document.  If multiple edits are made within the same
        /// expression context, then the document/semantic-model will be forked after each edit 
        /// so that further edits can see if they're still safe to apply.
        /// </summary>
        public static Task ApplyExpressionLevelSemanticEditsAsync<TNode>(
            this SyntaxEditor editor, Document document,
            ImmutableArray<TNode> originalNodes,
            Func<SemanticModel, TNode, bool> canReplace,
            Func<SemanticModel, SyntaxNode, TNode, SyntaxNode> updateRoot,
            CancellationToken cancellationToken) where TNode : SyntaxNode
        {
            return ApplySemanticEditsAsync(
                editor, document,
                originalNodes,
                (syntaxFacts, node) => GetExpressionSemanticBoundary(syntaxFacts, node),
                canReplace,
                updateRoot,
                cancellationToken);
        }

        /// <summary>
        /// Performs several edits to a document.  If multiple edits are made within a method
        /// body then the document/semantic-model will be forked after each edit so that further
        /// edits can see if they're still safe to apply.
        /// </summary>
        public static Task ApplyMethodBodySemanticEditsAsync<TNode>(
            this SyntaxEditor editor, Document document,
            ImmutableArray<TNode> originalNodes,
            Func<SemanticModel, TNode, bool> canReplace,
            Func<SemanticModel, SyntaxNode, TNode, SyntaxNode> updateRoot,
            CancellationToken cancellationToken) where TNode : SyntaxNode
        {
            return ApplySemanticEditsAsync(
                editor, document,
                originalNodes,
                (syntaxFacts, node) => GetMethodBodySemanticBoundary(syntaxFacts, node),
                canReplace,
                updateRoot,
                cancellationToken);
        }

        /// <summary>
        /// Helper function for fix-all fixes where individual fixes may affect the viability
        /// of another.  For example, consider the following code:
        /// 
        ///     if ((double)x == (double)y)
        ///     
        /// In this code either cast can be removed, but at least one cast must remain.  Even
        /// though an analyzer marks both, a fixer must not remove both.  One way to accomplish
        /// this would be to have the fixer do a semantic check after each application.  However
        /// This is extremely expensive, especially for hte common cases where one fix does
        /// not affect each other.
        /// 
        /// To address that, this helper groups fixes at certain boundary points.  i.e. at 
        /// statement boundaries.  If there is only one fix within the boundary, it does not
        /// do any semantic verification.  However, if there are multiple fixes in a boundary
        /// it will call into <paramref name="canReplace"/> to validate if the subsequent fix
        /// can be made or not.
        /// </summary>
        private static async Task ApplySemanticEditsAsync<TNode>(
            this SyntaxEditor editor, Document document,
            ImmutableArray<TNode> originalNodes,
            Func<ISyntaxFactsService, SyntaxNode, SyntaxNode> getSemanticBoundary,
            Func<SemanticModel, TNode, bool> canReplace,
            Func<SemanticModel, SyntaxNode, TNode, SyntaxNode> updateRoot,
            CancellationToken cancellationToken) where TNode : SyntaxNode
        {
            // This code fix will not make changes that affect the semantics of a statement
            // or declaration. Therefore, we can skip the expensive verification step in 
            // cases where only one expression appears within the group.
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var nodesBySemanticBoundary = originalNodes.GroupBy(node => getSemanticBoundary(syntaxFacts, node));
            var nodesToVerify = nodesBySemanticBoundary.Where(group => group.Skip(1).Any()).Flatten().ToSet();

            // We're going to be continually editing this tree.  Track all the nodes we
            // care about so we can find them across each edit.
            var originalRoot = editor.OriginalRoot;
            document = document.WithSyntaxRoot(originalRoot.TrackNodes(originalNodes));
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var currentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            foreach (var originalNode in originalNodes)
            {
                var currentNode = currentRoot.GetCurrentNode(originalNode);
                var skipVerification = !nodesToVerify.Contains(originalNode);

                if (skipVerification || canReplace(semanticModel, currentNode))
                {
                    var replacementRoot = updateRoot(semanticModel, currentRoot, currentNode);

                    document = document.WithSyntaxRoot(replacementRoot);

                    semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    currentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            editor.ReplaceNode(originalRoot, currentRoot);
        }

        private static SyntaxNode GetExpressionSemanticBoundary(ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            // Notes:
            // 1. Syntax which doesn't fall into one of the "safe buckets" will get placed into a 
            //    single group keyed off the root of the tree. If more than one such node exists
            //    in the document, all will be verified.
            // 2. Cannot include ArgumentSyntax because it could affect generic argument inference.
            return node.FirstAncestorOrSelf<SyntaxNode>(
                n => syntaxFacts.IsStatement(n) ||
                     syntaxFacts.IsParameter(n) ||
                     syntaxFacts.IsVariableDeclarator(n) ||
                     n.Parent == null);
        }

        private static SyntaxNode GetMethodBodySemanticBoundary(ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            return node.FirstAncestorOrSelf<SyntaxNode>(
                n => syntaxFacts.IsMethodBody(n) ||
                     n.Parent == null);
        }
    }
}
