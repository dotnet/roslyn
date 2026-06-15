// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

internal partial class RazorEditService
{
    public async Task MapWorkspaceEditAsync(IDocumentSnapshot contextDocumentSnapshot, WorkspaceEdit workspaceEdit, CancellationToken cancellationToken)
    {
        if (contextDocumentSnapshot is not RemoteDocumentSnapshot originSnapshot)
        {
            throw new InvalidOperationException("RemoteRazorEditService can only be used with RemoteDocumentSnapshot instances.");
        }

        // Collect both workspace edit shapes into TextDocumentEdits so URI coalescing and duplicate
        // edit handling run once across the whole edit.
        using var builder = new PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();

        if (workspaceEdit.DocumentChanges is not null)
        {
            foreach (var edit in workspaceEdit.EnumerateEdits())
            {
                if (edit.TryGetFirst(out var textDocumentEdit))
                {
                    await MapTextDocumentEditAsync(originSnapshot, textDocumentEdit, cancellationToken).ConfigureAwait(false);
                }

                builder.Add(edit);
            }
        }

        if (workspaceEdit.Changes is { } changeMap)
        {
            builder.AddRange(await MapDocumentEditsAsync(originSnapshot, changeMap, cancellationToken).ConfigureAwait(false));
        }

        var solution = originSnapshot.TextDocument.Project.Solution;
        var normalizedDocumentChanges = await NormalizeDocumentChangesAsync(solution, builder.ToArrayAndClear(), cancellationToken).ConfigureAwait(false);
        if (workspaceEdit.DocumentChanges is not null)
        {
            workspaceEdit.DocumentChanges = normalizedDocumentChanges;
        }
        else
        {
            workspaceEdit.Changes = ConvertToChangeMap(normalizedDocumentChanges);
        }
    }

    private static async Task<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]> NormalizeDocumentChangesAsync(
        Solution solution,
        SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] documentChanges,
        CancellationToken cancellationToken)
    {
        // Multiple generated C# documents can map edits back to the same Razor document.
        // Keep one TextDocumentEdit per URI so duplicate mapped edits are applied against the same source text.
        using var _ = DictionaryPool<DocumentUri, TextDocumentEdit>.GetPooledObject(out var textDocumentEditsByUri);
        using var builder = new PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>(documentChanges.Length);

        foreach (var documentChange in documentChanges)
        {
            if (!documentChange.TryGetFirst(out var textDocumentEdit))
            {
                builder.Add(documentChange);
                continue;
            }

            if (textDocumentEdit.Edits.Length == 0)
            {
                continue;
            }

            var uri = textDocumentEdit.TextDocument.DocumentUri;
            if (!textDocumentEditsByUri.TryGetValue(uri, out var existingTextDocumentEdit))
            {
                textDocumentEditsByUri.Add(uri, textDocumentEdit);
                builder.Add(textDocumentEdit);
                continue;
            }

            existingTextDocumentEdit.Edits = [.. existingTextDocumentEdit.Edits, .. textDocumentEdit.Edits];
        }

        // After coalescing by URI, collapse exact duplicate edits that can be produced by the
        // implementation and declaration generated documents mapping to the same Razor span.
        var normalizedDocumentChanges = builder.ToArrayAndClear();
        foreach (var documentChange in normalizedDocumentChanges)
        {
            if (documentChange.TryGetFirst(out var textDocumentEdit))
            {
                await DeduplicateTextDocumentEditAsync(solution, textDocumentEdit, cancellationToken).ConfigureAwait(false);
            }
        }

        return normalizedDocumentChanges;
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

    private async Task<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]> MapDocumentEditsAsync(RemoteDocumentSnapshot contextDocumentSnapshot, Dictionary<string, TextEdit[]> changes, CancellationToken cancellationToken)
    {
        // Map legacy Changes into TextDocumentEdits so MapWorkspaceEditAsync can normalize both shapes together.
        using var builder = new PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>(changes.Count);
        var solution = contextDocumentSnapshot.TextDocument.Project.Solution;

        foreach (var (uriString, edits) in changes)
        {
            var generatedDocumentUri = new Uri(uriString);

            // For Html we just map the Uri, the range will be the same
            if (_filePathService.IsVirtualHtmlFile(generatedDocumentUri))
            {
                var razorUri = _filePathService.GetRazorDocumentUri(generatedDocumentUri);
                builder.Add(CreateTextDocumentEdit(razorUri.CreateDocumentUriFromSystemUri(), edits.AsSpan()));
                continue;
            }

            // Check if the edit is actually for a generated document, because if not we don't need to do anything
            if (!_filePathService.IsVirtualCSharpFile(generatedDocumentUri))
            {
                builder.Add(CreateTextDocumentEdit(new(uriString), edits.AsSpan()));
                continue;
            }

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

            builder.Add(CreateTextDocumentEdit(razorDocument.GetURI(), mappedEdits.AsSpan()));
        }

        return builder.ToArrayAndClear();
    }

    private static TextDocumentEdit CreateTextDocumentEdit(DocumentUri documentUri, ReadOnlySpan<TextEdit> edits)
    {
        var textEdits = new SumType<TextEdit, AnnotatedTextEdit>[edits.Length];
        for (var i = 0; i < edits.Length; i++)
        {
            textEdits[i] = edits[i];
        }

        return new TextDocumentEdit
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier { DocumentUri = documentUri },
            Edits = textEdits
        };
    }

    private static Dictionary<string, TextEdit[]> ConvertToChangeMap(SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] documentChanges)
    {
        var changes = new Dictionary<string, TextEdit[]>(capacity: documentChanges.Length);

        foreach (var documentChange in documentChanges)
        {
            if (documentChange.TryGetFirst(out var textDocumentEdit))
            {
                var textEdits = new TextEdit[textDocumentEdit.Edits.Length];
                for (var i = 0; i < textDocumentEdit.Edits.Length; i++)
                {
                    textEdits[i] = (TextEdit)textDocumentEdit.Edits[i];
                }

                changes.Add(
                    textDocumentEdit.TextDocument.DocumentUri.GetRequiredSystemUri().AbsoluteUri,
                    textEdits);
            }
        }

        return changes;
    }

    private async Task<ImmutableArray<TextEdit>> GetMappedTextEditsAsync(RemoteDocumentSnapshot snapshot, RazorCSharpDocument csharpDocument, TextEdit[] edits, CancellationToken cancellationToken)
    {
        var razorSourceText = csharpDocument.CodeDocument.Source.Text;
        var csharpSourceText = csharpDocument.Text;
        var textChanges = edits.SelectAsArray(csharpSourceText.GetRazorTextChange);
        var mappedEdits = await MapCSharpEditsAsync(textChanges, snapshot, csharpDocument.IsDeclarationDocument, includeCSharpLanguageFeatureEdits: true, directlyMappedEditFilter: null, cancellationToken).ConfigureAwait(false);

        return mappedEdits.SelectAsArray(razorSourceText.GetTextEdit);
    }

    private static async Task DeduplicateTextDocumentEditAsync(Solution solution, TextDocumentEdit entry, CancellationToken cancellationToken)
    {
        if (entry.Edits.Length <= 1)
        {
            return;
        }

        var document = await solution.GetTextDocumentAsync(entry.TextDocument, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return;
        }

        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        entry.Edits = Deduplicate(sourceText, entry.Edits);
    }

    private static SumType<TextEdit, AnnotatedTextEdit>[] Deduplicate(SourceText sourceText, SumType<TextEdit, AnnotatedTextEdit>[] edits)
    {
        if (edits.Length <= 1)
        {
            return edits;
        }

        using var _ = HashSetPool<(int Start, int Length, string? NewText)>.GetPooledObject(out var seenEdits);
        using var builder = new PooledArrayBuilder<SumType<TextEdit, AnnotatedTextEdit>>(edits.Length);

        foreach (var edit in edits)
        {
            var textEdit = (TextEdit)edit;
            var change = sourceText.GetTextChange(textEdit);
            if (seenEdits.Add((change.Span.Start, change.Span.Length, change.NewText)))
            {
                builder.Add(edit);
            }
        }

        return builder.ToArrayAndClear();
    }
}
