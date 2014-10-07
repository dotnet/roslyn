#if false

using System.Collections.Generic;
using System.Threading;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.FindReferences
{
    internal class AliasSymbolReferencesFinder : AbstractReferencesFinder<IAliasSymbol>
    {
        protected override bool CanFind(IAliasSymbol symbol)
        {
            return true;
        }

        protected override IEnumerable<ISymbol> DetermineCascadedSymbols(IAliasSymbol symbol, ISolution solution, CancellationToken cancellationToken)
        {
            yield return symbol.Target;
        }

        protected override IEnumerable<IProject> DetermineProjectsToSearch(
            IAliasSymbol symbol,
            ISolution solution,
            CancellationToken cancellationToken)
        {
            return null;

#if false
            // TODO(cyrusn): This method should go away once alias symbols correctly identify
            // which assembly (and therefore which project) they came from.  Right now, aliases
            // say they have null as the containing assembly which is no good.
            var location = symbol.Locations.FirstOrDefault();
            if (location != null && location.InSource)
            {
                var document = solution.GetDocument(location.SourceTree);
                yield return solution.GetProject(document.Id.ProjectId);
            }
#endif
        }

        protected override IEnumerable<IDocument> DetermineDocumentsToSearch(
            IAliasSymbol symbol,
            IProject project,
            CancellationToken cancellationToken)
        {
            return null;

#if false
            // An alias is only available in the source file it was declared in.
            var location = symbol.Locations.FirstOrDefault();
            if (location != null && location.InSource)
            {
                yield return project.GetDocument(location.SourceTree);
            }
#endif
        }

        protected override IEnumerable<ReferenceLocation> FindReferencesInDocument(
            IAliasSymbol symbol,
            IDocument document,
            CancellationToken cancellationToken)
        {
            return null;

#if false
            Func<CommonSyntaxToken, ISemanticModel, bool> symbolsMatch =
                (t, m) => symbol.Equals(m.GetAliasInfo(t.Parent, cancellationToken));

            return FindReferencesInDocumentUsingIdentifier(
                symbol.Name, document, symbolsMatch, cancellationToken);
#endif
        }
    }
}

#endif