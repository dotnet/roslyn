// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal static class VSTypeScriptExtensions
{
    extension(Workspace workspace)
    {
        public void ApplyTextChanges(DocumentId id, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken)
        => Editor.Shared.Extensions.IWorkspaceExtensions.ApplyTextChanges(workspace, id, textChanges, cancellationToken);
    }

    extension(ITextView textView)
    {
        public SnapshotPoint? GetCaretPoint(ITextBuffer subjectBuffer)
        => Editor.Shared.Extensions.ITextViewExtensions.GetCaretPoint(textView, subjectBuffer);

        public bool TryMoveCaretToAndEnsureVisible(SnapshotPoint point)
            => Editor.Shared.Extensions.ITextViewExtensions.TryMoveCaretToAndEnsureVisible(textView, point);

        public SnapshotPoint? GetCaretPoint(Predicate<ITextSnapshot> match)
            => Editor.Shared.Extensions.ITextViewExtensions.GetCaretPoint(textView, match);

        public bool TryMoveCaretToAndEnsureVisible(VirtualSnapshotPoint point)
            => Editor.Shared.Extensions.ITextViewExtensions.TryMoveCaretToAndEnsureVisible(textView, point);
    }

    extension(ITextSnapshot snapshot)
    {
        public SnapshotSpan? TryGetSpan(int startLine, int startIndex, int endLine, int endIndex)
        => Text.Shared.Extensions.ITextSnapshotExtensions.TryGetSpan(snapshot, startLine, startIndex, endLine, endIndex);

        public void GetLineAndCharacter(int position, out int lineNumber, out int characterIndex)
            => Text.Shared.Extensions.ITextSnapshotExtensions.GetLineAndCharacter(snapshot, position, out lineNumber, out characterIndex);
    }

    extension(string line)
    {
        public int GetColumnFromLineOffset(int endPosition, int tabSize)
        => Shared.Extensions.StringExtensions.GetColumnFromLineOffset(line, endPosition, tabSize);
    }
}
