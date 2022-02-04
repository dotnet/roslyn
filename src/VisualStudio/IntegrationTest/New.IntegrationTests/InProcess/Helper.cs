// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal static class Helper
    {
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
}
