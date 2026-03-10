// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Shared]
[ExportCSharpVisualBasicStatelessLspService(typeof(AutoLoadProjectsInitializer))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class AutoLoadProjectsInitializer(
    LanguageServerProjectSystem projectSystem,
    ILoggerFactory loggerFactory,
    ServerConfiguration serverConfiguration,
    IGlobalOptionService globalOptionService) : ILspService, IOnInitialized
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<AutoLoadProjectsInitializer>();

    public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        if (!serverConfiguration.AutoLoadProjects)
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

        var solutionLoadSettings = TryGetSolutionToLoadFromVSCodeSettings(workspaceFolders, _logger);
        if (solutionLoadSettings.IsDefaultSolutionLoadDisabled)
        {
            _logger.LogInformation("Using VS Code settings to disable auto loading solution on startup.");
            return;
        }
        else if (solutionLoadSettings.DefaultSolutionPath is not null)
        {
            _logger.LogInformation("Using VS Code settings to auto load solution {SolutionFile}", solutionLoadSettings.DefaultSolutionPath);
            projectSystem.OpenSolutionAsync(solutionLoadSettings.DefaultSolutionPath).ReportNonFatalErrorAsync().Forget();
            return;
        }

        // If there's a single workspace folder with a single solution file at the root, load that solution.
        if (workspaceFolders.Length == 1)
        {
            var folder = workspaceFolders[0];
            if (folder.DocumentUri.ParsedUri is not null && folder.DocumentUri.ParsedUri.Scheme == Uri.UriSchemeFile)
            {
                var folderPath = ProtocolConversions.GetDocumentFilePathFromUri(folder.DocumentUri.ParsedUri);
                if (Directory.Exists(folderPath))
                {
                    var solutionFiles = Directory.EnumerateFiles(folderPath, "*.sln", SearchOption.TopDirectoryOnly)
                        .Concat(Directory.EnumerateFiles(folderPath, "*.slnx", SearchOption.TopDirectoryOnly))
                        .ToArray();

                    if (solutionFiles.Length == 1)
                    {
                        _logger.LogInformation("Found single solution file {SolutionFile} to auto load", solutionFiles[0]);
                        await StartAndReportProgressAsync(() => projectSystem.OpenSolutionAsync(solutionFiles[0]));
                        return;
                    }
                }
            }
        }

        using var _ = ArrayBuilder<string>.GetInstance(out var projectFiles);
        foreach (var folder in workspaceFolders)
        {
            _logger.LogTrace("Searching for projects to load in workspace folder: {FolderUri}", folder.DocumentUri);
            if (folder.DocumentUri.ParsedUri is null || folder.DocumentUri.ParsedUri.Scheme != Uri.UriSchemeFile)
            {
                _logger.LogWarning("Workspace folder {FolderUri} is not a file URI, skipping.", folder.DocumentUri);
                continue;
            }

            var folderPath = ProtocolConversions.GetDocumentFilePathFromUri(folder.DocumentUri.ParsedUri);
            if (!Directory.Exists(folderPath))
            {
                _logger.LogWarning("Workspace folder path {FolderPath} does not exist, skipping.", folderPath);
                continue;
            }

            projectFiles.AddRange(Directory.EnumerateFiles(folderPath, "*.csproj", SearchOption.AllDirectories));
        }

        _logger.LogInformation("Discovered {count} projects to auto load", projectFiles.Count);

        await StartAndReportProgressAsync(() => projectSystem.OpenProjectsAsync(projectFiles.ToImmutable()));

        async Task StartAndReportProgressAsync(Func<Task> loadOperation)
        {
            var workDoneProgressManager = context.GetRequiredLspService<WorkDoneProgressManager>();

            // We will await for the client to know that we are starting work...
            var progressReporter = await workDoneProgressManager.CreateWorkDoneProgressAsync(reportProgressToClient: true, cancellationToken);

            // ...but we'll fire-and-forget for the actual loading. Pass CancellationToken.None since we want to ensure the progressReporter is always disposed.
            Task.Run(async () =>
                {
                    try
                    {
                        await loadOperation();
                    }
                    catch (Exception ex) when (FatalError.ReportAndCatch(ex))
                    {
                    }
                    finally
                    {
                        progressReporter.Dispose();
                    }
                }, CancellationToken.None).Forget();
        }
    }

    internal static VSCodeSolutionLoadSettings TryGetSolutionToLoadFromVSCodeSettings(WorkspaceFolder[] workspaceFolders, ILogger logger)
    {
        Contract.ThrowIfTrue(workspaceFolders.Length == 0);

        for (var i = 0; i < workspaceFolders.Length; i++)
        {
            var folder = workspaceFolders[i];
            if (folder.DocumentUri.ParsedUri is null || folder.DocumentUri.ParsedUri.Scheme != Uri.UriSchemeFile)
            {
                logger.LogWarning("Workspace folder {FolderUri} is not a file URI, skipping VS Code settings lookup.", folder.DocumentUri);
                continue;
            }

            var folderPath = ProtocolConversions.GetDocumentFilePathFromUri(folder.DocumentUri.ParsedUri);
            if (!Directory.Exists(folderPath))
            {
                logger.LogWarning("Workspace folder path {FolderPath} does not exist, skipping VS Code settings lookup.", folderPath);
                continue;
            }

            var settings = VSCodeSettings.Read(Path.Combine(folderPath, ".vscode", "settings.json"), logger);
            if (workspaceFolders.Length == 1 && settings.IsDefaultSolutionLoadDisabled)
            {
                return VSCodeSolutionLoadSettings.Disabled;
            }

            var solutionPath = settings.ResolveDefaultSolutionPath(folderPath);
            if (solutionPath is not null)
            {
                return new VSCodeSolutionLoadSettings(isDefaultSolutionLoadDisabled: false, defaultSolutionPath: solutionPath);
            }
        }

        return default;
    }

    internal readonly struct VSCodeSolutionLoadSettings(bool isDefaultSolutionLoadDisabled, string? defaultSolutionPath)
    {
        public static VSCodeSolutionLoadSettings Disabled => new(isDefaultSolutionLoadDisabled: true, defaultSolutionPath: null);

        public bool IsDefaultSolutionLoadDisabled { get; } = isDefaultSolutionLoadDisabled;
        public string? DefaultSolutionPath { get; } = defaultSolutionPath;
    }
}
