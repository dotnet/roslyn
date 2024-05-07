// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

[Export, Shared]
internal sealed class DotnetCliHelper
{
    internal const string DotnetRootEnvVar = "DOTNET_ROOT";

    private readonly ILogger _logger;
    private readonly Lazy<string> _dotnetExecutablePath;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DotnetCliHelper(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DotnetCliHelper>();
        _dotnetExecutablePath = new Lazy<string>(() => GetDotNetPathOrDefault());
    }

    /// <summary>
    /// The folder the dotnet executable is in could contain multiple SDK paths.
    /// In order to figure out which one is the right one, we need to run dotnet --info
    /// from the project directory (in order to respect any global.json that might be present)
    /// which will output the correct SDK path.
    /// </summary>
    private async Task<string> GetDotnetSdkFolderFromDotnetExecutableAsync(string projectOutputDirectory, CancellationToken cancellationToken)
    {
        using var process = Run(["--info"], workingDirectory: projectOutputDirectory, shouldLocalizeOutput: false);

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
        var stdOutput = new StringBuilder();
        process.ErrorDataReceived += (_, e) => errorOutput.AppendLine(e.Data);
        process.OutputDataReceived += (_, e) => stdOutput.AppendLine(e.Data);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);
        _logger.LogDebug(stdOutput.ToString());
        if (process.ExitCode != 0 || dotnetSdkFolderPath == null)
        {
            _logger.LogError(errorOutput.ToString());
            throw new InvalidOperationException("Failed to get dotnet SDK folder from dotnet --info");
        }

        return dotnetSdkFolderPath;
    }

    public Process Run(string[] arguments, string? workingDirectory, bool shouldLocalizeOutput)
    {
        _logger.LogDebug($"Running dotnet CLI command at {_dotnetExecutablePath.Value} in directory {workingDirectory} with arguments {arguments}");

        var startInfo = new ProcessStartInfo(_dotnetExecutablePath.Value)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.ArgumentList.AddRange(arguments);

        if (workingDirectory != null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        // If we're relying on the user's configured CLI to run this we need to remove our custom DOTNET_ROOT that we set
        // Wto start the server; otherwise we won't find the users installed sdks.
        startInfo.Environment.Remove(DotnetRootEnvVar);

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

    public async Task<string> GetVsTestConsolePathAsync(string projectOutputDirectory, CancellationToken cancellationToken)
    {
        var dotnetSdkFolder = await GetDotnetSdkFolderFromDotnetExecutableAsync(projectOutputDirectory, cancellationToken);
        var vstestConsole = Path.Combine(dotnetSdkFolder, "vstest.console.dll");
        Contract.ThrowIfFalse(File.Exists(vstestConsole), $"VSTestConsole was not found at {vstestConsole}");
        _logger.LogDebug($"Using vstest console at {vstestConsole}");
        return vstestConsole;
    }

    /// <summary>
    /// Finds the dotnet executable path from the PATH environment variable.
    /// Based on https://github.com/dotnet/msbuild/blob/main/src/Utilities/ToolTask.cs#L1259
    /// We also do not include DOTNET_ROOT here, see https://github.com/dotnet/runtime/issues/88754
    /// </summary>
    /// <returns></returns>
    internal string GetDotNetPathOrDefault()
    {
        var (fileName, sep) = PlatformInformation.IsWindows
            ? ("dotnet.exe", ';')
            : ("dotnet", ':');

        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var item in path.Split(sep, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var filePath = Path.Combine(item, fileName);
                if (File.Exists(filePath))
                {
                    _logger.LogInformation("Using dotnet executable configured on the PATH");
                    return filePath;
                }
            }
            catch
            {
                // If we can't read a directory for any reason just skip it
            }
        }

        _logger.LogInformation("Could not find dotnet executable from PATH");
        return fileName;
    }
}
