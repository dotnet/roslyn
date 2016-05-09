using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface ISymbolTreeInfo
    {
        Task<IEnumerable<ISymbol>> FindAsync(SearchQuery query, IAssemblySymbol assembly, CancellationToken cancellationToken);
        Task<IEnumerable<ISymbol>> FindAsync(SearchQuery query, AsyncLazy<IAssemblySymbol> lazyAssembly, CancellationToken cancellationToken);
    }
}