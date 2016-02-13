// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Samples.CodeAction.CopyPasteWithUsing
{
    internal class TokenSymbolPairBuilder : CSharpSyntaxWalker
    {
        private readonly Document document;
        private readonly SemanticModel semanticModel;
        private readonly TextSpan span;
        private readonly List<Tuple<SyntaxToken, ISymbol>> tokenSymbolPair;

        public static List<Tuple<SyntaxToken, ISymbol>> Build(Document document, TextSpan span)
        {
            var tree = (SyntaxTree)document.GetSyntaxTreeAsync(CancellationToken.None).Result;
            if (tree == null)
            {
                return null;
            }

            var builder = new TokenSymbolPairBuilder(document, span);
            builder.Visit(tree.GetRoot());

            return builder.tokenSymbolPair;
        }

        private TokenSymbolPairBuilder(Document document, TextSpan span)
        {
            this.document = document;
            this.span = span;

            this.semanticModel = (SemanticModel)this.document.GetSemanticModelAsync(CancellationToken.None).Result;
            this.tokenSymbolPair = new List<Tuple<SyntaxToken, ISymbol>>();
        }

        public override void Visit(SyntaxNode node)
        {
            var fullSpan = node.FullSpan;
            if (fullSpan.End < this.span.Start || this.span.End < fullSpan.Start)
            {
                return;
            }

            base.Visit(node);
        }

        public override void VisitToken(SyntaxToken token)
        {
            var fullSpan = token.FullSpan;
            if (fullSpan.End < this.span.Start || this.span.End < fullSpan.Start)
            {
                return;
            }

            if (token.IsMissing || token.Span.Length <= 0 || token.Kind() == SyntaxKind.IdentifierToken)
            {
                return;
            }

            var node = token.Parent as ExpressionSyntax;
            if (node == null)
            {
                return;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(node);
            if (symbolInfo.Symbol == null)
            {
                // A token did not bind successfully to a symbol. Skip it.
                return;
            }

            var symbol = symbolInfo.Symbol;
            if (symbol.Kind != SymbolKind.Alias &&
                symbol.Kind != SymbolKind.NamedType &&
                symbol.Kind != SymbolKind.Namespace)
            {
                return;
            }

            this.tokenSymbolPair.Add(Tuple.Create(token, symbol));
        }
    }
}
