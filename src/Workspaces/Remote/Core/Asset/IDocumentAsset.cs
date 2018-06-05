using System;
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
    public interface IDocumentAsset
    {
        string Name { get; }

        IEnumerable<string> Folders { get; }

        DateTimeOffset? LastModified { get; }

        string FilePath { get; }

        Task<Stream> GetContentAsync(CancellationToken cancellationToken);
    }
}
