// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;
using LS = Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal abstract class AbstractLspClientServiceFactory : ICollaborationServiceFactory
    {
        protected abstract string LanguageSpecificProviderName { get; }

        protected abstract RoslynLSPClientLifeTimeService LspClientLifeTimeService { get; }

        public LS.ILanguageServerClient ActiveLanguageServerClient { get; private set; }

        public ServerCapabilities ServerCapabilities { get; private set; }

        public Task<ICollaborationService> CreateServiceAsync(CollaborationSession collaborationSession, CancellationToken cancellationToken)
        {
            var languageServerGuestService = (LS.ILanguageServerGuestService)collaborationSession.GetService(typeof(LS.ILanguageServerGuestService));

            collaborationSession.RemoteServicesChanged += (sender, e) =>
            {
                // VS will expose a roslyn LSP server.
                var roslynLspServerProviderName = LanguageServicesUtils.GetLanguageServerProviderServiceName(StringConstants.RoslynProviderName);
                // Newer versions of VS will expose language specific LSP servers for Roslyn.
                var languageSpecificLspServerProviderName = LanguageServicesUtils.GetLanguageServerProviderServiceName(LanguageSpecificProviderName);
                // VSCode will expose a "any" LSP provider and both support roslyn languages.
                var anyLspServerProviderName = LanguageServicesUtils.GetLanguageServerProviderServiceName(StringConstants.AnyProviderName);

                // For VS, Preferentially use the language specific server when it's available, otherwise fall back to the generic roslyn server.
                if (collaborationSession.RemoteServiceNames.Contains(languageSpecificLspServerProviderName))
                {
                    ActiveLanguageServerClient = languageServerGuestService.CreateLanguageServerClient(languageSpecificLspServerProviderName);
                }
                else if (collaborationSession.RemoteServiceNames.Contains(roslynLspServerProviderName))
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
                new LS.LanguageServerClientMetadata(
                    true,
                    JObject.FromObject(new ServerCapabilities
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
                    })));

            languageServerGuestService.RegisterClientMetadata(
                new string[] { StringConstants.CSharpLspContentTypeName, StringConstants.VBLspLanguageName },
                new LS.LanguageServerClientMetadata(
                    true,
                    JObject.FromObject(new ServerCapabilities
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
                    })));

            var lifeTimeService = LspClientLifeTimeService;
            lifeTimeService.Disposed += (s, e) =>
            {
                ActiveLanguageServerClient?.Dispose();
                ActiveLanguageServerClient = null;
            };

            return Task.FromResult<ICollaborationService>(lifeTimeService);
        }

        protected abstract class RoslynLSPClientLifeTimeService : ICollaborationService, IDisposable
        {
            public event EventHandler Disposed;

            public void Dispose()
            {
                Disposed?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Ensures the client has been fully initialized by making an initialize request
        /// and retrieving the server capabilities.
        /// TODO - This should be called in <see cref="CreateServiceAsync(CollaborationSession, CancellationToken)"/>
        /// and made private once LiveShare fixes the race in client creation.
        /// https://devdiv.visualstudio.com/DevDiv/_workitems/edit/964288
        /// </summary>
        public async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (ActiveLanguageServerClient == null)
            {
                return;
            }

            // Only request the server capabilities if we don't already have them.
            if (ServerCapabilities == null)
            {
                var initializeRequest = new LS.LspRequest<InitializeParams, InitializeResult>(Methods.InitializeName);
                var intializeResult = await ActiveLanguageServerClient.RequestAsync(initializeRequest, new InitializeParams(), cancellationToken).ConfigureAwait(false);

                var serverCapabilities = intializeResult?.Capabilities;
                if (serverCapabilities != null && LanguageServicesUtils.TryParseJson<RoslynExperimentalCapabilities>(serverCapabilities?.Experimental, out var roslynExperimentalCapabilities))
                {
                    serverCapabilities.Experimental = roslynExperimentalCapabilities;
                }

                ServerCapabilities = serverCapabilities;
            }
        }
    }

    [Export]
    [ExportCollaborationService(typeof(CSharpLSPClientLifeTimeService),
                                Scope = SessionScope.Guest,
                                Role = ServiceRole.LocalService,
                                Features = "LspServices",
                                CreationPriority = (int)ServiceRole.LocalService + 2000)]
    internal class CSharpLspClientServiceFactory : AbstractLspClientServiceFactory
    {
        protected override string LanguageSpecificProviderName => StringConstants.CSharpProviderName;

        protected override RoslynLSPClientLifeTimeService LspClientLifeTimeService => new CSharpLSPClientLifeTimeService();

        private class CSharpLSPClientLifeTimeService : RoslynLSPClientLifeTimeService
        {
        }
    }

    [Export]
    [ExportCollaborationService(typeof(VisualBasicLSPClientLifeTimeService),
                                Scope = SessionScope.Guest,
                                Role = ServiceRole.LocalService,
                                Features = "LspServices",
                                CreationPriority = (int)ServiceRole.LocalService + 2000)]
    internal class VisualBasicLspClientServiceFactory : AbstractLspClientServiceFactory
    {
        protected override string LanguageSpecificProviderName => StringConstants.VisualBasicProviderName;

        protected override RoslynLSPClientLifeTimeService LspClientLifeTimeService => new VisualBasicLSPClientLifeTimeService();

        private class VisualBasicLSPClientLifeTimeService : RoslynLSPClientLifeTimeService
        {
        }
    }

    [Export]
    [ExportCollaborationService(typeof(TypeScriptLSPClientLifeTimeService),
                                Scope = SessionScope.Guest,
                                Role = ServiceRole.LocalService,
                                Features = "LspServices",
                                CreationPriority = (int)ServiceRole.LocalService + 2000)]
    internal class TypeScriptLspClientServiceFactory : AbstractLspClientServiceFactory
    {
        protected override string LanguageSpecificProviderName => StringConstants.TypeScriptProviderName;

        protected override RoslynLSPClientLifeTimeService LspClientLifeTimeService => new TypeScriptLSPClientLifeTimeService();

        private class TypeScriptLSPClientLifeTimeService : RoslynLSPClientLifeTimeService
        {
        }
    }
}
