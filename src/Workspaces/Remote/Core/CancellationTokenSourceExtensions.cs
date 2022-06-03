// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class CancellationTokenSourceExtensions
    {
        /// <summary>
        /// Automatically cancels the <paramref name="cancellationTokenSource"/> if the input <paramref name="task"/>
        /// completes in a <see cref="TaskStatus.Canceled"/> or <see cref="TaskStatus.Faulted"/> state.
        /// </summary>
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        /// <param name="task">The task to monitor.</param>
        public static void CancelOnAbnormalCompletion(this CancellationTokenSource cancellationTokenSource, Task task)
        {
            if (cancellationTokenSource is null)
                throw new ArgumentNullException(nameof(cancellationTokenSource));

            _ = task.ContinueWith(
                static (_, state) =>
                {
                    try
                    {
                        ((CancellationTokenSource)state!).Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // cancellation source is already disposed
                    }
                },
                state: cancellationTokenSource,
                CancellationToken.None,
                TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
