// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal abstract partial class SyntaxList : SyntaxNode
    {
        internal SyntaxList(InternalSyntax.SyntaxList green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        public override string Language => throw ExceptionUtilities.Unreachable;

        protected override SyntaxTree SyntaxTreeCore => this.Parent.SyntaxTree;

        protected internal override SyntaxNode ReplaceCore<TNode>(IEnumerable<TNode> nodes = null, Func<TNode, TNode, SyntaxNode> computeReplacementNode = null, IEnumerable<SyntaxToken> tokens = null, Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken = null, IEnumerable<SyntaxTrivia> trivia = null, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia = null) =>
            throw ExceptionUtilities.Unreachable;

        protected internal override SyntaxNode ReplaceNodeInListCore(SyntaxNode originalNode, IEnumerable<SyntaxNode> replacementNodes) =>
            throw ExceptionUtilities.Unreachable;

        protected internal override SyntaxNode InsertNodesInListCore(SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore) =>
            throw ExceptionUtilities.Unreachable;

        protected internal override SyntaxNode ReplaceTokenInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens) =>
            throw ExceptionUtilities.Unreachable;

        protected internal override SyntaxNode InsertTokensInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens, bool insertBefore) =>
            throw ExceptionUtilities.Unreachable;

        protected internal override SyntaxNode ReplaceTriviaInListCore(SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia) =>
            throw ExceptionUtilities.Unreachable;

        protected internal override SyntaxNode InsertTriviaInListCore(SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia, bool insertBefore) =>
            throw ExceptionUtilities.Unreachable;

        protected internal override SyntaxNode RemoveNodesCore(IEnumerable<SyntaxNode> nodes, SyntaxRemoveOptions options) =>
            throw ExceptionUtilities.Unreachable;

        protected internal override SyntaxNode NormalizeWhitespaceCore(string indentation, string eol, bool elasticTrivia) =>
            throw ExceptionUtilities.Unreachable;

        protected override bool IsEquivalentToCore(SyntaxNode node, bool topLevel = false) =>
            throw ExceptionUtilities.Unreachable;

        internal override SyntaxDiffer GetDiffer(SyntaxNode newNode, bool computeNewText) =>
            throw ExceptionUtilities.Unreachable;
    }
}
