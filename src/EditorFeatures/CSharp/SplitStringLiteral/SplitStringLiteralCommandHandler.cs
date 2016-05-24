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

            var splitter = StringSplitter.Create(document, position, syntaxTree, root, sourceText, token, useTabs, tabSize, cancellationToken);
            if (splitter == null)
            {
                return false;
            }

            return await splitter.TrySplitAsync().ConfigureAwait(false);
        }

        private abstract class StringSplitter
        {
            protected readonly Document Document;
            protected readonly int Position;
            protected readonly SourceText SourceText;
            protected readonly SyntaxTree SyntaxTree;
            protected readonly SyntaxNode Root;
            protected readonly SyntaxToken Token;
            protected readonly int TabSize;
            protected readonly bool UseTabs;
            protected readonly CancellationToken CancellationToken;

            public StringSplitter(Document document, int position, SyntaxTree syntaxTree, SyntaxNode root, SourceText sourceText, SyntaxToken token, bool useTabs, int tabSize, CancellationToken cancellationToken)
            {
                Document = document;
                Position = position;
                SyntaxTree = syntaxTree;
                Root = root;
                SourceText = sourceText;
                Token = token;
                UseTabs = useTabs;
                TabSize = tabSize;
                CancellationToken = cancellationToken;
            }

            public static StringSplitter Create(
                Document document, int position, SyntaxTree syntaxTree, SyntaxNode root, SourceText sourceText, SyntaxToken token, bool useTabs, int tabSize, CancellationToken cancellationToken)
            {
                if (token.IsKind(SyntaxKind.StringLiteralToken))
                {
                    return new SimpleStringSplitter(document, position, syntaxTree, root, sourceText, token, useTabs, tabSize, cancellationToken);
                }
                else if (token.IsKind(SyntaxKind.InterpolatedStringTextToken))
                {
                    return new InterpolatedStringSplitter(document, position, syntaxTree, root, sourceText, token, useTabs, tabSize, cancellationToken);
                }

                return null;
            }

            public abstract Task<bool> TrySplitAsync();

            protected int GetTokenStart(string tokenText)
            {
                return tokenText[0] == '$' ? 2 : 1;
            }
        }

        private class SimpleStringSplitter : StringSplitter
        {
            public SimpleStringSplitter(Document document, int position, SyntaxTree syntaxTree, SyntaxNode root, SourceText sourceText, SyntaxToken token, bool useTabs, int tabSize, CancellationToken cancellationToken)
                : base(document, position, syntaxTree, root, sourceText, token, useTabs, tabSize, cancellationToken)
            {
            }

            public override async Task<bool> TrySplitAsync()
            {
                if (Token.IsVerbatimStringLiteral())
                {
                    // Don't split @"" strings.  They already support directly embedding newlines.
                    return false;
                }

                var tokenQuoteLength = GetTokenStart(Token.Text);
                var tokenStart = Token.SpanStart + tokenQuoteLength;
                if (Position < tokenStart)
                {
                    return false;
                }

                // TODO(cyrusn): Should we not do anything when the string literal contains errors in it?

                var indentation = await DetermineIndentationAsync().ConfigureAwait(false);
                if (indentation == null)
                {
                    return false;
                }

                var indentString = indentation.Value.CreateIndentationString(UseTabs, TabSize);
                var newDocument = await SplitStringAsync(indentString).ConfigureAwait(false);
                var workspace = Document.Project.Solution.Workspace;
                workspace.TryApplyChanges(newDocument.Project.Solution);

                return true;

            }

            private async Task<int?> DetermineIndentationAsync()
            {
                var newDocument = await SplitStringAsync(indentString: null).ConfigureAwait(false);

                var indentationService = newDocument.GetLanguageService<IIndentationService>();
                var currentLine = SourceText.Lines.GetLineFromPosition(Position);
                var indentation = await indentationService.GetDesiredIndentationAsync(
                    newDocument, currentLine.LineNumber + 1, CancellationToken).ConfigureAwait(false);

                if (indentation == null)
                {
                    return null;
                }

                var newSourceText = await newDocument.GetTextAsync(CancellationToken).ConfigureAwait(false);
                var baseLine = newSourceText.Lines.GetLineFromPosition(indentation.Value.BasePosition);
                var baseOffsetInLine = indentation.Value.BasePosition - baseLine.Start;

                var indent = baseOffsetInLine + indentation.Value.Offset;

                return indent;
            }

            private async Task<Document> SplitStringAsync(string indentString)
            {
                var parent = Token.Parent;
                var splitString = CreateSplitString(indentString).WithAdditionalAnnotations(Formatter.Annotation);
                var newRoot = Root.ReplaceNode(parent, splitString);

                var document1 = Document.WithSyntaxRoot(newRoot);
                var document2 = await Formatter.FormatAsync(document1, cancellationToken: CancellationToken).ConfigureAwait(false);

                return document2;
            }

            private SyntaxNode CreateSplitString(string indentString)
            {
                // TODO(cyrusn): Deal with the positoin being after a \ character
                var prefix = SourceText.GetSubText(TextSpan.FromBounds(Token.SpanStart, Position)).ToString();
                var suffix = SourceText.GetSubText(TextSpan.FromBounds(Position, Token.Span.End)).ToString();

                var tokenStart = GetTokenStart(Token.Text);

                var firstToken = SyntaxFactory.Token(
                    Token.LeadingTrivia,
                    Token.Kind(),
                    text: prefix + '"',
                    valueText: "",
                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.ElasticSpace));

                var plusToken = SyntaxFactory.Token(SyntaxKind.PlusToken).WithTrailingTrivia(
                    SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));

                var secondToken = SyntaxFactory.Token(
                    GetSecondTokenLeadingTrivia(indentString),
                    Token.Kind(),
                    text: Token.Text.Substring(0, tokenStart) + suffix,
                    valueText: "",
                    trailing: Token.TrailingTrivia);

                var firstExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, firstToken);
                var secondExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, secondToken);

                return SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    firstExpression, plusToken, secondExpression);
            }

            private static SyntaxTriviaList GetSecondTokenLeadingTrivia(string indentString)
            {
                return indentString == null
                    ? default(SyntaxTriviaList)
                    : SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(indentString));
            }
        }

        private class InterpolatedStringSplitter : StringSplitter
        {
            public InterpolatedStringSplitter(Document document, int position, SyntaxTree syntaxTree, SyntaxNode root, SourceText sourceText, SyntaxToken token, bool useTabs, int tabSize, CancellationToken cancellationToken) : base(document, position, syntaxTree, root, sourceText, token, useTabs, tabSize, cancellationToken)
            {
            }

            public override async Task<bool> TrySplitAsync()
            {
                var interpolatedString = (InterpolatedStringExpressionSyntax)Token.Parent;
                if (interpolatedString.StringStartToken.Kind() == SyntaxKind.InterpolatedVerbatimStringStartToken)
                {
                    // Don't offer on $@"" strings.  They support newlines directly in their content.
                    return false;
                }


            }
        }

    }
}
