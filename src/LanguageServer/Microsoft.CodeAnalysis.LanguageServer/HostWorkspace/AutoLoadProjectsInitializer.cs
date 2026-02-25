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
                        // We don't want to block initialization on loading the solution - fire and forget.
                        projectSystem.OpenSolutionAsync(solutionFiles[0]).ReportNonFatalErrorAsync().Forget();
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

        // We don't want to block initialization on loading projects - fire and forget.
        projectSystem.OpenProjectsAsync(projectFiles.ToImmutable()).ReportNonFatalErrorAsync().Forget();
    }
}
