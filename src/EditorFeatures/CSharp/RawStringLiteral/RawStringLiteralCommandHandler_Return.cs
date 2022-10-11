// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.RawStringLiteral
{
    internal partial class RawStringLiteralCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        public CommandState GetCommandState(ReturnKeyCommandArgs args)
            => CommandState.Unspecified;

        /// <summary>
        /// Checks to see if the user is typing <c>return</c> in <c>"""$$"""</c> and then properly indents the end
        /// delimiter of the raw string literal.
        /// </summary>
        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

            if (spans.Count != 1)
                return false;

            var span = spans.First();
            if (span.Length != 0)
                return false;

            var caret = textView.GetCaretPoint(subjectBuffer);
            if (caret == null)
                return false;

            var position = caret.Value.Position;
            var currentSnapshot = subjectBuffer.CurrentSnapshot;
            if (position >= currentSnapshot.Length)
                return false;

            if (currentSnapshot[position] != '"')
                return false;

            var quotesBefore = 0;
            var quotesAfter = 0;

            for (int i = position, n = currentSnapshot.Length; i < n; i++)
            {
                if (currentSnapshot[i] != '"')
                    break;

                quotesAfter++;
            }

            for (var i = position - 1; i >= 0; i--)
            {
                if (currentSnapshot[i] != '"')
                    break;

                quotesBefore++;
            }

            if (quotesAfter != quotesBefore)
                return false;

            if (quotesAfter < 3)
                return false;

            return SplitRawString(textView, subjectBuffer, span.Start.Position, CancellationToken.None);
        }

        private bool SplitRawString(ITextView textView, ITextBuffer subjectBuffer, int position, CancellationToken cancellationToken)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return false;

            var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);

            var token = parsedDocument.Root.FindToken(position);
            if (token.Kind() is not (SyntaxKind.SingleLineRawStringLiteralToken or
                                     SyntaxKind.MultiLineRawStringLiteralToken or
                                     SyntaxKind.InterpolatedSingleLineRawStringStartToken or
                                     SyntaxKind.InterpolatedMultiLineRawStringStartToken))
            {
                return false;
            }

            var indentationOptions = subjectBuffer.GetIndentationOptions(_editorOptionsService, document.Project.Services, explicitFormat: false);
            var indentation = token.GetPreferredIndentation(parsedDocument, indentationOptions, cancellationToken);

            var newLine = indentationOptions.FormattingOptions.NewLine;

            using var transaction = CaretPreservingEditTransaction.TryCreate(
                CSharpEditorResources.Split_string, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

            var edit = subjectBuffer.CreateEdit();

            // apply the change:
            edit.Insert(position, newLine + newLine + indentation);
            var snapshot = edit.Apply();

            // move caret:
            var lineInNewSnapshot = snapshot.GetLineFromPosition(position);
            var nextLine = snapshot.GetLineFromLineNumber(lineInNewSnapshot.LineNumber + 1);
            textView.Caret.MoveTo(new VirtualSnapshotPoint(nextLine, indentation.Length));

            transaction?.Complete();
            return true;
        }
    }
}
