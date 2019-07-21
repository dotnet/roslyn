// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal interface IExpeditableDelaySource
    {
        /// <summary>
        /// Creates a task that will complete after a time delay, but can be expedited if an operation is waiting for
        /// the task to complete.
        /// </summary>
        /// <param name="delay">The time to wait before completing the returned task, or <c>TimeSpan.FromMilliseconds(-1)</c> to wait indefinitely.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns><see langword="true"/> if the delay compeleted normally; otherwise, <see langword="false"/> if the delay completed due to a request to expedite the delay.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="delay"/> represents a negative time interval other than <c>TimeSpan.FromMilliseconds(-1)</c>.</para>
        /// <para>-or-</para>
        /// <para>The <paramref name="delay"/> argument's <see cref="TimeSpan.TotalMilliseconds"/> property is greater than <see cref="int.MaxValue"/>.</para>
        /// </exception>
        /// <exception cref="OperationCanceledException">The delay has been canceled.</exception>
        Task<bool> Delay(TimeSpan delay, CancellationToken cancellationToken);
    }
}
