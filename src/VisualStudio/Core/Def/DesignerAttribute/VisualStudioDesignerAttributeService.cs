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

        private readonly AsyncBatchingWorkQueue _workQueue;

        // We'll get notifications from the OOP server about new attribute arguments. Collect those notifications and
        // deliver them to VS in batches to prevent flooding the UI thread.
        private readonly AsyncBatchingWorkQueue<DesignerAttributeData> _projectSystemNotificationQueue;

        /// <summary>
        /// Keep track of the last information we reported.  We will avoid notifying the host if we recompute and these
        /// don't change.
        /// </summary>
        private readonly ConcurrentDictionary<DocumentId, (string? category, VersionStamp projectVersion)> _documentToLastReportedInformation = new();

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

            _projectSystemNotificationQueue = new AsyncBatchingWorkQueue<DesignerAttributeData>(
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

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
            _workQueue.AddWork(cancelExistingWork: true);
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            _workQueue.AddWork(cancelExistingWork: true);
        }

        private async ValueTask ProcessWorkspaceChangeAsync(CancellationToken cancellationToken)
        {
            var statusService = _workspace.Services.GetRequiredService<IWorkspaceStatusService>();
            await statusService.WaitUntilFullyLoadedAsync(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var solution = _workspace.CurrentSolution;

            // remove any data for projects that are no longer around.
            foreach (var (projectId, _) in _cpsProjects)
            {
                if (!solution.ContainsProject(projectId))
                    _cpsProjects.TryRemove(projectId, out _);
            }

            foreach (var documentId in _documentToLastReportedInformation.Keys)
            {
                if (!solution.ContainsProject(documentId.ProjectId))
                    _documentToLastReportedInformation.TryRemove(documentId, out _);
            }

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return;

            // Handle the priority doc first.
            var trackingService = _workspace.Services.GetRequiredService<IDocumentTrackingService>();
            var priorityDocumentId = trackingService.TryGetActiveDocument();
            var priorityDocument = solution.GetDocument(priorityDocumentId);

            if (priorityDocument != null)
                await ProcessProjectAsync(client, priorityDocument.Project, priorityDocumentId, cancellationToken).ConfigureAwait(false);

            // Process the rest of the projects in dependency order so that their data is ready when we hit the 
            // projects that depend on them.
            var dependencyGraph = solution.GetProjectDependencyGraph();
            foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects(cancellationToken))
            {
                if (projectId == priorityDocumentId?.ProjectId)
                    continue;

                await ProcessProjectAsync(client, solution.GetRequiredProject(projectId), priorityDocumentId: null, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task ProcessProjectAsync(
            RemoteHostClient client,
            CodeAnalysis.Project project,
            DocumentId? priorityDocumentId,
            CancellationToken cancellationToken)
        {


            var stream = client.TryInvokeStreamAsync<IRemoteDesignerAttributeDiscoveryService, DesignerAttributeData>(
                project,
                (service, checksum, cancellationToken) => service.DiscoverDesignerAttributesAsync(checksum, project.Id, priorityDocumentId, cancellationToken),
                cancellationToken);

            await foreach (var data in stream.ConfigureAwait(false))
                _projectSystemNotificationQueue.AddWork(data);
            var stream = client.TryInvokeStreamAsync<IRemoteDesignerAttributeDiscoveryService, DesignerAttributeData>(
        solution,
        (service, checksum, cancellationToken) => service.DiscoverDesignerAttributesAsync(checksum, priorityDocument, cancellationToken),
        cancellationToken);

            await foreach (var data in stream.ConfigureAwait(false))
                _projectSystemNotificationQueue.AddWork(data);
        }

        private async ValueTask NotifyProjectSystemAsync(
            ImmutableSegmentedList<DesignerAttributeData> data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _1 = ArrayBuilder<DesignerAttributeData>.GetInstance(out var filteredInfos);
            AddFilteredInfos(data, filteredInfos);

            // Now, group all the notifications by project and update all the projects in parallel.
            using var _2 = ArrayBuilder<Task>.GetInstance(out var tasks);
            foreach (var group in filteredInfos.GroupBy(a => a.DocumentId.ProjectId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(NotifyProjectSystemAsync(group.Key, group, cancellationToken));
            }

            // Wait until all project updates have happened before processing the next batch.
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static void AddFilteredInfos(ImmutableSegmentedList<DesignerAttributeData> data, ArrayBuilder<DesignerAttributeData> filteredData)
        {
            using var _ = PooledHashSet<DocumentId>.GetInstance(out var seenDocumentIds);

            // Walk the list of designer items in reverse, and skip any items for a project once
            // we've already seen it once.  That way, we're only reporting the most up to date
            // information for a project, and we're skipping the stale information.
            for (var i = data.Count - 1; i >= 0; i--)
            {
                var info = data[i];
                if (seenDocumentIds.Add(info.DocumentId))
                    filteredData.Add(info);
            }
        }

        private async Task NotifyProjectSystemAsync(
            ProjectId projectId,
            IEnumerable<DesignerAttributeData> data,
            CancellationToken cancellationToken)
        {
            // Delegate to the CPS or legacy notification services as necessary.
            var cpsUpdateService = await GetUpdateServiceIfCpsProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
            var task = cpsUpdateService == null
                ? NotifyLegacyProjectSystemAsync(projectId, data, cancellationToken)
                : NotifyCpsProjectSystemAsync(projectId, cpsUpdateService, data, cancellationToken);

            await task.ConfigureAwait(false);
        }

        private async Task NotifyLegacyProjectSystemAsync(
            ProjectId projectId,
            IEnumerable<DesignerAttributeData> data,
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

            foreach (var info in data)
            {
                cancellationToken.ThrowIfCancellationRequested();
                NotifyLegacyProjectSystemOnUIThread(designerService, hierarchy, info);
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
            IEnumerable<DesignerAttributeData> data,
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

            foreach (var info in data)
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(NotifyCpsProjectSystemAsync(updateService, info, cancellationToken));
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
