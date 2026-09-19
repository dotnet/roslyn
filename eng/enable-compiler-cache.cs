#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Workaround for https://github.com/dotnet/roslyn/issues/76197.
#:property SignAssembly=false

#:property PublishAot=false
#:package System.CommandLine

using System.CommandLine;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;

// Downloads the compiler cache artifact from the Azure DevOps CI pipeline and sets up
// a local Directory.Build.props.user to enable the caching compiler automatically.
//
// The script tries to find the closest available cache for your current context:
//   1. The current git branch
//   2. The PR merge ref (refs/pull/<number>/merge) if a PR is open
//   3. The base branch of the open PR (detected via `gh pr view`)
//   4. main
//
// Usage:
//   dotnet run --file eng/enable-compiler-cache.cs
//   dotnet run --file eng/enable-compiler-cache.cs -- --configuration Release
//   dotnet run --file eng/enable-compiler-cache.cs -- --configuration Debug --branch my-feature-branch
//   dotnet run --file eng/enable-compiler-cache.cs -- --dry-run

const string AzdoOrg = "dnceng-public";
const string AzdoProject = "public";

// Pipeline definition ID for the public Roslyn CI pipeline.
// https://dev.azure.com/dnceng-public/public/_build?definitionId=95
const int DefaultPipelineDefinitionId = 95;
const string DefaultCacheDirectoryName = "roslyn-cache";

var configOption = new Option<string>("--configuration", "-c")
{
    Description = "Build configuration to look for (Debug or Release).",
    DefaultValueFactory = _ => "Debug",
};
var branchOption = new Option<string?>("--branch", "-b")
{
    Description = "Branch to search for the cache. Defaults to the current git branch, then the PR base branch, then main.",
};
var pipelineIdOption = new Option<int?>("--pipeline-id")
{
    Description = $"Azure DevOps pipeline definition ID. Defaults to {DefaultPipelineDefinitionId}.",
};
var buildIdOption = new Option<int?>("--build-id")
{
    Description = "Download the cache from a specific Azure DevOps build ID.",
};
var dryRunOption = new Option<bool>("--dry-run")
{
    Description = "Show what would be done without downloading or writing any files.",
};

var rootCommand = new RootCommand("Downloads the Roslyn compiler cache from CI and enables it for local builds.")
{
    configOption,
    branchOption,
    pipelineIdOption,
    buildIdOption,
    dryRunOption,
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var configuration = parseResult.GetValue(configOption)!;
    var branch = parseResult.GetValue(branchOption);
    var pipelineId = parseResult.GetValue(pipelineIdOption);
    var buildId = parseResult.GetValue(buildIdOption);
    var dryRun = parseResult.GetValue(dryRunOption);

    await RunAsync(configuration, branch, pipelineId, buildId, dryRun, cancellationToken).ConfigureAwait(false);
});

return await rootCommand.Parse(args).InvokeAsync(new InvocationConfiguration(), CancellationToken.None).ConfigureAwait(false);

static async Task RunAsync(
    string configuration,
    string? explicitBranch,
    int? explicitPipelineId,
    int? explicitBuildId,
    bool dryRun,
    CancellationToken cancellationToken)
{
    var repoRoot = Path.GetFullPath(Path.Join(
        AppContext.GetData("EntryPointFileDirectoryPath") as string
            ?? throw new InvalidOperationException("Could not determine the script's directory path."),
        ".."));
    if (!Directory.Exists(repoRoot))
        throw new InvalidOperationException($"Could not locate repo root: {repoRoot}");

    var azdoBaseUrl = $"https://dev.azure.com/{AzdoOrg}/{AzdoProject}";

    var os = GetCurrentOsName();
    var artifactName = $"compiler-cache-{os}-{configuration.ToLowerInvariant()}";

    var existingCachePath = Environment.GetEnvironmentVariable("ROSLYN_CACHE_PATH");
    var useCachingCompiler = string.Equals(Environment.GetEnvironmentVariable("ROSLYN_USE_CACHING_COMPILER"), "true", StringComparison.OrdinalIgnoreCase);
    var alreadyEnabled = useCachingCompiler || !string.IsNullOrEmpty(existingCachePath);
    var cacheDestination = existingCachePath switch
    {
        { Length: > 0 } => existingCachePath,
        _ when useCachingCompiler => GetDefaultGlobalCachePath(),
        _ => Path.Combine(repoRoot, "artifacts", "compiler-cache"),
    };

    Console.WriteLine($"OS:            {os}");
    Console.WriteLine($"Configuration: {configuration}");
    Console.WriteLine($"Artifact:      {artifactName}");
    Console.WriteLine($"Cache path:    {cacheDestination}");
    if (alreadyEnabled)
        Console.WriteLine($"Compiler cache is already enabled globally.");
    Console.WriteLine();

    if (dryRun)
    {
        Console.WriteLine("(dry run — no files will be written)");
        Console.WriteLine();
    }

    using var httpClient = CreateHttpClient();

    (int buildId, string? sourceBranch) buildInfo;

    if (explicitBuildId is int bid)
    {
        Console.WriteLine($"Using explicit build ID: {bid}");
        buildInfo = (bid, null);
    }
    else
    {
        // Resolve pipeline definition ID.
        int pipelineDefinitionId;
        if (explicitPipelineId is int pid)
        {
            pipelineDefinitionId = pid;
            Console.WriteLine($"Using pipeline definition ID: {pipelineDefinitionId} (from --pipeline-id)");
        }
        else
        {
            pipelineDefinitionId = DefaultPipelineDefinitionId;
            Console.WriteLine($"Using pipeline definition ID: {pipelineDefinitionId}");
        }

        Console.WriteLine();

        // Determine branch fallback sequence.
        var branches = GetBranchFallbackSequence(explicitBranch, repoRoot);
        Console.WriteLine($"Branch fallback sequence: {string.Join(" → ", branches)}");
        Console.WriteLine();

        // Search for a build with the artifact on each branch.
        (int id, string branch)? found = null;
        foreach (var branch in branches)
        {
            Console.Write($"  Looking for builds on '{branch}'...");
            var foundBuildId = await FindLatestBuildWithArtifactAsync(httpClient, azdoBaseUrl, pipelineDefinitionId, branch, artifactName).ConfigureAwait(false);
            if (foundBuildId is int id)
            {
                Console.WriteLine($" found build {id}");
                found = (id, branch);
                break;
            }
            else
            {
                Console.WriteLine(" none found");
            }
        }

        Console.WriteLine();

        if (found is null)
        {
            if (!dryRun)
            {
                if (!alreadyEnabled)
                {
                    Console.WriteLine("No suitable build found. The compiler cache will be enabled without a pre-populated cache.");
                    Console.WriteLine("The cache will be built from scratch as you compile.");
                    Console.WriteLine();

                    WriteUserPropsFile(repoRoot, cacheDestination: null);

                    Console.WriteLine();
                    Console.WriteLine("Done.");
                    Console.WriteLine();
                    Console.WriteLine("The Directory.Build.props.user file has been created (or updated) to enable the");
                    Console.WriteLine("compiler cache automatically for all builds in this repository.");
                    Console.WriteLine();
                    Console.WriteLine("To disable local caching, delete Directory.Build.props.user or set the");
                    Console.WriteLine("ROSLYN_USE_CACHING_COMPILER environment variable to 'false'.");
                }
                else
                {
                    Console.WriteLine("No suitable build found. The compiler cache is already enabled globally;");
                    Console.WriteLine("the cache will be built from scratch as you compile.");
                }
            }
            else
            {
                Console.WriteLine("No suitable build found.");
                Console.WriteLine();
                Console.WriteLine("Dry run complete. Run without --dry-run to enable the compiler cache.");
            }

            return;
        }

        buildInfo = found.Value;
    }

    var sourceBranchDisplay = buildInfo.sourceBranch is not null ? $" (branch: {buildInfo.sourceBranch})" : "";
    Console.WriteLine($"Downloading compiler cache from build {buildInfo.buildId}{sourceBranchDisplay}...");

    var (downloadUrl, artifactSize) = await GetArtifactDownloadUrlAsync(httpClient, azdoBaseUrl, buildInfo.buildId, artifactName).ConfigureAwait(false);

    if (artifactSize.HasValue)
        Console.WriteLine($"  Artifact size (uncompressed): {artifactSize.Value / 1024 / 1024:N0} MB");

    if (!dryRun)
    {
        await DownloadAndExtractArtifactAsync(httpClient, downloadUrl, artifactName, cacheDestination, artifactSize, cancellationToken).ConfigureAwait(false);

        Console.WriteLine();
        Console.WriteLine($"Compiler cache extracted to: {cacheDestination}");

        if (!alreadyEnabled)
        {
            WriteUserPropsFile(repoRoot, cacheDestination);

            Console.WriteLine();
            Console.WriteLine("Done.");
            Console.WriteLine();
            Console.WriteLine("The Directory.Build.props.user file has been created (or updated) to enable the");
            Console.WriteLine("compiler cache automatically for all builds in this repository.");
            Console.WriteLine();
            Console.WriteLine("To disable local caching, delete Directory.Build.props.user or set the");
            Console.WriteLine("ROSLYN_USE_CACHING_COMPILER environment variable to 'false'.");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("Done. The compiler cache is already enabled globally; no props file changes needed.");
        }
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine("Dry run complete. Run without --dry-run to download and configure the cache.");
    }
}

static string GetCurrentOsName()
{
    if (OperatingSystem.IsWindows()) return "windows";
    if (OperatingSystem.IsMacOS()) return "macos";
    return "linux";
}

static string GetDefaultGlobalCachePath()
{
    var parentPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    if (string.IsNullOrEmpty(parentPath))
        throw new InvalidOperationException("Could not determine the compiler's default global cache path because LocalApplicationData is unavailable.");

    return Path.Combine(parentPath, DefaultCacheDirectoryName);
}

static List<string> GetBranchFallbackSequence(string? explicitBranch, string repoRoot)
{
    var branches = new List<string>();

    if (explicitBranch is not null)
    {
        branches.Add(NormalizeBranchName(explicitBranch));
    }
    else
    {
        var currentBranch = GetCurrentGitBranch(repoRoot);
        if (currentBranch is not null)
        {
            branches.Add(currentBranch);

            // Try to detect the PR via the `gh` CLI.
            var prInfo = GetPrInfo(repoRoot);
            if (prInfo is (int prNumber, string prBaseBranch))
            {
                // PR builds in Azure DevOps use refs/pull/<number>/merge as the source branch.
                var prMergeRef = $"refs/pull/{prNumber}/merge";
                if (!branches.Contains(prMergeRef, StringComparer.OrdinalIgnoreCase))
                    branches.Add(prMergeRef);

                var normalizedPrBase = NormalizeBranchName(prBaseBranch);
                if (!branches.Contains(normalizedPrBase, StringComparer.OrdinalIgnoreCase))
                    branches.Add(normalizedPrBase);
            }
        }
    }

    // Always include main as the final fallback.
    if (!branches.Any(b => string.Equals(b, "refs/heads/main", StringComparison.OrdinalIgnoreCase)))
    {
        branches.Add("refs/heads/main");
    }

    return branches;
}

static string NormalizeBranchName(string branch)
{
    // Azure DevOps expects full refs like refs/heads/main
    if (branch.StartsWith("refs/", StringComparison.OrdinalIgnoreCase))
        return branch;
    return $"refs/heads/{branch}";
}

static string? GetCurrentGitBranch(string repoRoot)
{
    try
    {
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = "rev-parse --abbrev-ref HEAD",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output) || output == "HEAD")
            return null;
        return NormalizeBranchName(output);
    }
    catch
    {
        return null;
    }
}

static (int number, string baseBranch)? GetPrInfo(string repoRoot)
{
    try
    {
        // First try `gh pr view` which works when the branch is on the same repo.
        var result = RunGhPrView(repoRoot);
        if (result is not null)
            return result;

        // For fork PRs, `gh pr view` won't find the PR. Fall back to `gh pr list --head <branch>`.
        var currentBranch = GetCurrentGitBranch(repoRoot);
        if (currentBranch is null)
            return null;

        // Strip refs/heads/ prefix for the --head filter.
        var branchName = currentBranch.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase)
            ? currentBranch["refs/heads/".Length..]
            : currentBranch;

        return RunGhPrList(repoRoot, branchName);
    }
    catch
    {
        return null;
    }
}

static (int number, string baseBranch)? RunGhPrView(string repoRoot)
{
    try
    {
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "gh",
            Arguments = "pr view --json number,baseRefName",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            return null;

        return ParsePrJson(output);
    }
    catch
    {
        return null;
    }
}

static (int number, string baseBranch)? RunGhPrList(string repoRoot, string headBranch)
{
    try
    {
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "gh",
            Arguments = $"pr list --head {headBranch} --json number,baseRefName --limit 1",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            return null;

        // `gh pr list` returns a JSON array.
        using var doc = JsonDocument.Parse(output);
        var arr = doc.RootElement;
        if (arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
            return null;

        return ParsePrJsonElement(arr[0]);
    }
    catch
    {
        return null;
    }
}

static (int number, string baseBranch)? ParsePrJson(string json)
{
    using var doc = JsonDocument.Parse(json);
    return ParsePrJsonElement(doc.RootElement);
}

static (int number, string baseBranch)? ParsePrJsonElement(JsonElement element)
{
    if (element.TryGetProperty("number", out var numberProp) &&
        element.TryGetProperty("baseRefName", out var baseProp))
    {
        var number = numberProp.GetInt32();
        var baseBranch = baseProp.GetString();
        if (number > 0 && !string.IsNullOrWhiteSpace(baseBranch))
            return (number, baseBranch);
    }

    return null;
}

static async Task<int?> FindLatestBuildWithArtifactAsync(
    HttpClient client,
    string azdoBaseUrl,
    int definitionId,
    string branchName,
    string artifactName)
{
    var url = $"{azdoBaseUrl}/_apis/build/builds" +
              $"?definitions={definitionId}" +
              $"&branchName={Uri.EscapeDataString(branchName)}" +
              $"&statusFilter=completed" +
              $"&$top=5" +
              $"&api-version=7.1";

    using var response = await client.GetAsync(url).ConfigureAwait(false);
    if (!response.IsSuccessStatusCode)
        return null;

    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    using var document = JsonDocument.Parse(body);
    var builds = document.RootElement.GetProperty("value");

    foreach (var build in builds.EnumerateArray())
    {
        var buildId = build.GetProperty("id").GetInt32();
        if (await BuildHasArtifactAsync(client, azdoBaseUrl, buildId, artifactName).ConfigureAwait(false))
            return buildId;
    }

    return null;
}

static async Task<bool> BuildHasArtifactAsync(HttpClient client, string azdoBaseUrl, int buildId, string artifactName)
{
    var url = $"{azdoBaseUrl}/_apis/build/builds/{buildId}/artifacts?artifactName={Uri.EscapeDataString(artifactName)}&api-version=7.1";
    using var response = await client.GetAsync(url).ConfigureAwait(false);
    return response.IsSuccessStatusCode;
}

static async Task<(string downloadUrl, long? size)> GetArtifactDownloadUrlAsync(HttpClient client, string azdoBaseUrl, int buildId, string artifactName)
{
    var url = $"{azdoBaseUrl}/_apis/build/builds/{buildId}/artifacts?artifactName={Uri.EscapeDataString(artifactName)}&api-version=7.1";
    using var response = await client.GetAsync(url).ConfigureAwait(false);
    if (!response.IsSuccessStatusCode)
        throw new InvalidOperationException($"Failed to get artifact info ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}");

    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    using var document = JsonDocument.Parse(body);
    var root = document.RootElement;

    long? artifactSize = null;
    if (root.TryGetProperty("resource", out var resource))
    {
        if (resource.TryGetProperty("properties", out var properties) &&
            properties.TryGetProperty("artifactsize", out var sizeProp) &&
            long.TryParse(sizeProp.GetString(), out var parsedSize))
        {
            artifactSize = parsedSize;
        }

        if (resource.TryGetProperty("downloadUrl", out var downloadUrlProp))
        {
            var downloadUrl = downloadUrlProp.GetString();
            if (!string.IsNullOrEmpty(downloadUrl))
                return (downloadUrl, artifactSize);
        }
    }

    throw new InvalidOperationException($"Artifact '{artifactName}' does not have a download URL.");
}

static async Task DownloadAndExtractArtifactAsync(HttpClient client, string downloadUrl, string artifactName, string destination, long? artifactSize, CancellationToken cancellationToken)
{
    // Ensure we request a zip format if the URL doesn't already include it.
    if (!downloadUrl.Contains("$format=zip", StringComparison.OrdinalIgnoreCase) &&
        !downloadUrl.Contains("%24format=zip", StringComparison.OrdinalIgnoreCase))
    {
        var separator = downloadUrl.Contains('?') ? "&" : "?";
        downloadUrl = $"{downloadUrl}{separator}$format=zip";
    }

    using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    if (!response.IsSuccessStatusCode)
        throw new InvalidOperationException($"Failed to download artifact ({(int)response.StatusCode}).");

    // The artifact size from the API is the pipeline artifact's logical size, not necessarily
    // the compressed zip download size. Only use Content-Length for percentage progress.
    var contentLength = response.Content.Headers.ContentLength;
    if (!contentLength.HasValue && artifactSize.HasValue)
        Console.WriteLine("  Compressed download size is not available; showing compressed bytes downloaded.");

    var tempZipPath = Path.GetTempFileName();
    try
    {
        using (var zipStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
        {
            await CopyWithProgressAsync(zipStream, fileStream, contentLength, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine("Extracting...");

        if (Directory.Exists(destination))
        {
            try
            {
                Directory.Delete(destination, recursive: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Could not remove the existing cache directory '{destination}'. " +
                    $"Close any processes that may have files open in that directory and try again. " +
                    $"Inner error: {ex.Message}", ex);
            }
        }
        Directory.CreateDirectory(destination);

        ExtractZipToDirectory(tempZipPath, artifactName, destination);
    }
    finally
    {
        File.Delete(tempZipPath);
    }
}

static void ExtractZipToDirectory(string zipPath, string artifactName, string destination)
{
    using var archive = ZipFile.OpenRead(zipPath);

    // Pipeline Artifacts may wrap contents in a top-level folder named after the artifact.
    // Detect this and strip it when extracting.
    var entries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
    var wrapperPrefix = artifactName + "/";
    var hasWrapper = entries.Count > 0 &&
        entries.All(e => e.FullName.StartsWith(wrapperPrefix, StringComparison.OrdinalIgnoreCase));
    var stripLength = hasWrapper ? wrapperPrefix.Length : 0;

    foreach (var entry in archive.Entries)
    {
        if (string.IsNullOrEmpty(entry.Name))
            continue; // directory entry

        var relativePath = entry.FullName[stripLength..].Replace('/', Path.DirectorySeparatorChar);
        var targetPath = Path.Combine(destination, relativePath);
        var targetDir = Path.GetDirectoryName(targetPath)!;

        Directory.CreateDirectory(targetDir);

        entry.ExtractToFile(targetPath, overwrite: true);
    }
}

static async Task CopyWithProgressAsync(Stream source, Stream destination, long? totalBytes, CancellationToken cancellationToken)
{
    const long ReportIntervalBytes = 10 * 1024 * 1024;

    var buffer = new byte[81920];
    long bytesRead = 0;
    long lastReportedBytes = 0;
    int read;
    while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
    {
        await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        bytesRead += read;
        if (totalBytes is > 0)
        {
            var percent = bytesRead * 100 / totalBytes.Value;
            Console.Write($"\r  Progress: {percent}% ({bytesRead / 1024 / 1024:N0} / {totalBytes.Value / 1024 / 1024:N0} MB)  ");
        }
        else if (bytesRead - lastReportedBytes >= ReportIntervalBytes)
        {
            Console.Write($"\r  Downloaded (compressed): {bytesRead / 1024 / 1024:N0} MB  ");
            lastReportedBytes = bytesRead;
        }
    }
    if (totalBytes is > 0)
        Console.WriteLine();
    else if (bytesRead > 0)
        Console.WriteLine($"\r  Downloaded (compressed): {bytesRead / 1024 / 1024:N0} MB  ");
    else
        Console.WriteLine();
}

static void WriteUserPropsFile(string repoRoot, string? cacheDestination)
{
    var propsFilePath = Path.Combine(repoRoot, "Directory.Build.props.user");

    string content;
    if (cacheDestination is not null)
    {
        // Use a relative MSBuild path via $(MSBuildThisFileDirectory) so the file is portable
        // even if the repo is moved, as long as the cache sits under the repo root.
        string cachePath;
        try
        {
            cachePath = "$(MSBuildThisFileDirectory)" + Path.GetRelativePath(repoRoot, cacheDestination).Replace('\\', '/');
        }
        catch
        {
            // Fall back to the absolute path if the relative path cannot be computed.
            cachePath = cacheDestination.Replace('\\', '/');
        }

        content = $"""
            <!-- This file is auto-generated by eng/enable-compiler-cache.cs and is gitignored. -->
            <!-- Delete this file or set ROSLYN_USE_CACHING_COMPILER=false to disable local compiler caching. -->
            <Project>
              <PropertyGroup>
                <ROSLYN_CACHE_PATH Condition="'$(ROSLYN_CACHE_PATH)' == ''">{cachePath}</ROSLYN_CACHE_PATH>
              </PropertyGroup>
            </Project>
            """;
    }
    else
    {
        // No cache downloaded — just enable the caching compiler with the global cache.
        content = """
            <!-- This file is auto-generated by eng/enable-compiler-cache.cs and is gitignored. -->
            <!-- Delete this file or set ROSLYN_USE_CACHING_COMPILER=false to disable local compiler caching. -->
            <Project>
              <PropertyGroup>
                <ROSLYN_USE_CACHING_COMPILER Condition="'$(ROSLYN_USE_CACHING_COMPILER)' == ''">true</ROSLYN_USE_CACHING_COMPILER>
              </PropertyGroup>
            </Project>
            """;
    }

    File.WriteAllText(propsFilePath, content);
    Console.WriteLine($"Wrote: {propsFilePath}");
}

static HttpClient CreateHttpClient()
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("roslyn-enable-compiler-cache", "1.0"));
    // Support optional PAT for private pipelines or to raise rate limits.
    var pat = Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN")
           ?? Environment.GetEnvironmentVariable("AZDO_PAT");
    if (!string.IsNullOrEmpty(pat))
    {
        var encoded = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{pat}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
    }
    return client;
}
