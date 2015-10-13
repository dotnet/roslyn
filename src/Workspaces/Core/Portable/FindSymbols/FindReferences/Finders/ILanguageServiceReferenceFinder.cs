using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal interface ILanguageServiceReferenceFinder : ILanguageService
    {
        Task<IEnumerable<ISymbol>> DetermineCascadedSymbolsAsync(INamedTypeSymbol symbol, Project project, CancellationToken cancellationToken);
    }
}