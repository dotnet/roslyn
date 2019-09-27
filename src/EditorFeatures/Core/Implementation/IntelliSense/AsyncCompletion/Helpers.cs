// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using EditorAsyncCompletion = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using EditorAsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;
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
        internal static RoslynTrigger GetRoslynTrigger(EditorAsyncCompletionData.CompletionTrigger trigger, SnapshotPoint triggerLocation)
        {
            var completionTriggerKind = GetRoslynTriggerKind(trigger);
            if (completionTriggerKind == CompletionTriggerKind.Deletion)
            {
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
            }
            else
            {
                return new RoslynTrigger(completionTriggerKind, trigger.Character);
            }
        }

        internal static CompletionTriggerKind GetRoslynTriggerKind(EditorAsyncCompletionData.CompletionTrigger trigger)
        {
            switch (trigger.Reason)
            {
                case EditorAsyncCompletionData.CompletionTriggerReason.InvokeAndCommitIfUnique:
                    return CompletionTriggerKind.InvokeAndCommitIfUnique;
                case EditorAsyncCompletionData.CompletionTriggerReason.Insertion:
                    return CompletionTriggerKind.Insertion;
                case EditorAsyncCompletionData.CompletionTriggerReason.Deletion:
                case EditorAsyncCompletionData.CompletionTriggerReason.Backspace:
                    return CompletionTriggerKind.Deletion;
                case EditorAsyncCompletionData.CompletionTriggerReason.SnippetsMode:
                    return CompletionTriggerKind.Snippets;
                default:
                    return CompletionTriggerKind.Invoke;
            }
        }

        internal static CompletionFilterReason GetFilterReason(EditorAsyncCompletionData.CompletionTrigger trigger)
        {
            switch (trigger.Reason)
            {
                case EditorAsyncCompletionData.CompletionTriggerReason.Insertion:
                    return CompletionFilterReason.Insertion;
                case EditorAsyncCompletionData.CompletionTriggerReason.Deletion:
                case EditorAsyncCompletionData.CompletionTriggerReason.Backspace:
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

        internal static bool TryGetInitialTriggerLocation(EditorAsyncCompletion.IAsyncCompletionSession session, out SnapshotPoint initialTriggerLocation)
        {
            if (session is EditorAsyncCompletion.IAsyncCompletionSessionOperations sessionOperations)
            {
                initialTriggerLocation = sessionOperations.InitialTriggerLocation;
                return true;
            }

            initialTriggerLocation = default;
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
