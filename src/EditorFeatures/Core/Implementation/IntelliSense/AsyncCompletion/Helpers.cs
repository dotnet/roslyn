﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
            // Exclude standard commit character upfront because TextTypedSoFarMatchesItem can miss them on non-Windows platforms.
            if (IsStandardCommitCharacter(ch))
            {
                return false;
            }

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
            if (TextTypedSoFarMatchesItem(item, textTypedSoFar))
            {
                return true;
            }

            return false;
        }

        internal static bool TextTypedSoFarMatchesItem(RoslynCompletionItem item, string textTypedSoFar)
        {
            if (textTypedSoFar.Length > 0)
            {
                // Note that StartsWith ignores \0 at the end of textTypedSoFar on VS Mac and Mono.
                return item.DisplayText.StartsWith(textTypedSoFar, StringComparison.CurrentCultureIgnoreCase) ||
                       item.FilterText.StartsWith(textTypedSoFar, StringComparison.CurrentCultureIgnoreCase);
            }

            return false;
        }

        // Tab, Enter and Null (call invoke commit) are always commit characters. 
        internal static bool IsStandardCommitCharacter(char c)
            => c == '\t' || c == '\n' || c == '\0';

        internal static bool TryGetInitialTriggerLocation(EditorAsyncCompletion.IAsyncCompletionSession session, out SnapshotPoint initialTriggerLocation)
            => session.Properties.TryGetProperty(CompletionSource.TriggerLocation, out initialTriggerLocation);

        // This is a temporarily method to support preference of IntelliCode items comparing to non-IntelliCode items.
        // We expect that Editor will introduce this support and we will get rid of relying on the "★" then.
        // We check both the display text and the display text prefix to account for IntelliCode item providers
        // that may be using the prefix to include the ★.
        internal static bool IsPreferredItem(this RoslynCompletionItem completionItem)
            => completionItem.DisplayText.StartsWith("★") || completionItem.DisplayTextPrefix.StartsWith("★");

        // This is a temporarily method to support preference of IntelliCode items comparing to non-IntelliCode items.
        // We expect that Editor will introduce this support and we will get rid of relying on the "★" then.
        internal static bool IsPreferredItem(this VSCompletionItem completionItem)
            => completionItem.DisplayText.StartsWith("★");
    }
}
