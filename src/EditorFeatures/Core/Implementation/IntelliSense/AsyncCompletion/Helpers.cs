// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal static class Helpers
    {
        /// <summary>
        /// Attempts to convert VS Completion trigger into Roslyn completion trigger
        /// </summary>
        /// <param name="trigger">VS completion trigger</param>
        /// <param name="triggerLocation">Character. 
        /// VS provides Backspace and Delete characters inside the trigger while Roslyn needs the char deleted by the trigger.
        /// Therefore, we provide this character separately and use it for Delete and Backspace cases only.
        /// We retrieve this character from triggerLocation.
        /// </param>
        /// <returns>Roslyn completion trigger</returns>
        internal static RoslynTrigger GetRoslynTrigger(AsyncCompletionData.CompletionTrigger trigger, SnapshotPoint triggerLocation)
        {
            switch (trigger.Reason)
            {
                case AsyncCompletionData.CompletionTriggerReason.InvokeAndCommitIfUnique:
                    return new RoslynTrigger(CompletionTriggerKind.InvokeAndCommitIfUnique);
                case AsyncCompletionData.CompletionTriggerReason.Insertion:
                    return RoslynTrigger.CreateInsertionTrigger(trigger.Character);
                case AsyncCompletionData.CompletionTriggerReason.Deletion:
                case AsyncCompletionData.CompletionTriggerReason.Backspace:
                    var snapshotBeforeEdit = trigger.ViewSnapshotBeforeTrigger;
                    char characterRemoved;
                    if (triggerLocation.Position >= 0 && triggerLocation.Position < snapshotBeforeEdit.Length)
                    {
                        // If multiple characters were removed (selection), this finds the first character from the left. 
                        characterRemoved = snapshotBeforeEdit[triggerLocation.Position];
                    }
                    else
                    {
                        characterRemoved = (char)0;
                    }

                    return RoslynTrigger.CreateDeletionTrigger(characterRemoved);
                case AsyncCompletionData.CompletionTriggerReason.SnippetsMode:
                    return new RoslynTrigger(CompletionTriggerKind.Snippets);
                default:
                    return RoslynTrigger.Invoke;
            }
        }

        internal static CompletionFilterReason GetFilterReason(AsyncCompletionData.CompletionTrigger trigger)
        {
            switch (trigger.Reason)
            {
                case AsyncCompletionData.CompletionTriggerReason.Insertion:
                    return CompletionFilterReason.Insertion;
                case AsyncCompletionData.CompletionTriggerReason.Deletion:
                case AsyncCompletionData.CompletionTriggerReason.Backspace:
                    return CompletionFilterReason.Deletion;
                default:
                    return CompletionFilterReason.Other;
            }
        }

        internal static bool IsFilterCharacter(RoslynCompletionItem item, char ch, string textTypedSoFar)
        {
            // First see if the item has any specific filter rules it wants followed.
            foreach (var rule in item.Rules.FilterCharacterRules)
            {
                switch (rule.Kind)
                {
                    case CharacterSetModificationKind.Add:
                        if (rule.Characters.Contains(ch))
                        {
                            return true;
                        }
                        continue;

                    case CharacterSetModificationKind.Remove:
                        if (rule.Characters.Contains(ch))
                        {
                            return false;
                        }
                        continue;

                    case CharacterSetModificationKind.Replace:
                        return rule.Characters.Contains(ch);
                }
            }

            // general rule: if the filtering text exactly matches the start of the item then it must be a filter character
            if (CommitManager.TextTypedSoFarMatchesItem(item, textTypedSoFar))
            {
                return true;
            }

            return false;
        }

        // This is a temporarily method to support preference of IntelliCode items comparing to non-IntelliCode items.
        // We expect that Editor will intorduce this support and we will get rid of relying on the "★" then.
        internal static bool IsPreferredItem(this RoslynCompletionItem completionItem)
            => completionItem.DisplayText.StartsWith("★");

        // This is a temporarily method to support preference of IntelliCode items comparing to non-IntelliCode items.
        // We expect that Editor will intorduce this support and we will get rid of relying on the "★" then.
        internal static bool IsPreferredItem(this VSCompletionItem completionItem)
            => completionItem.DisplayText.StartsWith("★");
    }
}
