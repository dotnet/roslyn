// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using AliasedVSCommitCharacters = Roslyn.LanguageServer.Protocol.SumType<string[], Roslyn.LanguageServer.Protocol.VSInternalCommitCharacter[]>;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal static class CompletionListOptimizer
{
    public static RazorVSInternalCompletionList Optimize(RazorVSInternalCompletionList completionList, CompletionSetting? completionCapability)
    {
        if (completionCapability is VSInternalCompletionSetting vsCompletionCapability)
        {
            completionList = OptimizeCommitCharacters(completionList, vsCompletionCapability);
        }

        completionList = PromoteEditRangeToListDefaults(completionList, completionCapability);

        return completionList;
    }

    private static RazorVSInternalCompletionList OptimizeCommitCharacters(RazorVSInternalCompletionList completionList, VSInternalCompletionSetting completionCapability)
    {
        var completionListCapability = completionCapability.CompletionList;
        if (completionListCapability?.CommitCharacters != true)
        {
            return completionList;
        }

        // The commit characters capability is a VS capability with how we utilize it, therefore we want to promote onto the VS list.
        completionList = PromoteVSCommonCommitCharactersOntoList(completionList);
        return completionList;
    }

    private static RazorVSInternalCompletionList PromoteVSCommonCommitCharactersOntoList(RazorVSInternalCompletionList completionList)
    {
        (AliasedVSCommitCharacters VsCommitCharacters, List<VSInternalCompletionItem> AssociatedCompletionItems)? mostUsedCommitCharacterToItems = null;
        var commitCharacterMap = new Dictionary<AliasedVSCommitCharacters, List<VSInternalCompletionItem>>(AliasedVSCommitCharactersComparer.Instance);
        foreach (var completionItem in completionList.Items)
        {
            if (completionItem is not VSInternalCompletionItem vsCompletionItem)
            {
                continue;
            }

            var vsCommitCharactersHolder = vsCompletionItem.VsCommitCharacters;
            if (vsCommitCharactersHolder is null)
            {
                continue;
            }

            var commitCharacters = vsCommitCharactersHolder.Value;
            if (!commitCharacterMap.TryGetValue(commitCharacters, out var associatedCompletionItems))
            {
                associatedCompletionItems = new List<VSInternalCompletionItem>();
                commitCharacterMap[commitCharacters] = associatedCompletionItems;
            }

            associatedCompletionItems.Add(vsCompletionItem);

            if (mostUsedCommitCharacterToItems is null ||
                associatedCompletionItems.Count > mostUsedCommitCharacterToItems.Value.AssociatedCompletionItems.Count)
            {
                mostUsedCommitCharacterToItems = (commitCharacters, associatedCompletionItems);
            }
        }

        if (mostUsedCommitCharacterToItems is null)
        {
            return completionList;
        }

        // Promote the most used commit characters onto the list and remove duplicates from child items.
        foreach (var completionItem in mostUsedCommitCharacterToItems.Value.AssociatedCompletionItems)
        {
            // Clear out the commit characters for all associated items
            completionItem.CommitCharacters = null;
            completionItem.VsCommitCharacters = null;
        }

        completionList.CommitCharacters = mostUsedCommitCharacterToItems.Value.VsCommitCharacters;
        return completionList;
    }

    private static RazorVSInternalCompletionList PromoteEditRangeToListDefaults(RazorVSInternalCompletionList completionList, CompletionSetting? completionCapability)
    {
        // Check if client supports editRange in ItemDefaults
        var itemDefaults = completionCapability?.CompletionListSetting?.ItemDefaults;
        if (itemDefaults is null || !itemDefaults.Contains("editRange"))
        {
            return completionList;
        }

        var items = completionList.Items;

        // Find the common TextEdit range across all items.
        // If any item lacks a TextEdit or has a different range, bail out.
        LspRange? commonRange = null;
        foreach (var item in items)
        {
            if (item.TextEdit?.Value is not TextEdit textEdit)
            {
                return completionList;
            }

            if (commonRange is null)
            {
                commonRange = textEdit.Range;
            }
            else if (!commonRange.Equals(textEdit.Range))
            {
                return completionList;
            }
        }

        if (commonRange is null)
        {
            return completionList;
        }

        // Promote the common range to ItemDefaults.EditRange and replace per-item TextEdits with TextEditText
        completionList.ItemDefaults ??= new CompletionListItemDefaults();
        completionList.ItemDefaults.EditRange = commonRange;

        foreach (var item in items)
        {
            var textEdit = (TextEdit)item.TextEdit!.Value;
            item.TextEditText = textEdit.NewText;
            item.TextEdit = null;
        }

        return completionList;
    }

    private class AliasedVSCommitCharactersComparer : IEqualityComparer<AliasedVSCommitCharacters>
    {
        public static readonly AliasedVSCommitCharactersComparer Instance = new();

        private AliasedVSCommitCharactersComparer()
        {
        }

        public bool Equals(AliasedVSCommitCharacters a, AliasedVSCommitCharacters b)
        {
            if (a.TryGetFirst(out var aFirstValue) && b.TryGetFirst(out var bFirstValue))
            {
                return Enumerable.SequenceEqual(aFirstValue, bFirstValue);
            }
            else if (a.TryGetSecond(out var aSecondValue) && b.TryGetSecond(out var bSecondValue))
            {
                if (aSecondValue.Length != bSecondValue.Length)
                {
                    return false;
                }

                for (var i = 0; i < aSecondValue.Length; i++)
                {
                    var aCommitCharacter = aSecondValue[i];
                    var bCommitCharacter = bSecondValue[i];

                    if (aCommitCharacter.Character != bCommitCharacter.Character ||
                        aCommitCharacter.Insert != bCommitCharacter.Insert)
                    {
                        return false;
                    }
                }

                return true;
            }

            // Mismatch in commit character types
            return false;
        }

        public int GetHashCode(AliasedVSCommitCharacters obj)
        {
            if (obj.TryGetFirst(out var stringVal))
            {
                return stringVal.Length;
            }
            else if (obj.TryGetSecond(out var commitCharVal))
            {
                return commitCharVal.Length;
            }

            return 0;
        }
    }
}
