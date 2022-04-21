// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal sealed partial class SolutionCrawlerRegistrationService
    {
        internal sealed partial class WorkCoordinator
        {
            private sealed partial class IncrementalAnalyzerProcessor
            {
                private abstract class AbstractPriorityProcessor : GlobalOperationAwareIdleProcessor
                {
                    protected readonly IncrementalAnalyzerProcessor Processor;

                    private readonly object _gate;
                    private Lazy<ImmutableArray<IIncrementalAnalyzer>> _lazyAnalyzers;

                    public AbstractPriorityProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers,
                        IGlobalOperationNotificationService globalOperationNotificationService,
                        TimeSpan backOffTimeSpan,
                        CancellationToken shutdownToken)
                        : base(listener, globalOperationNotificationService, backOffTimeSpan, shutdownToken)
                    {
                        _gate = new object();
                        _lazyAnalyzers = lazyAnalyzers;

                        Processor = processor;
                        Processor._documentTracker.NonRoslynBufferTextChanged += OnNonRoslynBufferTextChanged;
                    }

                    public ImmutableArray<IIncrementalAnalyzer> Analyzers
                    {
                        get
                        {
                            lock (_gate)
                            {
                                return _lazyAnalyzers.Value;
                            }
                        }
                    }

                    public void AddAnalyzer(IIncrementalAnalyzer analyzer)
                    {
                        lock (_gate)
                        {
                            var analyzers = _lazyAnalyzers.Value;
                            _lazyAnalyzers = new Lazy<ImmutableArray<IIncrementalAnalyzer>>(() => analyzers.Add(analyzer));
                        }
                    }

                    protected override void OnPaused()
                        => SolutionCrawlerLogger.LogGlobalOperation(Processor._logAggregator);

                    protected abstract Task HigherQueueOperationTask { get; }
                    protected abstract bool HigherQueueHasWorkItem { get; }

                    protected async Task WaitForHigherPriorityOperationsAsync()
                    {
                        using (Logger.LogBlock(FunctionId.WorkCoordinator_WaitForHigherPriorityOperationsAsync, CancellationToken))
                        {
                            while (true)
                            {
                                CancellationToken.ThrowIfCancellationRequested();

                                // we wait for global operation and higher queue operation if there is anything going on
                                await HigherQueueOperationTask.ConfigureAwait(false);

                                // if there are no more work left for higher queue, and we didn't enter a state where we
                                // should wait for idle again, then our time to go ahead.
                                var higherQueueHasWorkItem = HigherQueueHasWorkItem;

                                // If we have something more important in the queue.  Act as we need to back off again
                                // (e.g. call UpdateLastAccessTime) then wait that amount of time and check again.
                                if (higherQueueHasWorkItem)
                                {
                                    UpdateLastAccessTime();
                                    await WaitForIdleAsync(Listener).ConfigureAwait(false);
                                    continue;
                                }

                                // There was no higher queue work item.  However, we also may still be paused, or we may
                                // not have waited long enough to actually satisfy our our backoff-delay.  If so, wait
                                // until we're actually idle.
                                if (ShouldWaitForIdle())
                                {
                                    // Do the wait.  However, if it returns 'false' then that means the delay completed
                                    // quickly because some unit/integration test is asking us to expedite our work.  In
                                    // that case, just return out immediately so we can process what is in our queue.
                                    //
                                    // Otherwise, we did a normal wait, continue the loop in case higher priority work
                                    // came in. if nothing did, then we'll bail out below.
                                    if (await WaitForIdleAsync(Listener).ConfigureAwait(false))
                                        continue;

                                    // intentional fall-through.
                                }

                                // If we got here, then either:
                                //
                                // 1. there was no more important work *and* we are not paused *and* we waited long
                                //    enough to start running. or
                                // 2. we were paused or hadn't waited long enough *but* the test harness is asking to
                                //    expedite things, so we proceed anyways.

                                return;
                            }
                        }
                    }

                    public override void Shutdown()
                    {
                        base.Shutdown();

                        Processor._documentTracker.NonRoslynBufferTextChanged -= OnNonRoslynBufferTextChanged;
                    }

                    private void OnNonRoslynBufferTextChanged(object? sender, EventArgs e)
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
                        UpdateLastAccessTime();
                    }
                }
            }
        }
    }
}
