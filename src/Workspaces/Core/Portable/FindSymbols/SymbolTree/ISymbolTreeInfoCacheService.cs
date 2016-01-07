using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.SymbolTree
{
    internal interface ISymbolTreeInfoCacheService : IWorkspaceService
    {
        Task<ValueTuple<bool, SymbolTreeInfo>> TryGetSymbolTreeInfoAsync(Project project, CancellationToken cancellationToken);
        Task<ValueTuple<bool, SymbolTreeInfo>> TryGetSymbolTreeInfoAsync(PortableExecutableReference reference, CancellationToken cancellationToken);
    }
}