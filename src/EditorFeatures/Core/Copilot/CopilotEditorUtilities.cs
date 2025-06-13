// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Proposals;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class CopilotEditorUtilities
{
    public static Solution? TryGetAffectedSolution(ProposalBase proposal)
    {
        Solution? solution = null;
        foreach (var edit in proposal.Edits)
        {
            var document = edit.Span.Snapshot.GetOpenDocumentInCurrentContextWithChanges();

            // Edit touches a file roslyn doesn't know about.  Don't touch this.
            if (document is null)
                return null;

            // Only both for languages we can actually process semantics for.
            if (document.SupportsSemanticModel)
                return null;

            var currentSolution = document.Project.Solution;

            // Edit touches multiple solutions.  Don't bother with this for now for simplicities sake.
            if (solution != null && solution != currentSolution)
                return null;

            solution = currentSolution;
        }

        return solution;
    }
}
