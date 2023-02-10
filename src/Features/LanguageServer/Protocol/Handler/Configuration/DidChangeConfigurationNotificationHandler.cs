// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration
{
    internal class DidChangeConfigurationNotificationHandler : ILspServiceNotificationHandler<LSP.DidChangeConfigurationParams>, IOnInitialized
    {
        private readonly IClientLanguageServerManager _clientLanguageServerManager;
        private readonly Guid _registrationId;

        public DidChangeConfigurationNotificationHandler(IClientLanguageServerManager clientLanguageServerManager)
        {
            _clientLanguageServerManager = clientLanguageServerManager;
            _registrationId = Guid.NewGuid();
        }

        public bool MutatesSolutionState => false;

        public bool RequiresLSPSolution => true;

        [LanguageServerEndpoint(LSP.Methods.WorkspaceDidChangeConfigurationName)]
        public async Task HandleNotificationAsync(DidChangeConfigurationParams request, RequestContext requestContext, CancellationToken cancellationToken)
        {
            var configParams = new ConfigurationParams()
            {
                Items = new[]
                {
                    new ConfigurationItem()
                    {
                        Section = "dotnet.server.trace"
                    },
                }
            };

            var option = await _clientLanguageServerManager.SendRequestAsync<ConfigurationParams, JArray>(
                LSP.Methods.WorkspaceConfigurationName, configParams, cancellationToken).ConfigureAwait(false);
        }

        public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            if (clientCapabilities?.Workspace?.DidChangeConfiguration?.DynamicRegistration is true)
            {
                await _clientLanguageServerManager.SendRequestAsync<RegistrationParams, JObject>(
                    methodName: Methods.ClientRegisterCapabilityName,
                    @params: new RegistrationParams()
                    {
                        Registrations = new[]
                        {
                            new Registration { Id = _registrationId.ToString(), Method = Methods.WorkspaceDidChangeConfigurationName, RegisterOptions = null }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
