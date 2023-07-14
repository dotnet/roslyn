using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace DownloadNetSdkAnalyzers;

sealed class NetSdkDownloader : IDisposable
{
    private static readonly HttpClient s_httpClient = new();

    private readonly ZipArchive _archive;

    public SemanticVersion SdkVersion { get; }

    private NetSdkDownloader(ZipArchive archive, SemanticVersion sdkVersion)
    {
        this._archive = archive;
        SdkVersion = sdkVersion;
    }

    public static async Task<NetSdkDownloader> CreateAsync(string url, SemanticVersion sdkVersion)
    {
        var stream = await s_httpClient.GetSeekableStreamAsync(url);

        var archive = new ZipArchive(stream);

        return new(archive, sdkVersion);
    }

    public Version GetCodeAnalysisVersion()
    {
        var codeAnalysisEntry = _archive.Entries.Single(entry => Regex.IsMatch(entry.FullName, "sdk/[^/]+/Roslyn/bincore/Microsoft.CodeAnalysis.dll"));

        using var codeAnalysisStream = codeAnalysisEntry.Open();

        // MetadataLoadContext requires a seekable stream, so copy the assembly to MemoryStream.
        var codeAnalysisMemoryStream = new MemoryStream();
        codeAnalysisStream.CopyTo(codeAnalysisMemoryStream);

        var resolver = new PathAssemblyResolver(new[] { typeof(object).Assembly.Location });
        var mlc = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().Name);
        var assembly = mlc.LoadFromStream(codeAnalysisMemoryStream);

        return assembly.GetName().Version!;
    }

    public IEnumerable<ZipArchiveEntry> GetAnalyzers()
        => _archive.Entries.Where(entry =>
            (entry.FullName.Contains("/analyzers/", StringComparison.Ordinal)
             || entry.FullName.Contains("/source-generators/", StringComparison.Ordinal))
            && entry.Name.EndsWith(".dll", StringComparison.Ordinal)
            && !entry.Name.EndsWith(".resources.dll", StringComparison.Ordinal)
            && entry.Name != "System.Collections.Immutable.dll");

    public void Dispose() => _archive.Dispose();
}
