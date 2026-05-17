// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal static partial class CompletionListOptimizer
{
    /// <summary>
    /// Promotes per-item commit characters to list-level defaults. Items may use either standard LSP
    /// <see cref="CompletionItem.CommitCharacters"/> (string[]) or VS-internal
    /// <see cref="VSInternalCompletionItem.VsCommitCharacters"/> (<see cref="VSInternalCommitCharacter"/>[]).
    /// The most common set is promoted to the appropriate list-level property based on client capabilities:
    /// <list type="bullet">
    ///   <item>VS clients (<see cref="VSInternalCompletionSetting"/> with <c>CompletionList.CommitCharacters = true</c>):
    ///     promoted to the VS-internal list-level commit characters, preserving the original array type
    ///     (string[] or <see cref="VSInternalCommitCharacter"/>[]).</item>
    ///   <item>Standard LSP clients (<c>ItemDefaults</c> contains <c>"commitCharacters"</c>):
    ///     promoted to <see cref="CompletionListItemDefaults.CommitCharacters"/> as string[],
    ///     including only characters where <c>Insert != false</c> (since standard LSP cannot express "commit without inserting").</item>
    /// </list>
    /// </summary>
    private static RazorVSInternalCompletionList PromoteCommitCharacters(RazorVSInternalCompletionList completionList, CompletionSetting? completionCapability)
    {
        // Determine which promotion targets are available based on client capabilities.
        var canPromoteToVsList = completionCapability is VSInternalCompletionSetting { CompletionList.CommitCharacters: true };
        var itemDefaults = completionCapability?.CompletionListSetting?.ItemDefaults;
        var canPromoteToItemDefaults = itemDefaults is not null && Array.IndexOf(itemDefaults, "commitCharacters") >= 0;

        if (!canPromoteToVsList && !canPromoteToItemDefaults)
        {
            return completionList;
        }

        // If list-level commit characters are already set, don't overwrite them —
        // an upstream provider or optimizer made a deliberate choice.
        if (completionList.CommitCharacters is not null ||
            completionList.ItemDefaults?.CommitCharacters is not null)
        {
            return completionList;
        }

        if (!TryFindMostCommonCommitCharacterGroup(completionList, out var bestCommitCharacterGroup))
        {
            return completionList;
        }

        foreach (var completionItem in completionList.Items)
        {
            if (completionItem is not VSInternalCompletionItem vsItem)
            {
                continue;
            }

            if (TryGetCommitCharacterSource(vsItem, out var strings, out var vsChars))
            {
                // Clear per-item commit characters for items that match the promoted set.
                if (CommitCharactersEqual(bestCommitCharacterGroup.Strings, bestCommitCharacterGroup.VsChars, strings, vsChars))
                {
                    vsItem.CommitCharacters = null;
                    vsItem.VsCommitCharacters = null;
                }
            }
            else
            {
                // For items that intentionally have no commit characters, set an explicit empty
                // array so they don't inherit the promoted list-level defaults.
                vsItem.CommitCharacters = [];
            }
        }

        // Promote to the appropriate list-level property.
        if (canPromoteToVsList)
        {
            completionList.CommitCharacters = bestCommitCharacterGroup.VsChars is not null
                ? bestCommitCharacterGroup.VsChars
                : bestCommitCharacterGroup.Strings;
        }
        else
        {
            // Standard LSP only supports string[] — include only characters where Insert != false.
            completionList.ItemDefaults ??= new CompletionListItemDefaults();
            completionList.ItemDefaults.CommitCharacters = ToStandardCommitCharacters(bestCommitCharacterGroup.Strings, bestCommitCharacterGroup.VsChars);
        }

        return completionList;
    }

    /// <summary>
    /// Groups items by their commit characters and returns the most common group.
    /// There are typically only 2-3 distinct groups (e.g., elements with [" ", ">"] and
    /// attributes with ["="] Insert=false), so a linear scan is cheaper than a dictionary
    /// and avoids per-item allocations entirely. Each group references the original arrays —
    /// no normalization or copying.
    /// </summary>
    private static bool TryFindMostCommonCommitCharacterGroup(
        RazorVSInternalCompletionList completionList,
        out CommitCharacterGroup bestCommitCharacterGroup)
    {
        using var _ = ListPool<CommitCharacterGroup>.GetPooledObject(out var groups);

        bestCommitCharacterGroup = default;

        foreach (var completionItem in completionList.Items)
        {
            if (completionItem is not VSInternalCompletionItem vsItem)
            {
                continue;
            }

            if (!TryGetCommitCharacterSource(vsItem, out var strings, out var vsChars))
            {
                continue;
            }

            var found = false;
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (CommitCharactersEqual(group.Strings, group.VsChars, strings, vsChars))
                {
                    var updated = new CommitCharacterGroup(strings, vsChars, group.Count + 1);
                    groups[i] = updated;

                    if (updated.Count > bestCommitCharacterGroup.Count)
                    {
                        bestCommitCharacterGroup = updated;
                    }

                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var newGroup = new CommitCharacterGroup(strings, vsChars, Count: 1);
                groups.Add(newGroup);

                if (newGroup.Count > bestCommitCharacterGroup.Count)
                {
                    bestCommitCharacterGroup = newGroup;
                }
            }
        }

        return bestCommitCharacterGroup.Count > 0;
    }

    /// <summary>
    /// Extracts the commit character arrays from an item without allocating. Returns false if the item
    /// has no commit characters. Exactly one of <paramref name="strings"/> or <paramref name="vsChars"/>
    /// will be non-null on success.
    /// </summary>
    private static bool TryGetCommitCharacterSource(
        VSInternalCompletionItem item,
        out string[]? strings,
        out VSInternalCommitCharacter[]? vsChars)
    {
        strings = null;
        vsChars = null;

        // Prefer VsCommitCharacters if set (it has richer semantics).
        if (item.VsCommitCharacters is { } vsCommitChars)
        {
            switch (vsCommitChars.Value)
            {
                case VSInternalCommitCharacter[] vsInternalChars:
                    vsChars = vsInternalChars;
                    return true;

                case string[] stringChars:
                    strings = stringChars;
                    return true;
            }
        }

        // Fall back to standard CommitCharacters.
        if (item.CommitCharacters is { } commitChars)
        {
            strings = commitChars;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Compares two commit character sources for equality. Tries reference equality first
    /// (fast path for items from the same provider sharing static arrays), then falls back
    /// to content equality for items whose arrays were independently deserialized across
    /// serialization boundaries (e.g., OOP → devenv JSON round-trip produces distinct array
    /// instances with identical content).
    /// </summary>
    private static bool CommitCharactersEqual(
        string[]? aStrings, VSInternalCommitCharacter[]? aVsChars,
        string[]? bStrings, VSInternalCommitCharacter[]? bVsChars)
    {
        return ContentEqual(aStrings, bStrings) && ContentEqual(aVsChars, bVsChars);
    }

    private static bool ContentEqual(string[]? a, string[]? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null || a.Length != b.Length)
        {
            return false;
        }

        return a.AsSpan().SequenceEqual(b);
    }

    private static bool ContentEqual(VSInternalCommitCharacter[]? a, VSInternalCommitCharacter[]? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null || a.Length != b.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            if (a[i].Character != b[i].Character || a[i].Insert != b[i].Insert)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Converts a commit character source to string[], filtering out characters where Insert is false.
    /// </summary>
    private static string[]? ToStandardCommitCharacters(string[]? strings, VSInternalCommitCharacter[]? vsChars)
    {
        if (strings is not null)
        {
            return strings;
        }

        // Count insertable characters first to avoid over-allocating.
        var count = 0;
        for (var i = 0; i < vsChars!.Length; i++)
        {
            if (vsChars[i].Insert)
            {
                count++;
            }
        }

        if (count == 0)
        {
            return null;
        }

        var result = new string[count];
        var index = 0;
        for (var i = 0; i < vsChars.Length; i++)
        {
            if (vsChars[i].Insert)
            {
                result[index++] = vsChars[i].Character;
            }
        }

        return result;
    }

    /// <summary>
    /// Tracks a group of items sharing the same commit characters. References the original arrays
    /// without copying — zero per-item allocation.
    /// </summary>
    private record struct CommitCharacterGroup(string[]? Strings, VSInternalCommitCharacter[]? VsChars, int Count);
}
