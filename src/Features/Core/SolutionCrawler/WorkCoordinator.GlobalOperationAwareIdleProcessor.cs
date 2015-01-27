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
    internal sealed partial class WorkCoordinatorRegistrationService
    {
        private sealed partial class WorkCoordinator
        {
            private sealed partial class IncrementalAnalyzerProcessor
            {
                private abstract class GlobalOperationAwareIdleProcessor : IdleProcessor
                {
                    protected readonly IncrementalAnalyzerProcessor Processor;
                    private readonly IGlobalOperationNotificationService globalOperationNotificationService;

                    private TaskCompletionSource<object> globalOperation;
                    private Task globalOperationTask;
                    
                    public GlobalOperationAwareIdleProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        IGlobalOperationNotificationService globalOperationNotificationService,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken) :
                        base(listener, backOffTimeSpanInMs, shutdownToken)
                    {
                        this.Processor = processor;
                        this.globalOperation = null;
                        this.globalOperationTask = SpecializedTasks.EmptyTask;

                        this.globalOperationNotificationService = globalOperationNotificationService;
                        this.globalOperationNotificationService.Started += OnGlobalOperationStarted;
                        this.globalOperationNotificationService.Stopped += OnGlobalOperationStopped;
                    }
                    
                    private void OnGlobalOperationStarted(object sender, EventArgs e)
                    {
                        Contract.ThrowIfFalse(this.globalOperation == null);

                        // events are serialized. no lock is needed
                        this.globalOperation = new TaskCompletionSource<object>();
                        this.globalOperationTask = this.globalOperation.Task;

                        SolutionCrawlerLogger.LogGlobalOperation(this.Processor.logAggregator);
                    }

                    private void OnGlobalOperationStopped(object sender, GlobalOperationEventArgs e)
                    {
                        Contract.ThrowIfFalse(this.globalOperation != null);

                        // events are serialized. no lock is needed
                        this.globalOperation.SetResult(null);
                        this.globalOperation = null;

                        // set to empty task so that we don't need a lock
                        this.globalOperationTask = SpecializedTasks.EmptyTask;
                    }

                    protected async Task GlobalOperationWaitAsync()
                    {
                        // we wait for global operation if there is anything going on
                        await this.globalOperationTask.ConfigureAwait(false);
                    }

                    public virtual void Shutdown()
                    {
                        this.globalOperationNotificationService.Started -= OnGlobalOperationStarted;
                        this.globalOperationNotificationService.Stopped -= OnGlobalOperationStopped;
                    }
                }
            }
        }
    }
}
