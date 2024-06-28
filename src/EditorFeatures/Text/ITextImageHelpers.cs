// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text;

internal static class ITextImageHelpers
{
    private static readonly Func<int, int, string> s_textLog = (v1, v2) => string.Format("FullRange : from {0} to {1}", v1, v2);

    private static readonly Func<ITextChange, TextChangeRange> s_forwardTextChangeRange = c => CreateTextChangeRange(c, forward: true);
    private static readonly Func<ITextChange, TextChangeRange> s_backwardTextChangeRange = c => CreateTextChangeRange(c, forward: false);

    public static IReadOnlyList<TextChangeRange> GetChangeRanges(ITextImage oldImage, ITextImage newImage)
        => GetChangeRanges(oldImage.Version, newImage.Version);

    public static IReadOnlyList<TextChangeRange> GetChangeRanges(ITextImageVersion oldImageVersion, ITextImageVersion newImageVersion)
    {
        var forward = oldImageVersion.VersionNumber <= newImageVersion.VersionNumber;

        var oldSnapshotVersion = forward ? oldImageVersion : newImageVersion;
        var newSnapshotVersion = forward ? newImageVersion : oldImageVersion;

        INormalizedTextChangeCollection? changes = null;
        for (var oldVersion = oldSnapshotVersion;
            oldVersion != newSnapshotVersion;
            oldVersion = oldVersion.Next)
        {
            if (oldVersion.Changes.Count != 0)
            {
                if (changes != null)
                {
                    // Oops - more than one "textual" change between these snapshots, bail and try to find smallest changes span
                    Logger.Log(FunctionId.Workspace_SourceText_GetChangeRanges, s_textLog, oldImageVersion.VersionNumber, newImageVersion.VersionNumber);

                    return [GetChangeRanges(oldSnapshotVersion, newSnapshotVersion, forward)];
                }
                else
                {
                    changes = oldVersion.Changes;
                }
            }
        }

        if (changes == null)
        {
            return [];
        }
        else
        {
            return ImmutableArray.CreateRange(changes.Select(forward ? s_forwardTextChangeRange : s_backwardTextChangeRange));
        }
    }

    private static TextChangeRange GetChangeRanges(ITextImageVersion oldVersion, ITextImageVersion newVersion, bool forward)
    {
        TextChangeRange? range = null;
        var iterator = GetMultipleVersionTextChanges(oldVersion, newVersion, forward);
        foreach (var changes in forward ? iterator : iterator.Reverse())
        {
            range = range.Accumulate(changes);
        }

        RoslynDebug.Assert(range.HasValue);
        return range.Value;
    }

    private static IEnumerable<IEnumerable<TextChangeRange>> GetMultipleVersionTextChanges(
        ITextImageVersion oldVersion, ITextImageVersion newVersion, bool forward)
    {
        for (var version = oldVersion; version != newVersion; version = version.Next)
        {
            yield return version.Changes.Select(forward ? s_forwardTextChangeRange : s_backwardTextChangeRange);
        }
    }

    private static TextChangeRange CreateTextChangeRange(ITextChange change, bool forward)
    {
        if (forward)
        {
            return new TextChangeRange(new TextSpan(change.OldSpan.Start, change.OldSpan.Length), change.NewLength);
        }
        else
        {
            return new TextChangeRange(new TextSpan(change.NewSpan.Start, change.NewSpan.Length), change.OldLength);
        }
    }
}
