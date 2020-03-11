// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Services;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    internal class VisualStudioDesignerAttributeService
        : ForegroundThreadAffinitizedObject, IDesignerAttributeService, IDesignerAttributeServiceCallback
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IForegroundNotificationService _notificationService;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IAsynchronousOperationListener _listener;

        // cache the update service for cps projects. Computed on demand (slow), but then cached for
        // quick responses after that.
        private readonly ConcurrentDictionary<ProjectId, IProjectItemDesignerTypeUpdateService?> _cpsProjects
            = new ConcurrentDictionary<ProjectId, IProjectItemDesignerTypeUpdateService?>();

        /// <summary>
        /// Our connections to the remote OOP server. Created on demand when we startup and then
        /// kept around for the lifetime of this service.
        /// </summary>
        private RemoteHostClient? _client;
        private KeepAliveSession? _keepAliveSession;

        /// <summary>
        /// cache designer from UI thread
        /// 
        /// access this field through <see cref="GetDesignerServiceOnForegroundThread"/>
        /// </summary>
        private IVSMDDesignerService? _dotNotAccessDirectlyDesigner;

        // We'll get notifications from the OOP server about new attribute arguments. Batch those
        // notifications up and deliver them to VS every second.
        private readonly object _gate = new object();
        private readonly List<DesignerInfo> _updatedInfos = new List<DesignerInfo>();
        private Task _updateTask = Task.CompletedTask;
        private bool _taskInFlight = false;

        public VisualStudioDesignerAttributeService(
            IThreadingContext threadingContext,
            IServiceProvider serviceProvider,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider,
            VisualStudioWorkspaceImpl workspace)
            : base(threadingContext)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
            _notificationService = notificationService;
            _workspace = workspace;

            _listener = listenerProvider.GetListener(FeatureAttribute.DesignerAttribute);

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.ProjectRemoved)
                _cpsProjects.TryRemove(e.ProjectId, out _);
        }

        void IDesignerAttributeService.Start(CancellationToken cancellationToken)
            => _ = StartAsync(cancellationToken);

        private async Task StartAsync(CancellationToken cancellationToken)
        {
            // Have to catch all exceptions coming through here as this is called from a
            // fire-and-forget method and we want to make sure nothing leaks out.
            try
            {
                await StartWorkerAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is normal (during VS closing).  Just ignore.
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                // Otherwise report a watson for any other exception.  Don't bring down VS.  This is
                // a BG service we don't want impacting the user experience.
            }
        }

        private async Task StartWorkerAsync(CancellationToken cancellationToken)
        {
            _client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (_client == null)
                return;

            // Pass ourselves in as the callback target for the OOP service.  As it discovers
            // designer attributes it will call back into us to notify VS about it.
            _keepAliveSession = await _client.TryCreateKeepAliveSessionAsync(
                WellKnownServiceHubServices.RemoteDesignerAttributeService,
                callbackTarget: this, cancellationToken).ConfigureAwait(false);
            if (_keepAliveSession == null)
                return;

            var success = await _keepAliveSession.TryInvokeAsync(
                nameof(IRemoteDesignerAttributeService.ScanForDesignerAttributesAsync),
                solution: null,
                arguments: Array.Empty<object>(),
                cancellationToken).ConfigureAwait(false);
        }

        public Task RegisterDesignerAttributesAsync(
            IList<DesignerInfo> attributeInfos, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                // add our work to the set we'll process in the next batch.
                _updatedInfos.AddRange(attributeInfos);

                if (!_taskInFlight)
                {
                    // No in-flight task.  Kick one off to process these messages a second from now.
                    // We always attach the task to the previous one so that notifications to the ui
                    // follow the same order as the notification the OOP server sent to us.
                    _updateTask = _updateTask.ContinueWithAfterDelayFromAsync(
                        _ => NotifyProjectSystemAsync(cancellationToken),
                        cancellationToken,
                        1000/*ms*/,
                        TaskContinuationOptions.RunContinuationsAsynchronously,
                        TaskScheduler.Default);
                    _taskInFlight = true;
                }
            }

            return Task.CompletedTask;
        }

        private async Task NotifyProjectSystemAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _1 = ArrayBuilder<DesignerInfo>.GetInstance(out var attributeInfos);
            using var _2 = PooledHashSet<DocumentId>.GetInstance(out var seenDocumentIds);

            lock (_gate)
            {
                // walk the set of updates in reverse, and ignore documents if we see them a second
                // time.  This ensures that if we're batching up multiple notifications for the same
                // document, that we only bother processing the last one since it should beat out
                // all the prior ones.
                for (var i = _updatedInfos.Count - 1; i >= 0; i--)
                {
                    var designerArg = _updatedInfos[i];
                    if (seenDocumentIds.Add(designerArg.DocumentId))
                        attributeInfos.Add(designerArg);
                }

                // mark there being no existing update task so that the next OOP notification will
                // kick one off.
                _updatedInfos.Clear();
                _taskInFlight = false;
            }

            // Reconcile all the notifications against the latest workspace solution we have.  This
            // is technically racey as OOP may have computed against a different solution.  However,
            // we should normally always reach a fixed point as we continually receive updates from
            // OOP when teh solution changes that we should normally be able to map to this
            // solution.
            var currentSolution = _workspace.CurrentSolution;

            // Now, group all the notifications by project and update all the projects in parallel.
            using var _3 = ArrayBuilder<Task>.GetInstance(out var tasks);
            foreach (var group in attributeInfos.GroupBy(a => a.DocumentId.ProjectId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(NotifyProjectSystemAsync(currentSolution, group.Key, group, cancellationToken));
            }

            // Wait until all project updates have happened before processing the next batch.
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task NotifyProjectSystemAsync(
            Solution solution,
            ProjectId projectId,
            IEnumerable<DesignerInfo> attributeInfos,
            CancellationToken cancellationToken)
        {
            // Make sure this is a project that our current solution still knows about.
            var project = solution.GetProject(projectId);
            if (project == null)
                return;

            var cpsUpdateService = await GetUpdateServiceIfCpsProjectAsync(project, cancellationToken).ConfigureAwait(false);
            var task = cpsUpdateService != null
                ? NotifyCpsProjectSystemAsync(project, cpsUpdateService, attributeInfos, cancellationToken)
                : NotifyLegacyProjectSystemAsync(project, attributeInfos, cancellationToken);

            await task.ConfigureAwait(false);
        }

        private Task NotifyLegacyProjectSystemAsync(
            Project project,
            IEnumerable<DesignerInfo> attributeInfos,
            CancellationToken cancellationToken)
        {
            // Move over to the UI thread and attempt to notify it about all the documents from this
            // particular project at once. Use a task completion source here so we can kick over to
            // the UI thread, but still ensure that our caller knows when we complete.
            var completionSource = new TaskCompletionSource<bool>();

            _notificationService.RegisterNotification(() =>
            {
                AssertIsForeground();

                try
                {
                    NotifyLegacyProjectSystemOnUIThread(project, attributeInfos, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e) when (FatalError.ReportWithoutCrash(e))
                {
                }
                finally
                {
                    completionSource.SetResult(true);
                }
            }, _listener.BeginAsyncOperation("RegisterDesignerAttribute"));

            return completionSource.Task;
        }

        private void NotifyLegacyProjectSystemOnUIThread(
            Project project,
            IEnumerable<DesignerInfo> attributeInfos,
            CancellationToken cancellationToken)
        {
            AssertIsForeground();

            var designerService = GetDesignerServiceOnForegroundThread();
            if (designerService == null)
                return;

            var hierarchy = _workspace.GetHierarchy(project.Id);
            if (hierarchy == null)
                return;

            foreach (var info in attributeInfos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                NotifyLegacyProjectSystemOnUIThread(
                    project, designerService, hierarchy, info);
            }
        }

        private void NotifyLegacyProjectSystemOnUIThread(
            Project project,
            IVSMDDesignerService designerService,
            IVsHierarchy hierarchy,
            DesignerInfo info)
        {
            // Make sure this is a document we still know about when OOP notifies us.
            var document = project.GetDocument(info.DocumentId);
            if (document == null)
                return;

            if (document.FilePath == null)
                return;

            var itemId = hierarchy.TryGetItemId(document.FilePath);
            if (itemId == VSConstants.VSITEMID_NIL)
                return;

            // PERF: Avoid sending the message if the project system already has the current value.
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ItemSubType, out var currentValue)))
            {
                var currentStringValue = string.IsNullOrEmpty(currentValue as string) ? null : (string)currentValue;
                if (string.Equals(currentStringValue, info.Category, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            try
            {
                designerService.RegisterDesignViewAttribute(
                    hierarchy, (int)itemId, dwClass: 0,
                    pwszAttributeValue: info.Category);
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

        private IVSMDDesignerService GetDesignerServiceOnForegroundThread()
        {
            AssertIsForeground();
            return _dotNotAccessDirectlyDesigner ??= (IVSMDDesignerService)_serviceProvider.GetService(typeof(SVSMDDesignerService));
        }

        private async Task NotifyCpsProjectSystemAsync(
            Project project,
            IProjectItemDesignerTypeUpdateService updateService,
            IEnumerable<DesignerInfo> attributeInfos,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Broadcast all the information about all the documents in parallel to CPS.

            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            foreach (var info in attributeInfos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(NotifyCpsProjectSystemAsync(project, updateService, info, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task NotifyCpsProjectSystemAsync(
            Project project,
            IProjectItemDesignerTypeUpdateService updateService,
            DesignerInfo info,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Make sure we still know about this document that the OOP service is notifying us about.
            var document = project.GetDocument(info.DocumentId);
            if (document == null)
                return;

            try
            {
                await updateService.SetProjectItemDesignerTypeAsync(document.FilePath, info.Category).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // we might call update service after project is already removed and get object disposed exception.
                // we will catch the exception and ignore. 
                // see this PR for more detail - https://github.com/dotnet/roslyn/pull/35383
            }
        }

        private async Task<IProjectItemDesignerTypeUpdateService?> GetUpdateServiceIfCpsProjectAsync(
            Project project, CancellationToken cancellationToken)
        {
            var projectId = project.Id;
            if (_cpsProjects.TryGetValue(projectId, out var value))
                return value;

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            this.AssertIsForeground();

            var updateService = ComputeUpdateService();
            _cpsProjects.TryAdd(projectId, updateService);
            return updateService;

            IProjectItemDesignerTypeUpdateService? ComputeUpdateService()
            {
                if (!_workspace.IsCPSProject(project))
                    return null;

                var vsProject = (IVsProject?)_workspace.GetHierarchy(project.Id);
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
