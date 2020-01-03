// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.VisualStudio.Threading;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Handles a request to get diagnostics. This is a custom request that is issued on document
    /// open. This type also provides notifications when diagnostics change.
    /// TODO - Move implementation to lower LSP layer once we figure out how to deal with notify / client interactions.
    /// </summary>
    internal abstract class DiagnosticsHandler : ILspRequestHandler<TextDocumentParams, LSP.Diagnostic[], Solution>, ILspNotificationProvider
    {
        private readonly IDiagnosticService _diagnosticService;

        protected abstract ImmutableArray<string> SupportedLanguages { get; }

        public event AsyncEventHandler<LanguageServiceNotifyEventArgs> NotifyAsync;

        [ImportingConstructor]
        public DiagnosticsHandler(IDiagnosticService diagnosticService)
        {
            _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));
            _diagnosticService.DiagnosticsUpdated += DiagnosticService_DiagnosticsUpdated;
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void DiagnosticService_DiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            // Since this is an async void method, exceptions here will crash the host VS. We catch exceptions here to make sure that we don't crash the host since
            // the worst outcome here is that guests may not see all diagnostics.
            try
            {
                // LSP doesnt support diagnostics without a document. So if we get project level diagnostics without a document, ignore them.
                if (e.DocumentId != null && e.Solution != null)
                {
                    var document = e.Solution.GetDocument(e.DocumentId);
                    if (document == null || document.FilePath == null)
                    {
                        return;
                    }

                    // Only publish document diagnostics for the languages this provider supports.
                    if (!SupportedLanguages.Contains(document.Project.Language))
                    {
                        return;
                    }

                    var lspDiagnostics = await GetDiagnosticsAsync(e.Solution, document, CancellationToken.None).ConfigureAwait(false);
                    var publishDiagnosticsParams = new LSP.PublishDiagnosticParams { Diagnostics = lspDiagnostics, Uri = document.GetURI() };
                    var eventArgs = new LanguageServiceNotifyEventArgs(LSP.Methods.TextDocumentPublishDiagnosticsName, publishDiagnosticsParams);
                    await (NotifyAsync?.InvokeAsync(this, eventArgs)).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrash(ex))
            {
            }
        }

        public async Task<LSP.Diagnostic[]> HandleAsync(TextDocumentParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var solution = requestContext.Context;
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return Array.Empty<LSP.Diagnostic>();
            }

            return await GetDiagnosticsAsync(solution, document, cancellationToken).ConfigureAwait(false);
        }

        private async Task<LSP.Diagnostic[]> GetDiagnosticsAsync(Solution solution, Document document, CancellationToken cancellationToken)
        {
            var diagnostics = _diagnosticService.GetDiagnostics(solution.Workspace, document.Project.Id, document.Id, null, false, cancellationToken);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return diagnostics.Select(diag => new RoslynDiagnostic
            {
                Code = diag.Id,
                Message = diag.Message,
                Severity = ProtocolConversions.DiagnosticSeverityToLspDiagnositcSeverity(diag.Severity),
                Range = ProtocolConversions.TextSpanToRange(DiagnosticData.GetExistingOrCalculatedTextSpan(diag.DataLocation, text), text),
                Tags = diag.CustomTags.Where(s => s == "Unnecessary").ToArray()
            }).ToArray();
        }
    }
}
