using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
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

            var selectedSyntax = userSelectedNodeSymbol.DeclaringSyntaxReferences.First().GetSyntax(cancellationToken);
            builder.Visit(selectedSyntax);
            return builder.SymbolDependentsList;
        }

        public override void Visit(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.IdentifierName))
            {
                var symbol = SymbolFinder.FindSymbolAtPositionAsync(ContextDocument, node.SpanStart).Result;
                if (SymbolSet.Contains(symbol))
                {
                    SymbolDependentsList.Add(symbol);
                }
            }
            base.Visit(node);
        }
    }
}
