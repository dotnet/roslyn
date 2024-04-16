// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

internal static class IThreadingContextExtensions
{
    /// <summary>
    /// Returns true if any keyboard or mouse button input is pending on the message queue.
    /// </summary>
    public static bool IsInputPending()
    {
        // The code below invokes into user32.dll, which is not available in non-Windows.
        if (PlatformInformation.IsUnix)
        {
            return false;
        }

        // The return value of GetQueueStatus is HIWORD:LOWORD.
        // A non-zero value in HIWORD indicates some input message in the queue.
        var result = NativeMethods.GetQueueStatus(NativeMethods.QS_INPUT);

        const uint InputMask = NativeMethods.QS_INPUT | (NativeMethods.QS_INPUT << 16);
        return (result & InputMask) != 0;
    }

    public static Task InvokeBelowInputPriorityAsync(this IThreadingContext threadingContext, Action action, CancellationToken cancellationToken = default)
    {
        if (threadingContext.JoinableTaskContext.IsOnMainThread && !IsInputPending())
        {
            // Optimize to inline the action if we're already on the foreground thread
            // and there's no pending user input.
            action();

            return Task.CompletedTask;
        }
        else
        {
            return Task.Factory.SafeStartNewFromAsync(
                async () =>
                {
                    await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                    action();
                },
                cancellationToken,
                TaskScheduler.Default);
        }
    }
}
