// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;
// A document diagnostic partial report is defined as having the first literal send = DocumentDiagnosticReport (aka changed / unchanged) followed
// by n DocumentDiagnosticPartialResult literals.
// See https://github.com/microsoft/vscode-languageserver-node/blob/main/protocol/src/common/proposed.diagnostics.md#textDocument_diagnostic

internal sealed partial class PublicDocumentPullDiagnosticsHandler : IOnInitialized
{
    public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        // Dynamically register for all relevant diagnostic sources.
        if (clientCapabilities?.TextDocument?.Diagnostic?.DynamicRegistration is true)
        {
            // TODO: Hookup an option changed handler for changes to BackgroundAnalysisScopeOption
            //       to dynamically register/unregister the non-local document diagnostic source.

            var documentSources = DiagnosticSourceManager.GetDocumentSourceProviderNames(clientCapabilities);
            var workspaceSources = DiagnosticSourceManager.GetWorkspaceSourceProviderNames(clientCapabilities);

            // All diagnostic sources have to be registered under the document pull method name,
            // See https://github.com/microsoft/language-server-protocol/issues/1723
            //
            // Additionally if a source name is used by both document and workspace pull (e.g. enc)
            // we don't want to send two registrations, instead we should send a single registration
            // that also sets the workspace pull option.
            //
            // So we build up a unique set of source names and mark if each one is also a workspace source.
            var allSources = documentSources
                .AddRange(workspaceSources)
                .ToSet()
                .Select(name => (Name: name, IsWorkspaceSource: workspaceSources.Contains(name)));

            var registrations = allSources.Select(FromSourceName).ToArray();
            await _clientLanguageServerManager.SendRequestAsync(
                methodName: Methods.ClientRegisterCapabilityName,
                @params: new RegistrationParams()
                {
                    Registrations = registrations
                },
                cancellationToken).ConfigureAwait(false);
        }

        static Registration FromSourceName((string Name, bool IsWorkspaceSource) source)
        {
            return new()
            {
                Id = Guid.NewGuid().ToString(),
                Method = Methods.TextDocumentDiagnosticName,
                RegisterOptions = new DiagnosticRegistrationOptions { Identifier = source.Name, InterFileDependencies = true, WorkspaceDiagnostics = source.IsWorkspaceSource, WorkDoneProgress = source.IsWorkspaceSource }
            };
        }
    }
}
