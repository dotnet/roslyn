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

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(VisualStudioProjectFactory))]
    internal sealed class VisualStudioProjectFactory
    {
        private readonly VisualStudioWorkspaceImpl _visualStudioWorkspaceImpl;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;
        private readonly ImmutableArray<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> _dynamicFileInfoProviders;

        [ImportingConstructor]
        // TODO: remove the AllowDefault = true on HostDiagnosticUpdateSource by making it a proper mock
        public VisualStudioProjectFactory(
            VisualStudioWorkspaceImpl visualStudioWorkspaceImpl,
            [ImportMany]IEnumerable<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> fileInfoProviders,
            [Import(AllowDefault = true)] HostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            _visualStudioWorkspaceImpl = visualStudioWorkspaceImpl;
            _dynamicFileInfoProviders = fileInfoProviders.AsImmutableOrEmpty();
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
        }

        public VisualStudioProject CreateAndAddToWorkspace(string projectSystemName, string language)
        {
            return CreateAndAddToWorkspace(projectSystemName, language, new VisualStudioProjectCreationInfo());
        }

        public VisualStudioProject CreateAndAddToWorkspace(string projectSystemName, string language, VisualStudioProjectCreationInfo creationInfo)
        {
            // HACK: Fetch this service to ensure it's still created on the UI thread; once this is moved off we'll need to fix up it's constructor to be free-threaded.
            _visualStudioWorkspaceImpl.Services.GetRequiredService<VisualStudioMetadataReferenceManager>();

            // HACK: since we're on the UI thread, ensure we initialize our options provider which depends on a UI-affinitized experimentation service
            _visualStudioWorkspaceImpl.EnsureDocumentOptionProvidersInitialized();

            var id = ProjectId.CreateNewId(projectSystemName);
            var directoryNameOpt = creationInfo.FilePath != null ? Path.GetDirectoryName(creationInfo.FilePath) : null;

            // We will use the project system name as the default display name of the project
            var project = new VisualStudioProject(_visualStudioWorkspaceImpl, _dynamicFileInfoProviders, _hostDiagnosticUpdateSource, id, displayName: projectSystemName, language, directoryNameOpt);

            var versionStamp = creationInfo.FilePath != null ? VersionStamp.Create(File.GetLastWriteTimeUtc(creationInfo.FilePath))
                                                             : VersionStamp.Create();

            var assemblyName = creationInfo.AssemblyName ?? projectSystemName;

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
                        parseOptions: creationInfo.ParseOptions);

                // If we don't have any projects and this is our first project being added, then we'll create a new SolutionId
                if (w.CurrentSolution.ProjectIds.Count == 0)
                {
                    // Fetch the current solution path. Since we're on the UI thread right now, we can do that.
                    string solutionFilePath = null;
                    var solution = (IVsSolution)Shell.ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution));
                    if (solution != null)
                    {
                        if (ErrorHandler.Failed(solution.GetSolutionInfo(out _, out solutionFilePath, out _)))
                        {
                            // Paranoia: if the call failed, we definitely don't want to use any stuff that was set
                            solutionFilePath = null;
                        }
                    }

                    w.OnSolutionAdded(
                        SolutionInfo.Create(
                            SolutionId.CreateNewId(solutionFilePath),
                            VersionStamp.Create(),
                            solutionFilePath,
                            projects: new[] { projectInfo }));
                }
                else
                {
                    w.OnProjectAdded(projectInfo);
                }

                _visualStudioWorkspaceImpl.RefreshProjectExistsUIContextForLanguage(language);
            });

            // We do all these sets after the w.OnProjectAdded, as the setting of these properties is going to try to modify the workspace
            // again. Those modifications will all implicitly do nothing, since the workspace already has the values from above.
            // We could pass these all through the constructor (but that gets verbose), or have some other control to ignore these,
            // but that seems like overkill.
            project.AssemblyName = assemblyName;
            project.CompilationOptions = creationInfo.CompilationOptions;
            project.FilePath = creationInfo.FilePath;
            project.ParseOptions = creationInfo.ParseOptions;

            return project;
        }
    }
}
