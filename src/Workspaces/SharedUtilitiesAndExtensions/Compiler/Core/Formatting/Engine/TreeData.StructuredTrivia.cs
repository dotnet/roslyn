// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting;

internal abstract partial class TreeData
{
    private class StructuredTrivia : TreeData
    {
        private readonly int _initialColumn;
        private readonly SyntaxTrivia _trivia;
        private readonly TreeData _treeData;

        public StructuredTrivia(SyntaxTrivia trivia, int initialColumn)
            : base(trivia.GetStructure()!)
        {
            Contract.ThrowIfFalse(trivia.HasStructure);

            _trivia = trivia;

            var root = trivia.GetStructure()!;
            var text = GetText();

            _initialColumn = initialColumn;
            _treeData = (text == null) ? new Node(root) : new NodeAndText(root, text);
        }

        public override string GetTextBetween(SyntaxToken token1, SyntaxToken token2)
            => _treeData.GetTextBetween(token1, token2);

        public override int GetOriginalColumn(int tabSize, SyntaxToken token)
        {
            if (_treeData is NodeAndText)
            {
                return _treeData.GetOriginalColumn(tabSize, token);
            }

            var text = _trivia.ToFullString()[..(token.SpanStart - _trivia.FullSpan.Start)];

            return text.GetTextColumn(tabSize, _initialColumn);
        }

        private SourceText? GetText()
        {
            var root = _trivia.GetStructure()!;
            if (root.SyntaxTree != null && root.SyntaxTree.GetText() != null)
            {
                return root.SyntaxTree.GetText();
            }

            var parent = _trivia.Token.Parent;
            if (parent != null && parent.SyntaxTree != null && parent.SyntaxTree.GetText() != null)
            {
                return parent.SyntaxTree.GetText();
            }

            return null;
        }
    }
}
