// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
{
    internal static class Extensions
    {
        /// <summary>
        /// format span
        /// </summary>
        public static void Format(this ITextBuffer buffer, TextSpan span, IEnumerable<AbstractFormattingRule> rules)
        {
            var snapshot = buffer.CurrentSnapshot;
            snapshot.FormatAndApplyToBuffer(span, rules, CancellationToken.None);
        }

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

        public static SyntaxToken FindToken(this ITextSnapshot snapshot, int position, CancellationToken cancellationToken)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return default;
            }

            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            return root.FindToken(position, findInsideTrivia: true);
        }

        /// <summary>
        /// insert text to workspace and get updated version of the document
        /// </summary>
        public static Document InsertText(this Document document, int position, string text, CancellationToken cancellationToken = default)
        {
            return document.ReplaceText(new TextSpan(position, 0), text, cancellationToken);
        }

        /// <summary>
        /// replace text to workspace and get updated version of the document
        /// </summary>
        public static Document ReplaceText(this Document document, TextSpan span, string text, CancellationToken cancellationToken)
        {
            return document.ApplyTextChange(new TextChange(span, text), cancellationToken);
        }

        /// <summary>
        /// apply text changes to workspace and get updated version of the document
        /// </summary>
        public static Document ApplyTextChange(this Document document, TextChange textChange, CancellationToken cancellationToken)
        {
            return document.ApplyTextChanges(SpecializedCollections.SingletonEnumerable(textChange), cancellationToken);
        }

        /// <summary>
        /// apply text changes to workspace and get updated version of the document
        /// </summary>
        public static Document ApplyTextChanges(this Document document, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken)
        {
            // here assumption is that text change are based on current solution
            var oldSolution = document.Project.Solution;
            var newSolution = oldSolution.UpdateDocument(document.Id, textChanges, cancellationToken);

            if (oldSolution.Workspace.TryApplyChanges(newSolution))
            {
                return newSolution.Workspace.CurrentSolution.GetDocument(document.Id);
            }

            return document;
        }

        /// <summary>
        /// Update the solution so that the document with the Id has the text changes
        /// </summary>
        public static Solution UpdateDocument(this Solution solution, DocumentId id, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken = default)
        {
            var oldDocument = solution.GetDocument(id);
            var newText = oldDocument.GetTextSynchronously(cancellationToken).WithChanges(textChanges);
            return solution.WithDocumentText(id, newText);
        }

        public static SnapshotSpan GetSessionSpan(this IBraceCompletionSession session)
        {
            var snapshot = session.SubjectBuffer.CurrentSnapshot;
            var open = session.OpeningPoint.GetPoint(snapshot);
            var close = session.ClosingPoint.GetPoint(snapshot);

            return new SnapshotSpan(open, close);
        }

        public static int GetValueInValidRange(this int value, int smallest, int largest)
        {
            return Math.Max(smallest, Math.Min(value, largest));
        }

        public static bool PositionInSnapshot(this int position, ITextSnapshot snapshot)
        {
            return position.GetValueInValidRange(0, Math.Max(0, snapshot.Length - 1)) == position;
        }

        public static SnapshotPoint? GetCaretPosition(this IBraceCompletionSession session)
        {
            return GetCaretPoint(session, session.SubjectBuffer);
        }

        // get the caret position within the given buffer
        private static SnapshotPoint? GetCaretPoint(this IBraceCompletionSession session, ITextBuffer buffer)
        {
            return session.TextView.Caret.Position.Point.GetPoint(buffer, PositionAffinity.Predecessor);
        }
    }
}
