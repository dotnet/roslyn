﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare
{
    [Export]
    [ExportCollaborationService(typeof(RoslynLSPClientLifeTimeService),
                                Scope = SessionScope.Guest,
                                Role = ServiceRole.LocalService,
                                Features = "LspServices",
                                CreationPriority = (int)ServiceRole.LocalService + 2000)]
    internal class RoslynLspClientServiceFactory : ICollaborationServiceFactory
    {
        private const string RoslynProviderName = "Roslyn";
        private const string AnyProviderName = "any";

        public ILanguageServerClient ActiveLanguageServerClient { get; private set; }

        public Task<ICollaborationService> CreateServiceAsync(CollaborationSession collaborationSession, CancellationToken cancellationToken)
        {
            var languageServerGuestService = (ILanguageServerGuestService)collaborationSession.GetService(typeof(ILanguageServerGuestService));

            collaborationSession.RemoteServicesChanged += (sender, e) =>
            {
                // VS will expose a roslyn LSP server and VSCode will expose a "any" LSP provider and both support roslyn languages.
                var roslynLspServerProviderName = LanguageServicesUtils.GetLanguageServerProviderServiceName(RoslynProviderName);
                var anyLspServerProviderName = LanguageServicesUtils.GetLanguageServerProviderServiceName(AnyProviderName);

                if (collaborationSession.RemoteServiceNames.Contains(roslynLspServerProviderName))
                {
                    ActiveLanguageServerClient = languageServerGuestService.CreateLanguageServerClient(roslynLspServerProviderName);
                }
                else if (collaborationSession.RemoteServiceNames.Contains(anyLspServerProviderName))
                {
                    ActiveLanguageServerClient = languageServerGuestService.CreateLanguageServerClient(anyLspServerProviderName);
                }
            };

            // Register Roslyn supported capabilities
            languageServerGuestService.RegisterClientMetadata(
                new string[] { StringConstants.TypeScriptLanguageName },
                new LanguageServerClientMetadata(
                    true,
                    new ServerCapabilities
                    {
                        // Uses Roslyn client.
                        DocumentSymbolProvider = true,

                        // Uses LSP SDK client.
                        DocumentLinkProvider = null,
                        RenameProvider = false,
                        DocumentOnTypeFormattingProvider = null,
                        DocumentRangeFormattingProvider = false,
                        DocumentFormattingProvider = false,
                        CodeLensProvider = null,
                        CodeActionProvider = false,
                        ExecuteCommandProvider = null,
                        WorkspaceSymbolProvider = false,
                        DocumentHighlightProvider = false,
                        ReferencesProvider = false,
                        DefinitionProvider = false,
                        SignatureHelpProvider = null,
                        CompletionProvider = null,
                        HoverProvider = false,
                        TextDocumentSync = null,
                    }));

            languageServerGuestService.RegisterClientMetadata(
                new string[] { StringConstants.CSharpLspContentTypeName, StringConstants.VBLspLanguageName },
                new LanguageServerClientMetadata(
                    true,
                    new ServerCapabilities
                    {
                        // Uses Roslyn client.
                        DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions(),
                        DocumentRangeFormattingProvider = true,
                        DocumentFormattingProvider = true,
                        DocumentSymbolProvider = true,
                        CodeActionProvider = true,
                        ExecuteCommandProvider = new ExecuteCommandOptions(),
                        DocumentHighlightProvider = true,
                        ReferencesProvider = true,
                        DefinitionProvider = true,
                        SignatureHelpProvider = new SignatureHelpOptions() { },
                        CompletionProvider = new CompletionOptions(),
                        ImplementationProvider = true,

                        // Uses LSP SDK client.
                        DocumentLinkProvider = null,
                        RenameProvider = false,
                        CodeLensProvider = null,
                        WorkspaceSymbolProvider = false,
                        HoverProvider = false,
                        TextDocumentSync = null,
                    }));

            var lifeTimeService = new RoslynLSPClientLifeTimeService();
            lifeTimeService.Disposed += (s, e) =>
            {
                ActiveLanguageServerClient?.Dispose();
                ActiveLanguageServerClient = null;
            };

            return Task.FromResult<ICollaborationService>(lifeTimeService);
        }

        private class RoslynLSPClientLifeTimeService : ICollaborationService, IDisposable
        {
            public event EventHandler Disposed;

            public void Dispose()
            {
                Disposed?.Invoke(this, null);
            }
        }
    }
}
