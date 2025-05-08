// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncServiceProvider2 = Microsoft.VisualStudio.Shell.IAsyncServiceProvider2;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

[ExportWorkspaceServiceFactory(typeof(IWorkspaceStatusService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioWorkspaceStatusServiceFactory(
    SVsServiceProvider serviceProvider,
    IThreadingContext threadingContext,
    IAsynchronousOperationListenerProvider listenerProvider) : IWorkspaceServiceFactory
{
    private readonly IAsyncServiceProvider2 _serviceProvider = (IAsyncServiceProvider2)serviceProvider;
    private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.Workspace);

    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        return workspaceServices.Workspace is VisualStudioWorkspace
            ? new Service(_serviceProvider, threadingContext, _listener)
            : new DefaultWorkspaceStatusService();
    }

    /// <summary>
    /// for prototype, we won't care about what solution is actually fully loaded. 
    /// we will just see whatever solution VS has at this point of time has actually fully loaded
    /// </summary>
    private sealed class Service : IWorkspaceStatusService
    {
        private readonly IAsyncServiceProvider2 _serviceProvider;
        private readonly IThreadingContext _threadingContext;

        /// <summary>
        /// A task indicating that the <see cref="Guids.GlobalHubClientPackageGuid"/> package has loaded. Calls to
        /// <see cref="IServiceBroker.GetProxyAsync"/> may have a main thread dependency if the proffering package
        /// is not loaded prior to the call.
        /// </summary>
        private readonly JoinableTask _loadHubClientPackage;

        /// <summary>
        /// A task providing the result of asynchronous computation of
        /// <see cref="IVsOperationProgressStatusService.GetStageStatusForSolutionLoad"/>. The result of this
        /// operation is accessed through <see cref="GetProgressStageStatusAsync"/>.
        /// </summary>
        private readonly JoinableTask<IVsOperationProgressStageStatusForSolutionLoad?> _progressStageStatus;

        public event EventHandler? StatusChanged;

        public Service(IAsyncServiceProvider2 serviceProvider, IThreadingContext threadingContext, IAsynchronousOperationListener listener)
        {
            _serviceProvider = serviceProvider;
            _threadingContext = threadingContext;

            _loadHubClientPackage = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                // Use the disposal token, since the caller's cancellation token will apply instead to the
                // JoinAsync operation in GetProgressStageStatusAsync.
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, _threadingContext.DisposalToken);

                // Make sure the HubClient package is loaded, since we rely on it for proffered OOP services
                var shell = await _serviceProvider.GetServiceAsync<SVsShell, IVsShell7>(_threadingContext.DisposalToken).ConfigureAwait(true);

                await shell.LoadPackageAsync(Guids.GlobalHubClientPackageGuid);
            });

            _progressStageStatus = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                // preemptively make sure event is subscribed. if APIs are called before it is done, calls will be
                // blocked until event subscription is done
                using var asyncToken = listener.BeginAsyncOperation("StatusChanged_EventSubscription");

                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, _threadingContext.DisposalToken);
                var service = await serviceProvider.GetServiceAsync<SVsOperationProgress, IVsOperationProgressStatusService>(throwOnFailure: false, _threadingContext.DisposalToken).ConfigureAwait(true);
                if (service is null)
                    return null;

                var status = service.GetStageStatusForSolutionLoad(CommonOperationProgressStageIds.Intellisense);
                status.PropertyChanged += (_, _) => StatusChanged?.Invoke(this, EventArgs.Empty);

                return status;
            });
        }

        // unfortunately, IVsOperationProgressStatusService requires UI thread to let project system to proceed to next stages.
        // this method should only be used with either await or JTF.Run, it should be never used with Task.Wait otherwise, it can
        // deadlock
        //
        // This method also ensures the GlobalHubClientPackage package is loaded, since brokered services in Visual
        // Studio require this package to provide proxy interfaces for invoking out-of-process services.
        public async Task WaitUntilFullyLoadedAsync(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.PartialLoad_FullyLoaded, KeyValueLogMessage.NoProperty, cancellationToken))
            {
                var status = await GetProgressStageStatusAsync(cancellationToken).ConfigureAwait(false);
                if (status == null)
                {
                    return;
                }

                var completionTask = status.WaitForCompletionAsync();
                Logger.Log(FunctionId.PartialLoad_FullyLoaded, KeyValueLogMessage.Create(
                    LogType.Trace, static (m, completionTask) => m["AlreadyFullyLoaded"] = completionTask.IsCompleted, completionTask, LogLevel.Debug));

                // TODO: WaitForCompletionAsync should accept cancellation directly.
                //       for now, use WithCancellation to indirectly add cancellation
                await completionTask.WithCancellation(cancellationToken).ConfigureAwait(false);

                await _loadHubClientPackage.JoinAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
        {
            var status = await GetProgressStageStatusAsync(cancellationToken).ConfigureAwait(false);
            return status != null && !status.IsInProgress;
        }

        private async ValueTask<IVsOperationProgressStageStatusForSolutionLoad?> GetProgressStageStatusAsync(CancellationToken cancellationToken)
            => await _progressStageStatus.JoinAsync(cancellationToken).ConfigureAwait(false);
    }
}
