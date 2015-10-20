// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    // Based on CoreCLR's implementation of the TaskScheduler they return from TaskScheduler.FromCurrentSynchronizationContext
    internal class SynchronizationContextTaskScheduler : TaskScheduler
    {
        private readonly SendOrPostCallback _postCallback;
        private readonly SynchronizationContext _synchronizationContext;

        internal SynchronizationContextTaskScheduler(SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null)
                throw new ArgumentNullException(nameof(synchronizationContext));

            _postCallback = new SendOrPostCallback(PostCallback);
            _synchronizationContext = synchronizationContext;
        }

        public override Int32 MaximumConcurrencyLevel
        {
            get { return 1; }
        }

        protected override void QueueTask(Task task)
        {
            _synchronizationContext.Post(_postCallback, task);

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
