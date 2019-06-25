﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Diagnostics
{
    internal class RoslynRemoteDiagnosticsService : IRemoteDiagnosticsService
    {
        private readonly RoslynLspClientServiceFactory _roslynLspClientServiceFactory;

        public RoslynRemoteDiagnosticsService(RoslynLspClientServiceFactory roslynLspClientServiceFactory)
        {
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLspClientServiceFactory));
        }

        public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
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

            var request = new LSP.LspRequest<TextDocumentParams, RoslynDiagnostic[]>(Methods.GetDocumentDiagnosticsName);
            var lspDiagnostics = await lspClient.RequestAsync(request, textDocumentParams, cancellationToken).ConfigureAwait(false);
            if (lspDiagnostics == null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            foreach (var diagnostic in lspDiagnostics)
            {
                var location = Location.Create(document.FilePath, ProtocolConversions.RangeToTextSpan(diagnostic.Range, text),
                    ProtocolConversions.RangeToLinePositionSpan(diagnostic.Range));
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
