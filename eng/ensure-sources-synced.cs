#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using System.Text.Json;

// Verifies or updates the shared source files under `src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms`
// using files from dotnet/sdk at the commit specified in `src/Workspaces/CSharp/Portable/SyncedSource/commitid.txt`.
//
// Usage:
//   dotnet run --file eng/ensure-sources-synced.cs
//   dotnet run --file eng/ensure-sources-synced.cs -- --verify
//   dotnet run --file eng/ensure-sources-synced.cs -- --update
//   dotnet run --file eng/ensure-sources-synced.cs -- --update --sdk-branch <branch>
//   dotnet run --file eng/ensure-sources-synced.cs -- --update --repo-root <path> --fail-on-rate-limit
//
// Default mode when no args are passed: update
//
// When `--sdk-branch` is passed, the `commitid.txt` file is updated from a dotnet/sdk branch on GitHub, before updating the source files.
// This is the preferred way to pick up new upstream changes, e.g.:
//   dotnet run --file eng/ensure-sources-synced.cs -- --sdk-branch release/11.0.1xx
//
// Automation can use --repo-root to update another checkout and --fail-on-rate-limit to require a complete sync.
// Set GITHUB_TOKEN to authenticate GitHub API requests.

try
{
    await MainAsync(args).ConfigureAwait(false);
}
catch (GitHubRateLimitException ex) when (!args.Contains("--fail-on-rate-limit", StringComparer.Ordinal))
{
    LogAzDoWarning($"Skipping synced source check due to GitHub rate limit: {ex.Message}");
}

return;

static async Task MainAsync(string[] args)
{
    var (mode, sdkBranch, repoRoot) = ParseArgs(args);

    var root = repoRoot ?? Path.Join(AppContext.GetData("EntryPointFileDirectoryPath") as string, "..");
    if (!Directory.Exists(root)) throw new InvalidOperationException($"Could not locate repo root: {root}");

    var commitIdPath = Path.Combine(root, "src", "Workspaces", "CSharp", "Portable", "SyncedSource", "commitid.txt");
    if (!File.Exists(commitIdPath)) throw new InvalidOperationException($"'{commitIdPath}' not found.");

    var localSourceDir = Path.Combine(root, "src", "Workspaces", "CSharp", "Portable", "SyncedSource", "FileBasedPrograms");

    var httpClient = CreateHttpClient();

    if (sdkBranch is not null)
    {
        var branchCommit = await GetBranchHeadCommitAsync(httpClient, sdkBranch).ConfigureAwait(false);
        File.WriteAllText(commitIdPath, branchCommit);
        Console.WriteLine($"Updated '{commitIdPath}' to dotnet/sdk '{sdkBranch}' head commit '{branchCommit}'.");
    }

    var sdkCommit = File.ReadAllText(commitIdPath).Trim();
    if (string.IsNullOrWhiteSpace(sdkCommit)) throw new InvalidOperationException($"'{commitIdPath}' is empty.");

    var extensions = new[] { ".cs", ".resx", ".editorconfig" };
    var sourceFiles = await GetDirectoryFilesAsync(
        httpClient,
        sdkCommit,
        githubDirectoryPath: "src/Cli/Microsoft.DotNet.FileBasedPrograms",
        includeFile: static name =>
            name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".resx", StringComparison.OrdinalIgnoreCase),
        mapRelativePath: static name => name).ConfigureAwait(false);

    var commonFiles = await GetDirectoryFilesAsync(
        httpClient,
        sdkCommit,
        githubDirectoryPath: "src/Common",
        includeFile: static name => string.Equals(name, "MSBuildUtilities.cs", StringComparison.OrdinalIgnoreCase),
        mapRelativePath: static name => name).ConfigureAwait(false);

    var editorConfigFiles = await GetDirectoryFilesAsync(
        httpClient,
        sdkCommit,
        githubDirectoryPath: "eng",
        includeFile: static name => string.Equals(name, "SourcePackage.editorconfig", StringComparison.OrdinalIgnoreCase),
        mapRelativePath: static _ => ".editorconfig").ConfigureAwait(false);

    var sourcePackageFiles = sourceFiles
        .Concat(commonFiles)
        .Concat(editorConfigFiles)
        .ToList();
    if (sourcePackageFiles.Count == 0) throw new InvalidOperationException("No source files found in dotnet/sdk.");

    if (mode == SyncMode.Update)
    {
        Directory.CreateDirectory(localSourceDir);
    }

    var expectedRelativePaths = sourcePackageFiles
        .Select(f => f.RelativePath)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var mismatches = new List<string>();
    foreach (var sourceFile in sourcePackageFiles)
    {
        var localFile = Path.Combine(localSourceDir, sourceFile.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        var sourceContent = await GetGitHubStringAsync(httpClient, sourceFile.DownloadUrl).ConfigureAwait(false);

        if (!File.Exists(localFile))
        {
            if (mode == SyncMode.Update)
            {
                var directory = Path.GetDirectoryName(localFile);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(localFile, sourceContent);
            }

            mismatches.Add($"Added missing file: {sourceFile.RelativePath}");
            continue;
        }

        var localContent = File.ReadAllText(localFile);
        if (!string.Equals(localContent.ReplaceLineEndings(), sourceContent.ReplaceLineEndings(), StringComparison.Ordinal))
        {
            if (mode == SyncMode.Update)
                File.WriteAllText(localFile, sourceContent);

            mismatches.Add($"Updated file: {sourceFile.RelativePath}");
        }
    }

    if (Directory.Exists(localSourceDir))
    {
        var localMirrorFiles = Directory.GetFiles(localSourceDir, "*", SearchOption.AllDirectories)
            .Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .Select(f => Path.GetRelativePath(localSourceDir, f).Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var relativePath in expectedRelativePaths)
            localMirrorFiles.Remove(relativePath);

        if (localMirrorFiles.Count > 0)
        {
            if (mode == SyncMode.Verify)
            {
                mismatches.Add("Extra local files (not in dotnet/sdk): " + string.Join(", ", localMirrorFiles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)));
            }
            else
            {
                foreach (var remainingFile in localMirrorFiles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    File.Delete(Path.Combine(localSourceDir, remainingFile));
                    mismatches.Add($"Deleting extra local file (not in dotnet/sdk): {remainingFile}");
                }
            }
        }
    }

    if (mismatches.Count > 0)
    {
        var details = string.Join("\n", mismatches);

        if (mode == SyncMode.Verify)
        {
            throw new InvalidOperationException(
                "Shared source for FileBasedPrograms is out of sync with dotnet/sdk. " +
                "Run `dotnet run --file eng/ensure-sources-synced.cs -- --update` to refresh snapshots. Changes:\n" +
                details);
        }

        Console.WriteLine("Updated synced sources from dotnet/sdk:");
        Console.WriteLine(details);
        return;
    }

    Console.WriteLine("OK");
}

static (SyncMode Mode, string? SdkBranch, string? RepoRoot) ParseArgs(string[] args)
{
    SyncMode? mode = null;
    string? sdkBranch = null;
    string? repoRoot = null;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--update":
                if (mode == SyncMode.Verify)
                    throw new InvalidOperationException("Cannot pass both '--verify' and '--update'.");
                mode = SyncMode.Update;
                break;

            case "--verify":
                if (mode == SyncMode.Update)
                    throw new InvalidOperationException("Cannot pass both '--update' and '--verify'.");
                mode = SyncMode.Verify;
                break;

            case "--sdk-branch":
                i++;
                if (i == args.Length)
                    throw new InvalidOperationException("'--sdk-branch' requires a branch name argument.");

                sdkBranch = args[i];
                break;

            case "--repo-root":
                i++;
                if (i == args.Length)
                    throw new InvalidOperationException("'--repo-root' requires a path argument.");

                repoRoot = Path.GetFullPath(args[i]);
                break;

            case "--fail-on-rate-limit":
                break;

            default:
                throw new InvalidOperationException($"Unrecognized argument: '{args[i]}'. Expected '--update', '--verify', '--sdk-branch <branch>', '--repo-root <path>', or '--fail-on-rate-limit'.");
        }
    }

    mode ??= SyncMode.Update;

    if (sdkBranch is not null && mode != SyncMode.Update)
        throw new InvalidOperationException("'--sdk-branch' is only valid together with '--update' (or the default update mode).");

    return (mode.Value, sdkBranch, repoRoot);
}

static HttpClient CreateHttpClient()
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("roslyn-ensure-sources-synced", "1.0"));
    return client;
}

static async Task<string> GetGitHubStringAsync(HttpClient client, string url)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    if (request.RequestUri?.Host == "api.github.com" && !string.IsNullOrWhiteSpace(token))
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    using var response = await client.SendAsync(request).ConfigureAwait(false);
    var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

    if (IsRateLimitResponse(response.StatusCode, response.Headers, payload))
    {
        throw new GitHubRateLimitException($"GitHub request hit rate limit at '{url}'.");
    }

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException($"GitHub request failed ({(int)response.StatusCode}) at '{url}'.");
    }

    return payload;
}

static async Task<string> GetBranchHeadCommitAsync(HttpClient client, string branch)
{
    var url = $"https://api.github.com/repos/dotnet/sdk/branches/{Uri.EscapeDataString(branch)}";
    var payload = await GetGitHubStringAsync(client, url).ConfigureAwait(false);

    using var document = JsonDocument.Parse(payload);
    if (!document.RootElement.TryGetProperty("commit", out var commitElement) ||
        !commitElement.TryGetProperty("sha", out var shaElement))
    {
        throw new InvalidOperationException($"Unexpected GitHub API response at '{url}'.");
    }

    return shaElement.GetString() ?? throw new InvalidOperationException($"Branch '{branch}' has no commit sha in '{url}'.");
}

static async Task<List<SyncedFile>> GetDirectoryFilesAsync(
    HttpClient client,
    string commit,
    string githubDirectoryPath,
    Func<string, bool> includeFile,
    Func<string, string> mapRelativePath)
{
    var url = $"https://api.github.com/repos/dotnet/sdk/contents/{githubDirectoryPath}?ref={commit}";
    var payload = await GetGitHubStringAsync(client, url).ConfigureAwait(false);

    using var document = JsonDocument.Parse(payload);
    if (document.RootElement.ValueKind != JsonValueKind.Array)
        throw new InvalidOperationException($"Unexpected GitHub API response at '{url}'.");

    var files = new List<SyncedFile>();
    foreach (var item in document.RootElement.EnumerateArray())
    {
        var type = item.GetProperty("type").GetString();
        if (!string.Equals(type, "file", StringComparison.Ordinal))
            continue;

        var name = item.GetProperty("name").GetString()
            ?? throw new InvalidOperationException($"File entry missing 'name' in '{url}'.");
        if (!includeFile(name))
            continue;

        var downloadUrl = item.GetProperty("download_url").GetString();
        if (string.IsNullOrWhiteSpace(downloadUrl))
            throw new InvalidOperationException($"File '{name}' has no download URL in '{url}'.");

        files.Add(new SyncedFile(mapRelativePath(name), downloadUrl));
    }

    files.Sort(static (a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.RelativePath, b.RelativePath));
    return files;
}

static bool IsRateLimitResponse(System.Net.HttpStatusCode statusCode, HttpResponseHeaders headers, string responseBody)
{
    if (statusCode == System.Net.HttpStatusCode.TooManyRequests)
        return true;

    if (statusCode != System.Net.HttpStatusCode.Forbidden)
        return false;

    if (headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues)
        && remainingValues.Any(static v => string.Equals(v, "0", StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    return responseBody.Contains("rate limit", StringComparison.OrdinalIgnoreCase);
}

static void LogAzDoWarning(string message)
{
    Console.WriteLine($"##vso[task.logissue type=warning]{message}");
}

enum SyncMode
{
    Update,
    Verify,
}

readonly record struct SyncedFile(string RelativePath, string DownloadUrl);

sealed class GitHubRateLimitException(string message) : Exception(message);
