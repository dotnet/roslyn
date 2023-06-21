using System.IO.Compression;
using DownloadNetSdkAnalyzers;

using var sdkDownloader = await NetSdkReleaseInfo.GetLatestStableSdkForRoslynVersionAsync(Version.Parse(args[0]));

var fileName = $"Metalama.Compiler.SdkAnalyzers.{sdkDownloader.SdkVersion}.zip";

var directory = Path.Combine(Path.GetTempPath(), "Metalama", "SdkAnalyzers");

var path = Path.Combine(directory, fileName);

Directory.CreateDirectory(directory);

if (File.Exists(path))
{
    // Checks that the file is a valid archive.
    using var sdkAnalyzersArchive = ZipFile.OpenRead(path);
}
else
{
    try
    {
        using var sdkAnalyzersArchiveStream = File.Open(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);

        using var sdkAnalyzersArchive = new ZipArchive(sdkAnalyzersArchiveStream, ZipArchiveMode.Create);

        foreach (var downloadedAnalyzerEntry in sdkDownloader.GetAnalyzers())
        {
            var newEntry = sdkAnalyzersArchive.CreateEntry(downloadedAnalyzerEntry.Name);

            using var newEntryStream = newEntry.Open();
            using var downloadAnalyzerStream = downloadedAnalyzerEntry.Open();

            await downloadAnalyzerStream.CopyToAsync(newEntryStream);
        }
    }
    catch
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        throw;
    }
}

Console.WriteLine(path);
