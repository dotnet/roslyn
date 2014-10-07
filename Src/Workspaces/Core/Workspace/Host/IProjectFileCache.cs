using System.Threading;

namespace Roslyn.Services.Host
{
    public interface IProjectFileCache
    {
        IProjectFile GetProjectFile(string filePath, IProjectFileLoaderLanguageService loader, CancellationToken cancellationToken = default(CancellationToken));
    }
}