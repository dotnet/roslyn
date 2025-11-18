// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.LanguageServer;

[ExportCSharpVisualBasicLspServiceFactory(typeof(OnInitialized)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorDynamicDocumentSyncRegistration(IGlobalOptionService globalOptionService) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new OnInitialized(globalOptionService);

    public sealed class OnInitialized(IGlobalOptionService globalOptionService) : IOnInitialized, ILspService
    {
        public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            // Hot reload only works in devkit scenarios. Without, there is no need to register for dynamic document sync.
            if (!globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures))
            {
                return;
            }

            var languageServerManager = context.GetRequiredLspService<IClientLanguageServerManager>();

            // We know devkit is enabled, but we need to check cohosting too. Cohosting will register for document sync, and if we do
            // it here as well, VS Code will send us duplicate open/close/change events for the same file, corrupting our documents.
            if (clientCapabilities.Workspace?.Configuration == true &&
                await IsCohostingEnabledAsync(languageServerManager, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            // If dynamic registration for text document synchronization is supported, register for .razor and .cshtml files
            // so that they are up to date for hot reload scenarios rather than depending on the file watchers to update
            // the contents.
            if (clientCapabilities.TextDocument?.Synchronization?.DynamicRegistration is true)
            {
                var documentFilters = new[] { new DocumentFilter() { Pattern = "**/*.{razor, cshtml}", Language = "aspnetcorerazor" } };
                var registrationOptions = new TextDocumentRegistrationOptions()
                {
                    DocumentSelector = documentFilters
                };

                await languageServerManager.SendRequestAsync(Methods.ClientRegisterCapabilityName,
                    new RegistrationParams()
                    {
                        Registrations = [
                            new()
                            {
                                Id = Guid.NewGuid().ToString(), // No need to save this for unregistering
                                Method = Methods.TextDocumentDidOpenName,
                                RegisterOptions = registrationOptions
                            },
                            new()
                            {
                                Id = Guid.NewGuid().ToString(), // No need to save this for unregistering
                                Method = Methods.TextDocumentDidChangeName,
                                RegisterOptions = new TextDocumentChangeRegistrationOptions()
                                {
                                    DocumentSelector = documentFilters,
                                    SyncKind = TextDocumentSyncKind.Incremental
                                }
                            },
                            new()
                            {
                                Id = Guid.NewGuid().ToString(), // No need to save this for unregistering
                                Method = Methods.TextDocumentDidCloseName,
                                RegisterOptions = registrationOptions
                            }
                        ]
                    },
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<bool> IsCohostingEnabledAsync(IClientLanguageServerManager languageServerManager, CancellationToken cancellationToken)
        {
            var configurationParams = new ConfigurationParams()
            {
                Items = [
                    // Roslyn's typescript config handler will convert underscores to camelcase, so this checking
                    // the 'razor.languageServer.cohostingEnabled' option
                    new ConfigurationItem { Section = "razor.language_server.cohosting_enabled" },
                ]
            };

            var options = await languageServerManager.SendRequestAsync<ConfigurationParams, JsonArray>(
                Methods.WorkspaceConfigurationName,
                configurationParams,
                cancellationToken).ConfigureAwait(false);

            return options is [{ } result] &&
                result.ToString() == "true";
        }
    }
}

