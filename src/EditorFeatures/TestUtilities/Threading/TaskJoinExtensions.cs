// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit;

namespace Roslyn.Test.Utilities;

public static class TaskJoinExtensions
{
    /// <summary>
    /// Joins a <see cref="Task"/> to the current thread with a <see cref="Dispatcher"/> message pump in place
    /// during the join operation.
    /// </summary>
    public static void JoinUsingDispatcher(this Task task, CancellationToken cancellationToken)
    {
        JoinUsingDispatcherNoResult(task, cancellationToken);

        // Handle task completion by throwing the appropriate exception on failure
        task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Joins a <see cref="Task{TResult}"/> to the current thread with a <see cref="Dispatcher"/> message pump in
    /// place during the join operation.
    /// </summary>
    public static TResult JoinUsingDispatcher<TResult>(this Task<TResult> task, CancellationToken cancellationToken)
    {
        JoinUsingDispatcherNoResult(task, cancellationToken);

        // Handle task completion by throwing the appropriate exception on failure
        return task.GetAwaiter().GetResult();
    }

    private static void JoinUsingDispatcherNoResult(Task task, CancellationToken cancellationToken)
    {
        var frame = new DispatcherFrame();

        // When the task completes or cancellation is requested, mark the frame so we leave the message pump
        task.ContinueWith(
            t => frame.Continue = false,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        using (var registration = cancellationToken.Register(() => frame.Continue = false))
        {
            Dispatcher.PushFrame(frame);
        }

        // Handle cancellation by throwing an exception
        if (!task.IsCompleted)
        {
            Assert.True(cancellationToken.IsCancellationRequested);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
