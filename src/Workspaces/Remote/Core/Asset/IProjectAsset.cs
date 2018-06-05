using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Assets
{
    /// <summary>
    /// temporary stuff. I have this so that I can clearly separate external stuff from roslyn
    /// to see what info I actually need from it
    /// </summary>
    public interface IProjectAsset
    {
        object Id { get; }

        string LanguageName { get; }

        string Name { get; }

        string FullPath { get; }

        string CommandLineArgs { get; }

        Task<IEnumerable<(string, object)>> GetProjectReferencesAsync(CancellationToken cancellationToken);

        Task<IEnumerable<(string, Stream)>> GetMetadataReferencesAsync(CancellationToken cancellationToken);

        Task<IEnumerable<IDocumentAsset>> GetDocumentsAsync(CancellationToken cancellationToken);
    }
}
