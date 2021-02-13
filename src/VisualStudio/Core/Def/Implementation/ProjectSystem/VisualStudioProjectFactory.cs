// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(VisualStudioProjectFactory))]
    [Export(typeof(IVsTypeScriptVisualStudioProjectFactory))]
    internal sealed class VisualStudioProjectFactory : IVsTypeScriptVisualStudioProjectFactory
    {
        private const string SolutionContextName = "Solution";
        private const string SolutionSessionIdPropertyName = "SolutionSessionID";
        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioWorkspaceImpl _visualStudioWorkspaceImpl;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;
        private readonly ImmutableArray<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> _dynamicFileInfoProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioProjectFactory(
            IThreadingContext threadingContext,
            VisualStudioWorkspaceImpl visualStudioWorkspaceImpl,
            [ImportMany] IEnumerable<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> fileInfoProviders,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            _threadingContext = threadingContext;
            _visualStudioWorkspaceImpl = visualStudioWorkspaceImpl;
            _dynamicFileInfoProviders = fileInfoProviders.AsImmutableOrEmpty();
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
        }

        [Obsolete("Use CreateAndAddToWorkspaceAsync instead")]
        public VisualStudioProject CreateAndAddToWorkspace(string projectSystemName, string language)
            => CreateAndAddToWorkspace(projectSystemName, language, new VisualStudioProjectCreationInfo());

        [Obsolete("Use CreateAndAddToWorkspaceAsync instead")]
        public VisualStudioProject CreateAndAddToWorkspace(string projectSystemName, string language, VisualStudioProjectCreationInfo creationInfo)
            => _threadingContext.JoinableTaskFactory.Run(() => CreateAndAddToWorkspaceAsync(
                projectSystemName, language, creationInfo, CancellationToken.None));

        public async Task<VisualStudioProject> CreateAndAddToWorkspaceAsync(
            string projectSystemName, string language, VisualStudioProjectCreationInfo creationInfo, CancellationToken cancellationToken)
        {
            // HACK: Fetch this service to ensure it's still created on the UI thread; once this is moved off we'll need to fix up it's constructor to be free-threaded.
            _visualStudioWorkspaceImpl.Services.GetRequiredService<VisualStudioMetadataReferenceManager>();

            await _visualStudioWorkspaceImpl.EnsureDocumentOptionProvidersInitializedAsync(cancellationToken).ConfigureAwait(false);

            // From this point on, we start mutating the solution.  So make us non cancellable.
            cancellationToken = default;

            var id = ProjectId.CreateNewId(projectSystemName);
            var assemblyName = creationInfo.AssemblyName ?? projectSystemName;

            // We will use the project system name as the default display name of the project
            var project = new VisualStudioProject(
                _visualStudioWorkspaceImpl,
                _dynamicFileInfoProviders,
                _hostDiagnosticUpdateSource,
                id,
                displayName: projectSystemName,
                language,
                assemblyName: assemblyName,
                compilationOptions: creationInfo.CompilationOptions,
                filePath: creationInfo.FilePath,
                parseOptions: creationInfo.ParseOptions);

            var versionStamp = creationInfo.FilePath != null ? VersionStamp.Create(File.GetLastWriteTimeUtc(creationInfo.FilePath))
                                                             : VersionStamp.Create();

            _visualStudioWorkspaceImpl.AddProjectToInternalMaps(project, creationInfo.Hierarchy, creationInfo.ProjectGuid, projectSystemName);

            _visualStudioWorkspaceImpl.ApplyChangeToWorkspace(w =>
            {
                var projectInfo = ProjectInfo.Create(
                        id,
                        versionStamp,
                        name: projectSystemName,
                        assemblyName: assemblyName,
                        language: language,
                        filePath: creationInfo.FilePath,
                        compilationOptions: creationInfo.CompilationOptions,
                        parseOptions: creationInfo.ParseOptions)
                    .WithTelemetryId(creationInfo.ProjectGuid);

                // If we don't have any projects and this is our first project being added, then we'll create a new SolutionId
                if (w.CurrentSolution.ProjectIds.Count == 0)
                {
                    // Fetch the current solution path. Since we're on the UI thread right now, we can do that.
                    var solutionFilePath = _threadingContext.JoinableTaskFactory.Run(async () =>
                    {
                        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                        var solution = (IVsSolution)Shell.ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution));
                        if (solution != null)
                        {
                            if (ErrorHandler.Succeeded(solution.GetSolutionInfo(out _, out var filePath, out _)))
                                return filePath;
                        }

                        // Paranoia: if the call failed, we definitely don't want to use any stuff that was set
                        return null;
                    });

                    var solutionSessionId = GetSolutionSessionId();

                    w.OnSolutionAdded(
                        SolutionInfo.Create(
                            SolutionId.CreateNewId(solutionFilePath),
                            VersionStamp.Create(),
                            solutionFilePath,
                            projects: new[] { projectInfo },
                            analyzerReferences: w.CurrentSolution.AnalyzerReferences)
                        .WithTelemetryId(solutionSessionId));
                }
                else
                {
                    w.OnProjectAdded(projectInfo);
                }

                _visualStudioWorkspaceImpl.RefreshProjectExistsUIContextForLanguage(language, CancellationToken.None);
            });

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
            return _threadingContext.JoinableTaskFactory.Run(() =>
                ((IVsTypeScriptVisualStudioProjectFactory)this).CreateAndAddToWorkspaceAsync(projectSystemName, language, projectFilePath, hierarchy, projectGuid, cancellationToken));
        }

        async Task<VSTypeScriptVisualStudioProjectWrapper> IVsTypeScriptVisualStudioProjectFactory.CreateAndAddToWorkspaceAsync(
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
}
