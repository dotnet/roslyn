// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace Microsoft.CodeAnalysis.Editor.Interactive
{
    /// <summary>
    /// Implementers of this interface are responsible for retrieving source code that
    /// should be sent to the REPL given the user's selection.
    /// 
    /// If the user does not make a selection then a line should be selected.
    /// If the user selects code that fails to be parsed then the selection gets expanded
    /// to a syntax node.
    /// </summary>
    internal abstract class SendToInteractiveSubmissionProvider : ISendToInteractiveSubmissionProvider
    {
        /// <summary>Expands the selection span of an invalid selection to a span that should be sent to REPL.</summary>
        protected abstract IEnumerable<TextSpan> GetExecutableSyntaxTreeNodeSelection(TextSpan selectedSpan, SourceText source, SyntaxNode node, SemanticModel model);

        /// <summary>Returns whether the submission can be parsed in interactive.</summary>
        protected abstract bool CanParseSubmission(string code);

        public string GetSelectedText(IEditorOptions editorOptions, CommandArgs args, CancellationToken cancellationToken)
        {

            IEnumerable<SnapshotSpan> selectedSpans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer).Where(ss => ss.Length > 0);

            // If there is no selection select the current line.
            if (!selectedSpans.Any())
            {
                selectedSpans = GetSelectedLine(args);
            }

            // Send the selection as is if it does not contain any parsing errors.
            var candidateSubmission = GetSubmissionFromSelectedSpans(editorOptions, selectedSpans);
            if (CanParseSubmission(candidateSubmission))
            {
                return candidateSubmission;
            }

            // Otherwise heuristically try to expand it.
            return GetSubmissionFromSelectedSpans(editorOptions, ExpandSelection(selectedSpans, args, cancellationToken));
        }

        /// <summary>Returns the span for the currently selected line.</summary>
        private static IEnumerable<SnapshotSpan> GetSelectedLine(CommandArgs args)
        {
            SnapshotPoint? caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
            int caretPosition = args.TextView.Caret.Position.BufferPosition.Position;
            ITextSnapshotLine containingLine = caret.Value.GetContainingLine();
            return new SnapshotSpan[] {
                new SnapshotSpan(containingLine.Start, containingLine.End)
            };
        }

        private async Task<IEnumerable<SnapshotSpan>> GetExecutableSyntaxTreeNodeSelection(
            TextSpan selectionSpan,
            CommandArgs args,
            ITextSnapshot snapshot,
            CancellationToken cancellationToken)
        {
            Document doc = args.SubjectBuffer.GetRelatedDocuments().FirstOrDefault();
            var semanticDocument = await SemanticDocument.CreateAsync(doc, cancellationToken).ConfigureAwait(false);
            var text = semanticDocument.Text;
            var root = semanticDocument.Root;
            var model = semanticDocument.SemanticModel;

            return GetExecutableSyntaxTreeNodeSelection(selectionSpan, text, root, model)
                .Select(span => new SnapshotSpan(snapshot, span.Start, span.Length));
        }

        private IEnumerable<SnapshotSpan> ExpandSelection(IEnumerable<SnapshotSpan> selectedSpans, CommandArgs args, CancellationToken cancellationToken)
        {
            var selectedSpansStart = selectedSpans.Min(span => span.Start);
            var selectedSpansEnd = selectedSpans.Max(span => span.End);
            ITextSnapshot snapshot = args.TextView.TextSnapshot;

            IEnumerable<SnapshotSpan> newSpans = GetExecutableSyntaxTreeNodeSelection(
                TextSpan.FromBounds(selectedSpansStart, selectedSpansEnd),
                args,
                snapshot,
                cancellationToken).WaitAndGetResult(cancellationToken);

            return newSpans.Any()
                ? newSpans.Select(n => new SnapshotSpan(snapshot, n.Span.Start, n.Span.Length))
                : selectedSpans;
        }

        private static string GetSubmissionFromSelectedSpans(IEditorOptions editorOptions, IEnumerable<SnapshotSpan> selectedSpans)
        {
            return string.Join(editorOptions.GetNewLineCharacter(), selectedSpans.Select(ss => ss.GetText()));
        }
    }
}
