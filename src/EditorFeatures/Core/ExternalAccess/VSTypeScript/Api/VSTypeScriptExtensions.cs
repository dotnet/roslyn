// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal static class VSTypeScriptExtensions
    {
        public static void ApplyTextChanges(this Workspace workspace, DocumentId id, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken)
            => Editor.Shared.Extensions.IWorkspaceExtensions.ApplyTextChanges(workspace, id, textChanges, cancellationToken);

        public static SnapshotPoint? GetCaretPoint(this ITextView textView, ITextBuffer subjectBuffer)
            => Editor.Shared.Extensions.ITextViewExtensions.GetCaretPoint(textView, subjectBuffer);

        public static bool TryMoveCaretToAndEnsureVisible(this ITextView textView, SnapshotPoint point)
            => Editor.Shared.Extensions.ITextViewExtensions.TryMoveCaretToAndEnsureVisible(textView, point);

        public static SnapshotPoint? GetCaretPoint(this ITextView textView, Predicate<ITextSnapshot> match)
            => Editor.Shared.Extensions.ITextViewExtensions.GetCaretPoint(textView, match);

        public static bool TryMoveCaretToAndEnsureVisible(this ITextView textView, VirtualSnapshotPoint point)
            => Editor.Shared.Extensions.ITextViewExtensions.TryMoveCaretToAndEnsureVisible(textView, point);

        public static SnapshotSpan? TryGetSpan(this ITextSnapshot snapshot, int startLine, int startIndex, int endLine, int endIndex)
            => Text.Shared.Extensions.ITextSnapshotExtensions.TryGetSpan(snapshot, startLine, startIndex, endLine, endIndex);

        public static void GetLineAndCharacter(this ITextSnapshot snapshot, int position, out int lineNumber, out int characterIndex)
            => Text.Shared.Extensions.ITextSnapshotExtensions.GetLineAndCharacter(snapshot, position, out lineNumber, out characterIndex);

        public static int GetColumnFromLineOffset(this string line, int endPosition, int tabSize)
            => Shared.Extensions.StringExtensions.GetColumnFromLineOffset(line, endPosition, tabSize);
    }
}
