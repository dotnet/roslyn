using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral
{
    [ExportCommandHandler(nameof(SplitStringLiteralCommandHandler), ContentTypeNames.CSharpContentType)]
    internal partial class SplitStringLiteralCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        public CommandState GetCommandState(ReturnKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(ReturnKeyCommandArgs args, Action nextHandler)
        {
            if (!ExecuteCommandWorker(args))
            {
                nextHandler();
            }
        }

        public bool ExecuteCommandWorker(ReturnKeyCommandArgs args)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

            // Don't split strings if there is any actual selection.
            if (spans.Count == 1 && spans[0].IsEmpty)
            {
                var caret = textView.GetCaretPoint(subjectBuffer);
                if (caret != null)
                {
                    // Quick check.  If the line doesn't contain a quote in it before the caret,
                    // then no point in doing any more expensive synchronous work.
                    var line = subjectBuffer.CurrentSnapshot.GetLineFromPosition(caret.Value);
                    if (LineContainsQuote(line, caret.Value))
                    {
                        return SplitString(textView, subjectBuffer, caret.Value);
                    }
                }
            }

            return false;
        }

        private bool SplitString(ITextView textView, ITextBuffer subjectBuffer, SnapshotPoint caret)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

            if (document != null)
            {
                var enabled = document.Project.Solution.Workspace.Options.GetOption(
                    SplitStringLiteralOptions.Enabled, LanguageNames.CSharp);

                if (enabled)
                {
                    var cursorPosition = SplitStringLiteral(
                        subjectBuffer, document, caret, CancellationToken.None);

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

                        return true;
                    }
                }
            }

            return false;
        }

        private bool LineContainsQuote(ITextSnapshotLine line, int caretPosition)
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

        private int? SplitStringLiteral(
            ITextBuffer subjectBuffer, Document document, int position, CancellationToken cancellationToken)
        {
            var useTabs = subjectBuffer.GetOption(FormattingOptions.UseTabs);
            var tabSize = subjectBuffer.GetOption(FormattingOptions.TabSize);

            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var sourceText = root.SyntaxTree.GetText(cancellationToken);

            var splitter = StringSplitter.Create(document, position, root, sourceText, useTabs, tabSize, cancellationToken);
            if (splitter == null)
            {
                return null;
            }

            return splitter.TrySplit();
        }
    }
}