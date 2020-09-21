// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SemanticModelReuse
{
    internal abstract class AbstractSemanticModelReuseLanguageService<
        TMemberDeclarationSyntax,
        TBaseMethodDeclarationSyntax,
        TBasePropertyDeclarationSyntax,
        TAccessorDeclarationSyntax> : ISemanticModelReuseLanguageService
        where TMemberDeclarationSyntax : SyntaxNode
        where TBaseMethodDeclarationSyntax : TMemberDeclarationSyntax
        where TBasePropertyDeclarationSyntax : TMemberDeclarationSyntax
        where TAccessorDeclarationSyntax : SyntaxNode
    {
        protected abstract ISyntaxFacts SyntaxFacts { get; }

        public abstract SyntaxNode? TryGetContainingMethodBodyForSpeculation(SyntaxNode node);

        protected abstract Task<SemanticModel?> TryGetSpeculativeSemanticModelWorkerAsync(SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken);
        protected abstract SyntaxList<TAccessorDeclarationSyntax> GetAccessors(TBasePropertyDeclarationSyntax baseProperty);
        protected abstract TBasePropertyDeclarationSyntax GetBasePropertyDeclaration(TAccessorDeclarationSyntax accessor);

        public Task<SemanticModel?> TryGetSpeculativeSemanticModelAsync(SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken)
        {
            var previousSyntaxTree = previousSemanticModel.SyntaxTree;
            var currentSyntaxTree = currentBodyNode.SyntaxTree;

            // This operation is only valid if top-level equivalent trees were passed in.
            Contract.ThrowIfFalse(previousSyntaxTree.IsEquivalentTo(currentSyntaxTree, topLevel: true));
            return TryGetSpeculativeSemanticModelWorkerAsync(previousSemanticModel, currentBodyNode, cancellationToken);
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
                var currentMembers = this.SyntaxFacts.GetMethodLevelMembers(currentRoot);
                var index = currentMembers.IndexOf(currentBodyNode);
                if (index < 0)
                {
                    Debug.Fail($"Unhandled member type in {nameof(GetPreviousBodyNode)}");
                    return null;
                }

                var previousMembers = this.SyntaxFacts.GetMethodLevelMembers(previousRoot);
                if (currentMembers.Count != previousMembers.Count)
                {
                    Debug.Fail("Member count shouldn't have changed as there were no top level edits.");
                    return null;
                }

                return previousMembers[index];
            }
        }
    }
}
