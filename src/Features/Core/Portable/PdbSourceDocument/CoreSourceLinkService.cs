using System;
using System.Composition;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.CodeAnalysis.PooledObjects;
using Octokit;

namespace Microsoft.CodeAnalysis.PdbSourceDocument;

[Export(typeof(ISourceLinkService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CoreSourceLinkService() : ISourceLinkService
{
    private GitHubClient _client = new(
        new ProductHeaderValue("Microsoft.CodeAnalysis.LanguageServer"))
    {
        Credentials = new Credentials(Environment.GetEnvironmentVariable("GITHUB_PAT")),
    };

    public async Task<SourceFilePathResult?> GetSourceFilePathAsync(
        string url,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var uri = new UriBuilder(url);
        if (uri.Host != "raw.githubusercontent.com")
        {
            return null;
        }

        var uriPath = uri.Path[1..]; // trim leading slash
        var tempFile = Path.Combine(Path.GetTempPath(), uriPath);
        if (File.Exists(tempFile))
        {
            return new(tempFile);
        }

        var match = Regex.Match(uriPath, "^([^/]+)/([^/]+)/([^/]+)/(.*)$");
        if (match is not { Success: true, Groups: [_, var owner, var repo, var gitRef, var path] })
        {
            return null;
        }

        var files = await _client.Repository.Content.GetAllContentsByRef(
            owner.Value,
            repo.Value,
            path.Value,
            gitRef.Value);

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(tempFile));
        File.WriteAllText(tempFile, file.Content);

        return new(tempFile);
    }

    public Task<PdbFilePathResult?> GetPdbFilePathAsync(
        string dllPath,
        PEReader peReader,
        bool useDefaultSymbolServers,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Implementation not needed.");
    }
}
