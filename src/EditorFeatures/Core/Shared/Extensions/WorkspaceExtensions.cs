// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static partial class IWorkspaceExtensions
{
    /// <summary>
    /// Update the workspace so that the document with the Id of <paramref name="newDocument"/>
    /// has the text of newDocument.  If the document is open, then this method will determine a
    /// minimal set of changes to apply to the document.
    /// </summary>
    internal static void ApplyDocumentChanges(this Workspace workspace, Document newDocument, CancellationToken cancellationToken)
    {
        var oldSolution = workspace.CurrentSolution;
        var oldDocument = oldSolution.GetRequiredDocument(newDocument.Id);
        var changes = newDocument.GetTextChangesAsync(oldDocument, cancellationToken).WaitAndGetResult(cancellationToken);
        var newSolution = oldSolution.UpdateDocument(newDocument.Id, changes, cancellationToken);
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
