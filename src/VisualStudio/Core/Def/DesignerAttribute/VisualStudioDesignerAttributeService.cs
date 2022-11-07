// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Services;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal class VisualStudioDesignerAttributeService :
        ForegroundThreadAffinitizedObject, IEventListener<object>, IDisposable
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        /// <summary>
        /// Used to acquire the legacy project designer service.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Cache from project to the CPS designer service for it.  Computed on demand (which
        /// requires using the UI thread), but then cached for all subsequent notifications about
        /// that project.
        /// </summary>
        private readonly ConcurrentDictionary<ProjectId, IProjectItemDesignerTypeUpdateService?> _cpsProjects = new();

        /// <summary>
        /// Cached designer service for notifying legacy projects about designer attributes.
        /// </summary>
        private IVSMDDesignerService? _legacyDesignerService;

        /// <summary>
        /// Queue that tells us to recompute designer attributes when workspace changes happen.  This queue is
        /// cancellable and will be restarted when new changes come in.  That way we can be quickly update the designer
        /// attribute for a file when a user edits it.
        /// </summary>
        private readonly AsyncBatchingWorkQueue _workQueue;

        /// <summary>
        /// We'll get notifications from the OOP server about new attribute arguments. Collect those notifications and
        /// deliver them to VS in batches to prevent flooding the UI thread.  Importantly, we do not cancel this queue.
        /// Once we've decided to update the project system, we want to allow that to proceed.
        /// <para>
        /// This queue both sends the individual data objects we get back, or the solution instance once we're done with
        /// a particular project request.  The latter is used so that we can determine which documents are now gone, so
        /// we can dump our cached data for them.
        /// </para>
        /// </summary>
        private readonly AsyncBatchingWorkQueue<(CodeAnalysis.Solution? solution, DesignerAttributeData? data)> _projectSystemNotificationQueue;

        /// <summary>
        /// Keep track of the last version we were at when we processed a project.  We'll skip reprocessing projects if
        /// that version hasn't changed.
        /// </summary>
        private readonly ConcurrentDictionary<ProjectId, VersionStamp> _projectToLastComputedDependentSemanticVersion = new();

        /// <summary>
        /// Keep track of the last information we reported per document.  We will avoid notifying the host if we
        /// recompute and these don't change.  Note: we keep track if we reported <see langword="null"/> as well to
        /// represent that the file is not designable.
        /// </summary>
        private readonly ConcurrentDictionary<DocumentId, string?> _documentToLastReportedCategory = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDesignerAttributeService(
            VisualStudioWorkspaceImpl workspace,
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            Shell.SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _workspace = workspace;
            _serviceProvider = serviceProvider;

            var listener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.DesignerAttributes);

            _workQueue = new AsyncBatchingWorkQueue(
                TimeSpan.FromSeconds(1),
                this.ProcessWorkspaceChangeAsync,
                listener,
                ThreadingContext.DisposalToken);

            _projectSystemNotificationQueue = new AsyncBatchingWorkQueue<(CodeAnalysis.Solution? solution, DesignerAttributeData? data)>(
                TimeSpan.FromSeconds(1),
                this.NotifyProjectSystemAsync,
                listener,
                ThreadingContext.DisposalToken);
        }

        public void Dispose()
        {
            _workspace.WorkspaceChanged -= OnWorkspaceChanged;
        }

        void IEventListener<object>.StartListening(Workspace workspace, object _)
        {
            if (workspace != _workspace)
                return;

            // Register for changes, and kick off hte initial scan.
            _workspace.WorkspaceChanged += OnWorkspaceChanged;
            _workQueue.AddWork(cancelExistingWork: true);
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            // cancel any existing work and start rescanning.  this way we can respond to edits to a file very quickly.
            _workQueue.AddWork(cancelExistingWork: true);
        }

        private async ValueTask ProcessWorkspaceChangeAsync(CancellationToken cancellationToken)
        {
            var statusService = _workspace.Services.GetRequiredService<IWorkspaceStatusService>();
            await statusService.WaitUntilFullyLoadedAsync(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return;

            var solution = _workspace.CurrentSolution;

            // remove any data for projects that are no longer around.
            HandRemovedProjects(solution);

            // Now process all the projects we do have, prioritizing the active project/document first.
            var trackingService = _workspace.Services.GetRequiredService<IDocumentTrackingService>();

            var priorityDocument = solution.GetDocument(trackingService.TryGetActiveDocument());
            if (priorityDocument != null)
                await ProcessProjectAsync(client, priorityDocument.Project, priorityDocument.Id, cancellationToken).ConfigureAwait(false);

            // Process the rest of the projects in dependency order so that their data is ready when we hit the 
            // projects that depend on them.
            var dependencyGraph = solution.GetProjectDependencyGraph();
            foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects(cancellationToken))
            {
                // skip the prioritized project we handled above.
                if (projectId == priorityDocument?.Id.ProjectId)
                    continue;

                await ProcessProjectAsync(client, solution.GetRequiredProject(projectId), priorityDocumentId: null, cancellationToken).ConfigureAwait(false);
            }
        }

        private void HandRemovedProjects(CodeAnalysis.Solution solution)
        {
            foreach (var (projectId, _) in _cpsProjects)
            {
                if (!solution.ContainsProject(projectId))
                    _cpsProjects.TryRemove(projectId, out _);
            }

            // when a project is removed, remove our cached attribute data for it.  No point in actually notifying the
            // host as there isn't any project anymore to notify it about.

            foreach (var (projectId, _) in _projectToLastComputedDependentSemanticVersion)
            {
                if (!solution.ContainsProject(projectId))
                    _projectToLastComputedDependentSemanticVersion.TryRemove(projectId, out _);
            }
        }

        private async Task ProcessProjectAsync(
            RemoteHostClient client,
            CodeAnalysis.Project project,
            DocumentId? priorityDocumentId,
            CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
                return;

            // We need to recompute the designer attributes for a project if it's own semantic-version changes, or the
            // semantic-version of any dependent projects change.  The reason for checking dependent projects is that we
            // look for the designer attribute on subclasses as well (so we have to walk the inheritance tree).  This
            // tree may be unfortunately be affected by dependent projects.  In an ideal design we would require the
            // attribute be on the declaration point so we could only check things that are known to directly have an
            // attribute on them, and we wouldn't have to look up the inheritance hierarchy.
            var dependentSemanticVersion = await project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            if (!_projectToLastComputedDependentSemanticVersion.TryGetValue(project.Id, out var lastComputedVersion) ||
                lastComputedVersion != dependentSemanticVersion)
            {
                var stream = client.TryInvokeStreamAsync<IRemoteDesignerAttributeDiscoveryService, DesignerAttributeData>(
                    project,
                    (service, checksum, cancellationToken) => service.DiscoverDesignerAttributesAsync(checksum, project.Id, priorityDocumentId, cancellationToken),
                    cancellationToken);

                using var _ = PooledHashSet<DocumentId>.GetInstance(out var seenDocuments);

                // get the results and add all the documents we hear about to the notification queue so they can be batched up.
                await foreach (var data in stream.ConfigureAwait(false))
                {
                    seenDocuments.Add(data.DocumentId);
                    _projectSystemNotificationQueue.AddWork((solution: null, data));
                }

                // Also, for any documents we didn't hear about, ensure we emit a clear on its category if we have
                // currently stored for it.  This also ensures we initially report about all non-designer files when a
                // solution is opened.  This is needed in case the project file says it is designable, but the user made
                // some change outside of VS that makes it non-designable. We will pick this up here and ensure the data
                // is cleared.  From that point on, we won't issue any more notifications about those files unless the
                // category actually does change.
                foreach (var document in project.Documents)
                {
                    if (document.FilePath != null && !seenDocuments.Contains(document.Id))
                        _projectSystemNotificationQueue.AddWork((solution: null, new DesignerAttributeData(null, document.Id, document.FilePath)));
                }

                // once done, also enqueue the solution as well so that the project-system queue can cleanup
                // any stale data about it.
                _projectSystemNotificationQueue.AddWork((project.Solution, data: null));

                // now that we're done processing the project, record this version-stamp so we don't have to process it again in the future.
                _projectToLastComputedDependentSemanticVersion[project.Id] = dependentSemanticVersion;
            }
        }

        private async ValueTask NotifyProjectSystemAsync(
            ImmutableSegmentedList<(CodeAnalysis.Solution? solution, DesignerAttributeData? data)> dataList, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _1 = ArrayBuilder<DesignerAttributeData>.GetInstance(out var changedData);
            using var _2 = ArrayBuilder<Task>.GetInstance(out var tasks);

            var latestSolution = AddChangedData(dataList, changedData);

            // Now, group all the notifications by project and update all the projects in parallel.
            foreach (var group in changedData.GroupBy(a => a.DocumentId.ProjectId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(NotifyProjectSystemAsync(group.Key, group, cancellationToken));
            }

            // Wait until all project updates have happened before processing the next batch.
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // now that we've reported this data, record that we've done so so that we don't report the same data again in the future.
            foreach (var data in changedData)
                _documentToLastReportedCategory[data.DocumentId] = data.Category;

            // Now, check the documents we've stored against the changed projects to see if they're no longer around. If
            // so, dump what we have.  No need to notify anyone about this as the files are literally not in the
            // solution anymore.  this is just to ensure we don't hold onto data forever.
            if (latestSolution != null)
            {
                foreach (var (documentId, _) in _documentToLastReportedCategory)
                {
                    if (!latestSolution.ContainsDocument(documentId))
                        _documentToLastReportedCategory.TryRemove(documentId, out _);
                }
            }
        }

        private CodeAnalysis.Solution? AddChangedData(
            ImmutableSegmentedList<(CodeAnalysis.Solution? solution, DesignerAttributeData? data)> dataList,
            ArrayBuilder<DesignerAttributeData> changedData)
        {
            using var _1 = PooledHashSet<DocumentId>.GetInstance(out var seenDocumentIds);
            using var _2 = ArrayBuilder<DesignerAttributeData>.GetInstance(out var latestData);

            CodeAnalysis.Solution? lastSolution = null;

            for (var i = dataList.Count - 1; i >= 0; i--)
            {
                // go in reverse order so that results about the same document only take the later value.
                var (solution, data) = dataList[i];

                if (data != null)
                {
                    if (seenDocumentIds.Add(data.Value.DocumentId))
                        latestData.Add(data.Value);
                }

                lastSolution ??= solution;
            }

            foreach (var data in latestData)
            {
                // only issue a change notification for files we haven't issued a notification for, or for files that
                // changed their category.
                if (!_documentToLastReportedCategory.TryGetValue(data.DocumentId, out var existingCategory) ||
                    existingCategory != data.Category)
                {
                    changedData.Add(data);
                }
            }

            return lastSolution;
        }

        private async Task NotifyProjectSystemAsync(
            ProjectId projectId,
            IEnumerable<DesignerAttributeData> dataList,
            CancellationToken cancellationToken)
        {
            // Delegate to the CPS or legacy notification services as necessary.
            var cpsUpdateService = await GetUpdateServiceIfCpsProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
            var task = cpsUpdateService == null
                ? NotifyLegacyProjectSystemAsync(projectId, dataList, cancellationToken)
                : NotifyCpsProjectSystemAsync(projectId, cpsUpdateService, dataList, cancellationToken);

            await task.ConfigureAwait(false);
        }

        private async Task NotifyLegacyProjectSystemAsync(
            ProjectId projectId,
            IEnumerable<DesignerAttributeData> dataList,
            CancellationToken cancellationToken)
        {
            // legacy project system can only be talked to on the UI thread.
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);

            AssertIsForeground();

            var designerService = _legacyDesignerService ??= (IVSMDDesignerService)_serviceProvider.GetService(typeof(SVSMDDesignerService));
            if (designerService == null)
                return;

            var hierarchy = _workspace.GetHierarchy(projectId);
            if (hierarchy == null)
                return;

            foreach (var data in dataList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                NotifyLegacyProjectSystemOnUIThread(designerService, hierarchy, data);
            }
        }

        private void NotifyLegacyProjectSystemOnUIThread(
            IVSMDDesignerService designerService,
            IVsHierarchy hierarchy,
            DesignerAttributeData data)
        {
            this.AssertIsForeground();

            var itemId = hierarchy.TryGetItemId(data.FilePath);
            if (itemId == VSConstants.VSITEMID_NIL)
                return;

            // PERF: Avoid sending the message if the project system already has the current value.
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ItemSubType, out var currentValue)))
            {
                var currentStringValue = string.IsNullOrEmpty(currentValue as string) ? null : (string)currentValue;
                if (string.Equals(currentStringValue, data.Category, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            try
            {
                designerService.RegisterDesignViewAttribute(
                    hierarchy, (int)itemId, dwClass: 0,
                    pwszAttributeValue: data.Category);
            }
            catch
            {
                // DevDiv # 933717
                // turns out RegisterDesignViewAttribute can throw in certain cases such as a file failed to be checked out by source control
                // or IVSHierarchy failed to set a property for this project
                //
                // just swallow it. don't crash VS.
            }
        }

        private async Task NotifyCpsProjectSystemAsync(
            ProjectId projectId,
            IProjectItemDesignerTypeUpdateService updateService,
            IEnumerable<DesignerAttributeData> dataList,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // We may have updates for many different configurations of the same logical project system project.
            // However, the project system only associates designer attributes with one of those projects.  So just drop
            // the notifications for any sibling configurations.
            if (!_workspace.IsPrimaryProject(projectId))
                return;

            // Broadcast all the information about all the documents in parallel to CPS.

            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            foreach (var data in dataList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(NotifyCpsProjectSystemAsync(updateService, data, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static async Task NotifyCpsProjectSystemAsync(
            IProjectItemDesignerTypeUpdateService updateService,
            DesignerAttributeData data,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await updateService.SetProjectItemDesignerTypeAsync(data.FilePath, data.Category).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // we might call update service after project is already removed and get object disposed exception.
                // we will catch the exception and ignore. 
                // see this PR for more detail - https://github.com/dotnet/roslyn/pull/35383
            }
        }

        private async Task<IProjectItemDesignerTypeUpdateService?> GetUpdateServiceIfCpsProjectAsync(
            ProjectId projectId, CancellationToken cancellationToken)
        {
            if (!_cpsProjects.TryGetValue(projectId, out var updateService))
            {
                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
                this.AssertIsForeground();

                updateService = ComputeUpdateService();
                _cpsProjects.TryAdd(projectId, updateService);
            }

            return updateService;

            IProjectItemDesignerTypeUpdateService? ComputeUpdateService()
            {
                if (!_workspace.IsCPSProject(projectId))
                    return null;

                var vsProject = (IVsProject?)_workspace.GetHierarchy(projectId);
                if (vsProject == null)
                    return null;

                if (ErrorHandler.Failed(vsProject.GetItemContext((uint)VSConstants.VSITEMID.Root, out var projectServiceProvider)))
                    return null;

                var serviceProvider = new Shell.ServiceProvider(projectServiceProvider);
                return serviceProvider.GetService(typeof(IProjectItemDesignerTypeUpdateService)) as IProjectItemDesignerTypeUpdateService;
            }
        }
    }
}
