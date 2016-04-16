// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Completion.FileSystem
{
    internal partial class AbstractReferenceDirectiveCompletionProvider
    {
        private class ItemRules : CompletionItemRules
        {
            public static ItemRules Instance = new ItemRules();

            public override bool? IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
            {
                return PathCompletionUtilities.IsCommitcharacter(completionItem, ch, textTypedSoFar);
            }

            public override bool? IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
            {
                return PathCompletionUtilities.IsFilterCharacter(completionItem, ch, textTypedSoFar);
            }

            public override bool? SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
            {
                return PathCompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
            }
        }
    }
}