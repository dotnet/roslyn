// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// this provides information about the syntax tree formatting service is formatting.
    /// this provides necessary abstraction between different kinds of syntax trees so that ones that contain
    /// actual text or cache can answer queries more efficiently.
    /// </summary>
    internal abstract partial class TreeData
    {
        public static TreeData Create(SyntaxNode root)
        {
            // either there is no tree or a tree that is not generated from a text.
            var text = default(SourceText);
            if (root.SyntaxTree == null || !root.SyntaxTree.TryGetText(out text))
            {
                return new Node(root);
            }

#if DEBUG
            return new Debug(root, text);
#else
            return new NodeAndText(root, text);
#endif
        }

        public static TreeData Create(SyntaxTrivia trivia, int initialColumn)
        {
            return new StructuredTrivia(trivia, initialColumn);
        }

        private readonly SyntaxNode root;
        private readonly SyntaxToken firstToken;
        private readonly SyntaxToken lastToken;

        public TreeData(SyntaxNode root)
        {
            Contract.ThrowIfNull(root);
            this.root = root;

            this.firstToken = this.root.GetFirstToken(includeZeroWidth: true);
            this.lastToken = this.root.GetLastToken(includeZeroWidth: true);
        }

        public abstract string GetTextBetween(SyntaxToken token1, SyntaxToken token2);
        public abstract int GetOriginalColumn(int tabSize, SyntaxToken token);

        public SyntaxNode Root
        {
            get { return this.root; }
        }

        public bool IsFirstToken(SyntaxToken token)
        {
            return this.firstToken == token;
        }

        public bool IsLastToken(SyntaxToken token)
        {
            return this.lastToken == token;
        }

        public int StartPosition
        {
            get { return this.Root.FullSpan.Start; }
        }

        public int EndPosition
        {
            get { return this.Root.FullSpan.End; }
        }

        public IEnumerable<SyntaxToken> GetApplicableTokens(TextSpan textSpan)
        {
            return this.Root.DescendantTokens(textSpan);
        }
    }
}
