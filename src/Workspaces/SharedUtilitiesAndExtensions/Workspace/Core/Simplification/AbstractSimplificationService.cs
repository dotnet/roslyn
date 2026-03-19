// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Simplification;

internal abstract class AbstractSimplificationService<
        TCompilationUnitSyntax,
        TExpressionSyntax,
        TStatementSyntax,
        TCrefSyntax>(ImmutableArray<AbstractReducer> reducers) : ISimplificationService
    where TCompilationUnitSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TCrefSyntax : SyntaxNode
{
    protected static readonly Func<SyntaxNode, bool> s_containsAnnotations = n => n.ContainsAnnotations;
    protected static readonly Func<SyntaxNodeOrToken, bool> s_hasSimplifierAnnotation = n => n.HasAnnotation(Simplifier.Annotation);

    private readonly ImmutableArray<AbstractReducer> _reducers = reducers;

    protected abstract ImmutableArray<NodeOrTokenToReduce> GetNodesAndTokensToReduce(SyntaxNode root, Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpans);
    protected abstract SemanticModel GetSpeculativeSemanticModel(ref SyntaxNode nodeToSpeculate, SemanticModel originalSemanticModel, SyntaxNode originalNode);
    protected abstract bool NodeRequiresNonSpeculativeSemanticModel(SyntaxNode node);
    protected abstract void AddImportDeclarations(TCompilationUnitSyntax root, ArrayBuilder<SyntaxNode> importDeclarations);

    public abstract SimplifierOptions DefaultOptions { get; }
    public abstract SimplifierOptions GetSimplifierOptions(IOptionsReader options);

    protected virtual SyntaxNode TransformReducedNode(SyntaxNode reducedNode, SyntaxNode originalNode)
        => reducedNode;

    public abstract SyntaxNode Expand(SyntaxNode node, SemanticModel semanticModel, SyntaxAnnotation? annotationForReplacedAliasIdentifier, Func<SyntaxNode, bool>? expandInsideNode, bool expandParameter, CancellationToken cancellationToken);
    public abstract SyntaxToken Expand(SyntaxToken token, SemanticModel semanticModel, Func<SyntaxNode, bool>? expandInsideNode, CancellationToken cancellationToken);

    public async Task<Document> ReduceAsync(
        Document document,
        ImmutableArray<TextSpan> spans,
        SimplifierOptions options,
        ImmutableArray<AbstractReducer> reducers = default,
        CancellationToken cancellationToken = default)
    {
        using (Logger.LogBlock(FunctionId.Simplifier_ReduceAsync, cancellationToken))
        {
            var spanList = spans.NullToEmpty();

            // we have no span
            if (!spanList.Any())
                return document;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Chaining of the Speculative SemanticModel (i.e. Generating a speculative SemanticModel from an existing
            // Speculative SemanticModel) is not supported Hence make sure we always start working off of the actual
            // SemanticModel instead of a speculative SemanticModel.
            Debug.Assert(!semanticModel.IsSpeculativeSemanticModel);

            reducers = reducers.IsDefault ? _reducers : reducers;
            // Take out any reducers that don't even apply with the current
            // set of users options. i.e. no point running 'reduce to var'
            // if the user doesn't have the 'var' preference set.
            reducers = reducers.WhereAsArray(r => r.IsApplicable(options));

            return await this.ReduceCoreAsync(document, spanList, options, reducers, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<Document> ReduceCoreAsync(
        Document document,
        ImmutableArray<TextSpan> spans,
        SimplifierOptions options,
        ImmutableArray<AbstractReducer> reducers,
        CancellationToken cancellationToken)
    {
        // Create a simple interval tree for simplification spans.
        var spansTree = new TextSpanMutableIntervalTree(spans);

        var root = (TCompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // prep namespace imports marked for simplification 
        var removeIfUnusedAnnotation = new SyntaxAnnotation();
        var originalRoot = root;
        root = PrepareNamespaceImportsForRemovalIfUnused(root, removeIfUnusedAnnotation, IsNodeOrTokenOutsideSimplifySpans);
        var hasImportsToSimplify = root != originalRoot;

        if (hasImportsToSimplify)
        {
            document = document.WithSyntaxRoot(root);
            root = (TCompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        }

        // Get the list of syntax nodes and tokens that need to be reduced.
        var nodesAndTokensToReduce = this.GetNodesAndTokensToReduce(root, IsNodeOrTokenOutsideSimplifySpans);

        if (nodesAndTokensToReduce.Any())
        {
            var reducedNodesMap = new ConcurrentDictionary<SyntaxNode, SyntaxNode>();
            var reducedTokensMap = new ConcurrentDictionary<SyntaxToken, SyntaxToken>();

            // Reduce all the nodesAndTokensToReduce using the given reducers/rewriters and
            // store the reduced nodes and/or tokens in the reduced nodes/tokens maps.
            // Note that this method doesn't update the original syntax tree.
            await this.ReduceAsync(document, root, nodesAndTokensToReduce, reducers, options, reducedNodesMap, reducedTokensMap, cancellationToken).ConfigureAwait(false);

            if (!reducedNodesMap.IsEmpty || !reducedTokensMap.IsEmpty)
            {
                // Update the syntax tree with reduced nodes/tokens.
                root = root.ReplaceSyntax(
                    nodes: reducedNodesMap.Keys,
                    computeReplacementNode: (o, n) => TransformReducedNode(reducedNodesMap[o], n),
                    tokens: reducedTokensMap.Keys,
                    computeReplacementToken: (o, n) => reducedTokensMap[o],
                    trivia: [],
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

        bool IsNodeOrTokenOutsideSimplifySpans(SyntaxNodeOrToken nodeOrToken)
            => !spansTree.HasIntervalThatOverlapsWith(nodeOrToken.FullSpan.Start, nodeOrToken.FullSpan.Length);
    }

    private async Task ReduceAsync(
        Document document,
        SyntaxNode root,
        ImmutableArray<NodeOrTokenToReduce> nodesAndTokensToReduce,
        ImmutableArray<AbstractReducer> reducers,
        SimplifierOptions options,
        ConcurrentDictionary<SyntaxNode, SyntaxNode> reducedNodesMap,
        ConcurrentDictionary<SyntaxToken, SyntaxToken> reducedTokensMap,
        CancellationToken cancellationToken)
    {
        // Debug flag to help processing things serially instead of parallel.
        var executeSerially = Debugger.IsAttached;

        Contract.ThrowIfFalse(nodesAndTokensToReduce.Any());

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Reduce each node or token in the given list by running it through each reducer.
        if (executeSerially)
        {
            foreach (var nodeOrTokenToReduce in nodesAndTokensToReduce)
                await ReduceOneNodeOrTokenAsync(nodeOrTokenToReduce, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await Parallel.ForEachAsync(
                source: nodesAndTokensToReduce,
                cancellationToken,
                ReduceOneNodeOrTokenAsync).ConfigureAwait(false);
        }

        return;

        async ValueTask ReduceOneNodeOrTokenAsync(
            NodeOrTokenToReduce nodeOrTokenToReduce, CancellationToken cancellationToken)
        {
            // Reduce each node or token in the given list by running it through each reducer.

            var nodeOrToken = nodeOrTokenToReduce.OriginalNodeOrToken;
            var simplifyAllDescendants = nodeOrTokenToReduce.SimplifyAllDescendants;
            var currentNodeOrToken = nodeOrTokenToReduce.NodeOrToken;
            var semanticModelForReduce = semanticModel;
            var isNode = nodeOrToken.IsNode;

            foreach (var reducer in reducers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var rewriter = reducer.GetOrCreateRewriter();
                rewriter.Initialize(document.Project.ParseOptions!, options, cancellationToken);

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
                            (currentNodeOrToken.IsToken || currentNodeOrToken.AsNode() is TExpressionSyntax or TStatementSyntax or TCrefSyntax))
                        {
                            var annotation = new SyntaxAnnotation();
                            currentNodeOrToken = currentNodeOrToken.WithAdditionalAnnotations(annotation);

                            var replacedParent = isNode
                                ? nodeOrToken.Parent.ReplaceNode(nodeOrToken.AsNode()!, currentNodeOrToken.AsNode()!)
                                : nodeOrToken.Parent.ReplaceToken(nodeOrToken.AsToken(), currentNodeOrToken.AsToken());

                            currentNodeOrToken = replacedParent
                                .ChildNodesAndTokens()
                                .Single(c => c.HasAnnotation(annotation));
                        }

                        if (isNode)
                        {
                            var currentNode = currentNodeOrToken.AsNode()!;
                            if (this.NodeRequiresNonSpeculativeSemanticModel(nodeOrToken.AsNode()!))
                            {
                                // Since this node cannot be speculated, we are replacing the Document with the changes and get a new SemanticModel
                                var marker = new SyntaxAnnotation();
                                var newRoot = root.ReplaceNode(nodeOrToken.AsNode()!, currentNode.WithAdditionalAnnotations(marker));
                                var newDocument = document.WithSyntaxRoot(newRoot);
                                semanticModelForReduce = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                                newRoot = await semanticModelForReduce.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                                currentNodeOrToken = newRoot.DescendantNodes().Single(c => c.HasAnnotation(marker));
                            }
                            else
                            {
                                // Create speculative semantic model for simplified node.
                                semanticModelForReduce = GetSpeculativeSemanticModel(ref currentNode, semanticModel, nodeOrToken.AsNode()!);
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
                    reducedNodesMap[nodeOrToken.AsNode()!] = currentNodeOrToken.AsNode()!;
                }
                else
                {
                    reducedTokensMap[nodeOrToken.AsToken()] = currentNodeOrToken.AsToken();
                }
            }
        }
    }

    // find any namespace imports / using directives marked for simplification in the specified spans
    // and add removeIfUnused annotation
    private TCompilationUnitSyntax PrepareNamespaceImportsForRemovalIfUnused(
        TCompilationUnitSyntax root,
        SyntaxAnnotation removeIfUnusedAnnotation,
        Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpan)
    {
        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var importDeclarations);

        this.AddImportDeclarations(root, importDeclarations);

        return root.ReplaceNodes(
            importDeclarations.Where(n => !isNodeOrTokenOutsideSimplifySpan(n) && n.HasAnnotation(Simplifier.Annotation)),
            (o, r) => r.WithAdditionalAnnotations(removeIfUnusedAnnotation));
    }

    private async Task<Document> RemoveUnusedNamespaceImportsAsync(
        Document document,
        SyntaxAnnotation removeIfUnusedAnnotation,
        CancellationToken cancellationToken)
    {
        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
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
