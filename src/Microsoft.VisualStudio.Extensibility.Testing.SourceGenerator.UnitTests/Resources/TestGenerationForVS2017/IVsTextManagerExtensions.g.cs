// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Threading;

    internal static partial class IVsTextManagerExtensions
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
}
