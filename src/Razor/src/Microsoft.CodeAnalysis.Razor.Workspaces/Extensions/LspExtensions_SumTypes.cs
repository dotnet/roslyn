// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor;

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    internal static int Count(this SumType<TextDocumentEdit[], SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]> sumType)
    {
        if (sumType.TryGetFirst(out var textDocumentEdit))
        {
            return textDocumentEdit.Length;
        }

        if (sumType.TryGetSecond(out var edits))
        {
            return edits.Length;
        }

        return Assumed.Unreachable<int>();
    }

    internal static SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile> ElementAt(this SumType<TextDocumentEdit[], SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]> sumType, int elementIndex)
    {
        if (sumType.TryGetFirst(out var textDocumentEdits))
        {
            return textDocumentEdits[elementIndex];
        }

        if (sumType.TryGetSecond(out var edits))
        {
            return edits[elementIndex];
        }

        return Assumed.Unreachable<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();
    }

    internal static SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] ToArray(this SumType<TextDocumentEdit[], SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]> sumType)
    {
        if (sumType.TryGetFirst(out var textDocumentEdit))
        {
            return textDocumentEdit.Select(s => (SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>)s).ToArray();
        }

        if (sumType.TryGetSecond(out var edits))
        {
            return edits.ToArray();
        }

        return Assumed.Unreachable<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]>();
    }

    internal static SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile> First(this SumType<TextDocumentEdit[], SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]> sumType)
    {
        if (sumType.TryGetFirst(out var textDocumentEdits))
        {
            return textDocumentEdits.First();
        }

        if (sumType.TryGetSecond(out var edits))
        {
            return edits.First();
        }

        return Assumed.Unreachable<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();
    }

    internal static SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile> Last(this SumType<TextDocumentEdit[], SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]> sumType)
    {
        if (sumType.TryGetFirst(out var textDocumentEdits))
        {
            return textDocumentEdits.Last();
        }

        if (sumType.TryGetSecond(out var edits))
        {
            return edits.Last();
        }

        return Assumed.Unreachable<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();
    }
}
