// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.TextDifferencing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(IHtmlDocumentPublisher))]
[method: ImportingConstructor]
internal sealed class HtmlDocumentPublisher(
    LSPDocumentManager documentManager,
    JoinableTaskContext joinableTaskContext,
    ILoggerFactory loggerFactory) : IHtmlDocumentPublisher
{
    private readonly JoinableTaskContext _joinableTaskContext = joinableTaskContext;
    private readonly TrackingLSPDocumentManager _documentManager = documentManager as TrackingLSPDocumentManager ?? throw new InvalidOperationException("Expected TrackingLSPDocumentManager");
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<HtmlDocumentPublisher>();

    public async Task<bool> TryPublishAsync(TextDocument document, ChecksumWrapper checksum, string htmlText, CancellationToken cancellationToken)
    {
        var uri = document.CreateUri();
        if (!_documentManager.TryGetDocument(uri, out var documentSnapshot) ||
            !documentSnapshot.TryGetVirtualDocument<HtmlVirtualDocumentSnapshot>(out var htmlDocument))
        {
            _logger.LogDebug($"No {(documentSnapshot is null ? "" : "virtual")} document snapshot for {document.FilePath}.");
            return false;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        _logger.LogDebug($"The html document for {document.FilePath} is {htmlDocument.Uri}");

        await _joinableTaskContext.Factory.SwitchToMainThreadAsync(cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        // We have a string for the Html text here, so its tempting to just overwrite the whole buffer, but that causes issues with
        // the editors span tracking, which is used by the Html server to map diagnostic ranges. Updating with minimal changes helps
        // the editor understand what is actually happening to the virtual buffer.
        var currentHtmlSourceText = htmlDocument.Snapshot.AsText();
        var newHtmlSourceText = SourceText.From(htmlText, currentHtmlSourceText.Encoding, currentHtmlSourceText.ChecksumAlgorithm);
        var textChanges = SourceTextDiffer.GetMinimalTextChanges(currentHtmlSourceText, newHtmlSourceText);
        var changes = textChanges.SelectAsArray(c => new VisualStudioTextChange(c.Span.Start, c.Span.Length, c.NewText.AssumeNotNull()));
        _documentManager.UpdateVirtualDocument<HtmlVirtualDocument>(uri, changes, documentSnapshot.Version, state: checksum);

        _logger.LogDebug($"Finished Html document generation for {document.FilePath} (into {htmlDocument.Uri})");

        return true;
    }
}
