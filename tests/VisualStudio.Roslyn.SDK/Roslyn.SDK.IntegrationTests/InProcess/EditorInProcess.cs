// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.CodeAnalysis.Testing.InProcess
{
    public class EditorInProcess : InProcComponent
    {
        internal static readonly Guid IWpfTextViewId = new Guid("8C40265E-9FDB-4F54-A0FD-EBB72B7D0476");

        public EditorInProcess(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task<IWpfTextView> GetActiveTextViewAsync()
            => (await GetActiveTextViewHostAsync()).TextView;

        private async Task<IVsTextView> GetActiveVsTextViewAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var vsTextManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>();

            ErrorHandler.ThrowOnFailure(vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var vsTextView));

            return vsTextView;
        }

        private async Task<IWpfTextViewHost> GetActiveTextViewHostAsync()
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text
            // view.
            await WaitForApplicationIdleAsync(CancellationToken.None);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var activeVsTextView = (IVsUserData)await GetActiveVsTextViewAsync();

            ErrorHandler.ThrowOnFailure(activeVsTextView.GetData(IWpfTextViewId, out var wpfTextViewHost));

            return (IWpfTextViewHost)wpfTextViewHost;
        }

        public async Task SetTextAsync(string text)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var textSnapshot = view.TextSnapshot;
            var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
            view.TextBuffer.Replace(replacementSpan, text);
        }
    }
}
