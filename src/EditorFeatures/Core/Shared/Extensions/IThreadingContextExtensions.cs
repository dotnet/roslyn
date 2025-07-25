// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static class IThreadingContextExtensions
{
    extension(IThreadingContext threadingContext)
    {
        public void ThrowIfNotOnUIThread()
        => Contract.ThrowIfFalse(threadingContext.JoinableTaskContext.IsOnMainThread);

        public void ThrowIfNotOnBackgroundThread()
            => Contract.ThrowIfTrue(threadingContext.JoinableTaskContext.IsOnMainThread);
    }
}
