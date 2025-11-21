// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Threading;

    internal static partial class IVsTextViewExtensions
    {
        public static async Task<IWpfTextViewHost> GetTextViewHostAsync(this IVsTextView textView, JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ErrorHandler.ThrowOnFailure(((IVsUserData)textView).GetData(DefGuidList.guidIWpfTextViewHost, out var wpfTextViewHost));
            return (IWpfTextViewHost)wpfTextViewHost;
        }
    }
}
