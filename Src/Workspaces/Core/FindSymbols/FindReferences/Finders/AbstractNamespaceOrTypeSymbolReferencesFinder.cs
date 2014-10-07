#if false
using System.Collections.Generic;
using System.Threading;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.LanguageServices;

namespace Roslyn.Services.FindReferences
{
    internal abstract class AbstractNamespaceOrTypeSymbolReferencesFinder<TSymbol> : AbstractReferencesFinder<TSymbol>
            where TSymbol : INamespaceOrTypeSymbol
    {
        protected AbstractNamespaceOrTypeSymbolReferencesFinder()
        {
        }

        protected override IEnumerable<ISymbol> DetermineCascadedSymbols(
            TSymbol symbol,
            IDocument document,
            CommonLocation location,
            CancellationToken cancellationToken)
        {
            // We found a location for this namespace.  If it's inside an alias that binds to us,
            // then we want to cascade through that alias.
            if (location.InSource)
            {
                var tree = location.SourceTree;
                var token = tree.Root.FindToken(location.SourceSpan.Start);
                var node = token.Parent;

                var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                if (syntaxFacts.IsRightSideOfQualifiedName(node))
                {
                    node = node.Parent;
                }

                if (syntaxFacts.IsUsingDirectiveName(node))
                {
                    var directive = node.Parent;
                    var aliasSymbol = document.GetSemanticModel(cancellationToken).GetDeclaredSymbol(directive, cancellationToken);
                    if (aliasSymbol != null)
                    {
                        yield return aliasSymbol;
                    }
                }
            }
        }
    }
}
#endif