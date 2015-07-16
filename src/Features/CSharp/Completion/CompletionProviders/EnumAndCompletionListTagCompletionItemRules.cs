// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class EnumAndCompletionListTagCompletionItemRules : CompletionItemRules
    {
        public static readonly EnumAndCompletionListTagCompletionItemRules Instance = new EnumAndCompletionListTagCompletionItemRules();

        public override Result<bool> IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            // Only commit on dot.
            return ch == '.';
        }
    }
}
