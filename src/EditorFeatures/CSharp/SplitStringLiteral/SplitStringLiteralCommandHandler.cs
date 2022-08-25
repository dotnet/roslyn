// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
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
        private readonly EditorOptionsService _editorOptionsService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SplitStringLiteralCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            EditorOptionsService editorOptionsService)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _editorOptionsService = editorOptionsService;
        }

        public string DisplayName => CSharpEditorResources.Split_string;

        public CommandState GetCommandState(ReturnKeyCommandArgs args)
            => CommandState.Unspecified;

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
            => ExecuteCommandWorker(args);

        public bool ExecuteCommandWorker(ReturnKeyCommandArgs args)
        {
            if (!_editorOptionsService.GlobalOptions.GetOption(SplitStringLiteralOptions.Enabled, LanguageNames.CSharp))
            {
                return false;
            }

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

            IndentationOptions? lazyOptions = null;

            // We now go through the verified string literals and split each of them.
            // The list of spans is traversed in reverse order so we do not have to
            // deal with updating later caret positions to account for the added space
            // from splitting at earlier caret positions.
            foreach (var span in spans.Reverse())
            {
                if (!SplitString(textView, subjectBuffer, span.Start.Position, ref lazyOptions, CancellationToken.None))
                {
                    return false;
                }
            }

            return true;
        }

        private bool SplitString(ITextView textView, ITextBuffer subjectBuffer, int position, ref IndentationOptions? lazyOptions, CancellationToken cancellationToken)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            lazyOptions ??= subjectBuffer.GetIndentationOptions(_editorOptionsService, document.Project.Services, explicitFormat: false);

            using var transaction = CaretPreservingEditTransaction.TryCreate(
                CSharpEditorResources.Split_string, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

            var parsedDocument = ParsedDocument.CreateSynchronously(document, CancellationToken.None);
            var splitter = StringSplitter.TryCreate(parsedDocument, position, lazyOptions.Value, cancellationToken);
            if (splitter?.TrySplit(out var newRoot, out var newPosition) != true)
            {
                return false;
            }

            // apply the change:
            var newDocument = document.WithSyntaxRoot(newRoot!);
            var workspace = newDocument.Project.Solution.Workspace;
            workspace.TryApplyChanges(newDocument.Project.Solution);

            // move caret:
            var snapshotPoint = new SnapshotPoint(
                subjectBuffer.CurrentSnapshot, newPosition);
            var newCaretPoint = textView.BufferGraph.MapUpToBuffer(
                snapshotPoint, PointTrackingMode.Negative, PositionAffinity.Predecessor,
                textView.TextBuffer);

            if (newCaretPoint != null)
            {
                textView.Caret.MoveTo(newCaretPoint.Value);
            }

            transaction?.Complete();
            return true;
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
    }
}
