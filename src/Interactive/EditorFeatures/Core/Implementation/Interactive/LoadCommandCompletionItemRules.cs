// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    internal class LoadCommandCompletionItemRules : CompletionItemRules
    {
        public static LoadCommandCompletionItemRules Instance = new LoadCommandCompletionItemRules();

        public override Result<bool> IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return PathCompletionUtilities.IsCommitcharacter(completionItem, ch, textTypedSoFar);
        }

        public override Result<bool> IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            // If they've typed '\\', then we do not consider \ to be a filter character.  We want to
            // just commit at this point.
            if (textTypedSoFar == LoadCommandCompletionProvider.NetworkPath)
            {
                return false;
            }

            return PathCompletionUtilities.IsFilterCharacter(completionItem, ch, textTypedSoFar);
        }

        public override Result<bool> SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            return PathCompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
        }
    }
}
