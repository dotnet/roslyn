// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

public partial class VSCodeService
{
    /// <summary>
    /// Downloads and installs VS Code to a local directory for isolated E2E testing.
    /// </summary>
    public class Installer(IntegrationTestServices testServices)
    {
        private const string CSharpExtensionId = "ms-dotnettools.csharp";

        /// <summary>
        /// Ensures VS Code is installed in the specified directory.
        /// </summary>
        public async Task<string> EnsureVSCodeInstalledAsync(string installDir)
        {
            var vscodeDir = Path.Combine(installDir, "vscode");
            var executablePath = GetExecutablePath(vscodeDir);

            if (File.Exists(executablePath))
            {
                testServices.Logger.Log($"VS Code already installed at: {vscodeDir}");

                // Validate the installation is working (not corrupted)
                if (await ValidateInstallationAsync(vscodeDir))
                {
                    return executablePath;
                }

                // Installation is corrupted, delete and re-download
                testServices.Logger.Log("VS Code installation appears corrupted, re-downloading...");
                try
                {
                    Directory.Delete(vscodeDir, recursive: true);
                }
                catch (Exception ex)
                {
                    testServices.Logger.Log($"Warning: Failed to delete corrupted installation: {ex.Message}");
                }
            }

            testServices.Logger.Log($"Downloading VS Code to: {vscodeDir}");
            Directory.CreateDirectory(vscodeDir);

            var downloadUrl = GetDownloadUrl();
            var archivePath = Path.Combine(installDir, GetArchiveFileName());

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            // Download VS Code
            testServices.Logger.Log($"Downloading from: {downloadUrl}");
            using (var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using var fileStream = File.Create(archivePath);
                await response.Content.CopyToAsync(fileStream);
            }

            testServices.Logger.Log("Extracting VS Code...");
            ExtractArchive(archivePath, vscodeDir);

            // Clean up archive
            File.Delete(archivePath);

            executablePath = GetExecutablePath(vscodeDir);
            if (!File.Exists(executablePath))
            {
                throw new InvalidOperationException($"VS Code executable not found after installation: {executablePath}");
            }

            testServices.Logger.Log($"VS Code installed successfully: {executablePath}");
            return executablePath;
        }

        /// <summary>
        /// Validates that the VS Code installation is working (not corrupted).
        /// </summary>
        private async Task<bool> ValidateInstallationAsync(string vscodeDir)
        {
            var cliPath = GetCliPath(GetExecutablePath(vscodeDir));

            if (!File.Exists(cliPath))
            {
                testServices.Logger.Log($"CLI not found at: {cliPath}");
                return false;
            }

            // Try running --version to verify the installation works
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                // Set DISPLAY for Linux headless environments
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var display = Environment.GetEnvironmentVariable("DISPLAY");
                    if (!string.IsNullOrEmpty(display))
                    {
                        startInfo.Environment["DISPLAY"] = display;
                    }
                }

                var process = new System.Diagnostics.Process { StartInfo = startInfo };
                process.Start();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await process.WaitForExitAsync(cts.Token);

                if (process.ExitCode == 0)
                {
                    var version = await process.StandardOutput.ReadToEndAsync();
                    testServices.Logger.Log($"VS Code version: {version.Trim()}");
                    return true;
                }

                var stderr = await process.StandardError.ReadToEndAsync();
                testServices.Logger.Log($"VS Code validation failed (exit {process.ExitCode}): {stderr}");
                return false;
            }
            catch (Exception ex)
            {
                testServices.Logger.Log($"VS Code validation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Installs an extension from the VS Code marketplace.
        /// </summary>
        public async Task InstallExtensionAsync(string vscodePath, string extensionId, string? extensionsDir = null, bool preRelease = false)
        {
            testServices.Logger.Log($"Installing extension: {extensionId}{(preRelease ? " (pre-release)" : "")}");

            var args = new List<string>
            {
                "--install-extension", extensionId,
                "--force" // Overwrite if already installed
            };

            if (preRelease)
            {
                args.Add("--pre-release");
            }

            if (!string.IsNullOrEmpty(extensionsDir))
            {
                args.AddRange(["--extensions-dir", extensionsDir]);
            }

            var cliPath = GetCliPath(vscodePath);
            testServices.Logger.Log($"Using CLI: {cliPath}");
            testServices.Logger.Log($"CLI exists: {File.Exists(cliPath)}");

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            // Set DISPLAY for Linux headless environments
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var display = Environment.GetEnvironmentVariable("DISPLAY");
                if (!string.IsNullOrEmpty(display))
                {
                    startInfo.Environment["DISPLAY"] = display;
                }
            }

            testServices.Logger.Log($"Running: {startInfo.FileName} {startInfo.Arguments}");

            var process = new System.Diagnostics.Process { StartInfo = startInfo };
            process.Start();

            // Use a timeout to prevent hanging indefinitely
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            try
            {
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cts.Token);

                var output = await outputTask;
                var error = await errorTask;

                testServices.Logger.Log($"Extension install exit code: {process.ExitCode}");

                if (!string.IsNullOrWhiteSpace(output))
                {
                    testServices.Logger.Log($"Extension install output: {output}");
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    testServices.Logger.Log($"Extension install stderr: {error}");
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to install extension {extensionId}: exit code {process.ExitCode}");
                }

                testServices.Logger.Log($"Extension {extensionId} installed successfully");
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch { }

                throw new TimeoutException($"Extension installation timed out after 3 minutes for {extensionId}");
            }
        }

        /// <summary>
        /// Installs the C# extension required for Razor language support (pre-release version).
        /// </summary>
        public async Task InstallCSharpExtensionAsync(string vscodePath, string? extensionsDir = null)
        {
            // Use pre-release version to get latest Razor language server features
            await InstallExtensionAsync(vscodePath, CSharpExtensionId, extensionsDir, preRelease: true);
        }

        /// <summary>
        /// Checks if an extension is installed.
        /// </summary>
        public async Task<bool> IsExtensionInstalledAsync(string vscodePath, string extensionId, string? extensionsDir = null)
        {
            var args = new List<string> { "--list-extensions" };

            if (!string.IsNullOrEmpty(extensionsDir))
            {
                args.AddRange(new[] { "--extensions-dir", extensionsDir });
            }

            var cliPath = GetCliPath(vscodePath);
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            // Set DISPLAY for Linux headless environments
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var display = Environment.GetEnvironmentVariable("DISPLAY");
                if (!string.IsNullOrEmpty(display))
                {
                    startInfo.Environment["DISPLAY"] = display;
                }
            }

            testServices.Logger.Log($"Checking extensions: {cliPath} {string.Join(" ", args)}");

            var process = new System.Diagnostics.Process { StartInfo = startInfo };
            process.Start();

            // Use a timeout to prevent hanging indefinitely
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync(cts.Token);

                testServices.Logger.Log($"Installed extensions: {output.Trim()}");
                return output.Contains(extensionId, StringComparison.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch { }

                testServices.Logger.Log("Timeout checking extensions, assuming not installed");
                return false;
            }
        }

        /// <summary>
        /// Gets the CLI path for the given VS Code executable path.
        /// The CLI should be used for launching VS Code with folder arguments.
        /// </summary>
        public static string GetCliPathForExecutable(string vscodePath)
        {
            return GetCliPath(vscodePath);
        }

        private static string GetDownloadUrl()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
                return $"https://update.code.visualstudio.com/latest/win32-{arch}-archive/stable";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "darwin-arm64" : "darwin";
                return $"https://update.code.visualstudio.com/latest/{arch}/stable";
            }
            else // Linux
            {
                var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
                return $"https://update.code.visualstudio.com/latest/{arch}/stable";
            }
        }

        private static string GetArchiveFileName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "vscode.zip";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "vscode.zip";
            }
            else
            {
                return "vscode.tar.gz";
            }
        }

        private static void ExtractArchive(string archivePath, string destDir)
        {
            if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                // Use tar command on Unix systems
                // Use --strip-components=1 to extract contents directly without the top-level directory
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "tar",
                        Arguments = $"-xzf \"{archivePath}\" -C \"{destDir}\" --strip-components=1",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("Failed to extract VS Code archive");
                }
            }
            else if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // On macOS, we must use ditto to preserve symlinks and app bundle structure.
                    // .NET's ZipFile.ExtractToDirectory doesn't handle macOS app bundles correctly.
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "ditto",
                            Arguments = $"-xk \"{archivePath}\" \"{destDir}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };
                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException("Failed to extract VS Code archive on macOS");
                    }
                }
                else
                {
                    ZipFile.ExtractToDirectory(archivePath, destDir);
                }
            }
        }

        private static string GetExecutablePath(string vscodeDir)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(vscodeDir, "Code.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // The zip extracts to "Visual Studio Code.app"
                return Path.Combine(vscodeDir, "Visual Studio Code.app", "Contents", "MacOS", "Electron");
            }
            else // Linux
            {
                return Path.Combine(vscodeDir, "code");
            }
        }

        private static string GetCliPath(string vscodePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // The CLI is in the bin folder relative to Code.exe
                var dir = Path.GetDirectoryName(vscodePath)!;
                return Path.Combine(dir, "bin", "code.cmd");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Convert Electron path to CLI path
                // /path/to/Visual Studio Code.app/Contents/MacOS/Electron
                // -> /path/to/Visual Studio Code.app/Contents/Resources/app/bin/code
                var appPath = vscodePath.Replace("/Contents/MacOS/Electron", "");
                return Path.Combine(appPath, "Contents", "Resources", "app", "bin", "code");
            }
            else // Linux
            {
                // On Linux, vscodePath is /path/to/vscode/code
                // The CLI script is at /path/to/vscode/bin/code
                var dir = Path.GetDirectoryName(vscodePath)!;
                return Path.Combine(dir, "bin", "code");
            }
        }
    }
}
