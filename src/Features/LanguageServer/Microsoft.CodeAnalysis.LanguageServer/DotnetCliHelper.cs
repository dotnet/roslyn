// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

[Export, Shared]
internal sealed class DotnetCliHelper
{
    internal const string DotnetRootEnvVar = "DOTNET_ROOT";
    internal const string DotnetRootUserEnvVar = "DOTNET_ROOT_USER";

    private readonly ILogger _logger;
    private readonly Lazy<string> _dotnetExecutablePath;
    private readonly AsyncLazy<string> _dotnetSdkFolder;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DotnetCliHelper(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DotnetCliHelper>();
        _dotnetExecutablePath = new Lazy<string>(() => GetDotnetExecutablePath());
        _dotnetSdkFolder = new AsyncLazy<string>(GetDotnetSdkFolderFromDotnetExecutableAsync, cacheResult: true);
    }

    public string GetDotnetExecutablePath()
    {
        // The client can modify the value in DOTNET_ROOT to ensure that the server starts on the correct runtime.
        // It will save the user's DOTNET_ROOT in the DOTNET_ROOT_USER environment variable, so check there instead of using the DOTNET_ROOT.
        var dotnetPathFromDotnetRootUser = Environment.GetEnvironmentVariable(DotnetRootUserEnvVar);
        if (TryGetDotnetExecutableFromFolder(dotnetPathFromDotnetRootUser, DotnetRootUserEnvVar, _logger, out var dotnetRootUserExecutable))
        {
            return dotnetRootUserExecutable;
        }

        // Neither an option or env var was provided, find the dotnet path using what is currently on the path.
        _logger.LogInformation("Using dotnet executable configured on the PATH");
        return "dotnet";

        static bool TryGetDotnetExecutableFromFolder(string? dotnetFolder, string optionName, ILogger logger, [NotNullWhen(true)] out string? dotnetExecutablePath)
        {
            if (string.IsNullOrEmpty(dotnetFolder))
            {
                dotnetExecutablePath = null;
                return false;
            }

            var dotnetExecutableName = $"dotnet{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty)}";
            var executablePath = Path.Combine(dotnetFolder, dotnetExecutableName);
            if (File.Exists(executablePath))
            {
                logger.LogInformation($"Found dotnet executable at {executablePath} using {optionName}");
                dotnetExecutablePath = executablePath;
                return true;
            }
            else
            {
                logger.LogInformation($"The {optionName} option {dotnetFolder} does not contain a valid dotnet executable");
                dotnetExecutablePath = null;
                return false;
            }
        }
    }

    /// <summary>
    /// The folder the dotnet executable is in could contain multiple SDK paths.
    /// In order to figure out which one is the right one, we need to run dotnet --info
    /// which will output the correct SDK path.
    /// </summary>
    private async Task<string> GetDotnetSdkFolderFromDotnetExecutableAsync(CancellationToken cancellationToken)
    {
        using var process = Run("--info", workingDirectory: null, shouldLocalizeOutput: false);

        string? dotnetSdkFolderPath = null;
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                var line = e.Data.Trim();
                if (line.StartsWith("Base Path", StringComparison.OrdinalIgnoreCase))
                {
                    dotnetSdkFolderPath = e.Data.Split(':', count: 2)[1].Trim();
                }
            }
        };

        var errorOutput = new StringBuilder();
        process.ErrorDataReceived += (_, e) => errorOutput.AppendLine(e.Data);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0 || dotnetSdkFolderPath == null)
        {
            _logger.LogError(errorOutput.ToString());
            throw new InvalidOperationException("Failed to get dotnet SDK folder from dotnet --info");
        }

        return dotnetSdkFolderPath;
    }

    public Process Run(string arguments, string? workingDirectory, bool shouldLocalizeOutput)
    {
        _logger.LogDebug($"Running dotnet CLI command at {_dotnetExecutablePath.Value} in directory {workingDirectory} with arguments {arguments}");

        var startInfo = new ProcessStartInfo(_dotnetExecutablePath.Value, arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (workingDirectory != null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        // If we're relying on the user's configured CLI to run this we need to remove our custom DOTNET_ROOT that we set
        // Wto start the server; otherwise we won't find the users installed sdks.
        if (_dotnetExecutablePath.Value == "dotnet")
        {
            startInfo.Environment[DotnetRootEnvVar] = GetUserDotnetRoot();
        }

        if (!shouldLocalizeOutput)
        {
            // The caller requested that we not localize the output of the command (typically be cause it needs to be parsed)
            // Set the appropriate environment variable for the process so that we don't get localized output.
            startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
        }

        // Since we depend on MSBuild APIs, the following environment variables get set to the version of dotnet that runs this server.
        // However want to use the user specified dotnet version to run the tests, so we need to unset these.
        startInfo.Environment.Remove("MSBUILD_EXE_PATH");
        startInfo.Environment.Remove("MSBuildExtensionsPath");

        var process = Process.Start(startInfo);
        Contract.ThrowIfNull(process, $"Unable to start dotnet CLI at {_dotnetExecutablePath.Value} with arguments {arguments} in directory {workingDirectory}");
        return process;
    }

    /// <summary>
    /// Since we change DOTNET_ROOT in order to start the process, we need to get the original value
    /// whenever we shell out to the CLI or other processes (e.g. vstest console).
    /// </summary>
    /// <returns></returns>
    public static string GetUserDotnetRoot()
    {
        // If the user had a valid dotnet root, set it to that, otherwise leave it empty.
        var dotnetRootUser = Environment.GetEnvironmentVariable("DOTNET_ROOT_USER");
        var newDotnetRootValue = Path.Exists(dotnetRootUser) ? dotnetRootUser : string.Empty;
        return newDotnetRootValue;
    }

    public async Task<string> GetVsTestConsolePathAsync(CancellationToken cancellationToken)
    {
        var dotnetSdkFolder = await _dotnetSdkFolder.GetValueAsync(cancellationToken);
        var vstestConsole = Path.Combine(dotnetSdkFolder, "vstest.console.dll");
        Contract.ThrowIfFalse(File.Exists(vstestConsole), $"VSTestConsole was not found at {vstestConsole}");
        return vstestConsole;
    }
}
