// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class CrefCompletionItemRules : CompletionItemRules
    {
        public static CrefCompletionItemRules Instance { get; } = new CrefCompletionItemRules();

        public override Result<bool> IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            if (ch == '{' && completionItem.DisplayText.Contains('{'))
            {
                return false;
            }

            if (ch == '(' && completionItem.DisplayText.Contains('('))
            {
                return false;
            }

            return base.IsCommitCharacter(completionItem, ch, textTypedSoFar);
        }
    }
}
