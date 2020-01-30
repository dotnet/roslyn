// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
{
    internal abstract class AbstractCommentSplitter
    {
        protected Document _document;
        protected int _cursorPosition;
        protected SourceText _sourceText;
        protected SyntaxNode _root;
        protected int _tabSize;
        protected bool _useTabs;
        protected CancellationToken _cancellationToken;
        protected SyntaxTrivia _trivia;
        protected IndentStyle _indentStyle;

        protected SyntaxNode GetNodeToReplace() => _trivia.SyntaxTree.GetRoot();

        protected abstract SyntaxTriviaList CreateSplitComment(string indentString);
        protected abstract string GetIndentString(SyntaxNode newRoot);

        public int? TrySplit()
        {
            var nodeToReplace = GetNodeToReplace();

            if (_cursorPosition <= nodeToReplace.SpanStart || _cursorPosition >= nodeToReplace.Span.End)
            {
                return null;
            }

            return SplitWorker();
        }

        private int SplitWorker()
        {
            var (newDocument, finalCaretPosition) = SplitComment();
            var workspace = _document.Project.Solution.Workspace;

            workspace.TryApplyChanges(newDocument.Project.Solution);

            return finalCaretPosition;
        }

        private (Document document, int caretPosition) SplitComment()
        {
            var indentString = GetIndentString(_root);
            var nodeToRemove = GetNodeToReplace();

            var splitComment = CreateSplitComment(indentString);
            var commentToReplace = nodeToRemove.FindTrivia(_cursorPosition);
            var newRoot = _root.ReplaceTrivia(commentToReplace, splitComment);

            var newLineNumber = _sourceText.Lines.GetLineFromPosition(_cursorPosition).LineNumber + 1;
            var newPosition = _sourceText.Lines[newLineNumber].GetLastNonWhitespacePosition();
            var newDocument = _document.WithSyntaxRoot(newRoot);

            return (newDocument, newPosition.GetValueOrDefault());
        }
    }
}
