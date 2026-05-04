// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;

#if WORKSPACE
using Microsoft.CodeAnalysis.Internal.Log;
#endif

namespace Microsoft.CodeAnalysis.SemanticModelReuse;

internal abstract class AbstractSemanticModelReuseLanguageService<
    TMemberDeclarationSyntax,
    TBasePropertyDeclarationSyntax,
    TAccessorDeclarationSyntax> : ISemanticModelReuseLanguageService, IDisposable
    where TMemberDeclarationSyntax : SyntaxNode
    where TBasePropertyDeclarationSyntax : TMemberDeclarationSyntax
    where TAccessorDeclarationSyntax : SyntaxNode
{
#if WORKSPACE
    private readonly CountLogAggregator<bool> _logAggregator = new();
#endif

    protected abstract ISyntaxFacts SyntaxFacts { get; }

    public abstract SyntaxNode? TryGetContainingMethodBodyForSpeculation(SyntaxNode node);

    protected abstract SemanticModel? TryGetSpeculativeSemanticModelWorker(SemanticModel previousSemanticModel, SyntaxNode previousBodyNode, SyntaxNode currentBodyNode);
    protected abstract SyntaxList<TAccessorDeclarationSyntax> GetAccessors(TBasePropertyDeclarationSyntax baseProperty);
    protected abstract TBasePropertyDeclarationSyntax GetBasePropertyDeclaration(TAccessorDeclarationSyntax accessor);

    public void Dispose()
    {
#if WORKSPACE
        Logger.Log(FunctionId.SemanticModelReuseLanguageService_TryGetSpeculativeSemanticModelAsync_Equivalent, KeyValueLogMessage.Create(static (m, _logAggregator) =>
        {
            foreach (var kv in _logAggregator)
                m[kv.Key.ToString()] = kv.Value.GetCount();
        }, _logAggregator));
#endif
    }

    public async Task<SemanticModel?> TryGetSpeculativeSemanticModelAsync(SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken)
    {
        var previousSyntaxTree = previousSemanticModel.SyntaxTree;
        var currentSyntaxTree = currentBodyNode.SyntaxTree;

        // This operation is only valid if top-level equivalent trees were passed in.  If they're not equivalent
        // then something very bad happened as we did that document.Project.GetDependentSemanticVersionAsync was
        // still the same.  Log information so we can be alerted if this isn't being as successful as we expect.
        var isEquivalentTo = previousSyntaxTree.IsEquivalentTo(currentSyntaxTree, topLevel: true);

#if WORKSPACE
        _logAggregator.IncreaseCount(isEquivalentTo);
#endif

        if (!isEquivalentTo)
            return null;

        var previousRoot = await previousSemanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var currentRoot = await currentBodyNode.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var previousBodyNode = GetPreviousBodyNode(previousRoot, currentRoot, currentBodyNode);

        // Trivia is ignore when comparing two trees for equivalence at top level, since it has no effect to API shape
        // and it'd be safe to drop in the new method body as long as the shape doesn't change. However, trivia changes
        // around the method do make it tricky to decide whether a position is safe for speculation.

        // class C { void M() { return; } }";
        //                    ^ this is the position used to set OriginalPositionForSpeculation when creating the speculative model.
        //
        // class C {            void M() { return null; } }";
        //                               ^ it's unsafe to use the speculative model at this position, even though it's part of the
        //                                 method body and after OriginalPositionForSpeculation. 

        // Given that the common use case for us is continuously editing/typing inside a method body, we believe we can be conservative
        // in creating speculative model with those kind of trivia change, by requiring the method body block not to shift position,
        // w/o sacrificing performance in those common scenarios.
        if (previousBodyNode?.SpanStart != currentBodyNode.SpanStart)
            return null;

        return TryGetSpeculativeSemanticModelWorker(previousSemanticModel, previousBodyNode, currentBodyNode);
    }

    protected SyntaxNode? GetPreviousBodyNode(SyntaxNode previousRoot, SyntaxNode currentRoot, SyntaxNode currentBodyNode)
    {
        if (currentBodyNode is TAccessorDeclarationSyntax currentAccessor)
        {
            // in the case of an accessor, have to find the previous accessor in the previous prop/event corresponding
            // to the current prop/event.

            var currentContainer = GetBasePropertyDeclaration(currentAccessor);
            var previousContainer = GetPreviousBodyNode(previousRoot, currentRoot, currentContainer);

            if (previousContainer is not TBasePropertyDeclarationSyntax previousMember)
            {
                Debug.Fail("Previous container didn't map back to a normal accessor container.");
                return null;
            }

            var currentAccessors = GetAccessors(currentContainer);
            var previousAccessors = GetAccessors(previousMember);

            if (currentAccessors.Count != previousAccessors.Count)
            {
                Debug.Fail("Accessor count shouldn't have changed as there were no top level edits.");
                return null;
            }

            return previousAccessors[currentAccessors.IndexOf(currentAccessor)];
        }
        else
        {
            // Walk up the ancestor nodes of currentBodyNode, finding child indexes up to the root.
            using var _ = ArrayBuilder<int>.GetInstance(out var indexPath);
            GetNodeChildIndexPathToRootReversed(currentBodyNode, indexPath);

            // Then use those indexes to walk back down the previous tree to find the equivalent node.
            var previousNode = previousRoot;
            for (var i = indexPath.Count - 1; i >= 0; i--)
            {
                var childIndex = indexPath[i];
                var children = previousNode.ChildNodesAndTokens();

                if (children.Count <= childIndex)
                {
                    Debug.Fail("Member count shouldn't have changed as there were no top level edits.");
                    return null;
                }

                var childAsNode = children[childIndex].AsNode();
                if (childAsNode is null)
                {
                    Debug.Fail("Child at indicated index should be a node as there were no top level edits.");
                    return null;
                }

                previousNode = childAsNode;
            }

            return previousNode;
        }
    }

    private static void GetNodeChildIndexPathToRootReversed(SyntaxNode node, ArrayBuilder<int> path)
    {
        var current = node;
        var parent = current.Parent;

        while (parent != null)
        {
            var childIndex = 0;
            foreach (var child in parent.ChildNodesAndTokens())
            {
                if (child.AsNode() == current)
                {
                    path.Add(childIndex);
                    break;
                }

                childIndex++;
            }

            current = parent;
            parent = current.Parent;
        }
    }

    private sealed class NonEquivalentTreeException : Exception
    {
        // Used for analyzing dumps
#pragma warning disable IDE0052 // Remove unread private members
        private readonly SyntaxTree _originalSyntaxTree;
        private readonly SyntaxTree _updatedSyntaxTree;
#pragma warning restore IDE0052 // Remove unread private members

        public NonEquivalentTreeException(string message, SyntaxTree originalSyntaxTree, SyntaxTree updatedSyntaxTree)
            : base(message)
        {
            _originalSyntaxTree = originalSyntaxTree;
            _updatedSyntaxTree = updatedSyntaxTree;
        }
    }
}
