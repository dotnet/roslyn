﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace Microsoft.CodeAnalysis.Interactive;

/// <summary>
/// Implementers of this interface are responsible for retrieving source code that
/// should be sent to the REPL given the user's selection.
///
/// If the user does not make a selection then a line should be selected.
/// If the user selects code that fails to be parsed then the selection gets expanded
/// to a syntax node.
/// </summary>
internal abstract class AbstractSendToInteractiveSubmissionProvider : ISendToInteractiveSubmissionProvider
{
    /// <summary>Expands the selection span of an invalid selection to a span that should be sent to REPL.</summary>
    protected abstract IEnumerable<TextSpan> GetExecutableSyntaxTreeNodeSelection(TextSpan selectedSpan, SyntaxNode node);

    /// <summary>Returns whether the submission can be parsed in interactive.</summary>
    protected abstract bool CanParseSubmission(string code);

    string ISendToInteractiveSubmissionProvider.GetSelectedText(IEditorOptions editorOptions, EditorCommandArgs args, CancellationToken cancellationToken)
    {
        var selectedSpans = args.TextView.Selection.IsEmpty
            ? GetExpandedLine(editorOptions, args, cancellationToken)
            : args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer).Where(ss => ss.Length > 0);

        return GetSubmissionFromSelectedSpans(editorOptions, selectedSpans);
    }

    /// <summary>Returns the span for the selected line. Extends it if it is a part of a multi line statement or declaration.</summary>
    private IEnumerable<SnapshotSpan> GetExpandedLine(IEditorOptions editorOptions, EditorCommandArgs args, CancellationToken cancellationToken)
    {
        var selectedSpans = GetSelectedLine(args.TextView);
        var candidateSubmission = GetSubmissionFromSelectedSpans(editorOptions, selectedSpans);
        return CanParseSubmission(candidateSubmission)
            ? selectedSpans
            : ExpandSelection(selectedSpans, args, cancellationToken);
    }

    /// <summary>Returns the span for the currently selected line.</summary>
    private static IEnumerable<SnapshotSpan> GetSelectedLine(ITextView textView)
    {
        var snapshotLine = textView.Caret.Position.VirtualBufferPosition.Position.GetContainingLine();
        var span = new SnapshotSpan(snapshotLine.Start, snapshotLine.LengthIncludingLineBreak);
        return new NormalizedSnapshotSpanCollection(span);
    }

    private IEnumerable<SnapshotSpan> ExpandSelection(IEnumerable<SnapshotSpan> selectedSpans, EditorCommandArgs args, CancellationToken cancellationToken)
    {
        var selectedSpansStart = selectedSpans.Min(span => span.Start);
        var selectedSpansEnd = selectedSpans.Max(span => span.End);
        var snapshot = args.TextView.TextSnapshot;

        var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        var root = document.GetSyntaxRootSynchronously(cancellationToken);

        var newSpans = GetExecutableSyntaxTreeNodeSelection(TextSpan.FromBounds(selectedSpansStart, selectedSpansEnd), root).
            Select(span => new SnapshotSpan(snapshot, span.Start, span.Length));

        return newSpans.Any() ? newSpans : selectedSpans;
    }

    private static string GetSubmissionFromSelectedSpans(IEditorOptions editorOptions, IEnumerable<SnapshotSpan> selectedSpans)
        => string.Join(editorOptions.GetNewLineCharacter(), selectedSpans.Select(ss => ss.GetText()));
}
