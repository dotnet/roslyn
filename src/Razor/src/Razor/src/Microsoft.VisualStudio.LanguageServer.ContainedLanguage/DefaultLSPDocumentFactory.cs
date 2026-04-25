// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

[Shared]
[Export(typeof(LSPDocumentFactory))]
internal class DefaultLSPDocumentFactory : LSPDocumentFactory
{
    private readonly FileUriProvider _fileUriProvider;
    private readonly IEnumerable<Lazy<VirtualDocumentFactory, IContentTypeMetadata>> _virtualDocumentFactories;

    [ImportingConstructor]
    public DefaultLSPDocumentFactory(
        FileUriProvider fileUriProvider,
        [ImportMany] IEnumerable<Lazy<VirtualDocumentFactory, IContentTypeMetadata>> virtualBufferFactories)
    {
        if (fileUriProvider is null)
        {
            throw new ArgumentNullException(nameof(fileUriProvider));
        }

        if (virtualBufferFactories is null)
        {
            throw new ArgumentNullException(nameof(virtualBufferFactories));
        }

        _fileUriProvider = fileUriProvider;
        _virtualDocumentFactories = virtualBufferFactories;
    }

    public override LSPDocument Create(ITextBuffer buffer)
    {
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        var uri = _fileUriProvider.GetOrCreate(buffer);
        var virtualDocuments = CreateVirtualDocuments(buffer);
        var lspDocument = new DefaultLSPDocument(uri, buffer, virtualDocuments);

        return lspDocument;
    }

    internal override bool TryRefreshVirtualDocuments(LSPDocument document)
    {
        var result = false;
        foreach (var factory in _virtualDocumentFactories)
        {
            if (factory.Metadata.ContentTypes.Any(ct => document.TextBuffer.ContentType.IsOfType(ct)))
            {
                // The contract for TryRefreshVirtualDocuments is that factories shouldn't touch virtual
                // documents they don't own so calling it multiple times, and repeatedly setting the virtual
                // documents is fine. We might create a few intermediate snapshots, but change triggers are
                // sent by the caller of this method, so consumers see one set of new virtual documents.
                if (factory.Value.TryRefreshVirtualDocuments(document, out var newVirtualDocuments))
                {
                    document.SetVirtualDocuments(newVirtualDocuments);
                    result |= true;
                }
            }
        }

        return result;
    }

    private IReadOnlyList<VirtualDocument> CreateVirtualDocuments(ITextBuffer hostDocumentBuffer)
    {
        var virtualDocuments = new List<VirtualDocument>();
        foreach (var factory in _virtualDocumentFactories)
        {
            if (factory.Metadata.ContentTypes.Any(ct => hostDocumentBuffer.ContentType.IsOfType(ct)))
            {
                if (factory.Value.TryCreateMultipleFor(hostDocumentBuffer, out var newVirtualDocuments))
                {
                    virtualDocuments.AddRange(newVirtualDocuments);
                }
                else if (factory.Value.TryCreateFor(hostDocumentBuffer, out var virtualDocument))
                {
                    virtualDocuments.Add(virtualDocument);
                }
            }
        }

        return virtualDocuments;
    }
}
