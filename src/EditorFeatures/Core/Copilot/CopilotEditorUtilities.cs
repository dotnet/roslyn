// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
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

            // Only bother for languages we can actually process semantics for.
            if (document.SupportsSemanticModel)
                return null;

            var currentSolution = document.Project.Solution;

            // Edit touches multiple solutions.  Don't bother with this for now for simplicity's sake.
            if (solution != null && solution != currentSolution)
                return null;

            solution = currentSolution;
        }

        return solution;
    }

    public static ImmutableArray<TextChange> TryGetNormalizedTextChanges(IEnumerable<ProposedEdit> edits)
    {
        using var _ = ArrayBuilder<TextChange>.GetInstance(out var textChanges);
        foreach (var edit in edits)
            textChanges.Add(new TextChange(edit.Span.Span.ToTextSpan(), edit.ReplacementText));

        return CopilotUtilities.TryNormalizeCopilotTextChanges(textChanges);
    }
}
