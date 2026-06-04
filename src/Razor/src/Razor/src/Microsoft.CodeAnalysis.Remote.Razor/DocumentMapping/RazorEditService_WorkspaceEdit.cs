// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

internal partial class RazorEditService
{
    public async Task MapWorkspaceEditAsync(IDocumentSnapshot contextDocumentSnapshot, WorkspaceEdit workspaceEdit, CancellationToken cancellationToken)
    {
        if (contextDocumentSnapshot is not RemoteDocumentSnapshot originSnapshot)
        {
            throw new InvalidOperationException("RazorEditService can only be used with RemoteDocumentSnapshot instances.");
        }

        if (workspaceEdit.DocumentChanges is not null)
        {
            using var builder = new PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();
            foreach (var edit in workspaceEdit.EnumerateEdits())
            {
                if (edit.TryGetFirst(out var textDocumentEdit))
                {
                    await MapTextDocumentEditAsync(originSnapshot, textDocumentEdit, cancellationToken).ConfigureAwait(false);
                    if (textDocumentEdit.Edits.Length == 0)
                    {
                        continue;
                    }
                }

                builder.Add(edit);
            }

            workspaceEdit.DocumentChanges = builder.ToArrayAndClear();
        }

        if (workspaceEdit.Changes is { } changeMap)
        {
            workspaceEdit.Changes = await MapDocumentEditsAsync(originSnapshot, changeMap, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task MapTextDocumentEditAsync(RemoteDocumentSnapshot contextDocumentSnapshot, TextDocumentEdit entry, CancellationToken cancellationToken)
    {
        var generatedDocumentUri = entry.TextDocument.DocumentUri.GetRequiredSystemUri();

        // For Html we just map the Uri, the range will be the same
        if (_filePathService.IsVirtualHtmlFile(generatedDocumentUri))
        {
            var razorUri = _filePathService.GetRazorDocumentUri(generatedDocumentUri);
            entry.TextDocument = new OptionalVersionedTextDocumentIdentifier()
            {
                DocumentUri = razorUri.CreateDocumentUriFromSystemUri(),
            };
            return;
        }

        // Check if the edit is actually for a generated document, because if not we don't need to do anything
        if (!_filePathService.IsVirtualCSharpFile(generatedDocumentUri))
        {
            // This location doesn't point to a background razor file. No need to map.
            return;
        }

        var documentSnapshot = await TryGetRazorDocumentSnapshotForGeneratedUriAsync(contextDocumentSnapshot, generatedDocumentUri, cancellationToken).ConfigureAwait(false);
        if (documentSnapshot is null)
        {
            return;
        }

        var edits = new TextEdit[entry.Edits.Length];
        for (var i = 0; i < entry.Edits.Length; i++)
        {
            // entry.Edits is SumType<TextEdit, AnnotatedTextEdit> but AnnotatedTextEdit inherits from TextEdit, so we can just cast
            edits[i] = (TextEdit)entry.Edits[i];
        }

        var mappedEdits = await GetMappedTextEditsAsync(documentSnapshot, edits, cancellationToken).ConfigureAwait(false);

        // Update the entry in-place
        entry.TextDocument = new OptionalVersionedTextDocumentIdentifier()
        {
            DocumentUri = documentSnapshot.TextDocument.GetURI(),
        };
        entry.Edits = mappedEdits.SelectAsPlainArray(static e => new SumType<TextEdit, AnnotatedTextEdit>(e));
    }

    private async Task<Dictionary<string, TextEdit[]>> MapDocumentEditsAsync(RemoteDocumentSnapshot contextDocumentSnapshot, Dictionary<string, TextEdit[]> changes, CancellationToken cancellationToken)
    {
        var mappedChanges = new Dictionary<string, TextEdit[]>(capacity: changes.Count);

        foreach (var (uriString, edits) in changes)
        {
            var generatedDocumentUri = new Uri(uriString);

            // For Html we just map the Uri, the range will be the same
            if (_filePathService.IsVirtualHtmlFile(generatedDocumentUri))
            {
                var razorUri = _filePathService.GetRazorDocumentUri(generatedDocumentUri);
                mappedChanges[razorUri.AbsoluteUri] = edits;
            }

            // Check if the edit is actually for a generated document, because if not we don't need to do anything
            if (!_filePathService.IsVirtualCSharpFile(generatedDocumentUri))
            {
                mappedChanges[uriString] = edits;
                continue;
            }

            var documentSnapshot = await TryGetRazorDocumentSnapshotForGeneratedUriAsync(contextDocumentSnapshot, generatedDocumentUri, cancellationToken).ConfigureAwait(false);
            if (documentSnapshot is null)
            {
                continue;
            }

            var mappedEdits = await GetMappedTextEditsAsync(documentSnapshot, edits, cancellationToken).ConfigureAwait(false);
            if (mappedEdits.Length == 0)
            {
                // Nothing to do.
                continue;
            }

            mappedChanges[documentSnapshot.TextDocument.GetURI().GetRequiredSystemUri().AbsolutePath] = ImmutableCollectionsMarshal.AsArray(mappedEdits)!;
        }

        return mappedChanges;
    }

    private async Task<ImmutableArray<TextEdit>> GetMappedTextEditsAsync(RemoteDocumentSnapshot documentSnapshot, TextEdit[] edits, CancellationToken cancellationToken)
    {
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var razorSourceText = codeDocument.Source.Text;
        var csharpSourceText = codeDocument.GetCSharpSourceText();
        var textChanges = edits.SelectAsArray(csharpSourceText.GetRazorTextChange);
        var mappedEdits = await MapCSharpEditsAsync(textChanges, documentSnapshot, includeCSharpLanguageFeatureEdits: true, directlyMappedEditFilter: null, cancellationToken).ConfigureAwait(false);

        return mappedEdits.SelectAsArray(razorSourceText.GetTextEdit);
    }

    private async Task<RemoteDocumentSnapshot?> TryGetRazorDocumentSnapshotForGeneratedUriAsync(RemoteDocumentSnapshot originSnapshot, Uri generatedDocumentUri, CancellationToken cancellationToken)
    {
        var solution = originSnapshot.TextDocument.Project.Solution;
        var razorDocument = await _snapshotManager.TryGetRazorDocumentAsync(solution, generatedDocumentUri, cancellationToken).ConfigureAwait(false);
        if (razorDocument is null)
        {
            return null;
        }

        return _snapshotManager.GetSnapshot(razorDocument);
    }
}
