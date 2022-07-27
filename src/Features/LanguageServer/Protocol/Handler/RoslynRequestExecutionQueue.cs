// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;

#nullable enable

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class RoslynRequestExecutionQueue : RequestExecutionQueue<RequestContext>
    {
        private ImmutableArray<string> _supportedLanguages;

        public RoslynRequestExecutionQueue(ImmutableArray<string> supportedLanguages, string serverKind, ILspServices services, ILspLogger logger)
            : base(serverKind, services, logger)
        {
            _supportedLanguages = supportedLanguages;
        }

        public override Task<RequestContext?> CreateRequestContextAsync(IQueueItem<RequestContext> queueItem, CancellationToken cancellationToken)
        {
            return RequestContext.CreateAsync(
                queueItem.RequiresLSPSolution,
                queueItem.MutatesSolutionState,
                queueItem.TextDocument,
                _serverKind,
                queueItem.ClientCapabilities,
                _supportedLanguages,
                _lspServices,
                _logger,
                queueCancellationToken: this.CancellationToken,
                requestCancellationToken: cancellationToken);
        }

        #region Test Accessor
        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly RoslynRequestExecutionQueue _queue;

            public TestAccessor(RoslynRequestExecutionQueue queue)
                => _queue = queue;

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

        #endregion
    }
}
