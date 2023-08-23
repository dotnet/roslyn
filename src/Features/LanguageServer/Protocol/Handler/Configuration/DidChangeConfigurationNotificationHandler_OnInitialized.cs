// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration
{
    internal partial class DidChangeConfigurationNotificationHandler
    {
        public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
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

            _supportWorkspaceConfiguration = clientCapabilities?.Workspace?.Configuration ?? false;
            await RefreshOptionsAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
