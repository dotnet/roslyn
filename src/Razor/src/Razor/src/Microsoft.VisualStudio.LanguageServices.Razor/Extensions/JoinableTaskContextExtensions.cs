// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.Extensions;

internal static class JoinableTaskContextExtensions
{
    public static void AssertUIThread(this JoinableTaskContext joinableTaskContext, [CallerMemberName] string? caller = null)
    {
        if (!joinableTaskContext.IsOnMainThread)
        {
            caller = caller is null ? "The method" : $"'{caller}'";
            throw new InvalidOperationException($"{caller} must be called on the UI thread.");
        }
    }

    public static void AssertUIThread(this JoinableTaskFactory jtf, [CallerMemberName] string? caller = null)
    {
        jtf.Context.AssertUIThread(caller);
    }
}
