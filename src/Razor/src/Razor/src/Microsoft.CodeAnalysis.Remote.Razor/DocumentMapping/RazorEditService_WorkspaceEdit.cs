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
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

internal partial class RazorEditService
{
    public async Task MapWorkspaceEditAsync(IDocumentSnapshot contextDocumentSnapshot, WorkspaceEdit workspaceEdit, CancellationToken cancellationToken)
    {
        if (contextDocumentSnapshot is not RemoteDocumentSnapshot originSnapshot)
        {
            throw new InvalidOperationException("RemoteRazorEditService can only be used with RemoteDocumentSnapshot instances.");
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

        var solution = contextDocumentSnapshot.TextDocument.Project.Solution;
        var razorDocument = await _snapshotManager.TryGetRazorDocumentAsync(solution, generatedDocumentUri, cancellationToken).ConfigureAwait(false);
        if (razorDocument is null)
        {
            return;
        }

        var razorDocumentSnapshot = _snapshotManager.GetSnapshot(razorDocument);
        var codeDocument = await razorDocumentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        if (!codeDocument.TryGetCSharpDocumentForGeneratedUri(solution, generatedDocumentUri, out var csharpDocument))
        {
            return;
        }

        var edits = new TextEdit[entry.Edits.Length];
        for (var i = 0; i < entry.Edits.Length; i++)
        {
            // entry.Edits is SumType<TextEdit, AnnotatedTextEdit> but AnnotatedTextEdit inherits from TextEdit, so we can just cast
            edits[i] = (TextEdit)entry.Edits[i];
        }

        var mappedEdits = await GetMappedTextEditsAsync(razorDocumentSnapshot, csharpDocument, edits, cancellationToken).ConfigureAwait(false);

        // Update the entry in-place
        entry.TextDocument = new OptionalVersionedTextDocumentIdentifier()
        {
            DocumentUri = razorDocument.GetURI(),
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

            var solution = contextDocumentSnapshot.TextDocument.Project.Solution;
            var razorDocument = await _snapshotManager.TryGetRazorDocumentAsync(solution, generatedDocumentUri, cancellationToken).ConfigureAwait(false);
            if (razorDocument is null)
            {
                continue;
            }

            var razorDocumentSnapshot = _snapshotManager.GetSnapshot(razorDocument);
            var codeDocument = await razorDocumentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
            if (!codeDocument.TryGetCSharpDocumentForGeneratedUri(solution, generatedDocumentUri, out var csharpDocument))
            {
                continue;
            }

            var mappedEdits = await GetMappedTextEditsAsync(razorDocumentSnapshot, csharpDocument, edits, cancellationToken).ConfigureAwait(false);
            if (mappedEdits.Length == 0)
            {
                // Nothing to do.
                continue;
            }

            mappedChanges[razorDocument.CreateSystemUri().AbsoluteUri] = ImmutableCollectionsMarshal.AsArray(mappedEdits)!;
        }

        return mappedChanges;
    }

    private async Task<ImmutableArray<TextEdit>> GetMappedTextEditsAsync(RemoteDocumentSnapshot snapshot, RazorCSharpDocument csharpDocument, TextEdit[] edits, CancellationToken cancellationToken)
    {
        var razorSourceText = csharpDocument.CodeDocument.Source.Text;
        var csharpSourceText = csharpDocument.Text;
        var textChanges = edits.SelectAsArray(csharpSourceText.GetRazorTextChange);
        var mappedEdits = await MapCSharpEditsAsync(textChanges, snapshot, csharpDocument.IsDeclarationDocument, includeCSharpLanguageFeatureEdits: true, directlyMappedEditFilter: null, cancellationToken).ConfigureAwait(false);

        return mappedEdits.SelectAsArray(razorSourceText.GetTextEdit);
    }
}
