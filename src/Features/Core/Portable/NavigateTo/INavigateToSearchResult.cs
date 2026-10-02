// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal interface INavigateToSearchResult
{
    string AdditionalInformation { get; }
    string Kind { get; }
    NavigateToMatchKind MatchKind { get; }
    bool IsCaseSensitive { get; }
    string Name { get; }
    ImmutableArray<TextSpan> NameMatchSpans { get; }
    string SecondarySort { get; }
    string? Summary { get; }

    INavigableItem NavigableItem { get; }
    ImmutableArray<PatternMatch> Matches { get; }
}

internal static class NavigateToSearchResultHelpers
{
    private static readonly char[] s_dotArray = ['.'];

    /// <summary>
    /// Orders results that are otherwise equal, such as matches of the same kind.
    /// </summary>
    /// <param name="activeDocument">The document the user was editing when they invoked the navigate-to operation.</param>
    /// <param name="itemDocument">The document the result is contained within.</param>
    public static string ComputeSecondarySort(
        (DocumentId id, IReadOnlyList<string> folders)? activeDocument,
        INavigableItem.NavigableDocument itemDocument,
        int parameterCount,
        int typeParameterCount,
        string name)
    {
        using var _ = ArrayBuilder<string>.GetInstance(out var parts);

        // Ensure if all else is equal, that high-pri items (e.g. from the user's current file) come first
        // before low pri items.  This only applies if things like the MatchKind are the same.  So we'll
        // still show an exact match from another file before a substring match from the current file.
        parts.Add(ComputeFolderDistance().ToString("X4"));

        parts.Add(parameterCount.ToString("X4"));
        parts.Add(typeParameterCount.ToString("X4"));
        parts.Add(name);

        // For partial types, we break up the file name into pieces.  i.e. If we have
        // Outer.cs and Outer.Inner.cs  then we add "Outer" and "Outer Inner" to
        // the secondary sort string.  That way "Outer.cs" will be weighted above
        // "Outer.Inner.cs"
        var fileName = Path.GetFileNameWithoutExtension(itemDocument.FilePath ?? "");
        parts.AddRange(fileName.Split(s_dotArray));

        return string.Join(" ", parts);

        // How close these files are in terms of file system path.  Identical files will have distance 0. Files
        // in the same folder will have distance 1.  Files in different folders will have increasing values here
        // depending on how many folder elements they share/differ on.
        int ComputeFolderDistance()
        {
            // No need to compute anything if there is no active document.  Consider all documents equal.
            if (activeDocument is not { } active)
                return 0;

            // The result was in the active document, this get highest priority.
            if (active.id == itemDocument.Id)
                return 0;

            var activeFolders = active.folders;
            var itemFolders = itemDocument.Folders;

            // see how many folder they have in common.
            var commonCount = GetCommonFolderCount();

            // from this, we can see how many folders then differ between them.
            var activeDiff = activeFolders.Count - commonCount;
            var itemDiff = itemFolders.Count - commonCount;

            // Add one more to the result.  This way if they share all the same folders that we still return
            // '1', indicating that this close to, but not as good a match as an exact file match.
            return activeDiff + itemDiff + 1;

            int GetCommonFolderCount()
            {
                var maxCommon = Math.Min(activeFolders.Count, itemFolders.Count);
                for (var i = 0; i < maxCommon; i++)
                {
                    if (activeFolders[i] != itemFolders[i])
                        return i;
                }

                return maxCommon;
            }
        }
    }

    /// <summary>
    /// Helper to bridge from old api that only returned one pattern match, to new API which can return many.
    /// </summary>
    public static ImmutableArray<PatternMatch> GetMatches(INavigateToSearchResult result)
    {
        var patternMatch = new PatternMatch(
            GetPatternMatchKind(result.MatchKind),
            punctuationStripped: false,
            result.IsCaseSensitive,
            result.NameMatchSpans);
        return [patternMatch];
    }

    private static PatternMatchKind GetPatternMatchKind(NavigateToMatchKind matchKind)
        => matchKind switch
        {
            NavigateToMatchKind.Exact => PatternMatchKind.Exact,
            NavigateToMatchKind.Prefix => PatternMatchKind.Prefix,
            NavigateToMatchKind.Substring => PatternMatchKind.NonLowercaseSubstring,
            NavigateToMatchKind.Regular => PatternMatchKind.Fuzzy,
            NavigateToMatchKind.None => PatternMatchKind.Fuzzy,
            NavigateToMatchKind.CamelCaseExact => PatternMatchKind.CamelCaseExact,
            NavigateToMatchKind.CamelCasePrefix => PatternMatchKind.CamelCasePrefix,
            NavigateToMatchKind.CamelCaseNonContiguousPrefix => PatternMatchKind.CamelCaseNonContiguousPrefix,
            NavigateToMatchKind.CamelCaseSubstring => PatternMatchKind.CamelCaseSubstring,
            NavigateToMatchKind.CamelCaseNonContiguousSubstring => PatternMatchKind.CamelCaseNonContiguousSubstring,
            NavigateToMatchKind.Fuzzy => PatternMatchKind.Fuzzy,
            _ => throw ExceptionUtilities.UnexpectedValue(matchKind),
        };
}
