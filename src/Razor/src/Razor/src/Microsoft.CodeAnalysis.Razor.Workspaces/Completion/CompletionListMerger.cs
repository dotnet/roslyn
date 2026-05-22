// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal static class CompletionListMerger
{
    private const string Data1Key = nameof(MergedCompletionListData.Data1);
    private const string Data2Key = nameof(MergedCompletionListData.Data2);
    private static readonly object s_emptyData = new();

    [return: NotNullIfNotNull(nameof(razorCompletionList))]
    [return: NotNullIfNotNull(nameof(delegatedCompletionList))]
    public static RazorVSInternalCompletionList? Merge(RazorVSInternalCompletionList? razorCompletionList, RazorVSInternalCompletionList? delegatedCompletionList)
    {
        // In VSCode case we always think completion was invoked explicitly and create empty Razor completion list,
        // so check for empty Items collection as well. 
        if (razorCompletionList is null || razorCompletionList.Items.Length == 0)
        {
            return delegatedCompletionList;
        }

        if (delegatedCompletionList is null || delegatedCompletionList.Items.Length == 0)
        {
            return razorCompletionList;
        }

        EnsureMergeableCommitCharacters(razorCompletionList, delegatedCompletionList);
        EnsureMergeableEditRange(razorCompletionList, delegatedCompletionList);
        EnsureMergeableData(razorCompletionList, delegatedCompletionList);

        var mergedIsIncomplete = razorCompletionList.IsIncomplete || delegatedCompletionList.IsIncomplete;
        VSInternalCompletionItem[] mergedItems = [.. razorCompletionList.Items, .. delegatedCompletionList.Items];
        var mergedData = MergeData(razorCompletionList.Data, delegatedCompletionList.Data);
        var mergedSuggestionMode = razorCompletionList.SuggestionMode || delegatedCompletionList.SuggestionMode;

        // We don't fully support merging continue characters currently. Razor doesn't currently use them so delegated completion lists always win.
        var mergedContinueWithCharacters = razorCompletionList.ContinueCharacters ?? delegatedCompletionList.ContinueCharacters;

        var mergedItemDefaultsData = MergeData(razorCompletionList.ItemDefaults?.Data, delegatedCompletionList.ItemDefaults?.Data);

        // After EnsureMergeableEditRange, one of three states holds:
        // 1. At most one list had an EditRange (no conflict, pick the non-null one).
        // 2. Both share the same range (safe to pick either).
        // 3. Ranges differed and both were dematerialized to null.
        var mergedEditRange = razorCompletionList.ItemDefaults?.EditRange ?? delegatedCompletionList.ItemDefaults?.EditRange;

        var mergedCompletionList = new RazorVSInternalCompletionList()
        {
            // CommitCharacters intentionally null — EnsureMergeableCommitCharacters dematerialized
            // both lists to per-item. The post-merge optimizer will re-promote the best group.
            Data = mergedData,
            IsIncomplete = mergedIsIncomplete,
            Items = mergedItems,
            SuggestionMode = mergedSuggestionMode,
            ContinueCharacters = mergedContinueWithCharacters,
            ItemDefaults = new CompletionListItemDefaults()
            {
                Data = mergedItemDefaultsData,
                EditRange = mergedEditRange,
            }
        };

        return mergedCompletionList;
    }

    public static object? MergeData(object? data1, object? data2)
    {
        if (data1 is null)
        {
            return data2;
        }

        if (data2 is null)
        {
            return data1;
        }

        return new MergedCompletionListData(data1, data2);
    }

    public static bool TrySplit(object? data, out ImmutableArray<JsonElement> splitData)
    {
        if (data is null)
        {
            splitData = default;
            return false;
        }

        // Needed for tests. We shouldn't ever have RazorCompletionResolveData leak out, but in our tests we avoid some
        // serialization boundaries, like between devenv and OOP. In production not only should it never happen, but
        // if it did, the type of Data would be JsonElement, so we wouldn't fall into this branch anyway.
        if (data is RazorCompletionResolveData { OriginalData: var originalData })
        {
            return TrySplit(originalData, out splitData);
        }

        using var collector = new PooledArrayBuilder<JsonElement>();
        Split(data, ref collector.AsRef());

        if (collector.Count == 0)
        {
            splitData = default;
            return false;
        }

        splitData = collector.ToImmutable();
        return true;
    }

    private static void Split(object data, ref PooledArrayBuilder<JsonElement> collector)
    {
        if (data is MergedCompletionListData mergedData)
        {
            // Merged data adds an extra object wrapper around the original data, so remove
            // that to restore to the original form.
            Split(mergedData.Data1, ref collector);
            Split(mergedData.Data2, ref collector);
            return;
        }

        TrySplitJsonElement(data, ref collector);
    }

    private static void TrySplitJsonElement(object data, ref PooledArrayBuilder<JsonElement> collector)
    {
        if (data is not JsonElement jsonElement)
        {
            return;
        }

        if (jsonElement.TryGetProperty(Data1Key, out _) && jsonElement.TryGetProperty(Data2Key, out _))
        {
            // Merged data
            var mergedCompletionListData = jsonElement.Deserialize<MergedCompletionListData>();

            if (mergedCompletionListData is null)
            {
                Debug.Fail("Merged completion list data is null, this should never happen.");
                return;
            }

            Split(mergedCompletionListData.Data1, ref collector);
            Split(mergedCompletionListData.Data2, ref collector);
        }
        else
        {
            collector.Add(jsonElement);
        }
    }

    private static void EnsureMergeableData(RazorVSInternalCompletionList completionListA, RazorVSInternalCompletionList completionListB)
    {
        var completionListAData = completionListA.Data ?? completionListA.ItemDefaults?.Data;
        var completionListBData = completionListB.Data ?? completionListB.ItemDefaults?.Data;
        if (completionListAData != completionListBData &&
            (completionListAData is null || completionListBData is null))
        {
            // One of the completion lists have data while the other does not, we need to ensure that any non-data centric items don't get incorrect data associated

            // The candidate completion list will be one where we populate empty data for any `null` specifying data given we'll be merging
            // two completion lists together we don't want incorrect data to be inherited down
            var candidateCompletionList = completionListAData is null ? completionListA : completionListB;
            for (var i = 0; i < candidateCompletionList.Items.Length; i++)
            {
                var item = candidateCompletionList.Items[i];
                item.Data ??= s_emptyData;
            }
        }
    }

    private static void EnsureMergeableEditRange(RazorVSInternalCompletionList completionListA, RazorVSInternalCompletionList completionListB)
    {
        var editRangeA = completionListA.ItemDefaults?.EditRange?.Value;
        var editRangeB = completionListB.ItemDefaults?.EditRange?.Value;

        if (editRangeA is null || editRangeB is null)
        {
            // At most one list uses EditRange — no conflict. The merged list can
            // just take whichever is non-null (handled by the merged ItemDefaults).
            return;
        }

        // If both lists share the same EditRange, no dematerialization is needed.
        // Merge can preserve that shared range via the merged ItemDefaults.
        if (editRangeA is LspRange rangeA &&
            editRangeB is LspRange rangeB &&
            rangeA.Equals(rangeB))
        {
            return;
        }

        // Ranges differ — dematerialize both to per-item TextEdits so the post-merge
        // optimizer can evaluate the full combined list correctly.
        DematerializeEditRange(completionListA);
        DematerializeEditRange(completionListB);

        static void DematerializeEditRange(RazorVSInternalCompletionList completionList)
        {
            if (completionList.ItemDefaults?.EditRange?.Value is not LspRange range)
            {
                // Either no EditRange, or it's an InsertReplaceRange. InsertReplaceRange is intentionally
                // not dematerialized — it requires reconstructing InsertReplaceEdit (two ranges per item),
                // and no current provider produces it. Items using InsertReplaceRange remain valid after
                // merge because the merged ItemDefaults preserves it via null-coalescing.
                return;
            }

            completionList.ItemDefaults.EditRange = null;

            foreach (var item in completionList.Items)
            {
                if (item.TextEdit is null)
                {
                    // Reconstruct TextEdit from EditRange + TextEditText (or Label as fallback).
                    var newText = item.TextEditText ?? item.Label;
                    item.TextEditText = null;
                    item.TextEdit = new TextEdit { Range = range, NewText = newText };
                }
            }
        }
    }

    private static void EnsureMergeableCommitCharacters(RazorVSInternalCompletionList completionListA, RazorVSInternalCompletionList completionListB)
    {
        // Dematerialize list-level commit characters from both lists so that all items carry
        // explicit per-item chars. The post-merge optimizer will re-promote the best group.
        DematerializeCommitCharacters(completionListA);
        DematerializeCommitCharacters(completionListB);

        static void DematerializeCommitCharacters(RazorVSInternalCompletionList completionList)
        {
            var listChars = completionList.CommitCharacters;
            if (listChars is not null)
            {
                completionList.CommitCharacters = null;
            }
            else if (completionList.ItemDefaults?.CommitCharacters is { } defaultChars)
            {
                listChars = defaultChars;
                completionList.ItemDefaults.CommitCharacters = null;
            }

            if (listChars is not null)
            {
                foreach (var item in completionList.Items)
                {
                    if (item is VSInternalCompletionItem vsItem &&
                        vsItem.CommitCharacters is null &&
                        vsItem.VsCommitCharacters is null)
                    {
                        vsItem.VsCommitCharacters = listChars;
                    }
                }
            }
        }
    }

    private record MergedCompletionListData(
        [property: JsonPropertyName(Data1Key)] object Data1,
        [property: JsonPropertyName(Data2Key)] object Data2);
}
