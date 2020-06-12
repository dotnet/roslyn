// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Internal.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(VisualStudioProjectFactory))]
    internal sealed class VisualStudioProjectFactory
    {
        private const string SolutionContextName = "Solution";
        private const string SolutionSessionIdPropertyName = "SolutionSessionID";

        private readonly VisualStudioWorkspaceImpl _visualStudioWorkspaceImpl;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;
        private readonly ImmutableArray<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> _dynamicFileInfoProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioProjectFactory(
            VisualStudioWorkspaceImpl visualStudioWorkspaceImpl,
            [ImportMany] IEnumerable<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> fileInfoProviders,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            _visualStudioWorkspaceImpl = visualStudioWorkspaceImpl;
            _dynamicFileInfoProviders = fileInfoProviders.AsImmutableOrEmpty();
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
        }

        public VisualStudioProject CreateAndAddToWorkspace(string projectSystemName, string language)
            => CreateAndAddToWorkspace(projectSystemName, language, new VisualStudioProjectCreationInfo());

        public VisualStudioProject CreateAndAddToWorkspace(string projectSystemName, string language, VisualStudioProjectCreationInfo creationInfo)
        {
            // HACK: Fetch this service to ensure it's still created on the UI thread; once this is moved off we'll need to fix up it's constructor to be free-threaded.
            _visualStudioWorkspaceImpl.Services.GetRequiredService<VisualStudioMetadataReferenceManager>();

            // HACK: since we're on the UI thread, ensure we initialize our options provider which depends on a UI-affinitized experimentation service
            _visualStudioWorkspaceImpl.EnsureDocumentOptionProvidersInitialized();

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
                    string? solutionFilePath = null;
                    var solution = (IVsSolution)Shell.ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution));
                    if (solution != null)
                    {
                        if (ErrorHandler.Failed(solution.GetSolutionInfo(out _, out solutionFilePath, out _)))
                        {
                            // Paranoia: if the call failed, we definitely don't want to use any stuff that was set
                            solutionFilePath = null;
                        }
                    }

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

                _visualStudioWorkspaceImpl.RefreshProjectExistsUIContextForLanguage(language);
            });

            return project;

            static Guid GetSolutionSessionId()
            {
                try
                {
                    var solutionContext = TelemetryHelper.DataModelTelemetrySession.GetContext(SolutionContextName);
                    var sessionIdProperty = solutionContext is object
                        ? (string)solutionContext.SharedProperties[SolutionSessionIdPropertyName]
                        : "";
                    _ = Guid.TryParse(sessionIdProperty, out var solutionSessionId);
                    return solutionSessionId;
                }
                catch (TypeInitializationException)
                {
                    // The TelemetryHelper cannot be constructed during unittests.
                    return default;
                }
            }
        }
    }
}
