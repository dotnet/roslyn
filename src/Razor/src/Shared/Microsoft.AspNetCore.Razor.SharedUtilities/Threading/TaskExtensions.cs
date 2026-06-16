// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Threading;

internal static class TaskExtensions
{
    /// <summary>
    /// Asserts the <see cref="Task"/> passed has already been completed.
    /// </summary>
    /// <remarks>
    /// This is useful for a specific case: sometimes you might be calling an API that is "sometimes" async, and you're
    /// calling it from a synchronous method where you know it should have completed synchronously. This is an easy
    /// way to assert that while silencing any compiler complaints.
    /// </remarks>
    public static void VerifyCompleted(this Task task)
    {
        Assumed.True(task.IsCompleted);

        // Propagate any exceptions that may have been thrown.
        task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asserts the <see cref="Task"/> passed has already been completed.
    /// </summary>
    /// <remarks>
    /// This is useful for a specific case: sometimes you might be calling an API that is "sometimes" async, and you're
    /// calling it from a synchronous method where you know it should have completed synchronously. This is an easy
    /// way to assert that while silencing any compiler complaints.
    /// </remarks>
    public static TResult VerifyCompleted<TResult>(this Task<TResult> task)
    {
        Assumed.True(task.IsCompleted);

        // Propagate any exceptions that may have been thrown.
        return task.GetAwaiter().GetResult();
    }
}
