// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class RemoteHostClientServiceFactory
    {
        private class SolutionChecksumUpdater : GlobalOperationAwareIdleProcessor
        {
            private readonly RemoteHostClientService _service;
            private readonly SimpleTaskQueue _textChangeQueue;
            private readonly SemaphoreSlim _event;
            private readonly object _gate;

            private CancellationTokenSource _globalOperationCancellationSource;

            // hold last async token
            private IAsyncToken _lastToken;

            public SolutionChecksumUpdater(RemoteHostClientService service, CancellationToken shutdownToken)
                : base(service.Listener,
                       service.Workspace.Services.GetService<IGlobalOperationNotificationService>(),
                       service.Workspace.Options.GetOption(RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS), shutdownToken)
            {
                _service = service;
                _textChangeQueue = new SimpleTaskQueue(TaskScheduler.Default);

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
                if (e.Kind == WorkspaceChangeKind.DocumentChanged)
                {
                    PushTextChanges(e.OldSolution.GetDocument(e.DocumentId), e.NewSolution.GetDocument(e.DocumentId));
                }

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
                    _lastToken ??= Listener.BeginAsyncOperation(nameof(SolutionChecksumUpdater));
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

            private void PushTextChanges(Document oldDocument, Document newDocument)
            {
                // this pushes text changes to the remote side if it can.
                // this is purely perf optimization. whether this pushing text change
                // worked or not doesn't affect feature's functionality.
                //
                // this basically see whether it can cheaply find out text changes
                // between 2 snapshots, if it can, it will send out that text changes to
                // remote side.
                //
                // the remote side, once got the text change, will again see whether
                // it can use that text change information without any high cost and
                // create new snapshot from it.
                //
                // otherwise, it will do the normal behavior of getting full text from
                // VS side. this optimization saves times we need to do full text
                // synchronization for typing scenario.

                if ((oldDocument.TryGetText(out var oldText) == false) ||
                    (newDocument.TryGetText(out var newText) == false))
                {
                    // we only support case where text already exist
                    return;
                }

                // get text changes
                var textChanges = newText.GetTextChanges(oldText);
                if (textChanges.Count == 0)
                {
                    // no changes
                    return;
                }

                // whole document case
                if (textChanges.Count == 1 && textChanges[0].Span.Length == oldText.Length)
                {
                    // no benefit here. pulling from remote host is more efficient
                    return;
                }

                // only cancelled when remote host gets shutdown
                var token = Listener.BeginAsyncOperation(nameof(PushTextChanges));
                _textChangeQueue.ScheduleTask(async () =>
                {
                    var client = await _service.Workspace.TryGetRemoteHostClientAsync(CancellationToken).ConfigureAwait(false);
                    if (client == null)
                    {
                        return;
                    }

                    var state = await oldDocument.State.GetStateChecksumsAsync(CancellationToken).ConfigureAwait(false);

                    await client.TryRunRemoteAsync(
                        WellKnownRemoteHostServices.RemoteHostService, nameof(IRemoteHostService.SynchronizeTextAsync),
                        new object[] { oldDocument.Id, state.Text, textChanges }, CancellationToken).ConfigureAwait(false);
                }, CancellationToken).CompletesAsyncOperation(token);
            }
        }
    }
}
