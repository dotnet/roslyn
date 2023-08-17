// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;

using DocumentDiagnosticReport = SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport>;

// A document diagnostic partial report is defined as having the first literal send = DocumentDiagnosticReport (aka changed / unchanged) followed
// by n DocumentDiagnosticPartialResult literals.
// See https://github.com/microsoft/vscode-languageserver-node/blob/main/protocol/src/common/proposed.diagnostics.md#textDocument_diagnostic
using DocumentDiagnosticPartialReport = SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>;

internal sealed partial class PublicDocumentPullDiagnosticsHandler : AbstractDocumentPullDiagnosticHandler<DocumentDiagnosticParams, DocumentDiagnosticPartialReport, DocumentDiagnosticReport?>, IOnInitialized
{
    public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        // Dynamically register a non-local document diagnostic source if Full solution background analysis is enabled
        // for analyzer execution. This diagnostic source reports diagnostics in open documents that are reported
        // when analyzing other documents or at compilation end.
        if (clientCapabilities?.TextDocument?.Diagnostic?.DynamicRegistration is true && IsFsaEnabled())
        {
            // TODO: Hookup an option changed handler for changes to BackgroundAnalysisScopeOption
            //       to dynamically register/unregister the non-local document diagnostic source.

            await _clientLanguageServerManager.SendRequestAsync(
                methodName: Methods.ClientRegisterCapabilityName,
                @params: new RegistrationParams()
                {
                    Registrations = new[]
                    {
                        new Registration
                        {
                            Id = _nonLocalDiagnosticsSourceRegistrationId,
                            Method = Methods.TextDocumentDiagnosticName,
                            RegisterOptions = new DiagnosticRegistrationOptions
                            {
                                Identifier = DocumentNonLocalDiagnosticIdentifier.ToString()
                            }
                        }
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }

        bool IsFsaEnabled()
        {
            foreach (var language in context.SupportedLanguages)
            {
                if (GlobalOptions.GetBackgroundAnalysisScope(language) == BackgroundAnalysisScope.FullSolution)
                    return true;
            }

            return false;
        }
    }
}
