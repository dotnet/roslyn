// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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
        private readonly IInterceptionMiddleLayer? _interceptionMiddleLayer;

        public SemanticTokensRefreshListener(
            LspWorkspaceManager lspWorkspaceManager,
            JsonRpc jsonRpc,
            IInterceptionMiddleLayer? interceptionMiddleLayer)
        {
            _lspWorkspaceManager = lspWorkspaceManager;
            _jsonRpc = jsonRpc;
            _interceptionMiddleLayer = interceptionMiddleLayer;

            _lspWorkspaceManager.LspWorkspaceChanged += OnLspWorkspaceChanged;
        }

        private void OnLspWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
        {
            // TO-DO: Replace hardcoded string with const once LSP side is merged.
            _ = _jsonRpc.NotifyWithParameterObjectAsync("workspace/semanticTokens/refresh");

            if (_interceptionMiddleLayer is not null && _interceptionMiddleLayer.CanHandle("workspace/semanticTokens/refresh"))
            {
                _ = _interceptionMiddleLayer.HandleNotificationAsync(
                    methodName: "workspace/semanticTokens/refresh",
                    methodParam: JToken.Parse("{}"),
                    sendNotification: SendNotification);
            }

            static Task SendNotification(JToken token) => Task.CompletedTask;
        }

        public void Dispose()
        {
            _lspWorkspaceManager.LspWorkspaceChanged -= OnLspWorkspaceChanged;
        }
    }
}
