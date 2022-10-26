// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Services;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal class VisualStudioDesignerAttributeService
        : ForegroundThreadAffinitizedObject, IDesignerAttributeListener, IEventListener<object>, IDisposable
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        /// <summary>
        /// Used to acquire the legacy project designer service.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Our connection to the remote OOP server. Created on demand when we startup and then
        /// kept around for the lifetime of this service.
        /// </summary>
        private RemoteServiceConnection<IRemoteDesignerAttributeDiscoveryService>? _lazyConnection;

        /// <summary>
        /// Cache from project to the CPS designer service for it.  Computed on demand (which
        /// requires using the UI thread), but then cached for all subsequent notifications about
        /// that project.
        /// </summary>
        private readonly ConcurrentDictionary<ProjectId, IProjectItemDesignerTypeUpdateService?> _cpsProjects
            = new();

        /// <summary>
        /// Cached designer service for notifying legacy projects about designer attributes.
        /// </summary>
        private IVSMDDesignerService? _legacyDesignerService;

        // We'll get notifications from the OOP server about new attribute arguments. Batch those
        // notifications up and deliver them to VS every second.
        private readonly AsyncBatchingWorkQueue<DesignerAttributeData>? _workQueue;

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

            _workQueue = new AsyncBatchingWorkQueue<DesignerAttributeData>(
                TimeSpan.FromSeconds(1),
                this.NotifyProjectSystemAsync,
                asynchronousOperationListenerProvider.GetListener(FeatureAttribute.DesignerAttributes),
                ThreadingContext.DisposalToken);
        }

        public void Dispose()
        {
            _lazyConnection?.Dispose();
        }

        void IEventListener<object>.StartListening(Workspace workspace, object _)
        {
            if (workspace is VisualStudioWorkspace)
                _ = StartAsync();
        }

        private async Task StartAsync()
        {
            // Have to catch all exceptions coming through here as this is called from a
            // fire-and-forget method and we want to make sure nothing leaks out.
            try
            {
                await StartWorkerAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is normal (during VS closing).  Just ignore.
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e, ErrorSeverity.Diagnostic))
            {
                // Otherwise report a watson for any other exception.  Don't bring down VS.  This is
                // a BG service we don't want impacting the user experience.
            }
        }

        private async Task StartWorkerAsync()
        {
            var cancellationToken = ThreadingContext.DisposalToken;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                StartScanningForDesignerAttributesInCurrentProcess(cancellationToken);
                return;
            }

            // Pass ourselves in as the callback target for the OOP service.  As it discovers
            // designer attributes it will call back into us to notify VS about it.
            _lazyConnection = client.CreateConnection<IRemoteDesignerAttributeDiscoveryService>(callbackTarget: this);

            // Now kick off scanning in the OOP process.
            // If the call fails an error has already been reported and there is nothing more to do.
            _ = await _lazyConnection.TryInvokeAsync(
                (service, callbackId, cancellationToken) => service.StartScanningForDesignerAttributesAsync(callbackId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        public void StartScanningForDesignerAttributesInCurrentProcess(CancellationToken cancellation)
        {
            var registrationService = _workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            var analyzerProvider = new InProcDesignerAttributeIncrementalAnalyzerProvider(this);

            registrationService.AddAnalyzerProvider(
                analyzerProvider,
                new IncrementalAnalyzerProviderMetadata(
                    nameof(InProcDesignerAttributeIncrementalAnalyzerProvider),
                    highPriorityForActiveFile: false,
                    workspaceKinds: WorkspaceKind.Host));
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

        /// <summary>
        /// Callback from the OOP service back into us.
        /// </summary>
        public ValueTask ReportDesignerAttributeDataAsync(ImmutableArray<DesignerAttributeData> data, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_workQueue);
            _workQueue.AddWork(data);
            return ValueTaskFactory.CompletedTask;
        }

        public ValueTask OnProjectRemovedAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            _cpsProjects.TryRemove(projectId, out _);
            return ValueTaskFactory.CompletedTask;
        }
    }
}
