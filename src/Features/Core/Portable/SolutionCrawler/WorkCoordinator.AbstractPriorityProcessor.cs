// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal sealed partial class SolutionCrawlerRegistrationService
    {
        private sealed partial class WorkCoordinator
        {
            private sealed partial class IncrementalAnalyzerProcessor
            {
                private abstract class AbstractPriorityProcessor : GlobalOperationAwareIdleProcessor
                {
                    protected readonly IncrementalAnalyzerProcessor Processor;
                    public AbstractPriorityProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        IGlobalOperationNotificationService globalOperationNotificationService,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken) :
                        base(listener, globalOperationNotificationService, backOffTimeSpanInMs, shutdownToken)
                    {
                        this.Processor = processor;

                        if (this.Processor._documentTracker != null)
                        {
                            this.Processor._documentTracker.NonRoslynBufferTextChanged += OnNonRoslynBufferTextChanged;
                        }
                    }

                    private void OnNonRoslynBufferTextChanged(object sender, EventArgs e)
                    {
                        // There are 2 things incremental processor takes care of
                        //
                        // #1 is making sure we delay processing any work until there is enough idle (ex, typing) in host.
                        // #2 is managing cancellation and pending works.
                        //
                        // we used to do #1 and #2 only for Roslyn files. and that is usually fine since most of time solution contains only roslyn files.
                        //
                        // but for mixed solution (ex, Roslyn files + HTML + JS + CSS), #2 still makes sense but #1 doesn't. We want
                        // to pause any work while something is going on in other project types as well. 
                        //
                        // we need to make sure we play nice with neighbors as well.
                        //
                        // now, we don't care where changes are coming from. if there is any change in host, we pause ourselves for a while.
                        this.UpdateLastAccessTime();
                    }

                    protected override void PauseOnGlobalOperation()
                    {
                        SolutionCrawlerLogger.LogGlobalOperation(this.Processor._logAggregator);
                    }

                    protected abstract Task HigherQueueOperationTask { get; }
                    protected abstract bool HigherQueueHasWorkItem { get; }

                    protected async Task WaitForHigherPriorityOperationsAsync()
                    {
                        using (Logger.LogBlock(FunctionId.WorkCoordinator_WaitForHigherPriorityOperationsAsync, this.CancellationToken))
                        {
                            do
                            {
                                // Host is shutting down
                                if (this.CancellationToken.IsCancellationRequested)
                                {
                                    return;
                                }

                                // we wait for global operation and higher queue operation if there is anything going on
                                if (!this.GlobalOperationTask.IsCompleted || !this.HigherQueueOperationTask.IsCompleted)
                                {
                                    await Task.WhenAll(this.GlobalOperationTask, this.HigherQueueOperationTask).ConfigureAwait(false);
                                }

                                // if there are no more work left for higher queue, then it is our time to go ahead
                                if (!HigherQueueHasWorkItem)
                                {
                                    return;
                                }

                                // back off and wait for next time slot.
                                this.UpdateLastAccessTime();
                                await this.WaitForIdleAsync().ConfigureAwait(false);
                            }
                            while (true);
                        }
                    }

                    public override void Shutdown()
                    {
                        base.Shutdown();

                        if (this.Processor._documentTracker != null)
                        {
                            this.Processor._documentTracker.NonRoslynBufferTextChanged -= OnNonRoslynBufferTextChanged;
                        }
                    }
                }
            }
        }
    }
}
