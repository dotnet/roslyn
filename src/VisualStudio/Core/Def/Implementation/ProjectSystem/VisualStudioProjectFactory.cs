using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(VisualStudioProjectFactory))]
    internal sealed class VisualStudioProjectFactory
    {
        private readonly VisualStudioWorkspaceImpl _visualStudioWorkspaceImpl;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;

        [ImportingConstructor]
        public VisualStudioProjectFactory(VisualStudioWorkspaceImpl visualStudioWorkspaceImpl, HostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            _visualStudioWorkspaceImpl = visualStudioWorkspaceImpl;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
        }

        public VisualStudioProject CreateAndAddToWorkspace(string projectUniqueName, string language)
        {
            return CreateAndAddToWorkspace(projectUniqueName, language, new VisualStudioProjectCreationInfo());
        }

        public VisualStudioProject CreateAndAddToWorkspace(string projectUniqueName, string language, VisualStudioProjectCreationInfo creationInfo)
        {
            var id = ProjectId.CreateNewId(projectUniqueName);
            var directoryNameOpt = creationInfo.FilePath != null ? Path.GetDirectoryName(creationInfo.FilePath) : null;
            var project = new VisualStudioProject(_visualStudioWorkspaceImpl, _hostDiagnosticUpdateSource, id, projectUniqueName, language, directoryNameOpt);

            var versionStamp = creationInfo.FilePath != null ? VersionStamp.Create(File.GetLastWriteTimeUtc(creationInfo.FilePath))
                                                             : VersionStamp.Create();

            var assemblyName = creationInfo.AssemblyName ?? projectUniqueName;

            _visualStudioWorkspaceImpl.AddProjectToInternalMaps(project, creationInfo.Hierarchy, creationInfo.ProjectGuid, projectUniqueName);

            _visualStudioWorkspaceImpl.ApplyChangeToWorkspace(w =>
                w.OnProjectAdded(
                    ProjectInfo.Create(
                        id,
                        versionStamp,
                        name: projectUniqueName,
                        assemblyName: assemblyName,
                        language: language,
                        filePath: creationInfo.FilePath,
                        compilationOptions: creationInfo.CompilationOptions,
                        parseOptions: creationInfo.ParseOptions)));

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
