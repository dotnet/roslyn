// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(LanguageServerWorkspaceFactory)), Shared]
internal sealed class LanguageServerWorkspaceFactory
{
    private readonly ILogger _logger;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LanguageServerWorkspaceFactory(
        HostServicesProvider hostServicesProvider,
        IFileChangeWatcher fileChangeWatcher,
        [ImportMany] IEnumerable<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> dynamicFileInfoProviders,
        ProjectTargetFrameworkManager projectTargetFrameworkManager,
        ServerConfigurationFactory serverConfigurationFactory,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(LanguageServerWorkspaceFactory));

        var workspace = new LanguageServerWorkspace(hostServicesProvider.HostServices);
        Workspace = workspace;
        ProjectSystemProjectFactory = new ProjectSystemProjectFactory(
            Workspace, fileChangeWatcher, static (_, _) => Task.CompletedTask, _ => { },
            CancellationToken.None); // TODO: do we need to introduce a shutdown cancellation token for this?
        workspace.ProjectSystemProjectFactory = ProjectSystemProjectFactory;

        var razorSourceGenerator = serverConfigurationFactory?.ServerConfiguration?.RazorSourceGenerator;
        ProjectSystemHostInfo = new ProjectSystemHostInfo(
            DynamicFileInfoProviders: dynamicFileInfoProviders.ToImmutableArray(),
            new HostDiagnosticAnalyzerProvider(razorSourceGenerator));

        TargetFrameworkManager = projectTargetFrameworkManager;
    }

    public Workspace Workspace { get; }

    public ProjectSystemProjectFactory ProjectSystemProjectFactory { get; }
    public ProjectSystemHostInfo ProjectSystemHostInfo { get; }
    public ProjectTargetFrameworkManager TargetFrameworkManager { get; }

    public async Task InitializeSolutionLevelAnalyzersAsync(ImmutableArray<string> analyzerPaths)
    {
        var references = new List<AnalyzerFileReference>();
        var loaderProvider = Workspace.Services.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();

        // Load all analyzers into a fresh shadow copied load context.  In the future, if we want to support reloading
        // of solution-level analyzer references, we should just need to listen for changes to those analyzer paths and
        // then call back into this method to update the solution accordingly.
        var analyzerLoader = loaderProvider.CreateNewShadowCopyLoader();

        foreach (var analyzerPath in analyzerPaths)
        {
            if (File.Exists(analyzerPath))
            {
                references.Add(new AnalyzerFileReference(analyzerPath, analyzerLoader));
                _logger.LogDebug($"Solution-level analyzer at {analyzerPath} added to workspace.");
            }
            else
            {
                _logger.LogWarning($"Solution-level analyzer at {analyzerPath} could not be found.");
            }
        }

        await ProjectSystemProjectFactory.ApplyChangeToWorkspaceAsync(w =>
        {
            w.SetCurrentSolution(s => s.WithAnalyzerReferences(references), WorkspaceChangeKind.SolutionChanged);
        });
    }
}
