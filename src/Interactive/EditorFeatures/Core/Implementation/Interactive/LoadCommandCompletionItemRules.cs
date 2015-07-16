// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    internal class LoadCommandCompletionItemRules : CompletionItemRules
    {
        public static LoadCommandCompletionItemRules Instance = new LoadCommandCompletionItemRules();

        public override Result<bool> SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            return PathCompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
        }
    }
}
