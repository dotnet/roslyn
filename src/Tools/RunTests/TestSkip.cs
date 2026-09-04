// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace RunTests;

/// <summary>
/// Closure-fingerprint test skip. A test assembly's result is a pure function of its runtime input
/// closure (every deployed file it can load) plus the environment axis, under the axiom that tests
/// touch only in-memory state (no clock, network, disk, or randomness). So a test result is a cached
/// build output keyed by its input closure: if a PASS was recorded for (assembly | env | fingerprint)
/// an identical closure is skipped and the prior PASS replayed; after a green run the passes are
/// recorded. PASS-only -- a failed or unknown assembly always re-runs.
///
/// The fingerprint is computed on the build agent's deploy directory (which is exactly what is shipped
/// to the Helix machines as the work-item payload), so filtering here and recording after the run use
/// the same bytes. Deterministic builds make the fingerprint byte-stable across runs and agents.
/// </summary>
internal static class TestSkip
{
    internal static string? ResolveStore(Options options)
        => !string.IsNullOrEmpty(options.TestSkipStore)
            ? options.TestSkipStore
            : Environment.GetEnvironmentVariable("ROSLYN_TEST_SKIP_STORE");

    /// <summary>
    /// Partition the discovered assemblies into the ones that must run (no recorded PASS for their
    /// current closure) and the ones that can be skipped. Returns the fingerprints so a later record
    /// pass does not recompute them. When no store is configured, everything runs.
    /// </summary>
    internal static (ImmutableArray<AssemblyInfo> ToRun, Dictionary<string, string> Fingerprints, int Skipped) Plan(
        ImmutableArray<AssemblyInfo> assemblies, Options options)
    {
        var fingerprints = new Dictionary<string, string>(StringComparer.Ordinal);
        var store = ResolveStore(options);
        if (string.IsNullOrEmpty(store))
        {
            return (assemblies, fingerprints, 0);
        }

        var forceRun = string.Equals(
            Environment.GetEnvironmentVariable("ROSLYN_TEST_SKIP_FORCE_RUN"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (forceRun)
        {
            ConsoleUtil.WriteLine("Test-skip: force-run enabled; prior PASS records will not skip assemblies.");
        }

        var toRun = ImmutableArray.CreateBuilder<AssemblyInfo>();
        var skipped = 0;
        foreach (var asm in assemblies)
        {
            var deployDir = Path.GetDirectoryName(asm.AssemblyPath)!;
            var fp = ComputeFingerprint(deployDir);
            fingerprints[asm.AssemblyPath] = fp;
            var key = Key(asm.AssemblyName, EnvAxis(options, Path.GetFileName(deployDir)), fp);
            if (!forceRun && HasPass(store, key))
            {
                ConsoleUtil.WriteLine($"SKIP {asm.AssemblyName} (closure unchanged) fp={fp[..12]}");
                skipped++;
            }
            else
            {
                ConsoleUtil.WriteLine($"RUN  {asm.AssemblyName} fp={fp[..12]}");
                toRun.Add(asm);
            }
        }

        ConsoleUtil.WriteLine($"Test-skip: {skipped} skipped, {toRun.Count} to run (store: {store})");
        return (toRun.ToImmutable(), fingerprints, skipped);
    }

    /// <summary>
    /// Record a PASS for each assembly that ran. Called only after a green run, so every recorded
    /// assembly genuinely passed. A single failure records nothing, so the whole set re-runs next time.
    /// </summary>
    internal static void RecordPasses(ImmutableArray<AssemblyInfo> ranAssemblies, Dictionary<string, string> fingerprints, Options options)
    {
        var store = ResolveStore(options);
        if (string.IsNullOrEmpty(store))
        {
            return;
        }

        Directory.CreateDirectory(store);
        foreach (var asm in ranAssemblies)
        {
            var deployDir = Path.GetDirectoryName(asm.AssemblyPath)!;
            if (!fingerprints.TryGetValue(asm.AssemblyPath, out var fp))
            {
                fp = ComputeFingerprint(deployDir);
            }

            var env = EnvAxis(options, Path.GetFileName(deployDir));
            var key = Key(asm.AssemblyName, env, fp);
            var path = Path.Combine(store, Sha256Hex(Encoding.UTF8.GetBytes(key)) + ".json");
            var json = $"{{\"assembly\":\"{asm.AssemblyName}\",\"envAxis\":\"{env}\",\"fingerprint\":\"{fp}\",\"result\":\"PASS\",\"when\":\"{DateTime.UtcNow:o}\"}}";
            File.WriteAllText(path, json);
        }

        ConsoleUtil.WriteLine($"Test-skip: recorded PASS for {ranAssemblies.Length} assemblies to {store}");
    }

    /// <summary>
    /// The environment axis a result depends on beyond the closure bytes: configuration, target
    /// framework, OS, architecture, and culture. Culture matters -- roslyn's localized legs prove test
    /// output depends on it -- so the same closure under a different culture is a different key. The
    /// ROSLYN_TEST_* / DOTNET_RuntimeAsync mode variables also change results for the same bytes (the
    /// IOperation, UsedAssemblies and RuntimeAsync legs run the same assemblies with these set), and are
    /// exactly the variables the Helix runner forwards to the test machines, so they belong in the key.
    /// </summary>
    internal static string EnvAxis(Options options, string tfm)
    {
        var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx"
            : "linux";
        var arch = string.IsNullOrEmpty(options.Architecture) ? "x64" : options.Architecture;
        var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
        if (string.IsNullOrEmpty(culture))
        {
            culture = "neutral";
        }

        var modeVars = new[] { "ROSLYN_TEST_IOPERATION", "ROSLYN_TEST_USEDASSEMBLIES", "DOTNET_RuntimeAsync" };
        var modes = string.Join(",", modeVars.Select(v => $"{v}={Environment.GetEnvironmentVariable(v)}"));

        return $"{options.Configuration}|{tfm}|{os}|{arch}|{culture}|{modes}";
    }

    internal static string Key(string assembly, string envAxis, string fingerprint)
        => $"{assembly}|{envAxis}|{fingerprint}";

    private static bool HasPass(string store, string key)
    {
        var path = Path.Combine(store, Sha256Hex(Encoding.UTF8.GetBytes(key)) + ".json");
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            return File.ReadAllText(path).Contains("\"result\":\"PASS\"", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// sha256 over the sorted "relpath|sha256(content)" of every deployed file except pdbs (pdbs never
    /// affect test results). Relative paths use '/' and ordinal ordering so the value is stable across
    /// machines.
    /// </summary>
    internal static string ComputeFingerprint(string deployDir)
    {
        var entries = Directory.EnumerateFiles(deployDir, "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
            .Select(f => (rel: Path.GetRelativePath(deployDir, f).Replace('\\', '/'), full: f))
            .OrderBy(x => x.rel, StringComparer.Ordinal);

        var sb = new StringBuilder();
        foreach (var (rel, full) in entries)
        {
            sb.Append(rel).Append('|').Append(FileSha256Hex(full)).Append('\n');
        }

        return Sha256Hex(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private static string FileSha256Hex(string path)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(path);
        return Convert.ToHexString(sha.ComputeHash(fs)).ToLowerInvariant();
    }

    private static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }
}
