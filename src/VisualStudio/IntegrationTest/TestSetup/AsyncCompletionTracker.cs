// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    [Export]
    [Shared]
    internal class AsyncCompletionTracker
    {
        private readonly IAsynchronousOperationListenerProvider _asynchronousOperationListenerProvider;
        private readonly IAsyncCompletionBroker _asyncCompletionBroker;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AsyncCompletionTracker(
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            IAsyncCompletionBroker asyncCompletionBroker)
        {
            // Store the listener provider, but delay accessing the listener itself since tracking could still be
            // disabled during the initialization sequence for integration tests.
            _asynchronousOperationListenerProvider = asynchronousOperationListenerProvider;
            _asyncCompletionBroker = asyncCompletionBroker;
        }

        internal void StartListening()
        {
            _asyncCompletionBroker.CompletionTriggered += HandleAsyncCompletionTriggered;
        }

        internal void StopListening()
        {
            _asyncCompletionBroker.CompletionTriggered -= HandleAsyncCompletionTriggered;
        }

        private void HandleAsyncCompletionTriggered(object sender, CompletionTriggeredEventArgs e)
        {
            var listener = _asynchronousOperationListenerProvider.GetListener(FeatureAttribute.CompletionSet);
            var token = listener.BeginAsyncOperation(nameof(IAsyncCompletionBroker.CompletionTriggered));

            e.CompletionSession.Dismissed += ReleaseToken;
            e.CompletionSession.ItemCommitted += ReleaseToken;

            _ = Task.Run(async () =>
                {
                    // AsyncCompletion might fire multiple ItemsUpdated events per keystroke typed, which means
                    // we could see the first ItemsUpdated event even though items don't change (but computation finished).
                    // If test attempts to assert state after seeing first event it would cause flakiness. 
                    // Use SelectedItemProvider to wait for all pending work to be completed.
                    var item = await ((ISelectedItemProvider)e.CompletionSession).GetSelectedItemAsync(GetSelectedItemOptions.WaitForContextAndComputation, CancellationToken.None);
                    Interlocked.Exchange<IAsyncToken?>(ref token, null)?.Dispose();
                });

            return;

            // Local function
            void ReleaseToken(object sender, EventArgs e)
                => Interlocked.Exchange<IAsyncToken?>(ref token, null)?.Dispose();
        }
    }
}
