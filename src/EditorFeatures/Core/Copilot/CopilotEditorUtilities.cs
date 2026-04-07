// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class CopilotEditorUtilities
{
    /// <summary>
    /// Returns the single roslyn solution snapshot that is affected by the edits in the proposal.
    /// </summary>
    /// <remarks>
    /// Will fail in the event that the edits touch multiple workspaces, or if the edits touch
    /// multiple versions of a solution in a workspace (for example, edits to different versions of
    /// the same <see cref="ITextSnapshot"/>. Will also fail if this edits non-roslyn files, or files
    /// we cannot process semantics for (like non C#/VB files).
    /// </remarks>
    public static (Solution? affectedSolution, string? failureReason) TryGetAffectedSolution(ProposalBase proposal)
    {
        Solution? solution = null;
        foreach (var edit in proposal.Edits)
        {
            var document = edit.Span.Snapshot.GetOpenDocumentInCurrentContextWithChanges();

            // Edit touches a file roslyn doesn't know about.  Don't touch this.
            if (document is null)
                return (null, "NonRoslynDocumentAffected");

            // Only bother for languages we can actually process semantics for.
            if (!document.SupportsSemanticModel)
                return (null, "NonSemanticDocumentAffected");

            var currentSolution = document.Project.Solution;

            // Edit touches multiple solutions.  Don't bother with this for now for simplicity's sake.
            if (solution != null && solution != currentSolution)
                return (null, "MultipleSolutionsAffected");

            solution = currentSolution;
        }

        return (solution, null);
    }

    /// <inheritdoc cref="CopilotUtilities.TryNormalizeCopilotTextChanges"/>
    public static ImmutableArray<TextChange> TryGetNormalizedTextChanges(IEnumerable<ProposedEdit> edits)
    {
        using var _ = ArrayBuilder<TextChange>.GetInstance(out var textChanges);
        foreach (var edit in edits)
            textChanges.Add(new TextChange(edit.Span.Span.ToTextSpan(), edit.ReplacementText));

        return CopilotUtilities.TryNormalizeCopilotTextChanges(textChanges);
    }
}
