// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

/// <summary>
/// Allows integration tests to run Package Manager Console (PMC) commands. Commands are executed by
/// driving the console through DTE, and completion is observed through a sentinel result file which
/// the executed script always writes. This avoids depending on NuGet's private Apex test
/// infrastructure and avoids assuming <c>DTE.ExecuteCommand</c> completes the console operation
/// synchronously.
/// </summary>
[TestService]
internal sealed partial class PackageManagerConsoleInProcess
{
    /// <summary>
    /// DTE command which shows the Package Manager Console tool window. Passing arguments to this
    /// command places them on the console input line and executes them.
    /// </summary>
    private const string PackageManagerConsoleCommand = "View.PackageManagerConsole";

    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Shows the Package Manager Console. The console is not guaranteed to have finished
    /// initializing when this method returns; use <see cref="ExecuteCommandAsync"/> to run commands,
    /// which waits for the command result.
    /// </summary>
    public async Task ShowAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        dte.ExecuteCommand(PackageManagerConsoleCommand);
    }

    /// <summary>
    /// Executes a PowerShell command in the Package Manager Console and waits for it to complete.
    /// </summary>
    /// <param name="command">The PowerShell command to execute, e.g.
    /// <c>Update-Package Microsoft.CodeAnalysis -ProjectName MyProject</c>.</param>
    /// <param name="cancellationToken">A hang-mitigating cancellation token. Polling for the command
    /// result stops as soon as cancellation is requested.</param>
    /// <returns>The result of the command, including any captured PowerShell error text.</returns>
    public async Task<PackageManagerConsoleResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(command));

        var workingDirectory = Path.Combine(Path.GetTempPath(), "RoslynPmc", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);

        var resultPath = Path.Combine(workingDirectory, "result.txt");
        var errorPath = Path.Combine(workingDirectory, "error.txt");

        await ShowAsync(cancellationToken);

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        dte.ExecuteCommand(PackageManagerConsoleCommand, CreateCommandWrapper(command, resultPath, errorPath));

        return await WaitForResultAsync(command, resultPath, errorPath, cancellationToken);
    }

    /// <summary>
    /// Wraps <paramref name="command"/> in a script which always writes a sentinel result file, so
    /// completion can be observed regardless of whether the command succeeded or failed.
    /// </summary>
    private static string CreateCommandWrapper(string command, string resultPath, string errorPath)
    {
        // The console executes a single line, so the wrapper is written as a single statement chain.
        return string.Format(
            CultureInfo.InvariantCulture,
            "$ErrorActionPreference = 'Stop'; try {{ {0} -ErrorAction Stop; 'Succeeded' | Set-Content -LiteralPath '{1}' }} catch {{ $_ | Out-String | Set-Content -LiteralPath '{2}'; 'Failed' | Set-Content -LiteralPath '{1}' }}",
            command,
            resultPath,
            errorPath);
    }

    private async Task<PackageManagerConsoleResult> WaitForResultAsync(
        string command,
        string resultPath,
        string errorPath,
        CancellationToken cancellationToken)
    {
        await TaskScheduler.Default;

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(
                    $"Timed out waiting for the Package Manager Console command to complete: '{command}'.",
                    cancellationToken);
            }

            if (File.Exists(resultPath))
            {
                // The result file is written last by the wrapper, so any error text is already present.
                var result = File.ReadAllText(resultPath).Trim();
                var error = File.Exists(errorPath) ? File.ReadAllText(errorPath).Trim() : string.Empty;
                var succeeded = string.Equals(result, "Succeeded", StringComparison.Ordinal);
                return new PackageManagerConsoleResult(command, succeeded, error);
            }

            await Task.Delay(PollingInterval, cancellationToken).NoThrowAwaitable();
        }
    }
}

/// <summary>
/// The outcome of a Package Manager Console command.
/// </summary>
/// <param name="Command">The command which was executed.</param>
/// <param name="Succeeded">Whether the command completed without a PowerShell error.</param>
/// <param name="ErrorText">The captured PowerShell error text, or an empty string when the command succeeded.</param>
internal sealed record PackageManagerConsoleResult(string Command, bool Succeeded, string ErrorText)
{
    /// <summary>
    /// A message describing the command outcome, suitable for use in assertion failures.
    /// </summary>
    public string GetFailureMessage()
        => $"Package Manager Console command failed: '{Command}'.{Environment.NewLine}{ErrorText}";
}
