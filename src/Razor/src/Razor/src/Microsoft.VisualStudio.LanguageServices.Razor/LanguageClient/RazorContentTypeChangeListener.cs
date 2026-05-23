// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

[Name(nameof(RazorContentTypeChangeListener))]
[Export(typeof(ITextBufferContentTypeListener))]
[ContentType(RazorConstants.RazorLSPContentTypeName)]
internal class RazorContentTypeChangeListener : ITextBufferContentTypeListener
{
    private readonly TrackingLSPDocumentManager _lspDocumentManager;
    private readonly ITextDocumentFactoryService _textDocumentFactory;
    private readonly ILspEditorFeatureDetector _lspEditorFeatureDetector;
    private readonly IFileToContentTypeService _fileToContentTypeService;

    [ImportingConstructor]
    public RazorContentTypeChangeListener(
        ITextDocumentFactoryService textDocumentFactory,
        LSPDocumentManager lspDocumentManager,
        ILspEditorFeatureDetector lspEditorFeatureDetector,
        IFileToContentTypeService fileToContentTypeService)
    {
        if (textDocumentFactory is null)
        {
            throw new ArgumentNullException(nameof(textDocumentFactory));
        }

        if (lspDocumentManager is null)
        {
            throw new ArgumentNullException(nameof(lspDocumentManager));
        }

        if (lspEditorFeatureDetector is null)
        {
            throw new ArgumentNullException(nameof(lspEditorFeatureDetector));
        }

        if (fileToContentTypeService is null)
        {
            throw new ArgumentNullException(nameof(fileToContentTypeService));
        }

        if (lspDocumentManager is not TrackingLSPDocumentManager tracking)
        {
            throw new ArgumentException("The LSP document manager should be of type " + typeof(TrackingLSPDocumentManager).FullName, nameof(_lspDocumentManager));
        }

        _lspDocumentManager = tracking;

        _textDocumentFactory = textDocumentFactory;
        _lspEditorFeatureDetector = lspEditorFeatureDetector;
        _fileToContentTypeService = fileToContentTypeService;
    }

    public void ContentTypeChanged(ITextBuffer textBuffer, IContentType oldContentType, IContentType newContentType)
    {
        var supportedBefore = oldContentType.IsOfType(RazorConstants.RazorLSPContentTypeName);
        var supportedAfter = newContentType.IsOfType(RazorConstants.RazorLSPContentTypeName);

        if (supportedBefore == supportedAfter)
        {
            // We went from a Razor content type to another Razor content type.
            return;
        }

        if (supportedAfter)
        {
            RazorBufferCreated(textBuffer);
        }
        else if (supportedBefore)
        {
            // Stash the old content type so that listeners to
            textBuffer.Properties[DefaultLSPDocumentManager.LSPDocumentRemovalOldContentTypeKey] = oldContentType;

            RazorBufferDisposed(textBuffer);

            // Clean up after ourselves
            textBuffer.Properties.RemoveProperty(DefaultLSPDocumentManager.LSPDocumentRemovalOldContentTypeKey);
        }
    }

    // Internal for testing
    internal void RazorBufferCreated(ITextBuffer textBuffer)
    {
        if (textBuffer is null)
        {
            throw new ArgumentNullException(nameof(textBuffer));
        }

        if (!_lspEditorFeatureDetector.IsRemoteClient())
        {
            // Renames on open files don't dispose buffer state so we need to separately monitor the buffer for document renames to ensure
            // we can tell the lsp document manager when state changes.
            MonitorDocumentForRenames(textBuffer);

            // Only need to track documents on a host because we don't do any extra work on remote clients.
            _lspDocumentManager.TrackDocument(textBuffer);
        }
    }

    // Internal for testing
    internal void RazorBufferDisposed(ITextBuffer textBuffer)
    {
        if (textBuffer is null)
        {
            throw new ArgumentNullException(nameof(textBuffer));
        }

        StopMonitoringDocumentForRenames(textBuffer);

        // If we don't know about this document we'll no-op
        _lspDocumentManager.UntrackDocument(textBuffer);
    }

    // Internal for testing
    internal void TextDocument_FileActionOccurred(object sender, TextDocumentFileActionEventArgs args)
    {
        if (args.FileActionType != FileActionTypes.DocumentRenamed)
        {
            // We're only interested in document rename events.
            return;
        }

        if (sender is not ITextDocument textDocument)
        {
            return;
        }

        var textBuffer = textDocument.TextBuffer;

        if (textBuffer is null)
        {
            return;
        }

        // Document was renamed, translate that rename into an untrack -> track to refresh state.

        RazorBufferDisposed(textBuffer);

        // Normally we could just look at the buffer again to see if the content type was still Razor; however,
        // there's a bug in the platform which prevents that from working:
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1161307/
        // To counteract this we need to re-calculate the content type based off of the filepath.
        var newContentType = _fileToContentTypeService.GetContentTypeForFilePath(textDocument.FilePath);
        if (newContentType.IsOfType(RazorConstants.RazorLSPContentTypeName))
        {
            // Renamed to another RazorLSP based document, lets treat it as a re-creation.
            RazorBufferCreated(textBuffer);
        }
    }

    private void MonitorDocumentForRenames(ITextBuffer textBuffer)
    {
        if (!_textDocumentFactory.TryGetTextDocument(textBuffer, out var textDocument))
        {
            // Cannot monitor buffers that don't have an associated text document. In practice, this should never happen but being extra defensive here.
            return;
        }

        textDocument.FileActionOccurred += TextDocument_FileActionOccurred;
    }

    private void StopMonitoringDocumentForRenames(ITextBuffer textBuffer)
    {
        if (!_textDocumentFactory.TryGetTextDocument(textBuffer, out var textDocument))
        {
            // Text document must have been torn down, no need to unsubscribe to something that's already been torn down.
            return;
        }

        textDocument.FileActionOccurred -= TextDocument_FileActionOccurred;
    }
}
