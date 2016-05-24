using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral
{
    [ExportCommandHandler(nameof(SplitStringLiteralCommandHandler), ContentTypeNames.CSharpContentType)]
    internal class SplitStringLiteralCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
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
            var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (caret != null)
            {
                var snapshot = args.SubjectBuffer.CurrentSnapshot;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();

                if (document != null)
                {
                    return SplitStringLiteralAsync(args.SubjectBuffer, document, caret.Value.Position, CancellationToken.None).GetAwaiter().GetResult();
                }
            }

            return false;
        }

        private async Task<bool> SplitStringLiteralAsync(
            ITextBuffer subjectBuffer, Document document, int position, CancellationToken cancellationToken)
        {
            var useTabs = subjectBuffer.GetOption(FormattingOptions.UseTabs);
            var tabSize = subjectBuffer.GetOption(FormattingOptions.TabSize);

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindToken(position);
            if (position <= token.SpanStart || position >= token.Span.End)
            {
                return false;
            }

            if (!token.IsKind(SyntaxKind.StringLiteralToken))
            {
                return false;
            }

            if (token.IsVerbatimStringLiteral() ||
                token.Kind() == SyntaxKind.InterpolatedVerbatimStringStartToken)
            {
                // Don't do anything special for @""  strings.  
                return false;
            }

            var tokenQuoteLength = GetTokenStart(token.Text);
            var tokenStart = token.SpanStart + tokenQuoteLength;
            if (position < tokenStart)
            {
                return false;
            }

            // TODO(cyrusn): Should we not do anything when the string literal contains errors in it?

            var indentation = await DetermineIndentationAsync(
                document, root, sourceText, position, token, cancellationToken).ConfigureAwait(false);
            if (indentation == null)
            {
                return false;
            }

            var indentString = indentation.Value.CreateIndentationString(useTabs, tabSize);
            var newDocument = await SplitStringAsync(
                document, root, sourceText, position, token, indentString, cancellationToken).ConfigureAwait(false);
            var workspace = document.Project.Solution.Workspace;
            workspace.TryApplyChanges(newDocument.Project.Solution);

            return true;
        }

        private async Task<int?> DetermineIndentationAsync(
            Document document,
            SyntaxNode root,
            SourceText sourceText,
            int position,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var newDocument = await SplitStringAsync(
                document, root, sourceText, position, token, indentString: null, cancellationToken: cancellationToken).ConfigureAwait(false);

            var indentationService = newDocument.GetLanguageService<IIndentationService>();
            var currentLine = sourceText.Lines.GetLineFromPosition(position);
            var indentation = await indentationService.GetDesiredIndentationAsync(
                newDocument, currentLine.LineNumber + 1, cancellationToken).ConfigureAwait(false);

            if (indentation == null)
            {
                return null;
            }

            var newSourceText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var baseLine = newSourceText.Lines.GetLineFromPosition(indentation.Value.BasePosition);
            var baseOffsetInLine = indentation.Value.BasePosition - baseLine.Start;

            var indent = baseOffsetInLine + indentation.Value.Offset;

            return indent;
        }

        private async Task<Document> SplitStringAsync(
            Document document,
            SyntaxNode root,
            SourceText sourceText,
            int position,
            SyntaxToken token,
            string indentString,
            CancellationToken cancellationToken)
        {
            var parent = token.Parent;
            var splitString = CreateSplitString(sourceText, token, position, indentString).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(parent, splitString);

            var document1 = document.WithSyntaxRoot(newRoot);
            var document2 = await Formatter.FormatAsync(document1, cancellationToken: cancellationToken).ConfigureAwait(false);

            return document2;
        }

        private SyntaxNode CreateSplitString(
            SourceText text, SyntaxToken stringToken, int position, string indentString)
        {
            // TODO(cyrusn): Deal with the positoin being after a \ character
            var prefix = text.GetSubText(TextSpan.FromBounds(stringToken.SpanStart, position)).ToString();
            var suffix = text.GetSubText(TextSpan.FromBounds(position, stringToken.Span.End)).ToString();

            var tokenStart = GetTokenStart(stringToken.Text);

            var firstToken = SyntaxFactory.Token(
                stringToken.LeadingTrivia,
                stringToken.Kind(),
                text: prefix + '"',
                valueText: "",
                trailing: SyntaxFactory.TriviaList(SyntaxFactory.ElasticSpace));

            var plusToken = SyntaxFactory.Token(SyntaxKind.PlusToken).WithTrailingTrivia(
                SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));

            var secondToken = SyntaxFactory.Token(
                GetSecondTokenLeadingTrivia(text, indentString),
                stringToken.Kind(),
                text: stringToken.Text.Substring(0, tokenStart) + suffix,
                valueText: "",
                trailing: stringToken.TrailingTrivia);

            return SyntaxFactory.BinaryExpression(
                SyntaxKind.AddExpression,
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, firstToken),
                plusToken,
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, secondToken));
        }

        private static SyntaxTriviaList GetSecondTokenLeadingTrivia(
            SourceText text, string indentString)
        {
            return indentString == null
                ? default(SyntaxTriviaList)
                : SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(indentString));
        }

        private int GetTokenStart(string tokenText)
        {
            return tokenText[0] == '$' ? 2 : 1;
        }
    }
}
