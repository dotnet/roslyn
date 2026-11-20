#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// Usage:
//   dotnet run --file eng/update-fba-sources.cs -- discover
//   dotnet run --file eng/update-fba-sources.cs -- sync <target-checkout>
// Uses GITHUB_REPOSITORY and GITHUB_TOKEN, plus TARGET_BRANCH, ROSLYN_VERSION, SDK_BRANCH,
// and SDK_COMMIT for sync. Writes workflow outputs to GITHUB_OUTPUT, or stdout when run locally.

const string syncedRoot = "src/Workspaces/CSharp/Portable/SyncedSource";
const string sourceRoot = syncedRoot + "/FileBasedPrograms";

if (args is not ["discover"] and not ["sync", _])
    throw new ArgumentException("Expected 'discover' or 'sync <target-checkout>'.");

var repository = GetRequiredEnvironmentVariable("GITHUB_REPOSITORY");
using var client = new HttpClient { BaseAddress = new Uri("https://api.github.com/") };
client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("roslyn-update-fba-sources", "1.0"));
var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
if (!string.IsNullOrWhiteSpace(token))
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

if (args[0] == "discover")
    await DiscoverAsync(client, repository).ConfigureAwait(false);
else
    await SynchronizeAsync(client, repository, Path.GetFullPath(args[1])).ConfigureAwait(false);

static async Task DiscoverAsync(HttpClient client, string repository)
{
    var branches = await GetPagesAsync(client, $"repos/{repository}/branches", GitHubJsonContext.Default.GitHubBranchArray).ConfigureAwait(false);
    var sdkBranches = await GetPagesAsync(client, "repos/dotnet/sdk/branches", GitHubJsonContext.Default.GitHubBranchArray).ConfigureAwait(false);
    var sdks = new List<SdkBranch>();
    foreach (var branch in sdkBranches)
    {
        var release = Regex.Match(branch.Name, @"^release/(\d+)\.\d+\.\dxx(?:-(?:preview|rc)\.?\d+)?$");
        // File-based apps first shipped in .NET 10.
        if (branch.Name != "main" && (!release.Success || int.Parse(release.Groups[1].Value, CultureInfo.InvariantCulture) < 10))
            continue;

        var xml = await ReadFileAsync(client, "dotnet/sdk", "eng/Version.Details.xml", branch.Commit.Sha).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Missing SDK dependency metadata for {branch.Name}.");
        var dependency = XDocument.Parse(xml).Descendants("Dependency")
            .Single(element => (string?)element.Attribute("Name") == "Microsoft.CodeAnalysis.CSharp");
        var version = Version.Parse((dependency.Attribute("Version")?.Value
            ?? throw new InvalidOperationException($"Missing SDK Roslyn version for {branch.Name}.")).Split('-')[0]);
        sdks.Add(new SdkBranch(branch.Name, branch.Commit.Sha, $"{version.Major}.{version.Minor}"));
    }

    var targets = new List<TargetBranch>();
    foreach (var branch in branches)
    {
        if (branch.Name != "main" && !branch.Name.StartsWith("release/", StringComparison.Ordinal))
            continue;

        if (await ReadFileAsync(client, repository, $"{syncedRoot}/commitid.txt", branch.Commit.Sha, optional: true).ConfigureAwait(false) is null)
        {
            Console.WriteLine($"Skipping {branch.Name}: no synced FBA sources.");
            continue;
        }

        var xml = await ReadFileAsync(client, repository, "eng/Versions.props", branch.Commit.Sha).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Missing Roslyn version metadata for {branch.Name}.");
        var version = GetRoslynVersion(xml);
        var sdk = SelectSdk(branch.Name, version, sdks);
        if (sdk is null)
        {
            Console.WriteLine($"::warning::No SDK branch references Roslyn {version} for {branch.Name}. Its update job will fail rather than use another version.");
        }

        targets.Add(new TargetBranch(branch.Name, version, sdk?.Name ?? "", sdk?.Commit ?? ""));
    }

    // Something is wrong if there isn't at least one eligible branch (normally main), even if no sources need updating.
    // The upper bound is GitHub Actions' limit of 256 jobs per matrix.
    if (targets.Count is < 1 or > 256)
        throw new InvalidOperationException($"Expected between 1 and 256 FBA target branches, found {targets.Count}.");

    WriteOutput("matrix", JsonSerializer.Serialize(new BranchMatrix(targets), GitHubJsonContext.Default.BranchMatrix));
}

static SdkBranch? SelectSdk(string branch, string version, List<SdkBranch> sdks)
{
    var matches = sdks.Where(sdk => sdk.Version == version).ToList();
    var comparer = CultureInfo.GetCultureInfo("en").CompareInfo;
    matches.Sort((left, right) =>
    {
        if (left.Name == "main" || right.Name == "main")
        {
            var mainFirst = branch == "main" ? -1 : 1;
            return left.Name == "main" ? mainFirst : -mainFirst;
        }

        var leftBand = left.Name.Split('-')[0];
        var rightBand = right.Name.Split('-')[0];
        var bandOrder = comparer.Compare(rightBand, leftBand, CompareOptions.NumericOrdering);
        if (bandOrder != 0)
            return bandOrder;

        // Prefer a maintained feature-band branch over its preview/RC snapshots.
        if (left.Name == leftBand || right.Name == rightBand)
            return left.Name == leftBand ? -1 : 1;

        return comparer.Compare(right.Name, left.Name, CompareOptions.NumericOrdering);
    });

    return matches.FirstOrDefault();
}

static async Task SynchronizeAsync(HttpClient client, string repository, string target)
{
    var branch = GetRequiredEnvironmentVariable("TARGET_BRANCH");
    var version = GetRequiredEnvironmentVariable("ROSLYN_VERSION");
    var sdkBranch = GetRequiredEnvironmentVariable("SDK_BRANCH");
    var sdkCommit = GetRequiredEnvironmentVariable("SDK_COMMIT");
    if (!Regex.IsMatch(sdkCommit, "^[a-f0-9]{40}$"))
        throw new InvalidOperationException($"No matching SDK branch was found for {branch} (Roslyn {version}).");

    if (GetRoslynVersion(File.ReadAllText(Path.Combine(target, "eng", "Versions.props"))) != version)
        throw new InvalidOperationException($"Roslyn version on {branch} changed since discovery. Rerun this workflow.");

    var sources = Path.Combine(target, sourceRoot);
    var before = Fingerprint(sources);
    var commitFile = Path.Combine(target, syncedRoot, "commitid.txt");
    var previousCommit = File.ReadAllText(commitFile).Trim();
    File.WriteAllText(commitFile, sdkCommit);

    var engDirectory = AppContext.GetData("EntryPointFileDirectoryPath") as string
        ?? throw new InvalidOperationException("Could not locate the automation script directory.");
    var startInfo = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = Path.GetFullPath(Path.Combine(engDirectory, "..")),
        UseShellExecute = false,
    };
    foreach (var argument in new[]
    {
        "run", "--file", Path.Combine(engDirectory, "ensure-sources-synced.cs"),
        "--", "--update", "--repo-root", target, "--fail-on-rate-limit",
    })
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("Could not start the source-sync script.");
    await process.WaitForExitAsync().ConfigureAwait(false);
    if (process.ExitCode != 0)
        throw new InvalidOperationException($"Source-sync script failed with exit code {process.ExitCode}.");

    var snapshot = Fingerprint(sources);
    if (snapshot == before)
    {
        Console.WriteLine($"No FBA source changes for {branch}; ignoring SDK commit-only changes.");
        WriteOutput("changed", "false");
        return;
    }

    var head = $"automation/fba/{branch}";
    var owner = repository.Split('/')[0];
    var pulls = await GetPagesAsync(client,
        $"repos/{repository}/pulls?base={Uri.EscapeDataString(branch)}&head={Uri.EscapeDataString($"{owner}:{head}")}&state=closed",
        GitHubJsonContext.Default.GitHubPullRequestArray).ConfigureAwait(false);
    var marker = $"<!-- fba-source-snapshot: {snapshot} -->";
    var declinedPull = pulls.FirstOrDefault(pull => pull.MergedAt is null && pull.Body?.Contains(marker, StringComparison.Ordinal) == true);
    if (declinedPull is not null)
    {
        Console.WriteLine($"Skipping previously declined FBA source snapshot {snapshot} for {branch}: PR #{declinedPull.Number} (https://github.com/{repository}/pull/{declinedPull.Number}).");
        WriteOutput("changed", "false");
        return;
    }

    WriteOutput("changed", "true");
    WriteOutput("head", head);
    WriteOutput("body", $"""
        Update the synced file-based app sources for `{branch}` (Roslyn {version}) from dotnet/sdk `{sdkBranch}`.

        SDK commit: https://github.com/dotnet/sdk/commit/{sdkCommit}
        Upstream changes: https://github.com/dotnet/sdk/compare/{previousCommit}...{sdkCommit}

        Close this PR without merging to ignore this exact source snapshot. Unrelated SDK commits will not reopen it; new FBA source changes can produce another PR.

        {marker}
        """);
}

static string GetRoslynVersion(string xml)
{
    var document = XDocument.Parse(xml);
    var major = document.Descendants("MajorVersion").Single().Value;
    var minor = document.Descendants("MinorVersion").Single().Value;
    return new Version(int.Parse(major, CultureInfo.InvariantCulture), int.Parse(minor, CultureInfo.InvariantCulture)).ToString();
}

static string Fingerprint(string directory)
{
    var entries = Directory.EnumerateFiles(directory)
        .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) ||
            file.EndsWith(".resx", StringComparison.Ordinal) || Path.GetFileName(file) == ".editorconfig")
        .Select(file => new[]
        {
            Path.GetFileName(file),
            Encoding.UTF8.GetString(File.ReadAllBytes(file)).Replace("\r\n", "\n").Replace('\r', '\n'),
        })
        .OrderBy(entry => entry[0], StringComparer.Create(CultureInfo.GetCultureInfo("en"), ignoreCase: false))
        .ToArray();
    if (entries.Length == 0)
        throw new InvalidOperationException($"No synced FBA sources found in {directory}.");

    // Retain the JSON fingerprint format used by existing PR snapshot markers.
    var context = new GitHubJsonContext(new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
    var json = JsonSerializer.SerializeToUtf8Bytes(entries, context.StringArrayArray);
    return Convert.ToHexStringLower(SHA256.HashData(json));
}

static async Task<string?> ReadFileAsync(HttpClient client, string repository, string path, string commit, bool optional = false)
{
    var payload = await GetGitHubStringAsync(client, $"repos/{repository}/contents/{path}?ref={Uri.EscapeDataString(commit)}", optional).ConfigureAwait(false);
    if (payload is null)
        return null;

    var file = JsonSerializer.Deserialize(payload, GitHubJsonContext.Default.GitHubFile)
        ?? throw new InvalidOperationException($"Missing GitHub file response for {repository}/{path}@{commit}.");
    if (file.Type != "file" || file.Encoding != "base64" || file.Content is null)
        throw new InvalidOperationException($"Expected a file at {repository}/{path}@{commit}.");

    using var stream = new MemoryStream(Convert.FromBase64String(file.Content));
    using var reader = new StreamReader(stream);
    return await reader.ReadToEndAsync().ConfigureAwait(false);
}

static async Task<List<T>> GetPagesAsync<T>(HttpClient client, string endpoint, JsonTypeInfo<T[]> typeInfo)
{
    var results = new List<T>();
    var separator = endpoint.Contains('?') ? '&' : '?';
    for (var page = 1; ; page++)
    {
        var payload = await GetGitHubStringAsync(client, $"{endpoint}{separator}per_page=100&page={page}").ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Missing GitHub response for {endpoint}.");
        var batch = JsonSerializer.Deserialize(payload, typeInfo)
            ?? throw new InvalidOperationException($"Expected a GitHub array response for {endpoint}.");
        results.AddRange(batch);
        if (batch.Length < 100)
            return results;
    }
}

static async Task<string?> GetGitHubStringAsync(HttpClient client, string endpoint, bool optional = false)
{
    using var response = await client.GetAsync(endpoint).ConfigureAwait(false);
    if (optional && response.StatusCode == HttpStatusCode.NotFound)
        return null;

    if (!response.IsSuccessStatusCode)
        throw new HttpRequestException($"GitHub request failed ({(int)response.StatusCode}) at '{response.RequestMessage?.RequestUri}'.");

    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
}

static string GetRequiredEnvironmentVariable(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    return !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"Environment variable '{name}' must be set.");
}

static void WriteOutput(string name, string value)
{
    var output = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
    if (string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine($"{name}={value}");
        return;
    }

    var delimiter = Guid.NewGuid().ToString("N");
    File.AppendAllText(output, $"{name}<<{delimiter}\n{value}\n{delimiter}\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

sealed record GitHubBranch(string Name, GitHubCommit Commit);
sealed record GitHubCommit(string Sha);
sealed record GitHubFile(string Type, string Encoding, string Content);
sealed record GitHubPullRequest(int Number, string? Body, [property: JsonPropertyName("merged_at")] string? MergedAt);
sealed record SdkBranch(string Name, string Commit, string Version);
sealed record TargetBranch(string Branch, string Version, string SdkBranch, string SdkCommit);
sealed record BranchMatrix(List<TargetBranch> Include);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GitHubBranch[]))]
[JsonSerializable(typeof(GitHubFile))]
[JsonSerializable(typeof(GitHubPullRequest[]))]
[JsonSerializable(typeof(BranchMatrix))]
[JsonSerializable(typeof(string[][]))]
partial class GitHubJsonContext : JsonSerializerContext;
