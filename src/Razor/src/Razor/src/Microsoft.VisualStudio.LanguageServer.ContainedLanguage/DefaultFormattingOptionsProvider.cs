// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

[Shared]
[Export(typeof(FormattingOptionsProvider))]
internal class DefaultFormattingOptionsProvider : FormattingOptionsProvider
{
    private readonly LSPDocumentManager _documentManager;
    private readonly IIndentationManagerService _indentationManagerService;

    [ImportingConstructor]
    public DefaultFormattingOptionsProvider(
        LSPDocumentManager documentManager,
        IIndentationManagerService indentationManagerService)
    {
        _documentManager = documentManager;
        _indentationManagerService = indentationManagerService;
    }

    public override FormattingOptions? GetOptions(Uri uri)
    {
        if (!_documentManager.TryGetDocument(uri, out var documentSnapshot))
        {
            return null;
        }

        _indentationManagerService.GetIndentation(
            documentSnapshot.Snapshot.TextBuffer,
            explicitFormat: false,
            out var insertSpaces,
            out var tabSize,
            out _);
        var formattingOptions = new FormattingOptions()
        {
            InsertSpaces = insertSpaces,
            TabSize = tabSize,
        };
        return formattingOptions;
    }
}
