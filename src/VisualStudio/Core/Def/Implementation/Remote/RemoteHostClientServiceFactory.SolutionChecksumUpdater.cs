// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class RemoteHostClientServiceFactory
    {
        private class SolutionChecksumUpdater : GlobalOperationAwareIdleProcessor
        {
            private readonly RemoteHostClientService _service;
            private readonly SemaphoreSlim _event;
            private readonly object _gate;

            private CancellationTokenSource _globalOperationCancellationSource;

            // hold last async token
            private IAsyncToken _lastToken;

            public SolutionChecksumUpdater(RemoteHostClientService service, CancellationToken shutdownToken) :
                base(service.Listener,
                     service.Workspace.Services.GetService<IGlobalOperationNotificationService>(),
                     service.Workspace.Options.GetOption(RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS), shutdownToken)
            {
                _service = service;

                _event = new SemaphoreSlim(initialCount: 0);
                _gate = new object();

                // start listening workspace change event
                _service.Workspace.WorkspaceChanged += OnWorkspaceChanged;

                // create its own cancellation token source
                _globalOperationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);

                Start();
            }

            private CancellationToken ShutdownCancellationToken => CancellationToken;

            protected override async Task ExecuteAsync()
            {
                lock (_gate)
                {
                    _lastToken?.Dispose();
                    _lastToken = null;
                }

                // wait for global operation to finish
                await GlobalOperationTask.ConfigureAwait(false);

                // update primary solution in remote host
                await SynchronizePrimaryWorkspaceAsync(_globalOperationCancellationSource.Token).ConfigureAwait(false);
            }

            protected override void PauseOnGlobalOperation()
            {
                var previousCancellationSource = _globalOperationCancellationSource;

                // create new cancellation token source linked with given shutdown cancellation token
                _globalOperationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(ShutdownCancellationToken);

                CancelAndDispose(previousCancellationSource);
            }

            protected override Task WaitAsync(CancellationToken cancellationToken)
            {
                return _event.WaitAsync(cancellationToken);
            }

            public override void Shutdown()
            {
                base.Shutdown();

                // stop listening workspace change event
                _service.Workspace.WorkspaceChanged -= OnWorkspaceChanged;

                CancelAndDispose(_globalOperationCancellationSource);
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                // record that we are busy
                UpdateLastAccessTime();

                EnqueueChecksumUpdate();
            }

            private void EnqueueChecksumUpdate()
            {
                // event will raised sequencially. no concurrency on this handler
                if (_event.CurrentCount > 0)
                {
                    return;
                }

                lock (_gate)
                {
                    _lastToken = _lastToken ?? Listener.BeginAsyncOperation(nameof(SolutionChecksumUpdater));
                }

                _event.Release();
            }

            private Task SynchronizePrimaryWorkspaceAsync(CancellationToken cancellationToken)
            {
                return _service.Workspace.SynchronizePrimaryWorkspaceAsync(_service.Workspace.CurrentSolution, cancellationToken);
            }

            private static void CancelAndDispose(CancellationTokenSource cancellationSource)
            {
                // cancel running tasks
                cancellationSource.Cancel();

                // dispose cancellation token source
                cancellationSource.Dispose();
            }
        }
    }
}
