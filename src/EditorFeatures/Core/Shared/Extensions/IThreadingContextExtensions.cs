// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static class IThreadingContextExtensions
{
    public static void ThrowIfNotOnUIThread(this IThreadingContext threadingContext)
        => Contract.ThrowIfFalse(threadingContext.JoinableTaskContext.IsOnMainThread);

    public static void ThrowIfNotOnBackgroundThread(this IThreadingContext threadingContext)
        => Contract.ThrowIfTrue(threadingContext.JoinableTaskContext.IsOnMainThread);
}
