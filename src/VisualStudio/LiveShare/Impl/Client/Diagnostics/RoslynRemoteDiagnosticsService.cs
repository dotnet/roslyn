// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.LanguageServices.Protocol;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Remote.Shared.CustomProtocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal class RoslynRemoteDiagnosticsService : IRemoteDiagnosticsService
    {
        private readonly RoslynLSPClientServiceFactory roslynLSPClientServiceFactory;

        public RoslynRemoteDiagnosticsService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
        {
            this.roslynLSPClientServiceFactory = roslynLSPClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLSPClientServiceFactory));
        }

        public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var lspClient = this.roslynLSPClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var textDocumentParams = new TextDocumentParams
            {
                TextDocument = new LSP.TextDocumentIdentifier
                {
                    Uri = lspClient.ProtocolConverter.ToProtocolUri(new Uri(document.FilePath))
                }
            };

            var lspDiagnostics = await lspClient.RequestAsync(RoslynMethods.GetDocumentDiagnostics, textDocumentParams, cancellationToken).ConfigureAwait(false);
            if (lspDiagnostics == null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            foreach (var diagnostic in lspDiagnostics)
            {
                var location = Location.Create(document.FilePath, diagnostic.Range.ToTextSpan(text), diagnostic.Range.ToLinePositionSpan());
                var severity = ToDiagnosticSeverity(diagnostic.Severity);
                var diag = Diagnostic.Create(diagnostic.Code ?? "VSLS", string.Empty, diagnostic.Message, severity, severity,
                                true, severity == DiagnosticSeverity.Error ? 0 : 1, location: location, customTags: diagnostic.Tags);
                diagnostics.Add(diag);
            }

            return diagnostics.ToImmutableArray();
        }

        private static DiagnosticSeverity ToDiagnosticSeverity(LSP.DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case LSP.DiagnosticSeverity.Error:
                    return DiagnosticSeverity.Error;
                case LSP.DiagnosticSeverity.Warning:
                    return DiagnosticSeverity.Warning;
                case LSP.DiagnosticSeverity.Information:
                    return DiagnosticSeverity.Info;
                case LSP.DiagnosticSeverity.Hint:
                    return DiagnosticSeverity.Hidden;
                default:
                    throw new InvalidOperationException("Unknown severity");
            }
        }
    }
}
