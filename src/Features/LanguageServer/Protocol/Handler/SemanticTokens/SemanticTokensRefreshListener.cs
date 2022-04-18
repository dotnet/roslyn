// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Sends a notification from server->client indicating something has changed in the LSP workspace.
    /// The client will then send a request to the server for refreshed tokens.
    /// </summary>
    internal class SemanticTokensRefreshListener : IDisposable
    {
        private readonly LspWorkspaceManager _lspWorkspaceManager;
        private readonly JsonRpc _jsonRpc;
        private readonly AsyncBatchingWorkQueue _workQueue;

        public SemanticTokensRefreshListener(
            LspWorkspaceManager lspWorkspaceManager,
            JsonRpc jsonRpc,
            IAsynchronousOperationListener listener,
            CancellationToken cancellationToken)
        {
            _lspWorkspaceManager = lspWorkspaceManager;
            _jsonRpc = jsonRpc;
            _lspWorkspaceManager.LspWorkspaceChanged += OnLspWorkspaceChanged;

            // Only send a refresh notification to the client every 2s (if needed)
            // in order to avoid sending too many notifications at once.
            _workQueue = new AsyncBatchingWorkQueue(
                delay: TimeSpan.FromMilliseconds(2000),
                processBatchAsync: SendSemanticTokensRefreshNotificationAsync,
                asyncListener: listener,
                cancellationToken);
        }

        private void OnLspWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
        {
            _workQueue.AddWork();
        }

        public void Dispose()
        {
            _lspWorkspaceManager.LspWorkspaceChanged -= OnLspWorkspaceChanged;
        }

        private ValueTask SendSemanticTokensRefreshNotificationAsync(CancellationToken cancellationToken)
        {
            // TO-DO: Replace hardcoded string with const once LSP side is merged.
            _ = _jsonRpc.NotifyAsync("workspace/semanticTokens/refresh");
            return new ValueTask();
        }
    }
}
