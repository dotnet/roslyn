// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.TextDocumentContent;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SourceGenerators;

/// <summary>
/// Provides text content for source generated documents by running the actual source generator
/// (unfreezing the document) rather than returning the frozen/opened text.
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(SourceGeneratedDocumentContentProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SourceGeneratedDocumentContentProvider() : ITextDocumentContentProvider
{
    public string Scheme => SourceGeneratedDocumentUri.Scheme;

    public async Task<string> GetTextAsync(TextDocument document, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(document is SourceGeneratedDocument);

        // When a user has an open source-generated file, we ensure that the contents in the LSP snapshot match the contents that we
        // get through didOpen/didChanges, like any other file. That way operations in LSP file are in sync with the
        // contents the user has. However in this case, we don't want to look at that frozen text, but look at what the
        // generator would generate if we ran it again. Otherwise, we'll get "stuck" and never update the file with something new.
        // This can return null when the source generated file has been removed (but the queue itself is using the frozen non-null document).
        var unfrozenDocument = await document.Project.Solution.WithoutFrozenSourceGeneratedDocuments()
            .GetDocumentAsync(document.Id, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

        if (unfrozenDocument == null)
            return string.Empty;

        var text = await unfrozenDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return text.ToString();
    }
}
