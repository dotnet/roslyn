// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        /// <summary>
        /// Associates LSP document URIs with the roslyn source text containing the LSP document text.
        /// Called via <see cref="DidOpenHandler"/>, <see cref="DidChangeHandler"/> and <see cref="DidCloseHandler"/>
        /// </summary>
        internal interface IDocumentChangeTracker
        {
            void StartTracking(Uri documentUri, SourceText initialText);
            void UpdateTrackedDocument(Uri documentUri, SourceText text);
            void StopTracking(Uri documentUri);
        }

        private class NonMutatingDocumentChangeTracker : IDocumentChangeTracker
        {
            public void StartTracking(Uri documentUri, SourceText initialText)
            {
                throw new InvalidOperationException("Mutating documents not allowed in a non-mutating request handler");
            }

            public void StopTracking(Uri documentUri)
            {
                throw new InvalidOperationException("Mutating documents not allowed in a non-mutating request handler");
            }

            public void UpdateTrackedDocument(Uri documentUri, SourceText text)
            {
                throw new InvalidOperationException("Mutating documents not allowed in a non-mutating request handler");
            }
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly RequestExecutionQueue _queue;

            public TestAccessor(RequestExecutionQueue queue)
                => _queue = queue;

            public ImmutableArray<SourceText> GetTrackedTexts()
                => _queue._lspWorkspaceManager.GetTrackedLspText().Select(i => i.Value).ToImmutableArray();

            public LspWorkspaceManager GetLspWorkspaceManager() => _queue._lspWorkspaceManager;

            public bool IsComplete() => _queue._queue.IsCompleted && _queue._queue.IsEmpty;

            public async Task WaitForProcessingToStopAsync()
            {
                await _queue._queueProcessingTask.ConfigureAwait(false);
            }

            /// <summary>
            /// Test only method to validate that remaining items in the queue are cancelled.
            /// This directly mutates the queue in an unsafe way, so ensure that all relevant queue operations
            /// are done before calling.
            /// </summary>
            public async Task<bool> AreAllItemsCancelledUnsafeAsync()
            {
                while (!_queue._queue.IsEmpty)
                {
                    var (_, cancellationToken) = await _queue._queue.DequeueAsync().ConfigureAwait(false);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
