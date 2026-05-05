// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

[Shared]
[Export(typeof(FileUriProvider))]
internal class DefaultFileUriProvider : FileUriProvider
{
    private readonly ITextDocumentFactoryService _textDocumentFactory;
    private const string TextBufferUri = "__MsLspTextBufferUri";

    [ImportingConstructor]
    public DefaultFileUriProvider(ITextDocumentFactoryService textDocumentFactory)
    {
        if (textDocumentFactory is null)
        {
            throw new ArgumentNullException(nameof(textDocumentFactory));
        }

        _textDocumentFactory = textDocumentFactory;
    }

    public override void AddOrUpdate(ITextBuffer textBuffer, Uri uri)
    {
        if (textBuffer is null)
        {
            throw new ArgumentNullException(nameof(textBuffer));
        }

        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        textBuffer.Properties[TextBufferUri] = uri;
    }

    public override Uri GetOrCreate(ITextBuffer textBuffer)
    {
        if (textBuffer is null)
        {
            throw new ArgumentNullException(nameof(textBuffer));
        }

        if (TryGet(textBuffer, out var uri))
        {
            return uri;
        }

        string filePath;
        if (_textDocumentFactory.TryGetTextDocument(textBuffer, out var textDocument))
        {
            filePath = textDocument.FilePath;
        }
        else
        {
            // TextBuffer doesn't have a file path, we need to fabricate one.
            filePath = Path.GetTempFileName();
        }

        uri = new Uri(filePath, UriKind.Absolute);
        AddOrUpdate(textBuffer, uri);
        return uri;
    }

    public override bool TryGet(ITextBuffer textBuffer, [NotNullWhen(returnValue: true)] out Uri? uri)
    {
        if (textBuffer is null)
        {
            throw new ArgumentNullException(nameof(textBuffer));
        }

        if (textBuffer.Properties.TryGetProperty(TextBufferUri, out uri!))
        {
            return true;
        }

        return false;
    }

    public override void Remove(ITextBuffer textBuffer)
    {
        if (textBuffer is null)
        {
            throw new ArgumentNullException(nameof(textBuffer));
        }

        textBuffer.Properties.RemoveProperty(TextBufferUri);
    }
}
