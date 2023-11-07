using DownloadNetSdkAnalyzers;
using NuGet.Versioning;

using var sdkDownloader = await NetSdkReleaseInfo.GetLatestSdkForRoslynVersionAsync(SemanticVersion.Parse(args[0]));

var directory = Path.Combine(Path.GetTempPath(), "Metalama", "SdkAnalyzers", sdkDownloader.SdkVersion.ToString());

Directory.CreateDirectory(directory);

var completedFilePath = Path.Combine(directory, ".completed");

bool shouldSave = true;

if (File.Exists(directory))
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
