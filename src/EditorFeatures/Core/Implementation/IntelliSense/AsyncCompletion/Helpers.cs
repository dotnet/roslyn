// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.AsyncCompletion
{
    internal static class Helpers
    {
        internal static RoslynTrigger GetRoslynTrigger(AsyncCompletionData.CompletionTrigger trigger, SnapshotPoint triggerLocation)
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

            return GetRoslynTrigger(trigger, characterRemoved);
        }

        /// <summary>
        /// Attempts to convert VS Completion trigger into Roslyn completion trigget
        /// </summary>
        /// <param name="trigger">VS completion trigger</param>
        /// <param name="c">Character. 
        /// VS provides Backspace and Delete characters inside the trigger while Roslyn needs the char deleted by the trigger.
        /// Therefore, we provide this character separately and use it for Delete and Backspace cases only.
        /// </param>
        /// <returns>Roslyn completion trigger</returns>
        internal static RoslynTrigger GetRoslynTrigger(AsyncCompletionData.CompletionTrigger trigger, char c)
        {
            switch (trigger.Reason)
            {
                case AsyncCompletionData.CompletionTriggerReason.Insertion:
                    return RoslynTrigger.CreateInsertionTrigger(trigger.Character);
                case AsyncCompletionData.CompletionTriggerReason.Deletion:
                    return RoslynTrigger.CreateDeletionTrigger(c);
                case AsyncCompletionData.CompletionTriggerReason.Backspace:
                    return RoslynTrigger.CreateDeletionTrigger(c);
                case AsyncCompletionData.CompletionTriggerReason.SnippetsMode:
                    return new RoslynTrigger(CompletionTriggerKind.Snippets);
                default:
                    return RoslynTrigger.Invoke;
            }
        }

        internal static CompletionFilterReason GetFilterReason(RoslynTrigger trigger)
        {
            switch (trigger.Kind)
            {
                case CompletionTriggerKind.Insertion:
                    return CompletionFilterReason.Insertion;
                case CompletionTriggerKind.Deletion:
                    return CompletionFilterReason.Deletion;
                default:
                    return CompletionFilterReason.Other;
            }
        }
    }
}
