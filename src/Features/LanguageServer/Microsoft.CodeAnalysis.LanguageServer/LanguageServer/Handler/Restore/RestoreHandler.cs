// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
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
internal sealed class RestoreHandler(DotnetCliHelper dotnetCliHelper) : ILspServiceRequestHandler<RestoreParams, RestorePartialResult[]>
{
    internal const string MethodName = "workspace/_roslyn_restore";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<RestorePartialResult[]> HandleRequestAsync(RestoreParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);
        using var progress = BufferedProgress.Create(request.PartialResultToken);

        progress.Report(new RestorePartialResult(LanguageServerResources.Restore, LanguageServerResources.Restore_started));

        var restorePaths = GetRestorePaths(request, context.Solution, context);
        if (restorePaths.IsEmpty)
        {
            progress.Report(new RestorePartialResult(LanguageServerResources.Restore, LanguageServerResources.Nothing_found_to_restore));
            return progress.GetValues() ?? [];
        }

        await RestoreAsync(restorePaths, progress, cancellationToken);

        progress.Report(new RestorePartialResult(LanguageServerResources.Restore, $"{LanguageServerResources.Restore_complete}{Environment.NewLine}"));
        return progress.GetValues() ?? [];
    }

    private async Task RestoreAsync(ImmutableArray<string> pathsToRestore, BufferedProgress<RestorePartialResult> progress, CancellationToken cancellationToken)
    {
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

            process.OutputDataReceived += (sender, args) => ReportProgress(progress, stageName, args.Data);
            process.ErrorDataReceived += (sender, args) => ReportProgress(progress, stageName, args.Data);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                ReportProgress(progress, stageName, string.Format(LanguageServerResources.Failed_to_run_restore_on_0, path));
            }
        }

        static void ReportProgress(BufferedProgress<RestorePartialResult> progress, string stage, string? restoreOutput)
        {
            if (restoreOutput != null)
            {
                progress.Report(new RestorePartialResult(stage, restoreOutput));
            }
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

        context.TraceInformation($"Found {projects.Length} restorable projects from {solution.Projects.Count()} projects in solution");
        return projects;
    }
}
