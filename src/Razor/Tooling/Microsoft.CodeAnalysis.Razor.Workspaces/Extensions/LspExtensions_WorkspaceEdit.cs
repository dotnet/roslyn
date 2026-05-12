// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    /// <summary>
    /// Enumerates the <see cref="TextDocumentEdit"/> objects from the <see cref="WorkspaceEdit.DocumentChanges"/> property.
    /// </summary>
    /// <remarks>
    /// WARNING: This method only yields <see cref="TextDocumentEdit"/> objects. If the <see cref="WorkspaceEdit"/>
    /// contains <see cref="CreateFile"/>, <see cref="RenameFile"/>, or <see cref="DeleteFile"/> operations,
    /// they will NOT be included. Be careful not to create a new <see cref="WorkspaceEdit"/> with just the
    /// results of this method, as doing so would lose those operations and could lead to data loss.
    /// </remarks>
    public static IEnumerable<TextDocumentEdit> EnumerateTextDocumentEdits(this WorkspaceEdit workspaceEdit)
    {
        if (workspaceEdit.DocumentChanges?.Value is TextDocumentEdit[] documentEdits)
        {
            foreach (var edit in documentEdits)
            {
                yield return edit;
            }
        }
        else if (workspaceEdit.DocumentChanges?.Value is SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] sumTypeArray)
        {
            foreach (var sumType in sumTypeArray)
            {
                if (sumType.Value is TextDocumentEdit textDocumentEdit)
                {
                    yield return textDocumentEdit;
                }
            }
        }
    }

    public static IEnumerable<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>> EnumerateEdits(this WorkspaceEdit workspaceEdit)
    {
        if (workspaceEdit.DocumentChanges?.Value is TextDocumentEdit[] documentEdits)
        {
            foreach (var edit in documentEdits)
            {
                yield return edit;
            }
        }
        else if (workspaceEdit.DocumentChanges?.Value is SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] sumTypeArray)
        {
            foreach (var edit in sumTypeArray)
            {
                yield return edit;
            }
        }
    }

    public static WorkspaceEdit Concat(this WorkspaceEdit first, WorkspaceEdit? second)
    {
        if (second is null)
        {
            return first;
        }

        using var builder = new PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();

        AddEdits(ref builder.AsRef(), first);
        AddEdits(ref builder.AsRef(), second);

        return new WorkspaceEdit
        {
            DocumentChanges = builder.ToArrayAndClear()
        };

        static void AddEdits(ref PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>> builder, WorkspaceEdit edit)
        {
            if (edit.DocumentChanges?.Value is TextDocumentEdit[] documentEdits)
            {
                foreach (var e in documentEdits)
                {
                    builder.Add(e);
                }
            }
            else if (edit.DocumentChanges?.Value is SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] sumTypeArray)
            {
                builder.AddRange(sumTypeArray);
            }
            else if (edit.Changes is not null)
            {
                foreach (var (uri, textEdits) in edit.Changes)
                {
                    var edits = new SumType<TextEdit, AnnotatedTextEdit>[textEdits.Length];
                    for (var i = 0; i < textEdits.Length; i++)
                    {
                        edits[i] = textEdits[i];
                    }

                    var textDocumentEdit = new TextDocumentEdit
                    {
                        TextDocument = new OptionalVersionedTextDocumentIdentifier { DocumentUri = new(uri) },
                        Edits = edits
                    };
                    builder.Add(new SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>(textDocumentEdit));
                }
            }
        }
    }
}
