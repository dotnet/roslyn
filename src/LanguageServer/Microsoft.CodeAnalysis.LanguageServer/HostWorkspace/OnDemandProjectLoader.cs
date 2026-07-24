// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicLspServiceFactory(typeof(OnDemandProjectLoader)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OnDemandProjectLoaderFactory(
    IGlobalOptionService globalOptionService,
    ILoggerFactory loggerFactory) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new OnDemandProjectLoader(
            lspServices.GetRequiredService<WorkspaceProjectDiscoveryService>(),
            lspServices.GetRequiredService<LanguageServerProjectSystem>(),
            globalOptionService,
            loggerFactory);
}

internal sealed class OnDemandProjectLoader(
    WorkspaceProjectDiscoveryService workspaceProjectDiscoveryService,
    LanguageServerProjectSystem projectSystem,
    IGlobalOptionService globalOptionService,
    ILoggerFactory loggerFactory) : IOnDemandProjectLoader
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<OnDemandProjectLoader>();
    private readonly HashSet<string> _loadedProjectPaths = new(StringComparer.OrdinalIgnoreCase);

    public async ValueTask<bool> TryLoadProjectsForDocumentAsync(DocumentUri uri, CancellationToken cancellationToken)
    {
        if (!globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand))
            return false;

        if (!string.Equals(uri.ParsedUri?.Scheme, "file", StringComparison.OrdinalIgnoreCase))
            return false;

        var filePath = uri.GetDocumentFilePathFromUri();
        var candidateProjects = await workspaceProjectDiscoveryService.GetCandidateProjectsAsync(filePath, cancellationToken).ConfigureAwait(false);
        if (candidateProjects.IsDefaultOrEmpty)
            return false;

        using var _ = ArrayBuilder<string>.GetInstance(out var toLoad);
        foreach (var candidateProject in candidateProjects)
        {
            if (_loadedProjectPaths.Add(candidateProject))
                toLoad.Add(candidateProject);
        }

        if (toLoad.Count == 0)
            return false;

        _logger.LogInformation("Loading {ProjectCount} project(s) on demand for '{DocumentPath}'.", toLoad.Count, filePath);
        await projectSystem.OpenProjectsAsync(toLoad.ToImmutable()).ConfigureAwait(false);
        return true;
    }
}
