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
            if (currentSnapshot[position] != '"')
                return false;

            var quotesBefore = 0;
            var quotesAfter = 0;

            for (int i = position, n = currentSnapshot.Length; i < n; i++)
            {
                if (currentSnapshot[i] == '"')
                    quotesAfter++;
            }

            for (var i = position - 1; i >= 0; i--)
            {
                if (currentSnapshot[i] == '"')
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

            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var token = root.FindToken(position);
            if (token.Kind() is not (SyntaxKind.SingleLineRawStringLiteralToken or
                                     SyntaxKind.MultiLineRawStringLiteralToken or
                                     SyntaxKind.InterpolatedSingleLineRawStringStartToken or
                                     SyntaxKind.InterpolatedMultiLineRawStringStartToken))
            {
                return false;
            }

            var indentation = GetPreferredIndentation(document, token, cancellationToken);

            var newLine = document.Project.Solution.Options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);

            using var transaction = CaretPreservingEditTransaction.TryCreate(
                CSharpEditorResources.Split_string, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

            var indentService = document.GetRequiredLanguageService<IIndentationService>();

            var edit = subjectBuffer.CreateEdit();

            var sourceText = document.GetTextSynchronously(cancellationToken);
            var textToInsert = $"{newLine}{newLine}{indentation}";

            // apply the change:
            edit.Insert(position, textToInsert);
            var snapshot = edit.Apply();

            // move caret:
            var lineInNewSnapshot = snapshot.GetLineFromPosition(position);
            var nextLine = snapshot.GetLineFromLineNumber(lineInNewSnapshot.LineNumber + 1);
            textView.Caret.MoveTo(new VirtualSnapshotPoint(nextLine, indentation.Length));

            transaction?.Complete();
            return true;
        }

        private static string GetPreferredIndentation(Document document, SyntaxToken token, CancellationToken cancellationToken)
        {
            var sourceText = document.GetTextSynchronously(cancellationToken);
            var tokenLine = sourceText.Lines.GetLineFromPosition(token.SpanStart);
            var firstNonWhitespacePos = tokenLine.GetFirstNonWhitespacePosition();
            Contract.ThrowIfNull(firstNonWhitespacePos);
            if (firstNonWhitespacePos.Value == token.SpanStart)
            {
                // token was on it's own line.  Start the end delimiter at the same location as it.
                return tokenLine.Text!.ToString(TextSpan.FromBounds(tokenLine.Start, token.SpanStart));
            }

            // Token was on a line with something else.  Determine where we would indent the token if it was on the next
            // line and use that to determine the indentation of the final line.

            var options = document.Project.Solution.Options;
            var newLine = options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);

            var annotation = new SyntaxAnnotation();
            var newToken = token.WithAdditionalAnnotations(annotation);
            newToken = newToken.WithLeadingTrivia(newToken.LeadingTrivia.Add(SyntaxFactory.EndOfLine(newLine)));

            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var newRoot = root.ReplaceToken(token, newToken);
            var newDocument = document.WithSyntaxRoot(newRoot);
            var newText = newDocument.GetTextSynchronously(cancellationToken);

            var newTokenLine = newText.Lines.GetLineFromPosition(newRoot.GetAnnotatedTokens(annotation).Single().SpanStart);

            var indentStyle = document.Project.Solution.Options.GetOption(FormattingOptions.SmartIndent, LanguageNames.CSharp);
            var indenter = document.GetRequiredLanguageService<IIndentationService>();

            var indentation = indenter.GetIndentation(newDocument, newTokenLine.LineNumber, indentStyle, cancellationToken);

            return indentation.GetIndentationString(
                newText,
                options.GetOption(FormattingOptions.UseTabs, LanguageNames.CSharp),
                options.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp));
        }
    }
}
