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

    static NetSdkDownloader()
    {
        s_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PostSharp.Engineering");
    }

    private NetSdkDownloader(ZipArchive archive, SemanticVersion sdkVersion)
    {
        this._archive = archive;
        SdkVersion = sdkVersion;
    }

    public static async Task<NetSdkDownloader> CreateAsync(string url, SemanticVersion sdkVersion)
    {
        Stream? stream = null;
        var retries = 0;
        var maxRetries = 3;
        var delay = TimeSpan.FromSeconds(5);

        while (stream == null)
        {
            try
            {
                stream = await s_httpClient.GetSeekableStreamAsync(url);
            }
            catch (Exception)
            {
                if (retries > maxRetries)
                {
                    throw;
                }

                await Task.Delay(delay);
                retries++;
                delay *= 2;
            }
        }

        var archive = new ZipArchive(stream);

        return new(archive, sdkVersion);
    }

    public SemanticVersion GetCodeAnalysisVersion()
    {
        var codeAnalysisEntry = _archive.Entries.Single(entry => Regex.IsMatch(entry.FullName, "sdk/[^/]+/Roslyn/bincore/Microsoft.CodeAnalysis.dll"));

        using var codeAnalysisStream = codeAnalysisEntry.Open();

        // MetadataLoadContext requires a seekable stream, so copy the assembly to MemoryStream.
        var codeAnalysisMemoryStream = new MemoryStream();
        codeAnalysisStream.CopyTo(codeAnalysisMemoryStream);

        var resolver = new PathAssemblyResolver(new[] { typeof(object).Assembly.Location, Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll") });
        var mlc = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().Name);
        var assembly = mlc.LoadFromStream(codeAnalysisMemoryStream);

        return SemanticVersion.Parse((string)assembly.GetCustomAttributesData().Single(a => GetTypeOrNull(a)?.FullName == typeof(AssemblyInformationalVersionAttribute).FullName).ConstructorArguments.Single().Value!);

        static Type? GetTypeOrNull(CustomAttributeData attributeData)
        {
            try
            {
                return attributeData.AttributeType;
            }
            catch (FileNotFoundException)
            {
                // Could not load some dependent assembly, but we don't care about such attributes.
                return null;
            }
        }
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
