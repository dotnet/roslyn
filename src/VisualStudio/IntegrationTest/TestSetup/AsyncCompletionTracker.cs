// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

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
            e.CompletionSession.ItemsUpdated += ReleaseToken;

            return;

            // Local function
            void ReleaseToken(object sender, EventArgs e)
                => Interlocked.Exchange(ref token, null)?.Dispose();
        }
    }
}
