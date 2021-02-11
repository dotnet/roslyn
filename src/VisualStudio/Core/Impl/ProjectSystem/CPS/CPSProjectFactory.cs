// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    [Export(typeof(IWorkspaceProjectContextFactory))]
    internal partial class CPSProjectFactory : IWorkspaceProjectContextFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioProjectFactory _projectFactory;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IProjectCodeModelFactory _projectCodeModelFactory;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CPSProjectFactory(
            IThreadingContext threadingContext,
            VisualStudioProjectFactory projectFactory,
            VisualStudioWorkspaceImpl workspace,
            IProjectCodeModelFactory projectCodeModelFactory)
        {
            _threadingContext = threadingContext;
            _projectFactory = projectFactory;
            _workspace = workspace;
            _projectCodeModelFactory = projectCodeModelFactory;
        }

        IWorkspaceProjectContext IWorkspaceProjectContextFactory.CreateProjectContext(string languageName, string projectUniqueName, string projectFilePath, Guid projectGuid, object hierarchy, string binOutputPath)
        {
            return _threadingContext.JoinableTaskFactory.Run(async () =>
                await ((IWorkspaceProjectContextFactory)this).CreateProjectContextAsync(
                    languageName, projectUniqueName, projectFilePath, projectGuid, hierarchy, binOutputPath, CancellationToken.None).ConfigureAwait(false));
        }

        async Task<IWorkspaceProjectContext> IWorkspaceProjectContextFactory.CreateProjectContextAsync(
            string languageName,
            string projectUniqueName,
            string projectFilePath,
            Guid projectGuid,
            object hierarchy,
            string binOutputPath,
            CancellationToken cancellationToken)
        {
            var visualStudioProject = await CreateVisualStudioProjectAsync(
                languageName, projectUniqueName, projectFilePath, hierarchy as IVsHierarchy, projectGuid, cancellationToken).ConfigureAwait(false);
            return new CPSProject(visualStudioProject, _workspace, _projectCodeModelFactory, projectGuid, binOutputPath);
        }

        private async Task<VisualStudioProject> CreateVisualStudioProjectAsync(
            string languageName, string projectUniqueName, string projectFilePath, IVsHierarchy hierarchy, Guid projectGuid, CancellationToken cancellationToken)
        {
            var creationInfo = new VisualStudioProjectCreationInfo
            {
                FilePath = projectFilePath,
                Hierarchy = hierarchy,
                ProjectGuid = projectGuid,
            };

            var visualStudioProject = await _projectFactory.CreateAndAddToWorkspaceAsync(projectUniqueName, languageName, creationInfo, cancellationToken).ConfigureAwait(false);

            // At this point we've mutated the workspace.  So we're no longer cancellable.
            cancellationToken = default;

            if (languageName == LanguageNames.FSharp)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                var shell = (IVsShell)ServiceProvider.GlobalProvider.GetService(typeof(SVsShell));

                // Force the F# package to load; this is necessary because the F# package listens to WorkspaceChanged to 
                // set up some items, and the F# project system doesn't guarantee that the F# package has been loaded itself
                // so we're caught in the middle doing this.
                shell.LoadPackage(Guids.FSharpPackageId, out _);
            }

            return visualStudioProject;
        }
    }
}
