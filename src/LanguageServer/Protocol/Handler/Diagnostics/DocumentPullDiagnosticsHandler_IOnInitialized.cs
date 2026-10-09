// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
// A document diagnostic partial report is defined as having the first literal send = DocumentDiagnosticReport (aka changed / unchanged) followed
// by n DocumentDiagnosticPartialResult literals.
// See https://github.com/microsoft/vscode-languageserver-node/blob/main/protocol/src/common/proposed.diagnostics.md#textDocument_diagnostic

internal sealed partial class DocumentPullDiagnosticsHandler : IOnInitialized
{
    public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        // Dynamically register for all relevant diagnostic sources.
        if (clientCapabilities?.TextDocument?.Diagnostic?.DynamicRegistration is true)
        {
            // TODO: Hookup an option changed handler for changes to BackgroundAnalysisScopeOption
            //       to dynamically register/unregister the non-local document diagnostic source.

            // All diagnostic sources have to be registered under the document pull method name,
            // See https://github.com/microsoft/language-server-protocol/issues/1723
            //
            // Additionally if a source name is used by both document and workspace pull (e.g. enc)
            // we don't want to send two registrations, instead we should send a single registration
            // that also sets the workspace pull option.
            //
            // DiagnosticSourceManager merges metadata for source names used by more than one provider.
            var registrationOptions = _diagnosticSourceManager.GetDiagnosticRegistrationOptions(clientCapabilities);
            var registrations = registrationOptions.SelectAsArray(
                static options => new Registration
                {
                    Id = Guid.NewGuid().ToString(),
                    Method = Methods.TextDocumentDiagnosticName,
                    RegisterOptions = options,
                });
            await _clientLanguageServerManager.SendRequestAsync(
                methodName: Methods.ClientRegisterCapabilityName,
                @params: new RegistrationParams()
                {
                    Registrations = [.. registrations]
                },
                cancellationToken).ConfigureAwait(false);
        }
    }
}
