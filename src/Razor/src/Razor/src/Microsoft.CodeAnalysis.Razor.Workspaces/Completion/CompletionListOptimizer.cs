// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal static partial class CompletionListOptimizer
{
    public static RazorVSInternalCompletionList Optimize(RazorVSInternalCompletionList completionList, CompletionSetting? completionCapability)
    {
        completionList = PromoteCommitCharacters(completionList, completionCapability);
        completionList = PromoteEditRangeToListDefaults(completionList, completionCapability);

        return completionList;
    }

    private static RazorVSInternalCompletionList PromoteEditRangeToListDefaults(RazorVSInternalCompletionList completionList, CompletionSetting? completionCapability)
    {
        // Check if client supports editRange in ItemDefaults
        var itemDefaults = completionCapability?.CompletionListSetting?.ItemDefaults;
        if (itemDefaults is null || Array.IndexOf(itemDefaults, "editRange") < 0)
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
            item.TextEdit = null;

            // If TextEditText would equal Label, omit it — the client falls back to Label.
            if (textEdit.NewText != item.Label)
            {
                item.TextEditText = textEdit.NewText;
            }
        }

        return completionList;
    }
}
