// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal abstract class GlobalOperationAwareIdleProcessor : IdleProcessor
    {
        private readonly IGlobalOperationNotificationService _globalOperationNotificationService;

        private TaskCompletionSource<object> _globalOperation;
        private Task _globalOperationTask;

        public GlobalOperationAwareIdleProcessor(
            IAsynchronousOperationListener listener,
            IGlobalOperationNotificationService globalOperationNotificationService,
            int backOffTimeSpanInMs,
            CancellationToken shutdownToken)
            : base(listener, backOffTimeSpanInMs, shutdownToken)
        {
            _globalOperation = null;
            _globalOperationTask = Task.CompletedTask;

            _globalOperationNotificationService = globalOperationNotificationService;
            _globalOperationNotificationService.Started += OnGlobalOperationStarted;
            _globalOperationNotificationService.Stopped += OnGlobalOperationStopped;
        }

        protected Task GlobalOperationTask => _globalOperationTask;

        protected abstract void PauseOnGlobalOperation();

        private void OnGlobalOperationStarted(object sender, EventArgs e)
        {
            Contract.ThrowIfFalse(_globalOperation == null);

            // events are serialized. no lock is needed
            _globalOperation = new TaskCompletionSource<object>();
            _globalOperationTask = _globalOperation.Task;

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
            _globalOperationTask = Task.CompletedTask;
        }

        public virtual void Shutdown()
        {
            _globalOperationNotificationService.Started -= OnGlobalOperationStarted;
            _globalOperationNotificationService.Stopped -= OnGlobalOperationStopped;
        }
    }
}
