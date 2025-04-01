// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.AutomaticCompletion;

internal static class Extensions
{
    /// <summary>
    /// create caret preserving edit transaction with automatic code change undo merging policy
    /// </summary>
    public static CaretPreservingEditTransaction CreateEditTransaction(
        this ITextView view, string description, ITextUndoHistoryRegistry registry, IEditorOperationsFactoryService service)
    {
        return new CaretPreservingEditTransaction(description, view, registry, service)
        {
            MergePolicy = AutomaticCodeChangeMergePolicy.Instance
        };
    }

    public static SnapshotPoint? GetCaretPosition(this IBraceCompletionSession session)
        => GetCaretPoint(session, session.SubjectBuffer);

    // get the caret position within the given buffer
    private static SnapshotPoint? GetCaretPoint(this IBraceCompletionSession session, ITextBuffer buffer)
        => session.TextView.Caret.Position.Point.GetPoint(buffer, PositionAffinity.Predecessor);
}
