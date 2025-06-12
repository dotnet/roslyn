// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Test.Utilities;

// Based on CoreCLR's implementation of the TaskScheduler they return from TaskScheduler.FromCurrentSynchronizationContext
internal sealed class SynchronizationContextTaskScheduler : TaskScheduler
{
    private readonly SendOrPostCallback _postCallback;
    private readonly SynchronizationContext _synchronizationContext;

    internal SynchronizationContextTaskScheduler(SynchronizationContext synchronizationContext)
    {
        _postCallback = new SendOrPostCallback(PostCallback);
        _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
    }

    public override Int32 MaximumConcurrencyLevel => 1;

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
        => null;

    private void PostCallback(object obj)
        => TryExecuteTask((Task)obj);
}
