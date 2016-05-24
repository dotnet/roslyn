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
using Roslyn.Utilities;

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

            var splitter = StringSplitter.Create(document, position, syntaxTree, root, sourceText, useTabs, tabSize, cancellationToken);
            if (splitter == null)
            {
                return false;
            }

            return await splitter.TrySplitAsync().ConfigureAwait(false);
        }

        private abstract class StringSplitter
        {
            protected readonly Document Document;
            protected readonly int CursorPosition;
            protected readonly SourceText SourceText;
            protected readonly SyntaxTree SyntaxTree;
            protected readonly SyntaxNode Root;
            protected readonly int TabSize;
            protected readonly bool UseTabs;
            protected readonly CancellationToken CancellationToken;

            public StringSplitter(Document document, int position, SyntaxTree syntaxTree, SyntaxNode root, SourceText sourceText, bool useTabs, int tabSize, CancellationToken cancellationToken)
            {
                Document = document;
                CursorPosition = position;
                SyntaxTree = syntaxTree;
                Root = root;
                SourceText = sourceText;
                UseTabs = useTabs;
                TabSize = tabSize;
                CancellationToken = cancellationToken;
            }

            public static StringSplitter Create(
                Document document, int position,
                SyntaxTree syntaxTree, SyntaxNode root, SourceText sourceText, 
                bool useTabs, int tabSize, CancellationToken cancellationToken)
            {
                var token = root.FindToken(position);

                if (token.IsKind(SyntaxKind.StringLiteralToken))
                {
                    return new SimpleStringSplitter(
                        document, position, syntaxTree, root, 
                        sourceText, token, useTabs, tabSize,
                        cancellationToken);
                }

                var interpolatedStringExpression = TryGetInterpolatedStringExpression(token);
                if (interpolatedStringExpression != null)
                {
                    return new InterpolatedStringSplitter(
                        document, position, syntaxTree, root, 
                        sourceText, interpolatedStringExpression,
                        useTabs, tabSize, cancellationToken);
                }

                return null;
            }

            private static InterpolatedStringExpressionSyntax TryGetInterpolatedStringExpression(SyntaxToken token)
            {
                if (token.IsKind(SyntaxKind.InterpolatedStringTextToken) || 
                    token.IsKind(SyntaxKind.InterpolatedStringEndToken) ||
                    IsInterpolationOpenBrace(token))
                {
                    return token.GetAncestor<InterpolatedStringExpressionSyntax>();
                }

                return null;
            }

            private static bool IsInterpolationOpenBrace(SyntaxToken token)
            {
                return token.Kind() == SyntaxKind.OpenBraceToken && token.Parent.IsKind(SyntaxKind.Interpolation);
            }

            protected abstract bool CheckToken();

            protected abstract SyntaxNode GetNodeToReplace();

            protected abstract BinaryExpressionSyntax CreateSplitString(string indentString);

            public Task<bool> TrySplitAsync()
            {
                if (!CheckToken())
                {
                    return SpecializedTasks.False;
                }

                return TrySplitWorkerAsync();
            }

            private async Task<bool> TrySplitWorkerAsync()
            {
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

            protected int GetTokenStart(string tokenText)
            {
                return tokenText[0] == '$' ? 2 : 1;
            }

            protected async Task<int?> DetermineIndentationAsync()
            {
                var newDocument = await SplitStringAsync(indentString: null).ConfigureAwait(false);

                var indentationService = newDocument.GetLanguageService<IIndentationService>();
                var currentLine = SourceText.Lines.GetLineFromPosition(CursorPosition);
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

            protected static SyntaxToken GetPlusToken()
            {
                return SyntaxFactory.Token(SyntaxKind.PlusToken).WithTrailingTrivia(
                                    SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));
            }

            private async Task<Document> SplitStringAsync(string indentString)
            {
                var splitString = CreateSplitString(indentString).WithAdditionalAnnotations(Formatter.Annotation);
                var newRoot = Root.ReplaceNode(GetNodeToReplace(), splitString);

                var document1 = Document.WithSyntaxRoot(newRoot);
                var document2 = await Formatter.FormatAsync(document1, cancellationToken: CancellationToken).ConfigureAwait(false);

                return document2;
            }

            protected static SyntaxTriviaList GetLeadingIndentationTrivia(string indentString)
            {
                return indentString == null
                    ? default(SyntaxTriviaList)
                    : SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(indentString));
            }
        }

        private class SimpleStringSplitter : StringSplitter
        {
            private readonly SyntaxToken _token;

            public SimpleStringSplitter(Document document, int position, SyntaxTree syntaxTree, SyntaxNode root, SourceText sourceText, SyntaxToken token, bool useTabs, int tabSize, CancellationToken cancellationToken)
                : base(document, position, syntaxTree, root, sourceText, useTabs, tabSize, cancellationToken)
            {
                _token = token;
            }

            protected override bool CheckToken()
            {
                if (CursorPosition <= _token.SpanStart || CursorPosition >= _token.Span.End)
                {
                    return false;
                }

                if (_token.IsVerbatimStringLiteral())
                {
                    // Don't split @"" strings.  They already support directly embedding newlines.
                    return false;
                }

                var tokenQuoteLength = GetTokenStart(_token.Text);
                var tokenStart = _token.SpanStart + tokenQuoteLength;
                if (CursorPosition < tokenStart)
                {
                    return false;
                }

                return true;
            }

            protected override SyntaxNode GetNodeToReplace() => _token.Parent;

            protected override BinaryExpressionSyntax CreateSplitString(string indentString)
            {
                // TODO(cyrusn): Deal with the positoin being after a \ character
                var prefix = SourceText.GetSubText(TextSpan.FromBounds(_token.SpanStart, CursorPosition)).ToString();
                var suffix = SourceText.GetSubText(TextSpan.FromBounds(CursorPosition, _token.Span.End)).ToString();

                var tokenStart = GetTokenStart(_token.Text);

                var firstToken = SyntaxFactory.Token(
                    _token.LeadingTrivia,
                    _token.Kind(),
                    text: prefix + '"',
                    valueText: "",
                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.ElasticSpace));

                var secondToken = SyntaxFactory.Token(
                    GetLeadingIndentationTrivia(indentString),
                    _token.Kind(),
                    text: _token.Text.Substring(0, tokenStart) + suffix,
                    valueText: "",
                    trailing: _token.TrailingTrivia);

                var leftExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, firstToken);
                var rightExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, secondToken);

                return SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    leftExpression, GetPlusToken(), rightExpression);
            }
        }

        private class InterpolatedStringSplitter : StringSplitter
        {
            private readonly InterpolatedStringExpressionSyntax _interpolatedStringExpression;

            public InterpolatedStringSplitter(
                Document document, int position,
                SyntaxTree syntaxTree, SyntaxNode root, SourceText sourceText,
                InterpolatedStringExpressionSyntax interpolatedStringExpression,
                bool useTabs, int tabSize, CancellationToken cancellationToken) 
                : base(document, position, syntaxTree, root, sourceText, useTabs, tabSize, cancellationToken)
            {
                _interpolatedStringExpression = interpolatedStringExpression;
            }

            protected override SyntaxNode GetNodeToReplace() => _interpolatedStringExpression;

            protected override bool CheckToken()
            {
                if (_interpolatedStringExpression.StringStartToken.Kind() == SyntaxKind.InterpolatedVerbatimStringStartToken)
                {
                    // Don't offer on $@"" strings.  They support newlines directly in their content.
                    return false;
                }

                return true;
            }

            protected override BinaryExpressionSyntax CreateSplitString(string indentString)
            {
                // var v = $" a b c { expr2 } e f g h { expr2 } i j k"
                //
                // var v = $" a b c { expr1 } e f " +
                //     $"g h { expr2 } i j k"

                var contents = _interpolatedStringExpression.Contents.ToList();

                var beforeSplitContents = new List<InterpolatedStringContentSyntax>();
                var afterSplitContents = new List<InterpolatedStringContentSyntax>();

                foreach (var content in contents)
                {
                    if (content.Span.End <= CursorPosition)
                    {
                        // Content is entirely before the cursor.  Nothing needs to be done to it.
                        beforeSplitContents.Add(content);
                    }
                    else if (content.Span.Start >= CursorPosition)
                    {
                        // Content is entirely after the cursor.  Nothing needs to be done to it.
                        afterSplitContents.Add(content);
                    }
                    else
                    {
                        // Content crosses the cursor.  Need to split it.
                        beforeSplitContents.Add(CreateInterpolatedStringText(content.SpanStart, CursorPosition));
                        afterSplitContents.Insert(0, CreateInterpolatedStringText(CursorPosition, content.Span.End));
                    }
                }

                var leftExpression = SyntaxFactory.InterpolatedStringExpression(
                    _interpolatedStringExpression.StringStartToken,
                    SyntaxFactory.List(beforeSplitContents),
                    SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken));

                var rightExpressionFirstToken = SyntaxFactory.Token(
                    GetLeadingIndentationTrivia(indentString),
                    SyntaxKind.InterpolatedStringStartToken,
                    trailing: default(SyntaxTriviaList));

                var rightExpression = SyntaxFactory.InterpolatedStringExpression(
                    rightExpressionFirstToken,
                    SyntaxFactory.List(afterSplitContents),
                    _interpolatedStringExpression.StringEndToken);

                return SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    leftExpression, GetPlusToken(), rightExpression);
            }

            private InterpolatedStringTextSyntax CreateInterpolatedStringText(int start, int end)
            {
                var content = SourceText.ToString(TextSpan.FromBounds(start, end));
                return SyntaxFactory.InterpolatedStringText(
                    SyntaxFactory.Token(
                        leading: default(SyntaxTriviaList),
                        kind: SyntaxKind.InterpolatedStringTextToken,
                        text: content,
                        valueText: "",
                        trailing: default(SyntaxTriviaList)));
            }
        }
    }
}