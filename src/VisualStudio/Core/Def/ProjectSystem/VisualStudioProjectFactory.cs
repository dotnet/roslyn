// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

[Export(typeof(VisualStudioProjectFactory))]
[Export(typeof(IVsTypeScriptVisualStudioProjectFactory))]
internal sealed class VisualStudioProjectFactory : IVsTypeScriptVisualStudioProjectFactory
{
    private const string SolutionContextName = "Solution";
    private const string SolutionSessionIdPropertyName = "SolutionSessionID";

    private readonly IThreadingContext _threadingContext;
    private readonly VisualStudioWorkspaceImpl _visualStudioWorkspaceImpl;
    private readonly ImmutableArray<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> _dynamicFileInfoProviders;
    private readonly IVisualStudioDiagnosticAnalyzerProviderFactory _vsixAnalyzerProviderFactory;
    private readonly IInsertedAnalyzerProviderFactory _insertedAnalyzerProviderFactory;
    private readonly IVsService<SVsSolution, IVsSolution2> _solution2;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioProjectFactory(
        IThreadingContext threadingContext,
        VisualStudioWorkspaceImpl visualStudioWorkspaceImpl,
        [ImportMany] IEnumerable<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> fileInfoProviders,
        IVisualStudioDiagnosticAnalyzerProviderFactory vsixAnalyzerProviderFactory,
        IInsertedAnalyzerProviderFactory insertedAnalyzerProviderFactory,
        IVsService<SVsSolution, IVsSolution2> solution2)
    {
        _threadingContext = threadingContext;
        _visualStudioWorkspaceImpl = visualStudioWorkspaceImpl;
        _dynamicFileInfoProviders = fileInfoProviders.AsImmutableOrEmpty();
        _vsixAnalyzerProviderFactory = vsixAnalyzerProviderFactory;
        _insertedAnalyzerProviderFactory = insertedAnalyzerProviderFactory;
        _solution2 = solution2;
    }

    public Task<ProjectSystemProject> CreateAndAddToWorkspaceAsync(string projectSystemName, string language, CancellationToken cancellationToken)
        => CreateAndAddToWorkspaceAsync(projectSystemName, language, new VisualStudioProjectCreationInfo(), cancellationToken);

    public async Task<ProjectSystemProject> CreateAndAddToWorkspaceAsync(
        string projectSystemName, string language, VisualStudioProjectCreationInfo creationInfo, CancellationToken cancellationToken)
    {
        // HACK: Fetch this service to ensure it's still created on the UI thread; once this is
        // moved off we'll need to fix up it's constructor to be free-threaded.

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        _visualStudioWorkspaceImpl.Services.GetRequiredService<VisualStudioMetadataReferenceManager>();

        _visualStudioWorkspaceImpl.SubscribeExternalErrorDiagnosticUpdateSourceToSolutionBuildEvents();
        _visualStudioWorkspaceImpl.SubscribeToSourceGeneratorImpactingEvents();

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        // Since we're on the UI thread here anyways, use that as an opportunity to grab the
        // IVsSolution object and solution file path.
        var solution = await _solution2.GetValueOrNullAsync(cancellationToken);
        var solutionFilePath = solution != null && ErrorHandler.Succeeded(solution.GetSolutionInfo(out _, out var filePath, out _))
            ? filePath
            : null;

        var vsixAnalyzerProvider = await _vsixAnalyzerProviderFactory.GetOrCreateProviderAsync(cancellationToken).ConfigureAwait(false);

        _visualStudioWorkspaceImpl.LazyInsertedAnalyzerProvider = await _insertedAnalyzerProviderFactory.GetOrCreateProviderAsync(cancellationToken).ConfigureAwait(false);

        // The rest of this method can be ran off the UI thread. We'll only switch though if the UI thread isn't already blocked -- the legacy project
        // system creates project synchronously, and during solution load we've seen traces where the thread pool is sufficiently saturated that this
        // switch can't be completed quickly. For the rest of this method, we won't use ConfigureAwait(false) since we're expecting VS threading
        // rules to apply.
        if (!_threadingContext.JoinableTaskContext.IsMainThreadBlocked())
        {
            await TaskScheduler.Default;
        }

        // From this point on, we start mutating the solution.  So make us non cancellable.
#pragma warning disable IDE0059 // Unnecessary assignment of a value
        cancellationToken = CancellationToken.None;
#pragma warning restore IDE0059 // Unnecessary assignment of a value

        _visualStudioWorkspaceImpl.ProjectSystemProjectFactory.SolutionPath = solutionFilePath;
        _visualStudioWorkspaceImpl.ProjectSystemProjectFactory.SolutionTelemetryId = GetSolutionSessionId();

        var hostInfo = new ProjectSystemHostInfo(_dynamicFileInfoProviders, HostDiagnosticUpdateSource.Instance, vsixAnalyzerProvider);
        var project = await _visualStudioWorkspaceImpl.ProjectSystemProjectFactory.CreateAndAddToWorkspaceAsync(projectSystemName, language, creationInfo, hostInfo);

        _visualStudioWorkspaceImpl.AddProjectToInternalMaps(project, creationInfo.Hierarchy, creationInfo.ProjectGuid, projectSystemName);

        // Ensure that other VS contexts get accurate information that the UIContext for this language is now active.
        // This is not cancellable as we have already mutated the solution.
        await _visualStudioWorkspaceImpl.RefreshProjectExistsUIContextForLanguageAsync(language, CancellationToken.None);

        return project;

#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

        static Guid GetSolutionSessionId()
        {
            var dataModelTelemetrySession = TelemetryService.DefaultSession;
            var solutionContext = dataModelTelemetrySession.GetContext(SolutionContextName);
            var sessionIdProperty = solutionContext is object
                ? (string)solutionContext.SharedProperties[SolutionSessionIdPropertyName]
                : "";
            _ = Guid.TryParse(sessionIdProperty, out var solutionSessionId);
            return solutionSessionId;
        }
    }

    VSTypeScriptVisualStudioProjectWrapper IVsTypeScriptVisualStudioProjectFactory.CreateAndAddToWorkspace(string projectSystemName, string language, string projectFilePath, IVsHierarchy hierarchy, Guid projectGuid)
    {
        return _threadingContext.JoinableTaskFactory.Run(async () =>
            await ((IVsTypeScriptVisualStudioProjectFactory)this).CreateAndAddToWorkspaceAsync(projectSystemName, language, projectFilePath, hierarchy, projectGuid, CancellationToken.None).ConfigureAwait(false));
    }

    async ValueTask<VSTypeScriptVisualStudioProjectWrapper> IVsTypeScriptVisualStudioProjectFactory.CreateAndAddToWorkspaceAsync(
        string projectSystemName, string language, string projectFilePath, IVsHierarchy hierarchy, Guid projectGuid, CancellationToken cancellationToken)
    {
        var projectInfo = new VisualStudioProjectCreationInfo
        {
            FilePath = projectFilePath,
            Hierarchy = hierarchy,
            ProjectGuid = projectGuid,
        };
        var visualStudioProject = await this.CreateAndAddToWorkspaceAsync(projectSystemName, language, projectInfo, cancellationToken).ConfigureAwait(false);
        return new VSTypeScriptVisualStudioProjectWrapper(visualStudioProject);
    }
}
