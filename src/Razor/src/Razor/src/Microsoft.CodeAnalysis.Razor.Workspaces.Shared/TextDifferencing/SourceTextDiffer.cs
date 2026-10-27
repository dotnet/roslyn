// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.TextDifferencing;

internal abstract partial class SourceTextDiffer(SourceText oldText, SourceText newText) : TextDiffer, IDisposable
{
    protected readonly SourceText OldText = oldText;
    protected readonly SourceText NewText = newText;

    public abstract void Dispose();

    protected abstract int GetEditPosition(DiffEdit edit);
    protected abstract int AppendEdit(DiffEdit edit, StringBuilder builder);

    /// <summary>
    /// Rents a char array of at least <paramref name="minimumLength"/> from the shared array pool.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static char[] RentArray(int minimumLength)
        => ArrayPool<char>.Shared.Rent(minimumLength);

    /// <summary>
    /// Returns a char array to the shared array pool.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void ReturnArray(char[] array, bool clearArray = false)
        => ArrayPool<char>.Shared.Return(array, clearArray);

    /// <summary>
    /// Ensures that <paramref name="array"/> references a char array of at least <paramref name="minimumLength"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static char[] EnsureBuffer(ref char[] array, int minimumLength)
    {
        return array.Length >= minimumLength
            ? array
            : GetNewBuffer(ref array, minimumLength);

        static char[] GetNewBuffer(ref char[] array, int minimumLength)
        {
            // We need a larger buffer. Return this array to the pool
            // and rent a new one.
            ReturnArray(array);
            array = RentArray(minimumLength);

            return array;
        }
    }

    private ImmutableArray<TextChange> ConsolidateEdits(List<DiffEdit> edits)
    {
        // Scan through the list of edits and collapse them into a minimal set of TextChanges.
        // This method assumes that there are no overlapping changes and the changes are sorted.

        using var minimalChanges = new PooledArrayBuilder<TextChange>(capacity: edits.Count);

        var start = 0;
        var end = 0;

        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        foreach (var edit in edits)
        {
            var startPosition = GetEditPosition(edit);
            if (startPosition != end)
            {
                // Previous edit's end doesn't match the new edit's start.
                // Output the text change we were tracking.
                if (start != end || builder.Length > 0)
                {
                    minimalChanges.Add(new TextChange(TextSpan.FromBounds(start, end), builder.ToString()));
                    builder.Clear();
                }

                start = startPosition;
            }

            end = AppendEdit(edit, builder);
        }

        if (start != end || builder.Length > 0)
        {
            minimalChanges.Add(new TextChange(TextSpan.FromBounds(start, end), builder.ToString()));
        }

        return minimalChanges.ToImmutableAndClear();
    }

    public static ImmutableArray<TextChange> GetMinimalTextChanges(SourceText oldText, SourceText newText, DiffKind kind = DiffKind.Line)
    {
        if (oldText.ContentEquals(newText))
        {
            return [];
        }
        else if (oldText.Length == 0 || newText.Length == 0)
        {
            return newText.GetTextChangesArray(oldText);
        }

        using SourceTextDiffer differ = kind switch
        {
            DiffKind.Line => new LineDiffer(oldText, newText),
            DiffKind.Char => new CharDiffer(oldText, newText),
            DiffKind.Word => new WordDiffer(oldText, newText),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        var edits = differ.ComputeDiff();

        var changes = differ.ConsolidateEdits(edits);

        Debug.Assert(oldText.WithChanges(changes).ContentEquals(newText), "Incorrect minimal changes");

        return changes;
    }
}
