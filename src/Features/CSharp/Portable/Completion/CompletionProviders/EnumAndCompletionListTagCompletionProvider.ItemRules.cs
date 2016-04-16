// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class EnumAndCompletionListTagCompletionProvider
    {
        private class ItemRules : CompletionItemRules
        {
            public static readonly ItemRules Instance = new ItemRules();

            public override bool? IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
            {
                // Only commit on dot.
                return ch == '.';
            }
        }
    }
}
