// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public
{
    internal sealed partial class PublicWorkspacePullDiagnosticsHandler : IOnInitialized
    {
        public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            var sources = _diagnosticSourceManager.GetSourceNames(isDocument: false);
            await _clientLanguageServerManager.SendRequestAsync(
                methodName: Methods.ClientRegisterCapabilityName,
                @params: new RegistrationParams()
                {
                    Registrations = sources.Select(FromSourceName).ToArray()
                },
                cancellationToken).ConfigureAwait(false);

            Registration FromSourceName(string sourceName)
            => new()
            {
                Id = sourceName,
                Method = Methods.WorkspaceDiagnosticName,
                RegisterOptions = new DiagnosticRegistrationOptions { Identifier = sourceName }
            };
        }
    }
}
