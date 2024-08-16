// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.SemanticModelReuse;

internal abstract class AbstractSemanticModelReuseLanguageService<
    TMemberDeclarationSyntax,
    TBasePropertyDeclarationSyntax,
    TAccessorDeclarationSyntax> : ISemanticModelReuseLanguageService, IDisposable
    where TMemberDeclarationSyntax : SyntaxNode
    where TBasePropertyDeclarationSyntax : TMemberDeclarationSyntax
    where TAccessorDeclarationSyntax : SyntaxNode
{
    private readonly CountLogAggregator<bool> _logAggregator = new();

    protected abstract ISyntaxFacts SyntaxFacts { get; }

    public abstract SyntaxNode? TryGetContainingMethodBodyForSpeculation(SyntaxNode node);

    protected abstract SemanticModel? TryGetSpeculativeSemanticModelWorker(SemanticModel previousSemanticModel, SyntaxNode previousBodyNode, SyntaxNode currentBodyNode);
    protected abstract SyntaxList<TAccessorDeclarationSyntax> GetAccessors(TBasePropertyDeclarationSyntax baseProperty);
    protected abstract TBasePropertyDeclarationSyntax GetBasePropertyDeclaration(TAccessorDeclarationSyntax accessor);

    public void Dispose()
    {
        Logger.Log(FunctionId.SemanticModelReuseLanguageService_TryGetSpeculativeSemanticModelAsync_Equivalent, KeyValueLogMessage.Create(m =>
        {
            foreach (var kv in _logAggregator)
                m[kv.Key.ToString()] = kv.Value.GetCount();
        }));
    }

    public async Task<SemanticModel?> TryGetSpeculativeSemanticModelAsync(SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken)
    {
        var previousSyntaxTree = previousSemanticModel.SyntaxTree;
        var currentSyntaxTree = currentBodyNode.SyntaxTree;

        // This operation is only valid if top-level equivalent trees were passed in.  If they're not equivalent
        // then something very bad happened as we did that document.Project.GetDependentSemanticVersionAsync was
        // still the same.  Log information so we can be alerted if this isn't being as successful as we expect.
        var isEquivalentTo = previousSyntaxTree.IsEquivalentTo(currentSyntaxTree, topLevel: true);
        _logAggregator.IncreaseCount(isEquivalentTo);

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
        if (previousBodyNode.SpanStart != currentBodyNode.SpanStart)
            return null;

        return TryGetSpeculativeSemanticModelWorker(previousSemanticModel, previousBodyNode, currentBodyNode);
    }

    protected SyntaxNode GetPreviousBodyNode(SyntaxNode previousRoot, SyntaxNode currentRoot, SyntaxNode currentBodyNode)
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
            using var pooledCurrentMembers = SharedPools.Default<List<SyntaxNode>>().GetPooledObject();
            var currentMembers = pooledCurrentMembers.Object;

            this.SyntaxFacts.AddMethodLevelMembers(currentRoot, currentMembers);
            var index = currentMembers.IndexOf(currentBodyNode);
            if (index < 0)
            {
                Debug.Fail($"Unhandled member type in {nameof(GetPreviousBodyNode)}");
                return null;
            }

            using var pooledPreviousMembers = SharedPools.Default<List<SyntaxNode>>().GetPooledObject();
            var previousMembers = pooledPreviousMembers.Object;

            this.SyntaxFacts.AddMethodLevelMembers(previousRoot, previousMembers);
            if (currentMembers.Count != previousMembers.Count)
            {
                Debug.Fail("Member count shouldn't have changed as there were no top level edits.");
                return null;
            }

            return previousMembers[index];
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
