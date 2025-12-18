// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;

    [TestService]
    internal partial class EditorInProcess
    {
        public async Task<IWpfTextView> GetActiveTextViewAsync(CancellationToken cancellationToken)
            => (await GetActiveTextViewHostAsync(cancellationToken)).TextView;

        private async Task<IWpfTextViewHost> GetActiveTextViewHostAsync(CancellationToken cancellationToken)
        {
            var activeVsTextView = await GetActiveVsTextViewAsync(cancellationToken);
            return await activeVsTextView.GetTextViewHostAsync(JoinableTaskFactory, cancellationToken);
        }

        private async Task<IVsTextView> GetActiveVsTextViewAsync(CancellationToken cancellationToken)
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text
            // view.
            await WaitForApplicationIdleAsync(cancellationToken);

            var vsTextManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
            return await vsTextManager.GetActiveViewAsync(JoinableTaskFactory, cancellationToken);
        }
    }
}
