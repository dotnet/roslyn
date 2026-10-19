// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal static class SettingsSynchronizationRetry
{
    private static readonly TimeSpan s_defaultRetryDelay = TimeSpan.FromSeconds(5);

    public static async Task RunAsync(
        Func<Task> synchronizeAsync,
        Action<Task> observe,
        CancellationToken cancellationToken,
        TimeSpan? retryDelay = null)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await synchronizeAsync().ConfigureAwait(false);
                return;
            }
            catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                observe(Task.FromException(e));
            }

            await Task.Delay(retryDelay ?? s_defaultRetryDelay, cancellationToken).ConfigureAwait(false);
        }
    }
}
