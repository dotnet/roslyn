// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Notification;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal sealed partial class UnitTestingSolutionCrawlerRegistrationService
{
    internal sealed partial class UnitTestingWorkCoordinator
    {
        private sealed partial class UnitTestingIncrementalAnalyzerProcessor
        {
            private abstract class AbstractUnitTestingPriorityProcessor : UnitTestingGlobalOperationAwareIdleProcessor
            {
                protected readonly UnitTestingIncrementalAnalyzerProcessor Processor;

                private readonly object _gate = new();
                private Lazy<ImmutableArray<IUnitTestingIncrementalAnalyzer>> _lazyAnalyzers;

                public AbstractUnitTestingPriorityProcessor(
                    IAsynchronousOperationListener listener,
                    UnitTestingIncrementalAnalyzerProcessor processor,
                    Lazy<ImmutableArray<IUnitTestingIncrementalAnalyzer>> lazyAnalyzers,
                    IGlobalOperationNotificationService? globalOperationNotificationService,
                    TimeSpan backOffTimeSpan,
                    CancellationToken shutdownToken)
                    : base(listener, globalOperationNotificationService, backOffTimeSpan, shutdownToken)
                {
                    _lazyAnalyzers = lazyAnalyzers;

                    Processor = processor;
                }

                public ImmutableArray<IUnitTestingIncrementalAnalyzer> Analyzers
                {
                    get
                    {
                        lock (_gate)
                        {
                            return _lazyAnalyzers.Value;
                        }
                    }
                }

                public void AddAnalyzer(IUnitTestingIncrementalAnalyzer analyzer)
                {
                    lock (_gate)
                    {
                        var analyzers = _lazyAnalyzers.Value;
                        _lazyAnalyzers = new Lazy<ImmutableArray<IUnitTestingIncrementalAnalyzer>>(() => analyzers.Add(analyzer));
                    }
                }

                protected override void OnPaused()
                    => UnitTestingSolutionCrawlerLogger.LogGlobalOperation(Processor._logAggregator);

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

                            if (HigherQueueHasWorkItem)
                            {
                                // There was still something more important in another queue.  Back off again (e.g.
                                // call UpdateLastAccessTime) then wait that amount of time and check again to see
                                // if that queue is clear.
                                UpdateLastAccessTime();
                                await WaitForIdleAsync(Listener).ConfigureAwait(false);
                                continue;
                            }

                            if (GetIsPaused())
                            {
                                // if we're paused, we still want to keep waiting until we become unpaused. After we
                                // become unpaused though, loop around those to see if there is still high pri work
                                // to do.
                                await WaitForIdleAsync(Listener).ConfigureAwait(false);
                                continue;
                            }

                            // There was no higher queue work item and we're not paused. However, we may not have
                            // waited long enough to actually satisfy our own backoff-delay.  If so, wait until
                            // we're actually idle.
                            if (ShouldContinueToBackOff())
                            {
                                // Do the wait.  If it returns 'true' then we did the full wait.  Loop around again
                                // to see if there is higher priority work, or if we got paused.

                                // However, if it returns 'false' then that means the delay completed quickly
                                // because some unit/integration test is asking us to expedite our work.  In that
                                // case, just return out immediately so we can process what is in our queue.
                                if (await WaitForIdleAsync(Listener).ConfigureAwait(false))
                                    continue;

                                // intentional fall-through.
                            }

                            return;
                        }
                    }
                }
            }
        }
    }
}
