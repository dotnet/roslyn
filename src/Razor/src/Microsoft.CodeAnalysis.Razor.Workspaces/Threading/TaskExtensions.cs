// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;

namespace Microsoft.CodeAnalysis.Razor.Threading;

internal static class TaskExtensions
{
    /// <summary>
    /// Asserts the <see cref="ValueTask"/> passed has already been completed.
    /// </summary>
    /// <remarks>
    /// This is useful for a specific case: sometimes you might be calling an API that is "sometimes" async, and you're
    /// calling it from a synchronous method where you know it should have completed synchronously. This is an easy
    /// way to assert that while silencing any compiler complaints.
    /// </remarks>
    public static void VerifyCompleted(this ValueTask task)
    {
        Assumed.True(task.IsCompleted);

        // Propagate any exceptions that may have been thrown.

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        task.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    /// <summary>
    /// Asserts the <see cref="ValueTask"/> passed has already been completed.
    /// </summary>
    /// <remarks>
    /// This is useful for a specific case: sometimes you might be calling an API that is "sometimes" async, and you're
    /// calling it from a synchronous method where you know it should have completed synchronously. This is an easy
    /// way to assert that while silencing any compiler complaints.
    /// </remarks>
    public static TResult VerifyCompleted<TResult>(this ValueTask<TResult> task)
    {
        Assumed.True(task.IsCompleted);

        // Propagate any exceptions that may have been thrown.

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        return task.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }
}
