// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.SemanticModelReuse
{
    internal abstract class AbstractSemanticModelReuseLanguageService : ISemanticModelReuseLanguageService
    {
        protected abstract ISyntaxFacts SyntaxFacts { get; }

        public abstract SyntaxNode? TryGetContainingMethodBodyForSpeculation(SyntaxNode node);
        public abstract Task<SemanticModel?> TryGetSpeculativeSemanticModelAsync(SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken);

        protected virtual SyntaxNode? GetPreviousBodyNode(SyntaxNode previousRoot, SyntaxNode currentRoot, SyntaxNode currentBodyNode)
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
