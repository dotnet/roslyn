#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;

// Downloads the compiler cache artifact from the Azure DevOps CI pipeline and sets up
// a local Directory.Build.props.user to enable the caching compiler automatically.
//
// The script tries to find the closest available cache for your current context:
//   1. The current git branch
//   2. main
//
// Usage:
//   dotnet run --file eng/hydrate-compiler-cache.cs
//   dotnet run --file eng/hydrate-compiler-cache.cs -- --configuration Release
//   dotnet run --file eng/hydrate-compiler-cache.cs -- --configuration Debug --branch my-feature-branch
//   dotnet run --file eng/hydrate-compiler-cache.cs -- --dry-run

const string AzdoOrg = "dnceng";
const string AzdoProject = "public";

// Name of the azure-pipelines.yml file used to auto-discover the pipeline definition.
const string PipelineYamlFilename = "azure-pipelines.yml";

try
{
    await MainAsync(args).ConfigureAwait(false);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

return;

static async Task MainAsync(string[] args)
{
    var options = ParseArgs(args);

    var repoRoot = Path.GetFullPath(Path.Join(
        AppContext.GetData("EntryPointFileDirectoryPath") as string
            ?? throw new InvalidOperationException("Could not determine the script's directory path."),
        ".."));
    if (!Directory.Exists(repoRoot))
        throw new InvalidOperationException($"Could not locate repo root: {repoRoot}");

    var azdoBaseUrl = $"https://dev.azure.com/{AzdoOrg}/{AzdoProject}";

    var configuration = options.Configuration;
    var os = GetCurrentOsName();
    var artifactName = $"compiler-cache-{os}-{configuration.ToLowerInvariant()}";
    var cacheDestination = Path.Combine(repoRoot, "artifacts", "compiler-cache");

    Console.WriteLine($"OS:            {os}");
    Console.WriteLine($"Configuration: {configuration}");
    Console.WriteLine($"Artifact:      {artifactName}");
    Console.WriteLine($"Cache path:    {cacheDestination}");
    Console.WriteLine();

    if (options.DryRun)
    {
        Console.WriteLine("(dry run — no files will be written)");
        Console.WriteLine();
    }

    using var httpClient = CreateHttpClient();

    (int buildId, string? sourceBranch) buildInfo;

    if (options.ExplicitBuildId is int explicitBuildId)
    {
        Console.WriteLine($"Using explicit build ID: {explicitBuildId}");
        buildInfo = (explicitBuildId, null);
    }
    else
    {
        // Discover pipeline definition ID.
        int pipelineDefinitionId;
        if (options.PipelineDefinitionId is int explicitId)
        {
            pipelineDefinitionId = explicitId;
            Console.WriteLine($"Using pipeline definition ID: {pipelineDefinitionId} (from --pipeline-id)");
        }
        else
        {
            Console.Write("Discovering pipeline definition ID...");
            pipelineDefinitionId = await DiscoverPipelineDefinitionIdAsync(httpClient, azdoBaseUrl, PipelineYamlFilename).ConfigureAwait(false);
            Console.WriteLine($" {pipelineDefinitionId}");
        }

        Console.WriteLine();

        // Determine branch fallback sequence.
        var branches = GetBranchFallbackSequence(options.Branch, repoRoot);
        Console.WriteLine($"Branch fallback sequence: {string.Join(" → ", branches)}");
        Console.WriteLine();

        // Search for a build with the artifact on each branch.
        (int id, string branch)? found = null;
        foreach (var branch in branches)
        {
            Console.Write($"  Looking for builds on '{branch}'...");
            var buildId = await FindLatestBuildWithArtifactAsync(httpClient, azdoBaseUrl, pipelineDefinitionId, branch, artifactName).ConfigureAwait(false);
            if (buildId is int id)
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
            Console.WriteLine("No suitable build found. Try running the script again once a CI build completes on your branch or main.");
            Console.WriteLine();
            Console.WriteLine("If you have a specific build ID, you can pass it directly:");
            Console.WriteLine("  dotnet run --file eng/hydrate-compiler-cache.cs -- --build-id <id>");
            return;
        }

        buildInfo = found.Value;
    }

    var sourceBranchDisplay = buildInfo.sourceBranch is not null ? $" (branch: {buildInfo.sourceBranch})" : "";
    Console.WriteLine($"Downloading compiler cache from build {buildInfo.buildId}{sourceBranchDisplay}...");

    var downloadUrl = await GetArtifactDownloadUrlAsync(httpClient, azdoBaseUrl, buildInfo.buildId, artifactName).ConfigureAwait(false);

    if (!options.DryRun)
    {
        await DownloadAndExtractArtifactAsync(httpClient, downloadUrl, artifactName, cacheDestination).ConfigureAwait(false);

        Console.WriteLine();
        Console.WriteLine($"Compiler cache extracted to: {cacheDestination}");

        WriteUserPropsFile(repoRoot, cacheDestination);

        Console.WriteLine();
        Console.WriteLine("Done.");
        Console.WriteLine();
        Console.WriteLine("The Directory.Build.props.user file has been created (or updated) to enable the");
        Console.WriteLine("caching compiler automatically for all builds in this repository.");
        Console.WriteLine();
        Console.WriteLine("To disable local caching, delete Directory.Build.props.user or set the");
        Console.WriteLine("ROSLYN_USE_CACHING_COMPILER environment variable to 'false'.");
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
        if (currentBranch is not null && !string.Equals(currentBranch, "main", StringComparison.OrdinalIgnoreCase))
        {
            branches.Add(currentBranch);
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

static async Task<int> DiscoverPipelineDefinitionIdAsync(HttpClient client, string azdoBaseUrl, string yamlFilename)
{
    var url = $"{azdoBaseUrl}/_apis/build/definitions?api-version=7.1";
    var allDefinitions = new List<JsonElement>();
    string? continuationToken = null;

    do
    {
        var pageUrl = continuationToken is not null ? $"{url}&continuationToken={continuationToken}" : url;
        using var response = await client.GetAsync(pageUrl).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to list pipeline definitions ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}");

        continuationToken = response.Headers.TryGetValues("x-ms-continuationtoken", out var tokens) ? tokens.FirstOrDefault() : null;

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var document = JsonDocument.Parse(body);
        foreach (var def in document.RootElement.GetProperty("value").EnumerateArray())
        {
            allDefinitions.Add(def.Clone());
        }
    } while (continuationToken is not null);

    // Find the definition whose YAML filename matches (process.yamlFilename or process.filename).
    foreach (var def in allDefinitions)
    {
        if (def.TryGetProperty("process", out var process))
        {
            string? yf = null;
            if (process.TryGetProperty("yamlFilename", out var yfProp))
                yf = yfProp.GetString();
            else if (process.TryGetProperty("filename", out var fProp))
                yf = fProp.GetString();

            if (yf is not null && string.Equals(
                yf.TrimStart('/').TrimStart('\\'),
                yamlFilename.TrimStart('/').TrimStart('\\'),
                StringComparison.OrdinalIgnoreCase))
            {
                return def.GetProperty("id").GetInt32();
            }
        }
    }

    throw new InvalidOperationException(
        $"Could not find a pipeline definition for '{yamlFilename}' in {azdoBaseUrl}. " +
        $"Pass the definition ID explicitly with --pipeline-id.");
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
              $"&resultFilter=succeeded" +
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

static async Task<string> GetArtifactDownloadUrlAsync(HttpClient client, string azdoBaseUrl, int buildId, string artifactName)
{
    var url = $"{azdoBaseUrl}/_apis/build/builds/{buildId}/artifacts?artifactName={Uri.EscapeDataString(artifactName)}&api-version=7.1";
    using var response = await client.GetAsync(url).ConfigureAwait(false);
    if (!response.IsSuccessStatusCode)
        throw new InvalidOperationException($"Failed to get artifact info ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}");

    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    using var document = JsonDocument.Parse(body);
    var root = document.RootElement;

    if (root.TryGetProperty("resource", out var resource) &&
        resource.TryGetProperty("downloadUrl", out var downloadUrlProp))
    {
        var downloadUrl = downloadUrlProp.GetString();
        if (!string.IsNullOrEmpty(downloadUrl))
            return downloadUrl;
    }

    throw new InvalidOperationException($"Artifact '{artifactName}' does not have a download URL.");
}

static async Task DownloadAndExtractArtifactAsync(HttpClient client, string downloadUrl, string artifactName, string destination)
{
    Console.WriteLine("Downloading artifact...");

    // Ensure we request a zip format if the URL doesn't already include it.
    if (!downloadUrl.Contains("$format=zip", StringComparison.OrdinalIgnoreCase) &&
        !downloadUrl.Contains("%24format=zip", StringComparison.OrdinalIgnoreCase))
    {
        var separator = downloadUrl.Contains('?') ? "&" : "?";
        downloadUrl = $"{downloadUrl}{separator}$format=zip";
    }

    using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
    if (!response.IsSuccessStatusCode)
        throw new InvalidOperationException($"Failed to download artifact ({(int)response.StatusCode}).");

    var contentLength = response.Content.Headers.ContentLength;
    if (contentLength.HasValue)
        Console.WriteLine($"  Size: {contentLength.Value / 1024 / 1024:N0} MB");

    var tempZipPath = Path.GetTempFileName();
    try
    {
        using (var zipStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
        using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
        {
            await CopyWithProgressAsync(zipStream, fileStream, contentLength).ConfigureAwait(false);
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

static async Task CopyWithProgressAsync(Stream source, Stream destination, long? totalBytes)
{
    var buffer = new byte[81920];
    long bytesRead = 0;
    int read;
    while ((read = await source.ReadAsync(buffer).ConfigureAwait(false)) > 0)
    {
        await destination.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
        bytesRead += read;
        if (totalBytes.HasValue)
        {
            var percent = bytesRead * 100 / totalBytes.Value;
            Console.Write($"\r  Progress: {percent}% ({bytesRead / 1024 / 1024:N0} / {totalBytes.Value / 1024 / 1024:N0} MB)  ");
        }
    }
    if (totalBytes.HasValue)
        Console.WriteLine();
}

static void WriteUserPropsFile(string repoRoot, string cacheDestination)
{
    var propsFilePath = Path.Combine(repoRoot, "Directory.Build.props.user");

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

    var content = $"""
        <!-- This file is auto-generated by eng/hydrate-compiler-cache.cs and is gitignored. -->
        <!-- Delete this file or set ROSLYN_USE_CACHING_COMPILER=false to disable local compiler caching. -->
        <Project>
          <PropertyGroup>
            <ROSLYN_CACHE_PATH Condition="'$(ROSLYN_CACHE_PATH)' == ''">{cachePath}</ROSLYN_CACHE_PATH>
          </PropertyGroup>
        </Project>
        """;

    File.WriteAllText(propsFilePath, content);
    Console.WriteLine($"Wrote: {propsFilePath}");
}

static HttpClient CreateHttpClient()
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("roslyn-hydrate-compiler-cache", "1.0"));
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

static Options ParseArgs(string[] args)
{
    var options = new Options();
    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--configuration":
            case "-c":
                if (i + 1 >= args.Length) throw new InvalidOperationException($"Missing value for {args[i]}.");
                options = options with { Configuration = args[++i] };
                break;
            case "--branch":
            case "-b":
                if (i + 1 >= args.Length) throw new InvalidOperationException($"Missing value for {args[i]}.");
                options = options with { Branch = args[++i] };
                break;
            case "--pipeline-id":
                if (i + 1 >= args.Length) throw new InvalidOperationException($"Missing value for {args[i]}.");
                options = options with { PipelineDefinitionId = int.Parse(args[++i]) };
                break;
            case "--build-id":
                if (i + 1 >= args.Length) throw new InvalidOperationException($"Missing value for {args[i]}.");
                options = options with { ExplicitBuildId = int.Parse(args[++i]) };
                break;
            case "--dry-run":
                options = options with { DryRun = true };
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown argument: {args[i]}\n\n" +
                    "Usage: dotnet run --file eng/hydrate-compiler-cache.cs [-- [options]]\n" +
                    "Options:\n" +
                    "  --configuration <Debug|Release>  Build configuration (default: Debug)\n" +
                    "  --branch <name>                  Branch to search (default: current git branch, then main)\n" +
                    "  --pipeline-id <id>               Azure DevOps pipeline definition ID (auto-discovered if not set)\n" +
                    "  --build-id <id>                  Download from a specific build ID\n" +
                    "  --dry-run                        Show what would be done without making changes\n");
        }
    }
    return options;
}

record Options
{
    public string Configuration { get; init; } = "Debug";
    public string? Branch { get; init; }
    public int? PipelineDefinitionId { get; init; }
    public int? ExplicitBuildId { get; init; }
    public bool DryRun { get; init; }
}
