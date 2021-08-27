﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(SplitStringLiteralCommandHandler))]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal partial class SplitStringLiteralCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SplitStringLiteralCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public string DisplayName => CSharpEditorResources.Split_string;

        public CommandState GetCommandState(ReturnKeyCommandArgs args)
            => CommandState.Unspecified;

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
            => ExecuteCommandWorker(args);

        public bool ExecuteCommandWorker(ReturnKeyCommandArgs args)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

            // Don't split strings if there is any actual selection.
            // We must check all spans to account for multi-carets.
            if (spans.IsEmpty() || !spans.All(s => s.IsEmpty))
            {
                return false;
            }

            var caret = textView.GetCaretPoint(subjectBuffer);
            if (caret == null)
            {
                return false;
            }

            // First, we need to verify that we are only working with string literals.
            // Otherwise, let the editor handle all carets.
            foreach (var span in spans)
            {
                var spanStart = span.Start;
                var line = subjectBuffer.CurrentSnapshot.GetLineFromPosition(span.Start);
                if (!LineContainsQuote(line, span.Start))
                {
                    return false;
                }
            }

            // We now go through the verified string literals and split each of them.
            // The list of spans is traversed in reverse order so we do not have to
            // deal with updating later caret positions to account for the added space
            // from splitting at earlier caret positions.
            foreach (var span in spans.Reverse())
            {
                if (!SplitString(textView, subjectBuffer, span.Start))
                {
                    return false;
                }
            }

            return true;
        }

        private bool SplitString(ITextView textView, ITextBuffer subjectBuffer, SnapshotPoint caret)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

            if (document != null)
            {
                var options = document.GetOptionsAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                var enabled = options.GetOption(SplitStringLiteralOptions.Enabled);

                if (enabled)
                {
                    using var transaction = CaretPreservingEditTransaction.TryCreate(
                        CSharpEditorResources.Split_string, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

                    var cursorPosition = SplitStringLiteral(document, options, caret, CancellationToken.None);
                    if (cursorPosition != null)
                    {
                        var snapshotPoint = new SnapshotPoint(
                            subjectBuffer.CurrentSnapshot, cursorPosition.Value);
                        var newCaretPoint = textView.BufferGraph.MapUpToBuffer(
                            snapshotPoint, PointTrackingMode.Negative, PositionAffinity.Predecessor,
                            textView.TextBuffer);

                        if (newCaretPoint != null)
                        {
                            textView.Caret.MoveTo(newCaretPoint.Value);
                        }

                        transaction.Complete();
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool LineContainsQuote(ITextSnapshotLine line, int caretPosition)
        {
            var snapshot = line.Snapshot;
            for (int i = line.Start; i < caretPosition; i++)
            {
                if (snapshot[i] == '"')
                {
                    return true;
                }
            }

            return false;
        }

        private static int? SplitStringLiteral(
            Document document, DocumentOptionSet options, int position, CancellationToken cancellationToken)
        {
            var useTabs = options.GetOption(FormattingOptions.UseTabs);
            var tabSize = options.GetOption(FormattingOptions.TabSize);
            var indentStyle = options.GetOption(FormattingOptions.SmartIndent, LanguageNames.CSharp);

            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var sourceText = root.SyntaxTree.GetText(cancellationToken);

            var splitter = StringSplitter.Create(
                document, position, root, sourceText,
                useTabs, tabSize, indentStyle, cancellationToken);
            if (splitter == null)
            {
                return null;
            }

            return splitter.TrySplit();
        }
    }
}
