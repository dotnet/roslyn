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
using Microsoft.CodeAnalysis.Razor.Workspaces.Extensions;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

internal partial class RazorEditService
{
    public async Task MapWorkspaceEditAsync(Solution solution, WorkspaceEdit workspaceEdit, CancellationToken cancellationToken)
    {
        // Collect all edits and combine duplicates for each generated document before mapping to Razor.
        using var builder = new PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();

        if (workspaceEdit.DocumentChanges is not null)
        {
            foreach (var edit in workspaceEdit.EnumerateEdits())
            {
                builder.Add(edit);
            }
        }

        if (workspaceEdit.Changes is { } changeMap)
        {
            foreach (var (uri, edits) in changeMap)
            {
                builder.Add(CreateTextDocumentEdit(new DocumentUri(uri), edits.AsSpan()));
            }
        }

        var documentChanges = NormalizeDocumentChanges(builder.ToArrayAndClear());
        using var _ = DictionaryPool<DocumentId, (RemoteDocumentSnapshot Snapshot, ImmutableArray<GeneratedDocumentChanges> Changes, TextDocumentEdit Entry)>
            .GetPooledObject(out var changesByRazorDocument);

        foreach (var edit in documentChanges)
        {
            if (!edit.TryGetFirst(out var entry))
            {
                builder.Add(edit);
                continue;
            }

            var generatedDocumentUri = entry.TextDocument.DocumentUri;
            if (generatedDocumentUri.IsRazorHtmlDocumentUri(out var razorDocumentUri))
            {
                entry.TextDocument = new OptionalVersionedTextDocumentIdentifier { DocumentUri = razorDocumentUri };
                builder.Add(entry);
                continue;
            }

            // For either workspace edit shape, preserve edits we can't map rather than silently dropping them.
            if (!generatedDocumentUri.IsRazorCSharpDocumentUri(solution) ||
                await _snapshotManager.TryGetRazorDocumentAsync(solution, generatedDocumentUri, cancellationToken).ConfigureAwait(false) is not { } razorDocument)
            {
                builder.Add(entry);
                continue;
            }

            var snapshot = _snapshotManager.GetSnapshot(razorDocument);
            var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
            if (!codeDocument.TryGetCSharpDocumentForGeneratedUri(solution, generatedDocumentUri, out var csharpDocument))
            {
                builder.Add(entry);
                continue;
            }

            if (!changesByRazorDocument.TryGetValue(razorDocument.Id, out var razorChanges))
            {
                razorChanges = (snapshot, [], CreateTextDocumentEdit(razorDocument.GetURI(), []));
                builder.Add(razorChanges.Entry);
            }

            // Resolve ranges against this generated document's original C# text; impl and decl have different offsets.
            // AnnotatedTextEdit derives from TextEdit, so either edit shape can use the same conversion.
            var textChanges = entry.Edits.SelectAsArray(e => csharpDocument.Text.GetRazorTextChange((TextEdit)e));
            var syntaxTree = await snapshot.GetCSharpSyntaxTreeAsync(csharpDocument.IsDeclarationDocument, cancellationToken).ConfigureAwait(false);
            razorChanges.Changes = razorChanges.Changes.Add(new(textChanges, syntaxTree, csharpDocument.IsDeclarationDocument));
            changesByRazorDocument[razorDocument.Id] = razorChanges;
        }

        // Map both generated C# documents for a Razor document together. In particular, its shared using
        // directives must be rendered only after collecting changes from both impl and decl.
        foreach (var (snapshot, changes, entry) in changesByRazorDocument.Values)
        {
            // The snapshot cached this output while collecting changes above, so this doesn't regenerate it.
            var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
            var mappedChanges = await MapCSharpEditsAsync(
                snapshot.TextDocument,
                changes,
                codeDocument,
                includeCSharpLanguageFeatureEdits: true,
                directlyMappedEditFilter: null,
                cancellationToken).ConfigureAwait(false);
            entry.Edits = mappedChanges.SelectAsPlainArray(c => new SumType<TextEdit, AnnotatedTextEdit>(codeDocument.Source.Text.GetTextEdit(c)));
        }

        var normalizedDocumentChanges = NormalizeDocumentChanges(builder.ToArrayAndClear());
        if (workspaceEdit.DocumentChanges is not null)
        {
            workspaceEdit.DocumentChanges = normalizedDocumentChanges;
        }
        else
        {
            workspaceEdit.Changes = ConvertToChangeMap(normalizedDocumentChanges);
        }
    }

    private static SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] NormalizeDocumentChanges(
        SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] documentChanges)
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
                DeduplicateTextDocumentEdit(textDocumentEdit);
            }
        }

        return normalizedDocumentChanges;
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

    private static void DeduplicateTextDocumentEdit(TextDocumentEdit entry)
    {
        var edits = entry.Edits;

        if (edits.Length <= 1)
        {
            return;
        }

        using var _ = HashSetPool<(int StartLine, int StartCharacter, int EndLine, int EndCharacter, string? NewText)>.GetPooledObject(out var seenEdits);
        using var builder = new PooledArrayBuilder<SumType<TextEdit, AnnotatedTextEdit>>(edits.Length);

        foreach (var edit in edits)
        {
            var textEdit = (TextEdit)edit;
            var range = textEdit.Range;
            if (seenEdits.Add((range.Start.Line, range.Start.Character, range.End.Line, range.End.Character, textEdit.NewText)))
            {
                builder.Add(edit);
            }
        }

        if (builder.Count == edits.Length)
        {
            // No duplicates, return original array to avoid unnecessary allocations.
            return;
        }

        entry.Edits = builder.ToArrayAndClear();
    }
}
