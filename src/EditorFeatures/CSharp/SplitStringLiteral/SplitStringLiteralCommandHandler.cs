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
using Microsoft.VisualStudio.Text.Editor;
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

            return false;
        }

        private bool LineContainsQuote(ITextSnapshotLine line, int caretPosition)
        {
            var snapshot = line.Snapshot;
            for (int i = line.Start; i <= caretPosition; i++)
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

        private abstract class StringSplitter
        {
            protected static readonly SyntaxAnnotation RightNodeAnnotation = new SyntaxAnnotation();

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

                var interpolatedStringExpression = TryGetInterpolatedStringExpression(token, position);
                if (interpolatedStringExpression != null)
                {
                    return new InterpolatedStringSplitter(
                        document, position, syntaxTree, root, 
                        sourceText, interpolatedStringExpression,
                        useTabs, tabSize, cancellationToken);
                }

                return null;
            }

            private static InterpolatedStringExpressionSyntax TryGetInterpolatedStringExpression(
                SyntaxToken token, int position)
            {
                if (token.IsKind(SyntaxKind.InterpolatedStringTextToken) || 
                    token.IsKind(SyntaxKind.InterpolatedStringEndToken) ||
                    IsInterpolationOpenBrace(token, position))
                {
                    return token.GetAncestor<InterpolatedStringExpressionSyntax>();
                }

                return null;
            }

            private static bool IsInterpolationOpenBrace(SyntaxToken token, int position)
            {
                return token.Kind() == SyntaxKind.OpenBraceToken && 
                    token.Parent.IsKind(SyntaxKind.Interpolation) &&
                    position == token.SpanStart;
            }

            protected abstract int StringOpenQuoteLength();

            protected abstract bool CheckToken();

            protected abstract SyntaxNode GetNodeToReplace();

            protected abstract BinaryExpressionSyntax CreateSplitString(string indentString);

            public async Task<int?> TrySplitAsync()
            {
                if (!CheckToken())
                {
                    return null;
                }

                return await TrySplitWorkerAsync().ConfigureAwait(false);
            }

            private async Task<int?> TrySplitWorkerAsync()
            {
                var indentation = await DetermineIndentationAsync().ConfigureAwait(false);
                if (indentation == null)
                {
                    return null;
                }

                var indentString = indentation.Value.CreateIndentationString(UseTabs, TabSize);

                var newDocumentAndCaretPosition = SplitString(indentString);
                var newDocument = newDocumentAndCaretPosition.Item1;
                var finalCaretPosition = newDocumentAndCaretPosition.Item2;

                var workspace = Document.Project.Solution.Workspace;
                workspace.TryApplyChanges(newDocument.Project.Solution);

                return finalCaretPosition;
            }

            protected async Task<int?> DetermineIndentationAsync()
            {
                var newDocumentAndCaretPosition = SplitString(indentString: null);
                var newDocument = newDocumentAndCaretPosition.Item1;

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
                return SyntaxFactory.Token(
                    default(SyntaxTriviaList),
                    SyntaxKind.PlusToken,
                    SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));
            }

            private Tuple<Document, int> SplitString(string indentString)
            {
                var splitString = CreateSplitString(indentString);

                var newRoot = Root.ReplaceNode(GetNodeToReplace(), splitString);
                var rightExpression = newRoot.GetAnnotatedNodes(RightNodeAnnotation).Single();

                var newDocument = Document.WithSyntaxRoot(newRoot);
                return Tuple.Create(newDocument, rightExpression.Span.Start + StringOpenQuoteLength());
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
            private const char QuoteCharacter = '"';
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

                return true;
            }

            protected override SyntaxNode GetNodeToReplace() => _token.Parent;

            protected override BinaryExpressionSyntax CreateSplitString(string indentString)
            {
                // TODO(cyrusn): Deal with the positoin being after a \ character
                var prefix = SourceText.GetSubText(TextSpan.FromBounds(_token.SpanStart, CursorPosition)).ToString();
                var suffix = SourceText.GetSubText(TextSpan.FromBounds(CursorPosition, _token.Span.End)).ToString();

                var firstToken = SyntaxFactory.Token(
                    _token.LeadingTrivia,
                    _token.Kind(),
                    text: prefix + QuoteCharacter,
                    valueText: "",
                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.ElasticSpace));

                var secondToken = SyntaxFactory.Token(
                    GetLeadingIndentationTrivia(indentString),
                    _token.Kind(),
                    text: QuoteCharacter + suffix,
                    valueText: "",
                    trailing: _token.TrailingTrivia);

                var leftExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, firstToken);
                var rightExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, secondToken);

                return SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    leftExpression,
                    GetPlusToken(), 
                    rightExpression.WithAdditionalAnnotations(RightNodeAnnotation));
            }

            protected override int StringOpenQuoteLength() => "\"".Length;
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
                    SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken)
                                 .WithTrailingTrivia(SyntaxFactory.ElasticSpace));

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
                    leftExpression,
                    GetPlusToken(),
                    rightExpression.WithAdditionalAnnotations(RightNodeAnnotation));
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

            protected override int StringOpenQuoteLength() => "$\"".Length;
        }
    }
}