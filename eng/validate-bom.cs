#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.Enumeration;
using System.Text;

// Lists all files with an associated `.editorconfig` charset but an invalid BOM.
//
// Usage:
//   dotnet run --file validate-bom.cs
//   dotnet run --file validate-bom.cs -- --self-test
//
// Default mode when no args are passed: verify

var root = Path.GetFullPath(Path.Combine(AppContext.GetData("EntryPointFileDirectoryPath") as string ?? throw new InvalidOperationException(), ".."));
if (args is ["--self-test"])
{
    await RunSelfTestAsync();
    return;
}

if (args.Length != 0)
    throw new InvalidOperationException("Usage: dotnet run --file validate-bom.cs [-- --self-test]");

var maxFailuresToShow = 50;
var failures = Validate(root, await GetTrackedFilesAsync(root));
foreach (var (path, charset) in failures.Take(maxFailuresToShow))
    Console.WriteLine($"{path}: expected {charset}");

if (failures.Count > maxFailuresToShow)
    Console.WriteLine($"... and {failures.Count - maxFailuresToShow} more files");

if (failures.Count > 0)
    throw new InvalidOperationException($"{failures.Count} files violate EditorConfig charset settings.");

Console.WriteLine("All tracked files with a configured UTF-8 charset have the expected BOM.");

static List<(string Path, string Charset)> Validate(string root, IEnumerable<string> files)
{
    var configurations = new Dictionary<string, EditorConfig>(StringComparer.OrdinalIgnoreCase);
    var failures = new List<(string Path, string Charset)>();

    foreach (var relativePath in files)
    {
        if (relativePath.StartsWith("eng/common/", StringComparison.OrdinalIgnoreCase))
            continue;

        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(fullPath) || File.Exists(fullPath) is false)
            continue;

        var charset = GetCharset(root, relativePath, configurations);
        if (charset is null or "unset")
            continue;

        if (charset is not ("utf-8" or "utf-8-bom"))
            throw new InvalidOperationException($"Unsupported charset '{charset}' for {relativePath}.");

        using var stream = File.OpenRead(fullPath);
        Span<byte> preamble = stackalloc byte[3];
        var hasBom = stream.Read(preamble) == preamble.Length && preamble.SequenceEqual(Encoding.UTF8.Preamble);
        if (hasBom != (charset == "utf-8-bom"))
            failures.Add((relativePath, charset));
    }

    return failures;
}

static string? GetCharset(string root, string relativePath, Dictionary<string, EditorConfig> configurations)
{
    var charset = default(string);
    var directory = Path.GetDirectoryName(Path.Combine(root, relativePath))!;

    while (directory.StartsWith(root, StringComparison.OrdinalIgnoreCase))
    {
        var configPath = Path.Combine(directory, ".editorconfig");
        if (File.Exists(configPath))
        {
            if (!configurations.TryGetValue(configPath, out var config))
            {
                config = EditorConfig.Read(configPath);
                configurations.Add(configPath, config);
            }

            charset ??= config.GetCharset(Path.GetFileName(relativePath));
            if (config.IsRoot)
                break;
        }

        if (string.Equals(directory, root, StringComparison.OrdinalIgnoreCase))
            break;

        directory = Path.GetDirectoryName(directory)!;
    }

    return charset;
}

static async Task<string[]> GetTrackedFilesAsync(string root)
{
    using var process = Process.Start(new ProcessStartInfo("git", $"-C \"{root}\" ls-files -z")
    {
        RedirectStandardOutput = true,
        UseShellExecute = false,
    }) ?? throw new InvalidOperationException("Failed to start git.");

    var output = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();
    if (process.ExitCode != 0)
        throw new InvalidOperationException($"git ls-files failed with exit code {process.ExitCode}.");

    return output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
}

static async Task RunSelfTestAsync()
{
    var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    Directory.CreateDirectory(root);
    try
    {
        await File.WriteAllTextAsync(Path.Combine(root, ".editorconfig"), "root = true\n[*.{cs,vb}]\ncharset = utf-8-bom\n");
        await File.WriteAllTextAsync(Path.Combine(root, "a.cs"), "code", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await File.WriteAllTextAsync(Path.Combine(root, "b.vb"), "code", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        var failures = Validate(root, ["a.cs", "b.vb"]);
        Assert(failures.SequenceEqual([("a.cs", "utf-8-bom")]), "Missing BOM must fail validation.");

        await File.WriteAllTextAsync(Path.Combine(root, "a.cs"), "code", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        Directory.CreateDirectory(Path.Combine(root, "sub"));
        await File.WriteAllTextAsync(Path.Combine(root, "sub", ".editorconfig"), "[*.cs]\ncharset = utf-8\n");
        await File.WriteAllTextAsync(Path.Combine(root, "sub", "a.cs"), "code", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        failures = Validate(root, ["a.cs", "sub/a.cs"]);
        Assert(failures.SequenceEqual([("sub/a.cs", "utf-8")]), "Unexpected BOM must fail validation.");

        Directory.CreateDirectory(Path.Combine(root, "fixture"));
        await File.WriteAllTextAsync(Path.Combine(root, "fixture", ".editorconfig"), "root = true\n[*.cs]\ncharset = unset\n");
        await File.WriteAllTextAsync(Path.Combine(root, "fixture", "a.cs"), "code", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Assert(Validate(root, ["fixture/a.cs"]).Count == 0, "Unset charset must skip validation.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }

    Console.WriteLine("BOM validator self-test passed.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

file sealed class EditorConfig(bool isRoot, List<(string Pattern, string Charset)> rules)
{
    public bool IsRoot { get; } = isRoot;

    public static EditorConfig Read(string path)
    {
        var isRoot = false;
        var rules = new List<(string Pattern, string Charset)>();
        string? section = null;

        foreach (var rawLine in File.ReadLines(path, Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
                continue;

            if (line[0] == '[' && line[^1] == ']')
            {
                section = line[1..^1];
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator < 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().ToLowerInvariant();
            if (section is null && string.Equals(key, "root", StringComparison.OrdinalIgnoreCase))
                isRoot = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            else if (section is not null && string.Equals(key, "charset", StringComparison.OrdinalIgnoreCase))
                rules.Add((section, value));
        }

        return new EditorConfig(isRoot, rules);
    }

    public string? GetCharset(string fileName)
    {
        for (var i = rules.Count - 1; i >= 0; i--)
        {
            var (pattern, charset) = rules[i];
            if (Matches(pattern, fileName))
                return charset;
        }

        return null;
    }

    private static bool Matches(string pattern, string fileName)
    {
        if (pattern.Contains('/', StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported path-qualified charset pattern: {pattern}");

        return ExpandBraces(pattern).Any(expandedPattern => FileSystemName.MatchesSimpleExpression(expandedPattern, fileName, ignoreCase: false));
    }

    private static IEnumerable<string> ExpandBraces(string pattern)
    {
        var start = pattern.IndexOf('{');
        if (start < 0)
        {
            yield return pattern;
            yield break;
        }

        var end = pattern.IndexOf('}', start);
        if (end < 0)
            throw new InvalidOperationException($"Unclosed EditorConfig brace in {pattern}.");

        foreach (var item in pattern[(start + 1)..end].Split(','))
        {
            foreach (var expanded in ExpandBraces(pattern[..start] + item + pattern[(end + 1)..]))
                yield return expanded;
        }
    }
}
