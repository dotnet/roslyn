// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal sealed partial class AsynchronousOperationListenerProvider
    {
        private sealed class NullOperationListener : IAsynchronousOperationListener
        {
            public IAsyncToken BeginAsyncOperation(
                string name,
                object? tag = null,
                [CallerFilePath] string filePath = "",
                [CallerLineNumber] int lineNumber = 0) => EmptyAsyncToken.Instance;

            public Task<bool> Delay(TimeSpan delay, CancellationToken cancellationToken)
            {
                // This could be as simple as:
                //     await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                //     return true;
                // However, whereas in general cancellation is expected to be rare and thus throwing
                // an exception in response isn't very impactful, here it's expected to be the case
                // more often than not as the operation is being used to delay an operation because
                // it's expected something else is going to happen to obviate the need for that
                // operation.  Thus, we can spend a little more code avoiding the additional throw
                // for the common case of an exception occurring.

                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled<bool>(cancellationToken);
                }

                var t = Task.Delay(delay, cancellationToken);
                if (t.IsCompleted)
                {
                    // Avoid ContinueWith overheads for a 0 delay or if race conditions resulted
                    // in the delay task being complete by the time we checked.
                    return t.Status == TaskStatus.RanToCompletion
                        ? SpecializedTasks.True
                        : Task.FromCanceled<bool>(cancellationToken);
                }

                return t.ContinueWith(
                    _ => true,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnCanceled,
                    TaskScheduler.Default);

                // Note the above passes CancellationToken.None and TaskContinuationOptions.NotOnCanceled.
                // That's cheaper than passing cancellationToken and with the same semantics except
                // that if the returned task does end up being canceled, any operation canceled exception
                // thrown won't contain the cancellationToken.  If that ends up being impactful, it can
                // be switched to use `cancellationToken, TaskContinuationOptions.ExecuteSynchronously`.
            }
        }
    }
}
