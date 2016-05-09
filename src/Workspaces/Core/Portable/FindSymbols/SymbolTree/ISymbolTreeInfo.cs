using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface ISymbolTreeInfo
    {
        Task<IEnumerable<ISymbol>> FindAsync(SearchQuery query, AsyncLazy<IAssemblySymbol> lazyAssembly, CancellationToken cancellationToken);
    }

    internal static class ISymbolTreeInfoExtensions
    {
        public static Task<IEnumerable<ISymbol>> FindAsync(
            this ISymbolTreeInfo info, SearchQuery query, IAssemblySymbol assembly, CancellationToken cancellationToken)
        {
            return info.FindAsync(query, new AsyncLazy<IAssemblySymbol>(assembly), cancellationToken);
        }
    }
}