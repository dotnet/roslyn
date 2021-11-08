// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable disable

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    // Based on CoreCLR's implementation of the TaskScheduler they return from TaskScheduler.FromCurrentSynchronizationContext
    public class SynchronizationContextTaskScheduler : TaskScheduler
    {
        private readonly SendOrPostCallback _postCallback;
        private readonly SynchronizationContext _synchronizationContext;

        public SynchronizationContextTaskScheduler(SynchronizationContext synchronizationContext)
        {
            _postCallback = new SendOrPostCallback(PostCallback);
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        public override int MaximumConcurrencyLevel => 1;

        protected override void QueueTask(Task task)
        {
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            _synchronizationContext.Post(_postCallback, task);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                return TryExecuteTask(task);
            }

            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return null;
        }

        private void PostCallback(object obj)
        {
            TryExecuteTask((Task)obj);
        }
    }
}
