// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

internal abstract class VirtualDocumentFactoryBase : VirtualDocumentFactory
{
    /// <summary>
    /// This marker is understood by LiveShare in order to ensure that virtual documents are not serialized to disk.
    /// Also used in roslyn to filter out text buffer operations on virtual documents.
    /// </summary>
    private const string ContainedLanguageMarker = "ContainedLanguageMarker";

    private readonly ITextBufferFactoryService _textBufferFactory;
    private readonly ITextDocumentFactoryService _textDocumentFactory;
    private readonly FileUriProvider _fileUriProvider;

    protected IContentTypeRegistryService ContentTypeRegistry { get; }

    public VirtualDocumentFactoryBase(
        IContentTypeRegistryService contentTypeRegistry,
        ITextBufferFactoryService textBufferFactory,
        ITextDocumentFactoryService textDocumentFactory,
        FileUriProvider filePathProvider)
    {
        if (contentTypeRegistry is null)
        {
            throw new ArgumentNullException(nameof(contentTypeRegistry));
        }

        if (textBufferFactory is null)
        {
            throw new ArgumentNullException(nameof(textBufferFactory));
        }

        if (textDocumentFactory is null)
        {
            throw new ArgumentNullException(nameof(textDocumentFactory));
        }

        if (filePathProvider is null)
        {
            throw new ArgumentNullException(nameof(filePathProvider));
        }

        ContentTypeRegistry = contentTypeRegistry;

        _textBufferFactory = textBufferFactory;
        _textDocumentFactory = textDocumentFactory;
        _fileUriProvider = filePathProvider;
    }

    protected abstract IContentType LanguageContentType { get; }

    public override bool TryCreateFor(ITextBuffer hostDocumentBuffer, [NotNullWhen(returnValue: true)] out VirtualDocument? virtualDocument)
    {
        if (hostDocumentBuffer is null)
        {
            throw new ArgumentNullException(nameof(hostDocumentBuffer));
        }

        if (!hostDocumentBuffer.ContentType.IsOfType(HostDocumentContentTypeName))
        {
            // Another content type we don't care about.
            virtualDocument = null;
            return false;
        }

        var hostDocumentUri = _fileUriProvider.GetOrCreate(hostDocumentBuffer);

        // E.g. Index.cshtml => Index.cshtml__virtual.html (for html), similar for other languages
        var virtualLanguageFilePath = hostDocumentUri.GetAbsoluteOrUNCPath() + LanguageFileNameSuffix;
        var virtualLanguageUri = new Uri(virtualLanguageFilePath);

        var languageBuffer = CreateVirtualDocumentTextBuffer(virtualLanguageFilePath, virtualLanguageUri);

        virtualDocument = CreateVirtualDocument(virtualLanguageUri, languageBuffer);
        return true;
    }

    protected virtual ITextBuffer CreateVirtualDocumentTextBuffer(string virtualLanguageFilePath, Uri virtualLanguageUri)
    {
        var languageBuffer = _textBufferFactory.CreateTextBuffer();
        _fileUriProvider.AddOrUpdate(languageBuffer, virtualLanguageUri);

        // This ensures that LiveShare does not serialize this virtual document to disk in LiveShare & Codespaces scenarios.
        languageBuffer.Properties.AddProperty(ContainedLanguageMarker, true);

        if (LanguageBufferProperties is not null)
        {
            foreach (var keyValuePair in LanguageBufferProperties)
            {
                languageBuffer.Properties.AddProperty(keyValuePair.Key, keyValuePair.Value);
            }
        }

        // Create a text document to trigger language server initialization for the contained language.
        _textDocumentFactory.CreateTextDocument(languageBuffer, virtualLanguageFilePath);

        languageBuffer.ChangeContentType(LanguageContentType, editTag: null);
        return languageBuffer;
    }

    /// <summary>
    /// Creates and returns specific virtual document instance
    /// </summary>
    /// <param name="uri">Virtual document URI</param>
    /// <param name="textBuffer">Language text buffer</param>
    /// <returns>Language-specific virtual document instance</returns>
    protected abstract VirtualDocument CreateVirtualDocument(Uri uri, ITextBuffer textBuffer);

    /// <summary>
    /// Returns contained language uri suffix, e.g. __virtual.html or __virtual.css
    /// </summary>
    protected abstract string LanguageFileNameSuffix { get; }

    /// <summary>
    /// Returns additional properties (if any) to set on the language text buffer prior to language server init
    /// </summary>
    protected virtual IReadOnlyDictionary<object, object>? LanguageBufferProperties => null;

    /// <summary>
    /// Returns supported host document content type name (i.e. host document content type for which this factory can create virtual documents)
    /// </summary>
    protected abstract string HostDocumentContentTypeName { get; }
}
