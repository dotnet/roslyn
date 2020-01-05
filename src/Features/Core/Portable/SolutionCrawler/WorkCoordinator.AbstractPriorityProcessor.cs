// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private sealed partial class WorkCoordinator
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
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken)
                        : base(listener, globalOperationNotificationService, backOffTimeSpanInMs, shutdownToken)
                    {
                        _gate = new object();
                        _lazyAnalyzers = lazyAnalyzers;

                        Processor = processor;

                        if (Processor._documentTracker != null)
                        {
                            Processor._documentTracker.NonRoslynBufferTextChanged += OnNonRoslynBufferTextChanged;
                        }
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

                    protected override void PauseOnGlobalOperation()
                    {
                        SolutionCrawlerLogger.LogGlobalOperation(Processor._logAggregator);
                    }

                    protected abstract Task HigherQueueOperationTask { get; }
                    protected abstract bool HigherQueueHasWorkItem { get; }

                    protected async Task WaitForHigherPriorityOperationsAsync()
                    {
                        using (Logger.LogBlock(FunctionId.WorkCoordinator_WaitForHigherPriorityOperationsAsync, CancellationToken))
                        {
                            do
                            {
                                // Host is shutting down
                                if (CancellationToken.IsCancellationRequested)
                                {
                                    return;
                                }

                                // we wait for global operation and higher queue operation if there is anything going on
                                if (!GlobalOperationTask.IsCompleted || !HigherQueueOperationTask.IsCompleted)
                                {
                                    await Task.WhenAll(GlobalOperationTask, HigherQueueOperationTask).ConfigureAwait(false);
                                }

                                // if there are no more work left for higher queue, then it is our time to go ahead
                                if (!HigherQueueHasWorkItem)
                                {
                                    return;
                                }

                                // back off and wait for next time slot.
                                UpdateLastAccessTime();
                                await WaitForIdleAsync(Listener).ConfigureAwait(false);
                            }
                            while (true);
                        }
                    }

                    public override void Shutdown()
                    {
                        base.Shutdown();

                        if (Processor._documentTracker != null)
                        {
                            Processor._documentTracker.NonRoslynBufferTextChanged -= OnNonRoslynBufferTextChanged;
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
                        UpdateLastAccessTime();
                    }
                }
            }
        }
    }
}
