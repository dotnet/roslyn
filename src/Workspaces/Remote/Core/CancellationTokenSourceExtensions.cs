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
        public static void CancelOnAbnormalCompletion(this CancellationTokenSource cancellationTokenSource, Task task)
        {
            _ = task.ContinueWith(
                _ =>
                {
                    try
                    {
                        cancellationTokenSource.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // cancellation source is already disposed
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
