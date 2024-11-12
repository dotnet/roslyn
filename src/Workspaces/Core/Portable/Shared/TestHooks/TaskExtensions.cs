// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.TestHooks;

internal static partial class TaskExtensions
{
    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
    public static Task CompletesAsyncOperation(this Task task, IAsyncToken asyncToken)
    {
        if (asyncToken is AsynchronousOperationListener.DiagnosticAsyncToken diagnosticToken)
        {
            diagnosticToken.AssociateWithTask(task);
        }

        return task.CompletesTrackingOperation(asyncToken);
    }

    [PerformanceSensitive(
        "https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html",
        AllowCaptures = false)]
    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
    public static Task CompletesTrackingOperation(this Task task, IDisposable token)
    {
        if (token == null || token == EmptyAsyncToken.Instance)
        {
            return task;
        }

        return CompletesTrackingOperationSlow(task, token);

        static Task CompletesTrackingOperationSlow(Task task, IDisposable token)
        {
            return task.SafeContinueWith(
                t => token.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
