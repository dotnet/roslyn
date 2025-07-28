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
    extension(ITextView view)
    {
        /// <summary>
        /// create caret preserving edit transaction with automatic code change undo merging policy
        /// </summary>
        public CaretPreservingEditTransaction CreateEditTransaction(
    string description, ITextUndoHistoryRegistry registry, IEditorOperationsFactoryService service)
        {
            return new CaretPreservingEditTransaction(description, view, registry, service)
            {
                MergePolicy = AutomaticCodeChangeMergePolicy.Instance
            };
        }
    }

    extension(IBraceCompletionSession session)
    {
        public SnapshotPoint? GetCaretPosition()
        => GetCaretPoint(session, session.SubjectBuffer);

        // get the caret position within the given buffer
        private SnapshotPoint? GetCaretPoint(ITextBuffer buffer)
            => session.TextView.Caret.Position.Point.GetPoint(buffer, PositionAffinity.Predecessor);
    }
}
