using DownloadNetSdkAnalyzers;
using NuGet.Versioning;

var requestedRoslynVersion = SemanticVersion.Parse(args[0]);

if (args is [_, "-sdk-version", ..])
{
    Console.WriteLine(await NetSdkReleaseInfo.GetLatestSdkVersionForRoslynVersionAsync(requestedRoslynVersion));
    return;
}

using var sdkDownloader = await NetSdkReleaseInfo.GetLatestSdkDownloaderForRoslynVersionAsync(requestedRoslynVersion);

var directory = Path.Combine(Path.GetTempPath(), "Metalama", "SdkAnalyzers", sdkDownloader.SdkVersion.ToString());

var completedFilePath = Path.Combine(directory, ".completed");

bool shouldSave = true;

if (Directory.Exists(directory))
{
    if (File.Exists(completedFilePath))
    {
        shouldSave = false;
    }
    else
    {
        // Attempt to delete the directory and recreate it from scratch.
        Directory.Delete(directory, recursive: true);
    }
}

if (shouldSave)
{
    Directory.CreateDirectory(directory);

    foreach (var entry in sdkDownloader.GetAnalyzers())
    {
        var analyzerPath = Path.Combine(directory, entry.Name);

        using var downloadAnalyzerStream = entry.Open();
        using var savingAnalyzerStream = File.OpenWrite(analyzerPath);

        await downloadAnalyzerStream.CopyToAsync(savingAnalyzerStream);
    }

    File.WriteAllText(completedFilePath, "completed");
}

Console.WriteLine(directory);
