using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Assets
{
    /// <summary>
    /// temporary stuff. I have this so that I can clearly separate external stuff from roslyn
    /// to see what info I actually need from it
    /// </summary>
    public interface ISolutionAsset
    {
        Task<IEnumerable<IProjectAsset>> GetProjectsAsync(CancellationToken cancellationToken);
    }
}
