// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Given an input project (or none), runs restore on the project and streams the output
/// back to the client to display.
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(RestoreHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RestoreHandler(DotnetCliHelper dotnetCliHelper, ILoggerFactory loggerFactory) : ILspServiceRequestHandler<RestoreParams, RestoreResult>
{
    internal const string MethodName = "workspace/_roslyn_restore";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    private readonly ILogger<RestoreHandler> _logger = loggerFactory.CreateLogger<RestoreHandler>();

    public async Task<RestoreResult> HandleRequestAsync(RestoreParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        var restorePaths = GetRestorePaths(request, context.Solution, context);
        if (restorePaths.IsEmpty)
        {
            _logger.LogDebug($"Restore was requested but no paths were provided.");
            return new RestoreResult(true);
        }

        var workDoneProgressManager = context.GetRequiredService<WorkDoneProgressManager>();
        _logger.LogDebug($"Running restore on {restorePaths.Length} paths, starting with '{restorePaths.First()}'.");

        // We let cancellation here bubble up to the client as this is a client initiated operation.
        var didSucceed = await RestoreAsync(restorePaths, workDoneProgressManager, dotnetCliHelper, _logger, enableProgressReporting: true, cancellationToken);

        if (didSucceed)
        {
            _logger.LogDebug($"Restore completed successfully.");
        }
        else
        {
            _logger.LogError($"Restore completed with errors.");
        }

        return new RestoreResult(didSucceed);
    }

    /// <returns>True if all restore invocations exited with code 0. Otherwise, false.</returns>
    public static async Task<bool> RestoreAsync(
        ImmutableArray<string> pathsToRestore,
        WorkDoneProgressManager workDoneProgressManager,
        DotnetCliHelper dotnetCliHelper,
        ILogger logger,
        bool enableProgressReporting,
        CancellationToken cancellationToken)
    {
        using var progress = await workDoneProgressManager.CreateWorkDoneProgressAsync(reportProgressToClient: enableProgressReporting, cancellationToken);
        // Ensure we're observing cancellation token from the work done progress (to allow client cancellation).
        cancellationToken = progress.CancellationToken;
        return await RestoreCoreAsync(pathsToRestore, progress, dotnetCliHelper, logger, cancellationToken);

    }

    private static async Task<bool> RestoreCoreAsync(
        ImmutableArray<string> pathsToRestore,
        IWorkDoneProgressReporter progress,
        DotnetCliHelper dotnetCliHelper,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Report the start of the work done progress to the client.
        progress.Report(new WorkDoneProgressBegin()
        {
            Title = LanguageServerResources.Restore,
            // Adds a cancel button to the client side progress UI.
            // Cancellation here is fine, it just means the restore will be incomplete (same as a cntrl+C for a CLI restore).
            Cancellable = true,
            Message = LanguageServerResources.Restore_started,
            Percentage = 0,
        });

        var success = true;
        foreach (var path in pathsToRestore)
        {
            var arguments = new string[] { "restore", path };
            var workingDirectory = Path.GetDirectoryName(path);
            var stageName = string.Format(LanguageServerResources.Restoring_0, Path.GetFileName(path));
            ReportProgress(progress, stageName, string.Format(LanguageServerResources.Running_dotnet_restore_on_0, path));

            var process = dotnetCliHelper.Run(arguments, workingDirectory, shouldLocalizeOutput: true);

            cancellationToken.Register(() =>
            {
                process?.Kill();
            });

            process.OutputDataReceived += (sender, args) => ReportProgressInEvent(progress, stageName, args.Data);
            process.ErrorDataReceived += (sender, args) => ReportProgressInEvent(progress, stageName, args.Data);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                ReportProgress(progress, stageName, string.Format(LanguageServerResources.Failed_to_run_restore_on_0, path));
                success = false;
            }
        }

        // Report work done progress completion
        progress.Report(
            new WorkDoneProgressEnd()
            {
                Message = LanguageServerResources.Restore_complete
            });

        logger.LogInformation(LanguageServerResources.Restore_complete);
        return success;

        void ReportProgressInEvent(IWorkDoneProgressReporter progress, string stage, string? restoreOutput)
        {
            if (restoreOutput == null)
                return;

            try
            {
                ReportProgress(progress, stage, restoreOutput);
            }
            catch (Exception)
            {
                // Catch everything to ensure the exception doesn't escape the event handler.
                // Errors already reported via ReportNonFatalErrorUnlessCancelledAsync.
            }
        }

        void ReportProgress(IWorkDoneProgressReporter progress, string stage, string message)
        {
            logger.LogInformation("{stage}: {Output}", stage, message);
            var report = new WorkDoneProgressReport()
            {
                Message = stage,
                Percentage = null,
                Cancellable = true,
            };

            progress.Report(report);
        }
    }

    private static ImmutableArray<string> GetRestorePaths(RestoreParams request, Solution solution, RequestContext context)
    {
        if (request.ProjectFilePaths.Any())
        {
            return [.. request.ProjectFilePaths];
        }

        // No file paths were specified - this means we should restore all projects in the solution.
        // If there is a valid solution path, use that as the restore path.
        if (solution.FilePath != null)
        {
            return [solution.FilePath];
        }

        // We don't have an addressable solution, so lets find all addressable projects.
        // We can only restore projects with file paths as we are using the dotnet CLI to address them.
        // We also need to remove duplicates as in multi targeting scenarios there will be multiple projects with the same file path.
        var projects = solution.Projects
            .Select(p => p.FilePath)
            .WhereNotNull()
            .Distinct()
            .ToImmutableArray();

        context.TraceDebug($"Found {projects.Length} restorable projects from {solution.Projects.Count()} projects in solution");
        return projects;
    }
}
