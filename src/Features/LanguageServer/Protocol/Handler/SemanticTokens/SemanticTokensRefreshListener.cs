// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Sends a notification from server->client indicating something has changed in the LSP workspace
    /// and thus semantic tokens should be re-requested. The client will then send a request to the
    /// server for refreshed tokens.
    /// </summary>
    internal class SemanticTokensRefreshListener : IDisposable
    {
        private readonly LspWorkspaceManager _lspWorkspaceManager;
        private readonly JsonRpc _jsonRpc;

        public SemanticTokensRefreshListener(LspWorkspaceManager lspWorkspaceManager, JsonRpc jsonRpc)
        {
            _lspWorkspaceManager = lspWorkspaceManager;
            _jsonRpc = jsonRpc;
            _lspWorkspaceManager.LspWorkspaceChanged += OnLspWorkspaceChanged;
        }

        private void OnLspWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
        {
            // TO-DO: Replace hardcoded string with const once LSP side is merged.
            _ = _jsonRpc.NotifyWithParameterObjectAsync("workspace/semanticTokens/refresh");
        }

        public void Dispose()
        {
            _lspWorkspaceManager.LspWorkspaceChanged -= OnLspWorkspaceChanged;
        }
    }
}
