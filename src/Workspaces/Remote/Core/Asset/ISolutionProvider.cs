using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Roslyn.Assets
{
    public interface ISolutionProvider
    {
        Task<Solution> CreateSolutionAsync(CancellationToken cancellationToken);
    }
}
