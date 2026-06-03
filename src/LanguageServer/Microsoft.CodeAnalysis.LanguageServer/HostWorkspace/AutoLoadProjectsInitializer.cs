// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicLspServiceFactory(typeof(AutoLoadProjectsInitializer)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class AutoLoadProjectsInitializerFactory(
    ILoggerFactory loggerFactory,
    ServerConfiguration serverConfiguration,
    IGlobalOptionService globalOptionService) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new AutoLoadProjectsInitializer(
            lspServices.GetRequiredService<LanguageServerProjectSystem>(),
            loggerFactory,
            serverConfiguration,
            globalOptionService);
}

internal sealed class AutoLoadProjectsInitializer(
    LanguageServerProjectSystem projectSystem,
    ILoggerFactory loggerFactory,
    ServerConfiguration serverConfiguration,
    IGlobalOptionService globalOptionService) : ILspService, IOnInitialized
{
    private static readonly EnumerationOptions s_recursiveEnumerationOptions = new() { RecurseSubdirectories = true, IgnoreInaccessible = true };
    private readonly ILogger _logger = loggerFactory.CreateLogger<AutoLoadProjectsInitializer>();

    public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        if (serverConfiguration.AutoLoadProjects is not int projectAutoLoadMaximum)
        {
            return;
        }

        var isUsingDevKit = globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures);
        Contract.ThrowIfTrue(isUsingDevKit, "Auto load projects is not supported when using DevKit.");

        var initializeParams = context.GetRequiredService<IInitializeManager>().TryGetInitializeParams();
        Contract.ThrowIfNull(initializeParams, "Initialize params should be set during initialization.");

        var workspaceFolders = initializeParams.WorkspaceFolders;
        if (workspaceFolders is null || workspaceFolders.Length == 0)
        {
            _logger.LogWarning("No workspace folders provided during initialization; could not auto load projects.");
            return;
        }

        var (isLoadingDisabled, solutionPath) = TryGetVSCodeSolutionSettings(workspaceFolders, _logger);
        if (isLoadingDisabled)
        {
            _logger.LogInformation("Using VS Code settings to disable auto loading solution on startup.");
            return;
        }
        else if (solutionPath is not null)
        {
            _logger.LogInformation("Using VS Code settings to auto load solution {SolutionFile}", solutionPath);
            await StartAndReportProgressAsync(
                (reporter) => projectSystem.OpenSolutionAsync(solutionPath, reporter),
                startMessage: string.Format(LanguageServerResources.Loading_0, solutionPath),
                endMessage: string.Format(LanguageServerResources.Loaded_0, solutionPath));
            return;
        }

        // If there's a single workspace folder with a single solution file at the root, load that solution.
        if (workspaceFolders.Length == 1)
        {
            var folder = workspaceFolders[0];
            if (TryGetFolderPath(folder, _logger, out var folderPath))
            {
                var solutionFiles = Directory.EnumerateFiles(folderPath, "*.sln", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.EnumerateFiles(folderPath, "*.slnx", SearchOption.TopDirectoryOnly))
                    .ToArray();

                if (solutionFiles.Length == 1)
                {
                    _logger.LogInformation("Found single solution file {SolutionFile} to auto load", solutionFiles[0]);
                    await StartAndReportProgressAsync(
                        (reporter) => projectSystem.OpenSolutionAsync(solutionFiles[0], reporter),
                        startMessage: string.Format(LanguageServerResources.Loading_0, solutionFiles[0]),
                        endMessage: string.Format(LanguageServerResources.Loaded_0, solutionFiles[0]));
                    return;
                }
            }
        }

        using var _ = ArrayBuilder<string>.GetInstance(out var projectFiles);
        foreach (var folder in workspaceFolders)
        {
            _logger.LogTrace("Searching for projects to load in workspace folder: {FolderUri}", folder.DocumentUri);
            if (TryGetFolderPath(folder, _logger, out var folderPath))
            {
                projectFiles.AddRange(Directory.EnumerateFiles(folderPath, "*.csproj", s_recursiveEnumerationOptions));
            }
        }

        _logger.LogInformation("Discovered {count} projects to auto load", projectFiles.Count);

        if (projectFiles.Count > projectAutoLoadMaximum)
        {
            _logger.LogWarning("Number of projects exceeds the auto-load maximum of {ProjectAutoLoadMaximum}", projectAutoLoadMaximum);

            // We're going to trim the project count down a bit; rather than trimming arbitrarily let's try to first trim out tests on the rationale that some repos can have a
            // lot of test projects, but it's better to get the core projects loaded rather than the test projects (that then don't have functional references).
            projectFiles.RemoveAll(f =>
            {
                var fileComponents = f.Split(Path.DirectorySeparatorChar);
                return fileComponents.Contains("test", StringComparer.OrdinalIgnoreCase) ||
                    fileComponents.Contains("tests", StringComparer.OrdinalIgnoreCase);
            });

            if (projectFiles.Count > projectAutoLoadMaximum)
            {
                _logger.LogWarning("Even after trimming test projects, number of projects still exceeds the auto-load maximum. Trimming to first {ProjectAutoLoadMaximum} projects.", projectAutoLoadMaximum);
                projectFiles.RemoveRange(projectAutoLoadMaximum, projectFiles.Count - projectAutoLoadMaximum);
            }
        }

        await StartAndReportProgressAsync(
            (reporter) => projectSystem.OpenProjectsAsync(projectFiles.ToImmutable(), reporter),
            startMessage: string.Format(LanguageServerResources.Loading_0_projects, projectFiles.Count),
            endMessage: string.Format(LanguageServerResources.Loaded_0_projects, projectFiles.Count));

        async Task StartAndReportProgressAsync(Func<IWorkDoneProgressReporter, Task> loadOperation, string startMessage, string endMessage)
        {
            var workDoneProgressManager = context.GetRequiredLspService<WorkDoneProgressManager>();

            // We will await for the client to know that we are starting work...
            var progressReporter = await workDoneProgressManager.CreateWorkDoneProgressAsync(reportProgressToClient: true,
                title: startMessage,
                startMessage: startMessage,
                endMessage: endMessage,
                clientCanCancel: false,
                cancellationToken);

            // ...but we'll fire-and-forget for the actual loading. Pass CancellationToken.None since we want to ensure the progressReporter is always disposed.
            Task.Run(async () =>
                {
                    try
                    {
                        await loadOperation(progressReporter);
                    }
                    catch (Exception ex) when (FatalError.ReportAndCatch(ex))
                    {
                    }
                    finally
                    {
                        await progressReporter.DisposeAsync();
                    }
                }, CancellationToken.None).Forget();
        }
    }

    internal static bool TryGetFolderPath(WorkspaceFolder folder, ILogger logger, [NotNullWhen(returnValue: true)] out string? folderPath)
    {
        if (folder.DocumentUri.ParsedUri is null || folder.DocumentUri.ParsedUri.Scheme != Uri.UriSchemeFile)
        {
            logger.LogWarning("Workspace folder {FolderUri} is not a file URI, skipping.", folder.DocumentUri);
            folderPath = null;
            return false;
        }

        folderPath = folder.DocumentUri.GetDocumentFilePathFromUri();
        if (!Directory.Exists(folderPath))
        {
            logger.LogWarning("Workspace folder path {FolderPath} does not exist, skipping.", folderPath);
            return false;
        }

        return true;
    }

    internal static (bool isLoadingDisabled, string? solutionPath) TryGetVSCodeSolutionSettings(WorkspaceFolder[] workspaceFolders, ILogger logger)
    {
        foreach (var folder in workspaceFolders)
        {
            logger.LogTrace("Searching for VS Code settings to load in workspace folder: {FolderUri}", folder.DocumentUri);

            if (TryGetFolderPath(folder, logger, out var folderPath)
                && VSCodeSettings.TryRead(Path.Combine(folderPath, ".vscode", "settings.json"), logger, out var settings)
                && settings.TryGetStringSetting(VSCodeSettings.Names.DefaultSolution) is { Length: > 0 } defaultSolution)
            {
                var isLoadingDisabled = string.Equals(defaultSolution, "disable", StringComparison.Ordinal);
                if (isLoadingDisabled)
                {
                    if (workspaceFolders.Length == 1)
                        return (isLoadingDisabled: true, solutionPath: null);

                    continue;
                }

                var solutionPath = Path.IsPathRooted(defaultSolution)
                    ? defaultSolution
                    : Path.GetFullPath(Path.Combine(folderPath, defaultSolution));

                if (!File.Exists(solutionPath))
                {
                    logger.LogWarning("Default solution path {SolutionPath} does not exist, skipping.", solutionPath);
                    continue;
                }

                return (isLoadingDisabled: false, solutionPath);
            }
        }

        return (isLoadingDisabled: false, solutionPath: null);
    }
}
