// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LiveShare;
using Newtonsoft.Json.Linq;
using Roslyn.LanguageServer.Protocol;
using LS = Microsoft.VisualStudio.LiveShare.LanguageServices;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal abstract class AbstractLspClientServiceFactory : ICollaborationServiceFactory
    {
        protected abstract string LanguageSpecificProviderName { get; }

        protected abstract RoslynLSPClientLifeTimeService LspClientLifeTimeService { get; }

        public LS.ILanguageServerClient ActiveLanguageServerClient { get; private set; }

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
                [StringConstants.TypeScriptLanguageName],
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
                => Disposed?.Invoke(this, null);
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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpLspClientServiceFactory()
        {
        }

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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualBasicLspClientServiceFactory()
        {
        }

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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptLspClientServiceFactory()
        {
        }

        protected override string LanguageSpecificProviderName => StringConstants.TypeScriptProviderName;

        protected override RoslynLSPClientLifeTimeService LspClientLifeTimeService => new TypeScriptLSPClientLifeTimeService();

        private class TypeScriptLSPClientLifeTimeService : RoslynLSPClientLifeTimeService
        {
        }
    }
}
