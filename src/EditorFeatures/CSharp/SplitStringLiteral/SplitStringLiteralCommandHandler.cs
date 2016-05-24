using System;
using System.Threading;
using System.Threading.Tasks;
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
            var caret = textView.GetCaretPoint(subjectBuffer);

            if (caret != null)
            {
                // Quick check.  If the line doesn't contain a quote in it before the caret,
                // then no point in doing any more expensive synchronous work.
                var line = subjectBuffer.CurrentSnapshot.GetLineFromPosition(caret.Value);
                if (LineContainsQuote(line, caret.Value))
                {
                    return SplitString(textView, subjectBuffer, caret);
                }
            }

            return false;
        }

        private bool SplitString(ITextView textView, ITextBuffer subjectBuffer, SnapshotPoint? caret)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

            if (document != null)
            {
                var enabled = document.Project.Solution.Workspace.Options.GetOption(
                    SplitStringLiteralOptions.Enabled, LanguageNames.CSharp);

                if (enabled)
                {
                    var cursorPosition = SplitStringLiteralAsync(
                        subjectBuffer, document, caret.Value.Position, CancellationToken.None).GetAwaiter().GetResult();

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

        private async Task<int?> SplitStringLiteralAsync(
            ITextBuffer subjectBuffer, Document document, int position, CancellationToken cancellationToken)
        {
            var useTabs = subjectBuffer.GetOption(FormattingOptions.UseTabs);
            var tabSize = subjectBuffer.GetOption(FormattingOptions.TabSize);

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var splitter = StringSplitter.Create(document, position, syntaxTree, root, sourceText, useTabs, tabSize, cancellationToken);
            if (splitter == null)
            {
                return null;
            }

            return await splitter.TrySplitAsync().ConfigureAwait(false);
        }
    }
}