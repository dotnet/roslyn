// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.AsyncCompletion
{
    internal static class Helpers
    {
        internal static bool TryGetRoslynTrigger(AsyncCompletionData.CompletionTrigger trigger, SnapshotPoint triggerLocation, out RoslynTrigger roslynTrigger)
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

            return TryGetRoslynTrigger(trigger, characterRemoved, out roslynTrigger);
        }

        internal static bool TryGetRoslynTrigger(AsyncCompletionData.CompletionTrigger trigger, char c, out RoslynTrigger roslynTrigger)
        {
            switch (trigger.Reason)
            {
                case AsyncCompletionData.CompletionTriggerReason.Invoke:
                case AsyncCompletionData.CompletionTriggerReason.InvokeAndCommitIfUnique:
                    roslynTrigger = RoslynTrigger.Invoke;
                    return true;
                case AsyncCompletionData.CompletionTriggerReason.Insertion:
                    roslynTrigger = RoslynTrigger.CreateInsertionTrigger(trigger.Character);
                    return true;
                case AsyncCompletionData.CompletionTriggerReason.Deletion:
                    roslynTrigger = RoslynTrigger.CreateDeletionTrigger(c);
                    return true;
                case AsyncCompletionData.CompletionTriggerReason.Backspace:
                    roslynTrigger = RoslynTrigger.CreateDeletionTrigger(c);
                    return true;
                case AsyncCompletionData.CompletionTriggerReason.SnippetsMode:
                    roslynTrigger = new RoslynTrigger(CompletionTriggerKind.Snippets);
                    return true;
            }

            roslynTrigger = default;
            return false;
        }

        internal static CompletionFilterReason GetFilterReason(RoslynTrigger trigger)
        {
            switch(trigger.Kind)
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
