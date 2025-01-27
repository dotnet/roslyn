// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static partial class IWorkspaceExtensions
{
    /// <summary>
    /// Update the workspace so that the document with the Id of <paramref name="newDocument"/>
    /// has the text of newDocument.  If the document is open, then this method will determine a
    /// minimal set of changes to apply to the document.
    /// </summary>
    internal static async Task ApplyDocumentChangesAsync(
        this Workspace workspace, IThreadingContext threadingContext, Document newDocument, CancellationToken cancellationToken)
    {
        var oldSolution = workspace.CurrentSolution;
        var oldDocument = oldSolution.GetRequiredDocument(newDocument.Id);

        // Stay on the current context if we can so we don't bounce to the BG just to try to bounce back to the UI thread.
        var changes = await newDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(true);
        var newSolution = oldSolution.UpdateDocument(newDocument.Id, changes, cancellationToken);

        // VS has a requirement that we're on the main thread to apply changes.
        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        workspace.TryApplyChanges(newSolution);
    }

    /// <summary>
    /// Update the solution so that the document with the Id has the text changes
    /// </summary>
    internal static void ApplyTextChanges(this Workspace workspace, DocumentId id, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken)
    {
        var oldSolution = workspace.CurrentSolution;
        var newSolution = oldSolution.UpdateDocument(id, textChanges, cancellationToken);
        workspace.TryApplyChanges(newSolution);
    }

    private static Solution UpdateDocument(this Solution solution, DocumentId id, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken)
    {
        var oldDocument = solution.GetRequiredDocument(id);
        var oldText = oldDocument.GetTextSynchronously(cancellationToken);
        var newText = oldText.WithChanges(textChanges);
        return solution.WithDocumentText(id, newText, PreservationMode.PreserveIdentity);
    }
}
