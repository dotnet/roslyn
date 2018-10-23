// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMembrUp.Dialog
{
    internal class SymbolDependentsBuilder : SyntaxWalker
    {
        internal List<ISymbol> SymbolDependentsList { get; }

        private SemanticModel SemanticModel { get; set; }

        private HashSet<ISymbol> SymbolSet { get; set; }

        private Document ContextDocument { get; set; }

        private SymbolDependentsBuilder()
        {
            SymbolDependentsList = new List<ISymbol>();
        }

        internal static List<ISymbol> Build(
            SemanticModel semanticModel,
            ISymbol userSelectedNodeSymbol,
            HashSet<ISymbol> members,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var builder = new SymbolDependentsBuilder()
            {
                SymbolSet = new HashSet<ISymbol>(members),
                SemanticModel = semanticModel,
                ContextDocument = contextDocument
            };

            var selectedSyntax = userSelectedNodeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (selectedSyntax != null)
            {
                builder.Visit(selectedSyntax);
            }
            return builder.SymbolDependentsList;
        }

        public override void Visit(SyntaxNode node)
        {
            var symbol = SemanticModel.GetDeclaredSymbol(node);
            if (symbol != null &&
                (symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Method ||
                symbol.Kind == SymbolKind.Property || symbol.Kind == SymbolKind.Event))
            {
                if (SymbolSet.Contains(symbol))
                {
                    SymbolDependentsList.Add(symbol);
                }
            }
            base.Visit(node);
        }
    }
}
