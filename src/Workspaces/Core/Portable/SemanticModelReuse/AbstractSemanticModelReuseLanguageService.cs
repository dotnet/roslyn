// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;

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
        /// <summary>
        /// Used to make sure we only report one watson per sessoin here.
        /// </summary>
        private static bool s_watsonReported;

        protected abstract ISyntaxFacts SyntaxFacts { get; }

        public abstract SyntaxNode? TryGetContainingMethodBodyForSpeculation(SyntaxNode node);

        protected abstract Task<SemanticModel?> TryGetSpeculativeSemanticModelWorkerAsync(SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken);
        protected abstract SyntaxList<TAccessorDeclarationSyntax> GetAccessors(TBasePropertyDeclarationSyntax baseProperty);
        protected abstract TBasePropertyDeclarationSyntax GetBasePropertyDeclaration(TAccessorDeclarationSyntax accessor);

        public async Task<SemanticModel?> TryGetSpeculativeSemanticModelAsync(SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken)
        {
            var previousSyntaxTree = previousSemanticModel.SyntaxTree;
            var currentSyntaxTree = currentBodyNode.SyntaxTree;

            // This operation is only valid if top-level equivalent trees were passed in.  If they're not equivalent
            // then something very bad happened as we did that document.Project.GetDependentSemanticVersionAsync was
            // still the same.  So somehow w don't have top-level equivalence, but we do have the same semantic version.
            //
            // log a NFW to help diagnose what the source looks like as it may help us determine what sort of edit is
            // causing this.
            if (!previousSyntaxTree.IsEquivalentTo(currentSyntaxTree, topLevel: true))
            {
                if (!s_watsonReported)
                {
                    s_watsonReported = true;

                    try
                    {
                        throw new InvalidOperationException(
                            $@"Syntax trees should have been equivalent.
---
{previousSyntaxTree.GetText(CancellationToken.None)}
---
{currentSyntaxTree.GetText(CancellationToken.None)}
---");

                    }
                    catch (Exception e) when (FatalError.ReportAndCatch(e))
                    {
                    }
                }

                return null;
            }

            return await TryGetSpeculativeSemanticModelWorkerAsync(
                previousSemanticModel, currentBodyNode, cancellationToken).ConfigureAwait(false);
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
