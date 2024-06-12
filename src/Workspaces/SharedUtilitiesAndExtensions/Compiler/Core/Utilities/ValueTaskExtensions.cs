// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal static class ValueTaskExtensions
    {
        /// <summary>
        /// Asserts the <see cref="ValueTask"/> passed has already been completed.
        /// </summary>
        /// <remarks>
        /// This is useful for a specific case: sometimes you might be calling an API that is "sometimes" async, and you're
        /// calling it from a synchronous method where you know it should have completed synchronously. This is an easy
        /// way to assert that while silencing any compiler complaints.
        /// </remarks>
        public static T VerifyCompleted<T>(this ValueTask<T> task, string message = "ValueTask should have already been completed")
        {
            Contract.ThrowIfFalse(task.IsCompleted, message);

            // Propagate any exceptions that may have been thrown.
            return task.GetAwaiter().GetResult();
        }
    }
}
