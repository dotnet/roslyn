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
        /// <returns>A <see cref="Task"/> representing the asynchronous delay.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="delay"/> represents a negative time interval other than <c>TimeSpan.FromMilliseconds(-1)</c>.</para>
        /// <para>-or-</para>
        /// <para>The <paramref name="delay"/> argument's <see cref="TimeSpan.TotalMilliseconds"/> property is greater than <see cref="int.MaxValue"/>.</para>
        /// </exception>
        /// <exception cref="OperationCanceledException">The delay has been canceled, either in response to a
        /// cancellation request from <paramref name="cancellationToken"/> or in response to a request to expedite an
        /// operation. Callers may distinguish between the two by checking the state of
        /// <paramref name="cancellationToken"/> in the exception handler.</exception>
        Task Delay(TimeSpan delay, CancellationToken cancellationToken);
    }
}
