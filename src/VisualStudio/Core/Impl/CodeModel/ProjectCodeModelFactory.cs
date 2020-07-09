// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

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

        /// <summary>
        /// Cancellation token that controls existing async work that we have kicked off.  Canceled when we're finally
        /// disposed.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly IForegroundNotificationService _notificationService;
        private readonly IAsynchronousOperationListener _listener;
        private readonly AsyncBatchingWorkQueue<DocumentId> _documentsToFireEventsFor;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public ProjectCodeModelFactory(
            VisualStudioWorkspace visualStudioWorkspace,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IThreadingContext threadingContext,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _visualStudioWorkspace = visualStudioWorkspace;
            _serviceProvider = serviceProvider;
            _threadingContext = threadingContext;
            _deferredCleanupTasks = new JoinableTaskCollection(threadingContext.JoinableTaskContext);
            _deferredCleanupTasks.DisplayName = nameof(ProjectCodeModelFactory) + "." + nameof(_deferredCleanupTasks);

            _notificationService = notificationService;
            _listener = listenerProvider.GetListener(FeatureAttribute.CodeModel);

            // Queue up notifications we hear about docs changing.  that way we don't have to fire events multiple times
            // for the same documents.  Once enough time has passed, take the documents that were changed and run
            // through them, firing their latest events.
            _documentsToFireEventsFor = new AsyncBatchingWorkQueue<DocumentId>(
                TimeSpan.FromMilliseconds(visualStudioWorkspace.Options.GetOption(InternalSolutionCrawlerOptions.AllFilesWorkerBackOffTimeSpanInMS)),
                ProcessNextDocumentBatchAsync,
                // We only care about unique doc-ids, so pass in this comparer to collapse streams of changes for a
                // single document down to one notification.
                EqualityComparer<DocumentId>.Default,
                _listener,
                _cancellationTokenSource.Token);

            _visualStudioWorkspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        private System.Threading.Tasks.Task ProcessNextDocumentBatchAsync(
            ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            foreach (var documentId in documentIds)
            {
                // Now, enqueue foreground work to actually process these documents in a serialized and incremental
                // fashion.  FireEventsForDocument will actually limit how much time it spends firing events so that it
                // doesn't saturate  the UI thread.
                _notificationService.RegisterNotification(
                    () => FireEventsForDocument(documentId),
                    _listener.BeginAsyncOperation("CodeModelEvent"),
                    cancellationToken);
            }

            return System.Threading.Tasks.Task.CompletedTask;

            bool FireEventsForDocument(DocumentId documentId)
            {
                // If we've been asked to shutdown, don't bother reporting any more events.
                if (_cancellationTokenSource.IsCancellationRequested)
                    return false;

                var projectCodeModel = this.TryGetProjectCodeModel(documentId.ProjectId);
                if (projectCodeModel == null)
                    return false;

                var filename = _visualStudioWorkspace.GetFilePath(documentId);
                if (filename == null)
                    return false;

                if (!projectCodeModel.TryGetCachedFileCodeModel(filename, out var fileCodeModelHandle))
                    return false;

                var codeModel = fileCodeModelHandle.Object;
                return codeModel.FireEvents();
            }
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            // Events that can't change existing code model items.  Can just ignore them.
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.DocumentAdded:
                case WorkspaceChangeKind.AdditionalDocumentAdded:
                case WorkspaceChangeKind.AdditionalDocumentRemoved:
                case WorkspaceChangeKind.AdditionalDocumentReloaded:
                case WorkspaceChangeKind.AdditionalDocumentChanged:
                case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
                case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                    return;
                case WorkspaceChangeKind.DocumentRemoved:
                case WorkspaceChangeKind.DocumentChanged:
                    // Fast path when we know we affected a document that could have had code model elements in it.  No
                    // need to do a solution diff in this case.
                    _documentsToFireEventsFor.AddWork(e.DocumentId!);
                    return;
            }

            // Other type of event that could indicate a doc change/removal. Have to actually analyze the change to
            // determine what we should do here.

            var changes = e.OldSolution.GetChanges(e.NewSolution);

            foreach (var project in changes.GetRemovedProjects())
                _documentsToFireEventsFor.AddWork(project.DocumentIds);

            foreach (var projectChange in changes.GetProjectChanges())
            {
                _documentsToFireEventsFor.AddWork(projectChange.GetRemovedDocuments());
                _documentsToFireEventsFor.AddWork(projectChange.GetChangedDocuments());
            }
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
        {
            // Stop any outstanding BG work we've queued up.
            _cancellationTokenSource.Cancel();

            // Now wait for all our cleanup tasks to finish before we return.
            _threadingContext.JoinableTaskFactory.Run(() => _deferredCleanupTasks.JoinTillEmptyAsync());
        }
    }
}
