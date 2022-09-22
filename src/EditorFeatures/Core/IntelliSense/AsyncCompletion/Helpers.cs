// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
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
        public static RoslynTrigger GetRoslynTrigger(EditorAsyncCompletionData.CompletionTrigger trigger, SnapshotPoint triggerLocation)
        {
            var completionTriggerKind = GetRoslynTriggerKind(trigger.Reason);
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

        public static CompletionTriggerKind GetRoslynTriggerKind(EditorAsyncCompletionData.CompletionTriggerReason triggerReason)
        {
            return triggerReason switch
            {
                EditorAsyncCompletionData.CompletionTriggerReason.InvokeAndCommitIfUnique => CompletionTriggerKind.InvokeAndCommitIfUnique,
                EditorAsyncCompletionData.CompletionTriggerReason.Insertion => CompletionTriggerKind.Insertion,
                EditorAsyncCompletionData.CompletionTriggerReason.Deletion or EditorAsyncCompletionData.CompletionTriggerReason.Backspace => CompletionTriggerKind.Deletion,
                EditorAsyncCompletionData.CompletionTriggerReason.SnippetsMode => CompletionTriggerKind.Snippets,
                _ => CompletionTriggerKind.Invoke,
            };
        }

        public static CompletionFilterReason GetFilterReason(EditorAsyncCompletionData.CompletionTriggerReason triggerReason)
        {
            return triggerReason switch
            {
                EditorAsyncCompletionData.CompletionTriggerReason.Insertion => CompletionFilterReason.Insertion,
                EditorAsyncCompletionData.CompletionTriggerReason.Deletion or EditorAsyncCompletionData.CompletionTriggerReason.Backspace => CompletionFilterReason.Deletion,
                _ => CompletionFilterReason.Other,
            };
        }

        public static bool IsFilterCharacter(RoslynCompletionItem item, char ch, string textTypedSoFar)
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

        public static bool TextTypedSoFarMatchesItem(RoslynCompletionItem item, string textTypedSoFar)
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
        public static bool IsStandardCommitCharacter(char c)
            => c is '\t' or '\n' or '\0';

        // This is a temporarily method to support preference of IntelliCode items comparing to non-IntelliCode items.
        // We expect that Editor will introduce this support and we will get rid of relying on the "★" then.
        public static bool IsPreferredItem(this VSCompletionItem completionItem)
            => completionItem.DisplayText.StartsWith("★");
    }
}
