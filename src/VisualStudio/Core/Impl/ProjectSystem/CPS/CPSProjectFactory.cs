// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    [Export(typeof(IWorkspaceProjectContextFactory))]
    internal partial class CPSProjectFactory : IWorkspaceProjectContextFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioProjectFactory _projectFactory;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IProjectCodeModelFactory _projectCodeModelFactory;
        private readonly IAsyncServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CPSProjectFactory(
            IThreadingContext threadingContext,
            VisualStudioProjectFactory projectFactory,
            VisualStudioWorkspaceImpl workspace,
            IProjectCodeModelFactory projectCodeModelFactory,
            SVsServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _projectFactory = projectFactory;
            _workspace = workspace;
            _projectCodeModelFactory = projectCodeModelFactory;
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
        }

        public ImmutableArray<string> EvaluationPropertyNames
            => BuildPropertyNames.InitialEvaluationPropertyNames;

        public ImmutableArray<string> EvaluationItemNames
            => BuildPropertyNames.InitialEvaluationItemNames;

        public Task<IWorkspaceProjectContext> CreateProjectContextAsync(Guid id, string uniqueName, string languageName, EvaluationData data, object? hostObject, CancellationToken cancellationToken)
            => CreateProjectContextAsync(
                languageName: languageName,
                projectUniqueName: uniqueName,
                projectFilePath: data.GetRequiredPropertyAbsolutePathValue(BuildPropertyNames.MSBuildProjectFullPath),
                projectGuid: id,
                hierarchy: hostObject,
                binOutputPath: (languageName is LanguageNames.CSharp or LanguageNames.VisualBasic) ?
                    data.GetRequiredPropertyAbsolutePathValue(BuildPropertyNames.TargetPath) :
                    data.GetPropertyValue(BuildPropertyNames.TargetPath),
                assemblyName: data.GetPropertyValue(BuildPropertyNames.AssemblyName),
                cancellationToken);

        public async Task<IWorkspaceProjectContext> CreateProjectContextAsync(
            string languageName,
            string projectUniqueName,
            string? projectFilePath,
            Guid projectGuid,
            object? hierarchy,
            string? binOutputPath,
            string? assemblyName,
            CancellationToken cancellationToken)
        {
            var creationInfo = new VisualStudioProjectCreationInfo
            {
                AssemblyName = assemblyName,
                FilePath = projectFilePath,
                Hierarchy = hierarchy as IVsHierarchy,
                ProjectGuid = projectGuid,
            };

            var visualStudioProject = await _projectFactory.CreateAndAddToWorkspaceAsync(
                projectUniqueName, languageName, creationInfo, cancellationToken).ConfigureAwait(false);

            // At this point we've mutated the workspace.  So we're no longer cancellable.
            cancellationToken = CancellationToken.None;

            if (languageName == LanguageNames.FSharp)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var shell = await _serviceProvider.GetServiceAsync<SVsShell, IVsShell7>(_threadingContext.JoinableTaskFactory).ConfigureAwait(true);

                // Force the F# package to load; this is necessary because the F# package listens to WorkspaceChanged to 
                // set up some items, and the F# project system doesn't guarantee that the F# package has been loaded itself
                // so we're caught in the middle doing this.
                var packageId = Guids.FSharpPackageId;
                await shell.LoadPackageAsync(ref packageId);

                await TaskScheduler.Default;
            }

            var project = new CPSProject(visualStudioProject, _workspace, _projectCodeModelFactory, projectGuid);

            // Set the output path in a batch; if we set the property directly we'll be taking a synchronous lock here and
            // potentially block up thread pool threads. Doing this in a batch means the global lock will be acquired asynchronously.
            project.StartBatch();
            project.BinOutputPath = binOutputPath;
            await project.EndBatchAsync().ConfigureAwait(false);

            return project;
        }
    }
}
