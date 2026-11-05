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
        if (workspaceEdit.DocumentChanges is { } changes)
        {
            foreach (var change in changes)
            {
                if (change.Value is TextDocumentEdit textDocumentEdit)
                {
                    yield return textDocumentEdit;
                }
            }
        }
    }

    public static IEnumerable<WorkspaceEdit._DocumentChangesItem> EnumerateEdits(this WorkspaceEdit workspaceEdit)
    {
        if (workspaceEdit.DocumentChanges is { } changes)
        {
            foreach (var edit in changes)
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

        using var builder = new PooledArrayBuilder<WorkspaceEdit._DocumentChangesItem>();

        AddEdits(ref builder.AsRef(), first);
        AddEdits(ref builder.AsRef(), second);

        return new WorkspaceEdit
        {
            DocumentChanges = builder.ToArrayAndClear()
        };

        static void AddEdits(ref PooledArrayBuilder<WorkspaceEdit._DocumentChangesItem> builder, WorkspaceEdit edit)
        {
            if (edit.DocumentChanges is { } changes)
            {
                builder.AddRange(changes);
            }
            else if (edit.Changes is not null)
            {
                foreach (var (uri, textEdits) in edit.Changes)
                {
                    var edits = new TextDocumentEdit._EditsItem[textEdits.Length];
                    for (var i = 0; i < textEdits.Length; i++)
                    {
                        edits[i] = textEdits[i];
                    }

                    var textDocumentEdit = new TextDocumentEdit
                    {
                        TextDocument = new OptionalVersionedTextDocumentIdentifier { Uri = new(uri) },
                        Edits = edits
                    };
                    builder.Add(new(textDocumentEdit));
                }
            }
        }
    }
}
