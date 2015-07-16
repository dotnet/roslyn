// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders
{
    internal class ReferenceDirectiveCompletionItemRules : CompletionItemRules
    {
        public static ReferenceDirectiveCompletionItemRules Instance = new ReferenceDirectiveCompletionItemRules();

        public override Result<bool> IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return PathCompletionUtilities.IsFilterCharacter(completionItem, ch, textTypedSoFar);
        }

        public override Result<bool> SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            return PathCompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
        }
    }
}
