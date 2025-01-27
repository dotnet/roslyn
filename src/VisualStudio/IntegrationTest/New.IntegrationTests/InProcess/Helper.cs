// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UIAutomationClient;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

internal static class Helper
{
    private static IUIAutomation2? _automation;

    public static IUIAutomation2 Automation
    {
        get
        {
            if (_automation == null)
            {
                Interlocked.CompareExchange(ref _automation, new CUIAutomation8(), null);
            }

            return _automation;
        }
    }

    /// <summary>
    /// This method will retry the action represented by the 'action' argument,
    /// waiting for 'delay' time after each retry. If a given retry returns a value 
    /// other than default(T), this value is returned.
    /// </summary>
    /// <param name="action">the action to retry</param>
    /// <param name="delay">the amount of time to wait between retries</param>
    /// <typeparam name="T">type of return value</typeparam>
    /// <returns>the return value of 'action'</returns>
    public static T? Retry<T>(Func<CancellationToken, T> action, TimeSpan delay, int retryCount = -1, CancellationToken cancellationToken = default)
    {
        return RetryHelper(
            cancellationToken =>
            {
                try
                {
                    return action(cancellationToken);
                }
                catch (COMException)
                {
                    // Devenv can throw COMExceptions if it's busy when we make DTE calls.
                    return default;
                }
            },
            delay,
            retryCount,
            cancellationToken);
    }

    /// <summary>
    /// This method will retry the asynchronous action represented by <paramref name="action"/>,
    /// waiting for <paramref name="delay"/> time after each retry. If a given retry returns a value 
    /// other than the default value of <typeparamref name="T"/>, this value is returned.
    /// </summary>
    /// <param name="action">the asynchronous action to retry</param>
    /// <param name="delay">the amount of time to wait between retries</param>
    /// <typeparam name="T">type of return value</typeparam>
    /// <returns>the return value of <paramref name="action"/></returns>
    public static Task<T?> RetryAsync<T>(Func<CancellationToken, Task<T>> action, TimeSpan delay, CancellationToken cancellationToken)
    {
        return RetryAsyncHelper(
            async cancellationToken =>
            {
                try
                {
                    return await action(cancellationToken);
                }
                catch (COMException)
                {
                    // Devenv can throw COMExceptions if it's busy when we make DTE calls.
                    return default;
                }
            },
            delay,
            cancellationToken);
    }

    private static T RetryHelper<T>(Func<CancellationToken, T> action, TimeSpan delay, int retryCount, CancellationToken cancellationToken)
    {
        for (var i = 0; true; i++)
        {
            var retval = action(cancellationToken);
            if (i == retryCount)
            {
                return retval;
            }

            if (!Equals(default(T), retval))
            {
                return retval;
            }

            Task.Delay(delay, cancellationToken).GetAwaiter().GetResult();
        }
    }

    private static async Task<T> RetryAsyncHelper<T>(Func<CancellationToken, Task<T>> action, TimeSpan delay, CancellationToken cancellationToken)
    {
        while (true)
        {
            var retval = await action(cancellationToken).ConfigureAwait(true);
            if (!Equals(default(T), retval))
            {
                return retval;
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(true);
        }
    }
}
