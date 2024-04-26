// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;
// A document diagnostic partial report is defined as having the first literal send = DocumentDiagnosticReport (aka changed / unchanged) followed
// by n DocumentDiagnosticPartialResult literals.
// See https://github.com/microsoft/vscode-languageserver-node/blob/main/protocol/src/common/proposed.diagnostics.md#textDocument_diagnostic

internal sealed partial class PublicDocumentPullDiagnosticsHandler : IOnInitialized
{
    public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        // Dynamically register for all of our document diagnostic sources.
        if (clientCapabilities?.TextDocument?.Diagnostic?.DynamicRegistration is true)
        {
            // TODO: Hookup an option changed handler for changes to BackgroundAnalysisScopeOption
            //       to dynamically register/unregister the non-local document diagnostic source.

            // Task diagnostics shouldn't be reported through VSCode (it has its own task stuff). Additional cleanup needed.
            var sources = DiagnosticSourceManager.GetDocumentSourceProviderNames(clientCapabilities);
            var registrations = sources.Select(FromSourceName).ToArray();
            await _clientLanguageServerManager.SendRequestAsync(
                methodName: Methods.ClientRegisterCapabilityName,
                @params: new RegistrationParams()
                {
                    Registrations = registrations
                },
                cancellationToken).ConfigureAwait(false);
        }

        Registration FromSourceName(string sourceName)
        {
            return new()
            {
                Id = Guid.NewGuid().ToString(),
                Method = Methods.TextDocumentDiagnosticName,
                RegisterOptions = new DiagnosticRegistrationOptions { Identifier = sourceName, InterFileDependencies = true, WorkspaceDiagnostics = false }
            };
        }
    }
}
