// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Services;
using Roslyn.Utilities;
using Shell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    [ExportWorkspaceServiceFactory(typeof(INewDesignerAttributeService), ServiceLayer.Host), Shared]
    internal class VisualStudioNewDesignerAttributeServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IForegroundNotificationService _notificationService;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioNewDesignerAttributeServiceFactory(
            IThreadingContext threadingContext,
            Shell.SVsServiceProvider serviceProvider,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
            _notificationService = notificationService;
            _listenerProvider = listenerProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (!(workspaceServices.Workspace is VisualStudioWorkspaceImpl workspace))
                return null;

            return new VisualStudioNewDesignerAttributeService(
                _threadingContext, _serviceProvider, _notificationService,
                _listenerProvider, workspace);
        }
    }

    internal class VisualStudioNewDesignerAttributeService
        : ForegroundThreadAffinitizedObject, INewDesignerAttributeService, INewDesignerAttributeServiceCallback
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IForegroundNotificationService _notificationService;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IAsynchronousOperationListener _listener;

        // cache whether a project is cps project or not. Computed on demand (slow), but then cached
        // for quick responses after that.
        private readonly ConcurrentDictionary<ProjectId, IProjectItemDesignerTypeUpdateService> _cpsProjects
            = new ConcurrentDictionary<ProjectId, IProjectItemDesignerTypeUpdateService>();

        /// <summary>
        /// cache designer from UI thread
        /// 
        /// access this field through <see cref="GetDesignerServiceOnForegroundThread"/>
        /// </summary>
        private IVSMDDesignerService _dotNotAccessDirectlyDesigner;

        // We'll get notifications from the OOP server about new attribute arguments. Batch those
        // notifications up and deliver them to VS every second.
        private readonly object _gate = new object();
        private readonly List<RemoteDesignerAttributeArgument> _updateArguments = new List<RemoteDesignerAttributeArgument>();
        private Task _updateTask = Task.CompletedTask;
        private bool _taskInFlight = false;

        public VisualStudioNewDesignerAttributeService(
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
            {
                _cpsProjects.TryRemove(e.ProjectId, out _);
            }
        }

        void INewDesignerAttributeService.Start(CancellationToken cancellationToken)
            => _ = StartAsync(cancellationToken);

        public async Task StartAsync(CancellationToken cancellationToken)
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
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return;

            // Pass ourselves in as the callback target for the OOP service.  As it discovers
            // designer attributes it will call back into us to notify VS about it.

            var success = await client.TryRunRemoteAsync(
                WellKnownServiceHubServices.CodeAnalysisService,
                nameof(IRemoteNewDesignerAttributeService.ScanForDesignerAttributesAsync),
                solution: null,
                arguments: Array.Empty<object>(),
                callbackTarget: this,
                cancellationToken).ConfigureAwait(false);
        }

        public Task RegisterDesignerAttributesAsync(
            IList<RemoteDesignerAttributeArgument> arguments, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                // add our work to the set we'll process in the next batch.
                _updateArguments.AddRange(arguments);

                if (!_taskInFlight)
                {
                    // No in-flight task.  Kick one off to process these messages a couple of
                    // seconds from now.  We always attach the task to the previous one so that
                    // notifications to the ui follow the same order as the notification the OOP
                    // server sent to us.
                    _updateTask = _updateTask.ContinueWithAfterDelayFromAsync(
                        _ => NotifyProjectSystemAsync(cancellationToken),
                        cancellationToken,
                        2000,
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

            using var _1 = ArrayBuilder<RemoteDesignerAttributeArgument>.GetInstance(out var attributeArguments);
            lock (_gate)
            {
                // grab the entire set of notifications so far so we can process them.
                attributeArguments.AddRange(_updateArguments);
                _updateArguments.Clear();

                // mark there being no existing update task so that the next OOP notification will
                // kick one off.
                _taskInFlight = false;
            }

            var currentSolution = _workspace.CurrentSolution;

            using var _2 = ArrayBuilder<Task>.GetInstance(out var tasks);
            foreach (var group in _updateArguments.GroupBy(a => a.DocumentId.ProjectId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(NotifyProjectSystemAsync(currentSolution, group.Key, group, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task NotifyProjectSystemAsync(
            Solution solution,
            ProjectId projectId,
            IEnumerable<RemoteDesignerAttributeArgument> arguments,
            CancellationToken cancellationToken)
        {
            var project = solution.GetProject(projectId);
            if (project == null)
                return;

            var cpsUpdateService = await GetUpdateServiceIfCpsProjectAsync(project, cancellationToken).ConfigureAwait(false);
            var task = cpsUpdateService != null
                ? NotifyCpsProjectSystemAsync(project, cpsUpdateService, arguments, cancellationToken)
                : NotifyLegacyProjectSystemAsync(project, arguments, cancellationToken);

            await task.ConfigureAwait(false);
        }

        private Task NotifyLegacyProjectSystemAsync(
            Project project,
            IEnumerable<RemoteDesignerAttributeArgument> arguments,
            CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<bool>();

            _notificationService.RegisterNotification(() =>
            {
                AssertIsForeground();

                try
                {
                    NotifyLegacyProjectSystemOnUIThread(project, arguments, cancellationToken);
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
            IEnumerable<RemoteDesignerAttributeArgument> arguments,
            CancellationToken cancellationToken)
        {
            AssertIsForeground();

            var designerService = GetDesignerServiceOnForegroundThread();
            if (designerService == null)
                return;

            var hierarchy = _workspace.GetHierarchy(project.Id);
            if (hierarchy == null)
                return;

            foreach (var designerArg in arguments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                NotifyLegacyProjectSystemOnUIThread(
                    project, designerService, hierarchy, designerArg);
            }
        }

        private void NotifyLegacyProjectSystemOnUIThread(
            Project project,
            IVSMDDesignerService designerService,
            IVsHierarchy hierarchy,
            RemoteDesignerAttributeArgument designerArg)
        {
            // Make sure this is a document we still know about when OOP notifies us.

            var document = project.GetDocument(designerArg.DocumentId);
            if (document == null)
                return;

            var itemId = hierarchy.TryGetItemId(document.FilePath);
            if (itemId == VSConstants.VSITEMID_NIL)
                return;

            // PERF: Avoid sending the message if the project system already has the current value.
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ItemSubType, out var currentValue)))
            {
                var currentStringValue = string.IsNullOrEmpty(currentValue as string) ? null : (string)currentValue;
                if (string.Equals(currentStringValue, designerArg.Argument, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            try
            {
                designerService.RegisterDesignViewAttribute(
                    hierarchy, (int)itemId, dwClass: 0,
                    pwszAttributeValue: designerArg.Argument);
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

            if (_dotNotAccessDirectlyDesigner != null)
                return _dotNotAccessDirectlyDesigner;

            _dotNotAccessDirectlyDesigner = _serviceProvider.GetService(typeof(SVSMDDesignerService)) as IVSMDDesignerService;

            return _dotNotAccessDirectlyDesigner;
        }

        private async Task NotifyCpsProjectSystemAsync(
            Project project,
            IProjectItemDesignerTypeUpdateService updateService,
            IEnumerable<RemoteDesignerAttributeArgument> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Broadcast all the information about all the documents in parallel to CPS.

            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            foreach (var arg in arguments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(NotifyCpsProjectSystemAsync(project, updateService, arg, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task NotifyCpsProjectSystemAsync(
            Project project,
            IProjectItemDesignerTypeUpdateService updateService,
            RemoteDesignerAttributeArgument argument,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Make sure we still know about this document that the OOP service is notifying us about.
            var document = project.GetDocument(argument.DocumentId);
            if (document == null)
                return;

            try
            {
                await updateService.SetProjectItemDesignerTypeAsync(document.FilePath, argument.Argument).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // we might call update service after project is already removed and get object disposed exception.
                // we will catch the exception and ignore. 
                // see this PR for more detail - https://github.com/dotnet/roslyn/pull/35383
            }
        }

        private async Task<IProjectItemDesignerTypeUpdateService> GetUpdateServiceIfCpsProjectAsync(
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

            IProjectItemDesignerTypeUpdateService ComputeUpdateService()
            {
                if (!_workspace.IsCPSProject(project))
                    return null;

                var vsProject = (IVsProject)_workspace.GetHierarchy(project.Id);
                if (ErrorHandler.Failed(vsProject.GetItemContext((uint)VSConstants.VSITEMID.Root, out var projectServiceProvider)))
                    return null;

                var serviceProvider = new Shell.ServiceProvider(projectServiceProvider);
                return serviceProvider.GetService(typeof(IProjectItemDesignerTypeUpdateService)) as IProjectItemDesignerTypeUpdateService;
            }
        }
    }
}
