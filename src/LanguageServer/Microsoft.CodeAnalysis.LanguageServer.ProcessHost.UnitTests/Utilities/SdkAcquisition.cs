// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Utilities;

/// <summary>
/// Ensures that the .NET SDK versions required by tests are available on the machine.
/// If a needed SDK is already installed system-wide (ambient), it is used directly.
/// If not, <c>dotnetup</c> is used to acquire it into a user-local directory.
/// </summary>
internal static class SdkAcquisition
{
    /// <summary>
    /// The directory where dotnetup installs SDKs for testing.
    /// All needed SDKs are co-located here so a single <c>dotnet.exe</c> can resolve them.
    /// </summary>
    private static readonly string s_dotnetupInstallPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "dotnetup-sdks");

    private static readonly object s_gate = new();
    private static bool s_pathPrepended;

    /// <summary>
    /// Ensures the specified SDK version is available for use in tests.
    /// If the SDK is already installed system-wide, no download occurs.
    /// If it's already in the dotnetup-managed directory, no download occurs.
    /// Only downloads when the SDK is genuinely missing from both locations.
    /// </summary>
    /// <remarks>
    /// After this call, <c>DotnetCliHelper</c> will find a <c>dotnet.exe</c> on PATH
    /// that can resolve the requested SDK version via <c>global.json</c> pinning.
    /// </remarks>
    public static void EnsureSdkAvailable(string sdkVersion, bool allowPrerelease = false)
    {
        // Fast path: check if the system dotnet already has this SDK.
        if (IsAvailableFromSystemDotnet(sdkVersion, allowPrerelease))
            return;

        // Check if it's already in the dotnetup-managed directory.
        if (IsAvailableFromDotnetupRoot(sdkVersion, allowPrerelease))
        {
            EnsureDotnetupRootOnPath();
            return;
        }

        // Need to acquire via dotnetup.
        InstallViaDotnetup(sdkVersion);
        EnsureDotnetupRootOnPath();
    }

    /// <summary>
    /// Checks whether the system-installed <c>dotnet</c> can resolve the given SDK version.
    /// Uses the same resolution the tests will use: writes a temp <c>global.json</c> and
    /// runs <c>dotnet --version</c> to see if it succeeds.
    /// </summary>
    private static bool IsAvailableFromSystemDotnet(string sdkVersion, bool allowPrerelease)
    {
        var systemDotnet = FindSystemDotnet();
        if (systemDotnet is null)
            return false;

        return CanResolveSdk(systemDotnet, sdkVersion, allowPrerelease);
    }

    /// <summary>
    /// Checks whether the dotnetup-managed <c>dotnet</c> can resolve the given SDK version.
    /// </summary>
    private static bool IsAvailableFromDotnetupRoot(string sdkVersion, bool allowPrerelease)
    {
        var dotnetupDotnet = GetDotnetupDotnetPath();
        if (!File.Exists(dotnetupDotnet))
            return false;

        return CanResolveSdk(dotnetupDotnet, sdkVersion, allowPrerelease);
    }

    /// <summary>
    /// Tests whether a specific <c>dotnet</c> executable can resolve the given SDK version
    /// by writing a temporary <c>global.json</c> and running <c>dotnet --version</c>.
    /// </summary>
    private static bool CanResolveSdk(string dotnetPath, string sdkVersion, bool allowPrerelease)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"sdk-check-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var globalJson = $$"""
                {
                    "sdk": {
                        "version": "{{sdkVersion}}",
                        "allowPrerelease": {{(allowPrerelease ? "true" : "false")}},
                        "rollForward": "latestPatch"
                    }
                }
                """;
            File.WriteAllText(Path.Combine(tempDir, "global.json"), globalJson);

            using var process = Process.Start(new ProcessStartInfo(dotnetPath, "--version")
            {
                WorkingDirectory = tempDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
                return false;

            process.WaitForExit(15_000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private static void InstallViaDotnetup(string sdkVersion)
    {
        var dotnetup = FindDotnetup();
        Assert.True(dotnetup is not null,
            $"SDK {sdkVersion} is not installed and dotnetup was not found. " +
            $"Install it with: iwr https://aka.ms/dotnetup/get-dotnetup.ps1 | iex");

        var startInfo = new ProcessStartInfo(dotnetup)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("sdk");
        startInfo.ArgumentList.Add("install");
        startInfo.ArgumentList.Add(sdkVersion);
        startInfo.ArgumentList.Add("--install-path");
        startInfo.ArgumentList.Add(s_dotnetupInstallPath);
        startInfo.ArgumentList.Add("--no-progress");

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0,
            $"dotnetup failed to install SDK {sdkVersion} (exit code {process.ExitCode}).\n" +
            $"stdout: {stdout}\nstderr: {stderr}");
    }

    private static void EnsureDotnetupRootOnPath()
    {
        lock (s_gate)
        {
            if (s_pathPrepended)
                return;

            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            if (!currentPath.Contains(s_dotnetupInstallPath, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable("PATH",
                    s_dotnetupInstallPath + Path.PathSeparator + currentPath);
            }

            s_pathPrepended = true;
        }
    }

    /// <summary>
    /// Finds the system-installed <c>dotnet</c> executable from PATH,
    /// excluding the dotnetup-managed directory.
    /// </summary>
    private static string? FindSystemDotnet()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var fileName = isWindows ? "dotnet.exe" : "dotnet";
        var sep = isWindows ? ';' : ':';
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";

        foreach (var dir in path.Split(sep, StringSplitOptions.RemoveEmptyEntries))
        {
            // Skip the dotnetup-managed directory — we want to check the system dotnet.
            if (dir.Contains("dotnetup-sdks", StringComparison.OrdinalIgnoreCase))
                continue;

            var filePath = Path.Combine(dir, fileName);
            if (File.Exists(filePath))
                return filePath;
        }

        return null;
    }

    private static string GetDotnetupDotnetPath()
    {
        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
        return Path.Combine(s_dotnetupInstallPath, fileName);
    }

    /// <summary>
    /// Finds <c>dotnetup</c> on PATH or in well-known install locations.
    /// </summary>
    private static string? FindDotnetup()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var fileName = isWindows ? "dotnetup.exe" : "dotnetup";

        // Check PATH first.
        var sep = isWindows ? ';' : ':';
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(sep, StringSplitOptions.RemoveEmptyEntries))
        {
            var filePath = Path.Combine(dir, fileName);
            if (File.Exists(filePath))
                return filePath;
        }

        // Check well-known install locations.
        string[] wellKnownPaths =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnetup", fileName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "dotnetup", fileName),
        ];

        foreach (var candidate in wellKnownPaths)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
