// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal abstract class AbstractSimplificationService<TExpressionSyntax, TStatementSyntax, TCrefSyntax> : ISimplificationService
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TCrefSyntax : SyntaxNode
    {
        protected abstract IEnumerable<AbstractReducer> GetReducers();

        protected abstract ImmutableArray<NodeOrTokenToReduce> GetNodesAndTokensToReduce(SyntaxNode root, Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpans);
        protected abstract SemanticModel GetSpeculativeSemanticModel(ref SyntaxNode nodeToSpeculate, SemanticModel originalSemanticModel, SyntaxNode originalNode);
        protected abstract bool CanNodeBeSimplifiedWithoutSpeculation(SyntaxNode node);

        protected virtual SyntaxNode TransformReducedNode(SyntaxNode reducedNode, SyntaxNode originalNode)
        {
            return reducedNode;
        }

        public abstract SyntaxNode Expand(SyntaxNode node, SemanticModel semanticModel, SyntaxAnnotation annotationForReplacedAliasIdentifier, Func<SyntaxNode, bool> expandInsideNode, bool expandParameter, CancellationToken cancellationToken);
        public abstract SyntaxToken Expand(SyntaxToken token, SemanticModel semanticModel, Func<SyntaxNode, bool> expandInsideNode, CancellationToken cancellationToken);

        public async Task<Document> ReduceAsync(Document document, IEnumerable<TextSpan> spans, OptionSet optionSet = null, IEnumerable<AbstractReducer> reducers = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Logger.LogBlock(FunctionId.Simplifier_ReduceAsync, cancellationToken))
            {
                var spanList = spans?.ToList() ?? new List<TextSpan>();

                // we have no span
                if (!spanList.Any())
                {
                    return document;
                }

                optionSet = optionSet ?? document.Project.Solution.Workspace.Options;

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // Chaining of the Speculative SemanticModel (i.e. Generating a speculative SemanticModel from an existing Speculative SemanticModel) is not supported
                // Hence make sure we always start working off of the actual SemanticModel instead of a speculative SemanticModel.
                Contract.Assert(!semanticModel.IsSpeculativeSemanticModel);

                var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);

#if DEBUG
                bool originalDocHasErrors = await document.HasAnyErrorsAsync(cancellationToken).ConfigureAwait(false);
#endif

                var reduced = await this.ReduceAsyncInternal(document, spanList, optionSet, reducers, cancellationToken).ConfigureAwait(false);

                if (reduced != document)
                {
#if DEBUG
                    if (!originalDocHasErrors)
                    {
                        await reduced.VerifyNoErrorsAsync("Error introduced by Simplification Service", cancellationToken).ConfigureAwait(false);
                    }
#endif
                }

                return reduced;
            }
        }

        private async Task<Document> ReduceAsyncInternal(
            Document document,
            List<TextSpan> spans,
            OptionSet optionSet,
            IEnumerable<AbstractReducer> reducers,
            CancellationToken cancellationToken)
        {
            // Create a simple interval tree for simplification spans.
            var spansTree = new SimpleIntervalTree<TextSpan>(TextSpanIntervalIntrospector.Instance, spans);

            Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpans = (nodeOrToken) =>
                !spansTree.GetOverlappingIntervals(nodeOrToken.FullSpan.Start, nodeOrToken.FullSpan.Length).Any();

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);

            // prep namespace imports marked for simplification 
            var removeIfUnusedAnnotation = new SyntaxAnnotation();
            var originalRoot = root;
            root = this.PrepareNamespaceImportsForRemovalIfUnused(document, root, removeIfUnusedAnnotation, isNodeOrTokenOutsideSimplifySpans);
            var hasImportsToSimplify = root != originalRoot;

            if (hasImportsToSimplify)
            {
                document = document.WithSyntaxRoot(root);
                semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
            }

            // Get the list of syntax nodes and tokens that need to be reduced.
            var nodesAndTokensToReduce = this.GetNodesAndTokensToReduce(root, isNodeOrTokenOutsideSimplifySpans);

            if (nodesAndTokensToReduce.Any())
            {
                if (reducers == null)
                {
                    reducers = this.GetReducers();
                }

                var reducedNodesMap = new ConcurrentDictionary<SyntaxNode, SyntaxNode>();
                var reducedTokensMap = new ConcurrentDictionary<SyntaxToken, SyntaxToken>();

                // Reduce all the nodesAndTokensToReduce using the given reducers/rewriters and
                // store the reduced nodes and/or tokens in the reduced nodes/tokens maps.
                // Note that this method doesn't update the original syntax tree.
                await this.ReduceAsync(document, root, nodesAndTokensToReduce, reducers, optionSet, semanticModel, reducedNodesMap, reducedTokensMap, cancellationToken).ConfigureAwait(false);

                if (reducedNodesMap.Any() || reducedTokensMap.Any())
                {
                    // Update the syntax tree with reduced nodes/tokens.
                    root = root.ReplaceSyntax(
                        nodes: reducedNodesMap.Keys,
                        computeReplacementNode: (o, n) => TransformReducedNode(reducedNodesMap[o], n),
                        tokens: reducedTokensMap.Keys,
                        computeReplacementToken: (o, n) => reducedTokensMap[o],
                        trivia: SpecializedCollections.EmptyEnumerable<SyntaxTrivia>(),
                        computeReplacementTrivia: null);

                    document = document.WithSyntaxRoot(root);
                }
            }

            if (hasImportsToSimplify)
            {
                // remove any unused namespace imports that were marked for simplification
                document = await this.RemoveUnusedNamespaceImportsAsync(document, removeIfUnusedAnnotation, cancellationToken).ConfigureAwait(false);
            }

            return document;
        }

        private Task ReduceAsync(
            Document document,
            SyntaxNode root,
            ImmutableArray<NodeOrTokenToReduce> nodesAndTokensToReduce,
            IEnumerable<AbstractReducer> reducers,
            OptionSet optionSet,
            SemanticModel semanticModel,
            ConcurrentDictionary<SyntaxNode, SyntaxNode> reducedNodesMap,
            ConcurrentDictionary<SyntaxToken, SyntaxToken> reducedTokensMap,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(nodesAndTokensToReduce.Any());

            // Reduce each node or token in the given list by running it through each reducer.
            var simplifyTasks = new Task[nodesAndTokensToReduce.Length];
            for (int i = 0; i < nodesAndTokensToReduce.Length; i++)
            {
                var nodeOrTokenToReduce = nodesAndTokensToReduce[i];
                simplifyTasks[i] = Task.Run(async () =>
            {
                var nodeOrToken = nodeOrTokenToReduce.OriginalNodeOrToken;
                var simplifyAllDescendants = nodeOrTokenToReduce.SimplifyAllDescendants;
                var semanticModelForReduce = semanticModel;
                var currentNodeOrToken = nodeOrTokenToReduce.NodeOrToken;
                var isNode = nodeOrToken.IsNode;

                foreach (var reducer in reducers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rewriter = reducer.CreateExpressionRewriter(optionSet, cancellationToken);

                    do
                    {
                        if (currentNodeOrToken.SyntaxTree != semanticModelForReduce.SyntaxTree)
                        {
                            // currentNodeOrToken was simplified either by a previous reducer or
                            // a previous iteration of the current reducer.
                            // Create a speculative semantic model for the simplified node for semantic queries.

                            // Certain node kinds (expressions/statements) require non-null parent nodes during simplification.
                            // However, the reduced nodes haven't been parented yet, so do the required parenting using the original node's parent.
                            if (currentNodeOrToken.Parent == null &&
                                nodeOrToken.Parent != null &&
                                (currentNodeOrToken.IsToken ||
                                currentNodeOrToken.AsNode() is TExpressionSyntax ||
                                currentNodeOrToken.AsNode() is TStatementSyntax ||
                                currentNodeOrToken.AsNode() is TCrefSyntax))
                            {
                                var annotation = new SyntaxAnnotation();
                                currentNodeOrToken = currentNodeOrToken.WithAdditionalAnnotations(annotation);

                                var replacedParent = isNode ?
                                    nodeOrToken.Parent.ReplaceNode(nodeOrToken.AsNode(), currentNodeOrToken.AsNode()) :
                                    nodeOrToken.Parent.ReplaceToken(nodeOrToken.AsToken(), currentNodeOrToken.AsToken());

                                currentNodeOrToken = replacedParent
                                    .ChildNodesAndTokens()
                                    .Single((c) => c.HasAnnotation(annotation));
                            }

                            if (isNode)
                            {
                                var currentNode = currentNodeOrToken.AsNode();
                                if (this.CanNodeBeSimplifiedWithoutSpeculation(nodeOrToken.AsNode()))
                                {
                                    // Since this node cannot be speculated, we are replacing the Document with the changes and get a new SemanticModel
                                    SyntaxAnnotation marker = new SyntaxAnnotation();
                                    var newRoot = root.ReplaceNode(nodeOrToken.AsNode(), currentNode.WithAdditionalAnnotations(marker));
                                    var newDocument = document.WithSyntaxRoot(newRoot);
                                    semanticModelForReduce = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                                    newRoot = await semanticModelForReduce.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                                    currentNodeOrToken = newRoot.DescendantNodes().Single((c) => c.HasAnnotation(marker));
                                }
                                else
                                {
                                    // Create speculative semantic model for simplified node.
                                    semanticModelForReduce = GetSpeculativeSemanticModel(ref currentNode, semanticModel, nodeOrToken.AsNode());
                                    currentNodeOrToken = currentNode;
                                }
                            }
                        }

                        // Reduce the current node or token.
                        currentNodeOrToken = rewriter.VisitNodeOrToken(currentNodeOrToken, semanticModelForReduce, simplifyAllDescendants);
                    }
                    while (rewriter.HasMoreWork);
                }

                // If nodeOrToken was simplified, add it to the appropriate dictionary of replaced nodes/tokens.
                if (currentNodeOrToken != nodeOrToken)
                {
                    if (isNode)
                    {
                        reducedNodesMap[nodeOrToken.AsNode()] = currentNodeOrToken.AsNode();
                    }
                    else
                    {
                        reducedTokensMap[nodeOrToken.AsToken()] = currentNodeOrToken.AsToken();
                    }
                }
            }, cancellationToken);
            }

            return Task.WhenAll(simplifyTasks);
        }

        // find any namespace imports / using directives marked for simplification in the specified spans
        // and add removeIfUnused annotation
        private SyntaxNode PrepareNamespaceImportsForRemovalIfUnused(
            Document document,
            SyntaxNode root,
            SyntaxAnnotation removeIfUnusedAnnotation,
            Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpan)
        {
            var gen = SyntaxGenerator.GetGenerator(document);

            var importsToSimplify = root.DescendantNodes().Where(n =>
                !isNodeOrTokenOutsideSimplifySpan(n)
                && gen.GetDeclarationKind(n) == DeclarationKind.NamespaceImport
                && n.HasAnnotation(Simplifier.Annotation));

            return root.ReplaceNodes(importsToSimplify, (o, r) => r.WithAdditionalAnnotations(removeIfUnusedAnnotation));
        }

        private async Task<Document> RemoveUnusedNamespaceImportsAsync(
            Document document,
            SyntaxAnnotation removeIfUnusedAnnotation,
            CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = model.SyntaxTree.GetRoot();
            var addedImports = root.GetAnnotatedNodes(removeIfUnusedAnnotation);
            var unusedImports = new HashSet<SyntaxNode>();
            this.GetUnusedNamespaceImports(model, unusedImports, cancellationToken);

            // only remove the unused imports that we added
            unusedImports.IntersectWith(addedImports);

            if (unusedImports.Count > 0)
            {
                var gen = SyntaxGenerator.GetGenerator(document);
                var newRoot = gen.RemoveNodes(root, unusedImports);
                return document.WithSyntaxRoot(newRoot);
            }
            else
            {
                return document;
            }
        }

        protected abstract void GetUnusedNamespaceImports(SemanticModel model, HashSet<SyntaxNode> namespaceImports, CancellationToken cancellationToken);
    }

    internal struct NodeOrTokenToReduce
    {
        public readonly SyntaxNodeOrToken NodeOrToken;
        public readonly bool SimplifyAllDescendants;
        public readonly SyntaxNodeOrToken OriginalNodeOrToken;
        public readonly bool CanBeSpeculated;

        public NodeOrTokenToReduce(SyntaxNodeOrToken nodeOrToken, bool simplifyAllDescendants, SyntaxNodeOrToken originalNodeOrToken, bool canBeSpeculated = true)
        {
            this.NodeOrToken = nodeOrToken;
            this.SimplifyAllDescendants = simplifyAllDescendants;
            this.OriginalNodeOrToken = originalNodeOrToken;
            this.CanBeSpeculated = canBeSpeculated;
        }
    }
}
