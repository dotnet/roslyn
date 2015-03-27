// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal sealed partial class SolutionCrawlerRegistrationService
    {
        private sealed partial class WorkCoordinator
        {
            private sealed partial class IncrementalAnalyzerProcessor
            {
                private abstract class GlobalOperationAwareIdleProcessor : IdleProcessor
                {
                    protected readonly IncrementalAnalyzerProcessor Processor;
                    private readonly IGlobalOperationNotificationService _globalOperationNotificationService;

                    private TaskCompletionSource<object> _globalOperation;
                    private Task _globalOperationTask;

                    public GlobalOperationAwareIdleProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        IGlobalOperationNotificationService globalOperationNotificationService,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken) :
                        base(listener, backOffTimeSpanInMs, shutdownToken)
                    {
                        this.Processor = processor;
                        _globalOperation = null;
                        _globalOperationTask = SpecializedTasks.EmptyTask;

                        _globalOperationNotificationService = globalOperationNotificationService;
                        _globalOperationNotificationService.Started += OnGlobalOperationStarted;
                        _globalOperationNotificationService.Stopped += OnGlobalOperationStopped;
                    }

                    protected Task GlobalOperationTask
                    {
                        get
                        {
                            return _globalOperationTask;
                        }
                    }

                    protected abstract void PauseOnGlobalOperation();

                    private void OnGlobalOperationStarted(object sender, EventArgs e)
                    {
                        Contract.ThrowIfFalse(_globalOperation == null);

                        // events are serialized. no lock is needed
                        _globalOperation = new TaskCompletionSource<object>();
                        _globalOperationTask = _globalOperation.Task;

                        SolutionCrawlerLogger.LogGlobalOperation(this.Processor._logAggregator);

                        PauseOnGlobalOperation();
                    }

                    private void OnGlobalOperationStopped(object sender, GlobalOperationEventArgs e)
                    {
                        if (_globalOperation == null)
                        {
                            // we subscribed to the event while it is already running.
                            return;
                        }

                        // events are serialized. no lock is needed
                        _globalOperation.SetResult(null);
                        _globalOperation = null;

                        // set to empty task so that we don't need a lock
                        _globalOperationTask = SpecializedTasks.EmptyTask;
                    }

                    protected abstract Task HigherQueueOperationTask { get; }
                    protected abstract bool HigherQueueHasWorkItem { get; }

                    protected async Task WaitForHigherPriorityOperationsAsync()
                    {
                        do
                        {
                            // we wait for global operation and higher queue operation if there is anything going on
                            if (!this.GlobalOperationTask.IsCompleted || !this.HigherQueueOperationTask.IsCompleted)
                            {
                                await Task.WhenAll(this.GlobalOperationTask, this.HigherQueueOperationTask).ConfigureAwait(false);
                            }

                            // if there are no more work left for higher queue, then it is our time to go ahead
                            if (!HigherQueueHasWorkItem)
                            {
                                break;
                            }

                            // back off and wait for next time slot.
                            this.UpdateLastAccessTime();
                            await this.WaitForIdleAsync().ConfigureAwait(false);
                        }
                        while (true);
                    }

                    public virtual void Shutdown()
                    {
                        _globalOperationNotificationService.Started -= OnGlobalOperationStarted;
                        _globalOperationNotificationService.Stopped -= OnGlobalOperationStopped;
                    }
                }
            }
        }
    }
}
