﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
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
    private readonly ImmutableArray<IAnalyzerAssemblyRedirector> _analyzerAssemblyRedirectors;
    private readonly IVsService<SVsBackgroundSolution, IVsBackgroundSolution> _solution;

    private readonly JoinableTask<VisualStudioDiagnosticAnalyzerProvider> _initializationTask;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioProjectFactory(
        IThreadingContext threadingContext,
        VisualStudioWorkspaceImpl visualStudioWorkspaceImpl,
        [ImportMany] IEnumerable<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> fileInfoProviders,
        IVisualStudioDiagnosticAnalyzerProviderFactory vsixAnalyzerProviderFactory,
        [ImportMany] IEnumerable<IAnalyzerAssemblyRedirector> analyzerAssemblyRedirectors,
        IVsService<SVsBackgroundSolution, IVsBackgroundSolution> solution)
    {
        _threadingContext = threadingContext;
        _visualStudioWorkspaceImpl = visualStudioWorkspaceImpl;
        _dynamicFileInfoProviders = fileInfoProviders.AsImmutableOrEmpty();
        _vsixAnalyzerProviderFactory = vsixAnalyzerProviderFactory;
        _analyzerAssemblyRedirectors = analyzerAssemblyRedirectors.AsImmutableOrEmpty();
        _solution = solution;

        _initializationTask = _threadingContext.JoinableTaskFactory.RunAsync(
            async () =>
            {
                var cancellationToken = _threadingContext.DisposalToken;

                // HACK: Fetch this service to ensure it's still created on the UI thread; once this is
                // moved off we'll need to fix up it's constructor to be free-threaded.

                // yield if on the main thread, as the VisualStudioMetadataReferenceManager construction can be fairly expensive
                // and we don't want the case where VisualStudioProjectFactory is constructed on the main thread to block on that.
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
                _visualStudioWorkspaceImpl.Services.GetRequiredService<VisualStudioMetadataReferenceManager>();

                _visualStudioWorkspaceImpl.SubscribeExternalErrorDiagnosticUpdateSourceToSolutionBuildEvents();
                _visualStudioWorkspaceImpl.SubscribeToSourceGeneratorImpactingEvents();

                return await _vsixAnalyzerProviderFactory.GetOrCreateProviderAsync(cancellationToken).ConfigureAwait(true);
            });
    }

    public Task<ProjectSystemProject> CreateAndAddToWorkspaceAsync(string projectSystemName, string language, CancellationToken cancellationToken)
        => CreateAndAddToWorkspaceAsync(projectSystemName, language, new VisualStudioProjectCreationInfo(), cancellationToken);

    public async Task<ProjectSystemProject> CreateAndAddToWorkspaceAsync(
        string projectSystemName, string language, VisualStudioProjectCreationInfo creationInfo, CancellationToken cancellationToken)
    {
        var vsixAnalyzerProvider = await _initializationTask.JoinAsync(cancellationToken).ConfigureAwait(false);

        // The rest of this method can be ran off the UI thread. We'll only switch though if the UI thread isn't already blocked -- the legacy project
        // system creates project synchronously, and during solution load we've seen traces where the thread pool is sufficiently saturated that this
        // switch can't be completed quickly. For the rest of this method, we won't use ConfigureAwait(false) since we're expecting VS threading
        // rules to apply.
        if (!_threadingContext.JoinableTaskContext.IsMainThreadBlocked())
        {
            await TaskScheduler.Default;
        }

        var solution = await _solution.GetValueOrNullAsync(cancellationToken).ConfigureAwait(true);

        // From this point on, we start mutating the solution.  So make us non cancellable.
        cancellationToken = CancellationToken.None;

        _visualStudioWorkspaceImpl.ProjectSystemProjectFactory.SolutionPath = solution?.SolutionFileName;
        _visualStudioWorkspaceImpl.ProjectSystemProjectFactory.SolutionTelemetryId = GetSolutionSessionId();

        var hostInfo = new ProjectSystemHostInfo(_dynamicFileInfoProviders, vsixAnalyzerProvider, _analyzerAssemblyRedirectors);
        var project = await _visualStudioWorkspaceImpl.ProjectSystemProjectFactory.CreateAndAddToWorkspaceAsync(projectSystemName, language, creationInfo, hostInfo).ConfigureAwait(true);

        _visualStudioWorkspaceImpl.AddProjectToInternalMaps(project, creationInfo.Hierarchy, creationInfo.ProjectGuid, projectSystemName);

        // Ensure that other VS contexts get accurate information that the UIContext for this language is now active.
        await _visualStudioWorkspaceImpl.RefreshProjectExistsUIContextForLanguageAsync(language, cancellationToken).ConfigureAwait(true);

        return project;

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
