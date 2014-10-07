// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class TreeData
    {
        private class StructuredTrivia : TreeData
        {
            private readonly int initialColumn;
            private readonly SyntaxTrivia trivia;
            private readonly TreeData treeData;

            public StructuredTrivia(SyntaxTrivia trivia, int initialColumn) :
                base(trivia.GetStructure())
            {
                Contract.ThrowIfFalse(trivia.HasStructure);

                this.trivia = trivia;

                var root = trivia.GetStructure();
                var text = GetText();

                this.initialColumn = initialColumn;
                this.treeData = (text == null) ? (TreeData)new Node(root) : new NodeAndText(root, text);
            }

            public override string GetTextBetween(SyntaxToken token1, SyntaxToken token2)
            {
                return this.treeData.GetTextBetween(token1, token2);
            }

            public override int GetOriginalColumn(int tabSize, SyntaxToken token)
            {
                if (this.treeData is NodeAndText)
                {
                    return this.treeData.GetOriginalColumn(tabSize, token);
                }

                var text = trivia.ToFullString().Substring(0, token.SpanStart - trivia.FullSpan.Start);

                return text.GetTextColumn(tabSize, initialColumn);
            }

            private SourceText GetText()
            {
                var root = trivia.GetStructure();
                if (root.SyntaxTree != null && root.SyntaxTree.GetText() != null)
                {
                    return root.SyntaxTree.GetText();
                }

                var parent = trivia.Token.Parent;
                if (parent != null && parent.SyntaxTree != null && parent.SyntaxTree.GetText() != null)
                {
                    return parent.SyntaxTree.GetText();
                }

                return null;
            }
        }
    }
}
