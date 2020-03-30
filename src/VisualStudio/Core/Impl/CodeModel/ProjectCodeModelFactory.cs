// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    [Export(typeof(IProjectCodeModelFactory))]
    [Export(typeof(ProjectCodeModelFactory))]
    internal sealed class ProjectCodeModelFactory : IProjectCodeModelFactory, IDisposable
    {
        private readonly ConcurrentDictionary<ProjectId, ProjectCodeModel> _projectCodeModels = new ConcurrentDictionary<ProjectId, ProjectCodeModel>();

        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        private readonly IServiceProvider _serviceProvider;

        private readonly IThreadingContext _threadingContext;

        /// <summary>
        /// A collection of cleanup tasks that were deferred to the UI thread. In some cases, we have to clean up
        /// certain things on the UI thread but it's not critical when those are cleared up. This just exists so we can
        /// wait on them to be shut down before we shut down VS entirely.
        /// </summary>
        private readonly JoinableTaskCollection _deferredCleanupTasks;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public ProjectCodeModelFactory(VisualStudioWorkspace visualStudioWorkspace, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, IThreadingContext threadingContext)
        {
            _visualStudioWorkspace = visualStudioWorkspace;
            _serviceProvider = serviceProvider;
            _threadingContext = threadingContext;
            _deferredCleanupTasks = new JoinableTaskCollection(threadingContext.JoinableTaskContext);
            _deferredCleanupTasks.DisplayName = nameof(ProjectCodeModelFactory) + "." + nameof(_deferredCleanupTasks);
        }

        public IProjectCodeModel CreateProjectCodeModel(ProjectId id, ICodeModelInstanceFactory codeModelInstanceFactory)
        {
            var projectCodeModel = new ProjectCodeModel(_threadingContext, id, codeModelInstanceFactory, _visualStudioWorkspace, _serviceProvider, this);
            if (!_projectCodeModels.TryAdd(id, projectCodeModel))
            {
                throw new InvalidOperationException($"A {nameof(IProjectCodeModel)} has already been created for project with ID {id}");
            }

            return projectCodeModel;
        }

        public ProjectCodeModel GetProjectCodeModel(ProjectId id)
        {
            if (!_projectCodeModels.TryGetValue(id, out var projectCodeModel))
            {
                throw new InvalidOperationException($"No {nameof(ProjectCodeModel)} exists for project with ID {id}");
            }

            return projectCodeModel;
        }

        public IEnumerable<ProjectCodeModel> GetAllProjectCodeModels()
            => _projectCodeModels.Values;

        internal void OnProjectClosed(ProjectId projectId)
            => _projectCodeModels.TryRemove(projectId, out _);

        public ProjectCodeModel TryGetProjectCodeModel(ProjectId id)
        {
            _projectCodeModels.TryGetValue(id, out var projectCodeModel);
            return projectCodeModel;
        }

        public EnvDTE.FileCodeModel GetOrCreateFileCodeModel(ProjectId id, string filePath)
            => GetProjectCodeModel(id).GetOrCreateFileCodeModel(filePath).Handle;

        public void ScheduleDeferredCleanupTask(Action a)
            => _deferredCleanupTasks.Add(_threadingContext.JoinableTaskFactory.StartOnIdle(a, VsTaskRunContext.UIThreadNormalPriority));

        void IDisposable.Dispose()
            => _deferredCleanupTasks.Join();
    }
}
