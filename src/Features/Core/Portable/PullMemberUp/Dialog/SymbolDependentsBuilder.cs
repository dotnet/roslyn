// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog
{
    internal class SymbolDependentsBuilder : SyntaxWalker
    {
        private ImmutableList<ISymbol>.Builder SymbolDependentsListBuilder { get; }

        private IImmutableSet<ISymbol> _symbolSet;

        private Document _contextDocument;

        private CancellationToken _cancellationToken;

        private SymbolDependentsBuilder()
        {
            SymbolDependentsListBuilder = ImmutableList.CreateBuilder<ISymbol>();
        }

        internal static ImmutableList<ISymbol> Build(
            ISymbol userSelectedNodeSymbol,
            HashSet<ISymbol> members,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var builder = new SymbolDependentsBuilder()
            {
                _symbolSet = members.ToImmutableHashSet(),
                _contextDocument = contextDocument,
                _cancellationToken = cancellationToken
            };

            var selectedSyntax = userSelectedNodeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (selectedSyntax != null)
            {
                builder.Visit(selectedSyntax);
            }

            return builder.SymbolDependentsListBuilder.ToImmutableList();
        }

        public override void Visit(SyntaxNode node)
        {
            var symbol = SymbolFinder.FindSymbolAtPositionAsync(_contextDocument, node.SpanStart, _cancellationToken).Result;
            if (symbol != null &&
                (symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Method ||
                symbol.Kind == SymbolKind.Property || symbol.Kind == SymbolKind.Event))
            {
                if (_symbolSet.Contains(symbol))
                {
                    SymbolDependentsListBuilder.Add(symbol);
                }
            }

            base.Visit(node);
        }
    }
}
