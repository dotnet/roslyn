// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Roslyn.Test.Utilities
{
    public static class DispatcherExtensions
    {
        public static void DoEvents(this Dispatcher dispatcher)
        {
            // A DispatcherFrame represents a loop that processes pending work
            // items.
            var frame = new DispatcherFrame();
            var callback = (Action<DispatcherFrame>)(f => f.Continue = false);

            // Executes the specified delegate asynchronously.  When it is 
            // complete mark the frame as complete so the dispatcher loop
            // pops out (stops).
            var operation = dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle, callback, frame);

            // Start the loop.  It will process all items in the queue, then 
            // will process the above callback.  That callback will tell the
            // loop to then stop processing.
            Dispatcher.PushFrame(frame);

            if (operation.Status != DispatcherOperationStatus.Completed)
            {
                operation.Abort();
            }
        }

        public static void DoEvents(this Dispatcher dispatcher, Func<CancellationToken, Task<bool>> continueUntilAsync = null, CancellationToken cancellationToken = default)
        {
            // A DispatcherFrame represents a loop that processes pending work
            // items.
            var frame = new DispatcherFrame();
            Action<DispatcherFrame> callback = null;
            callback = f =>
            {
                if (cancellationToken.IsCancellationRequested || continueUntilAsync == null)
                {
                    f.Continue = false;
                    return;
                }

                var continueAsync = continueUntilAsync(cancellationToken);
                if (continueAsync.IsCompleted)
                {
                    f.Continue = continueAsync.GetAwaiter().GetResult();
                    return;
                }

                continueAsync.ContinueWith(
                    t => dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, callback, frame),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            };

            // Executes the specified delegate asynchronously.  When it is 
            // complete mark the frame as complete so the dispatcher loop
            // pops out (stops).
            var operation = dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle, callback, frame);

            // Start the loop.  It will process all items in the queue, then 
            // will process the above callback.  That callback will tell the
            // loop to then stop processing.
            Dispatcher.PushFrame(frame);

            if (operation.Status != DispatcherOperationStatus.Completed)
            {
                operation.Abort();
            }
        }

        public static Task<TaskScheduler> ToTaskSchedulerAsync(this Dispatcher dispatcher, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return dispatcher.InvokeAsync(
                () => TaskScheduler.FromCurrentSynchronizationContext(),
                priority).Task;
        }
    }
}
