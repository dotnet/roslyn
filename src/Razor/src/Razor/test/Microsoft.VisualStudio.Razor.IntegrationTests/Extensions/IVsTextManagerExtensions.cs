// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.IntegrationTests.Extensions;

internal static class IVsTextManagerExtensions
{
    public static Task<IVsTextView> GetActiveViewAsync(this IVsTextManager textManager, JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
        => textManager.GetActiveViewAsync(joinableTaskFactory, mustHaveFocus: true, buffer: null, cancellationToken);

    public static async Task<IVsTextView> GetActiveViewAsync(this IVsTextManager textManager, JoinableTaskFactory joinableTaskFactory, bool mustHaveFocus, IVsTextBuffer? buffer, CancellationToken cancellationToken)
    {
        await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        ErrorHandler.ThrowOnFailure(textManager.GetActiveView(fMustHaveFocus: mustHaveFocus ? 1 : 0, pBuffer: buffer, ppView: out var vsTextView));

        return vsTextView;
    }
}
