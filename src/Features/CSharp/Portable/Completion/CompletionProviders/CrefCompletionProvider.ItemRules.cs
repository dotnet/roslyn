// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class CrefCompletionProvider
    {
        internal class ItemRules : CompletionItemRules
        {
            public static ItemRules Instance { get; } = new ItemRules();

            public override TextChange? GetTextChange(CompletionItem selectedItem, char? ch = default(char?), string textTypedSoFar = null)
            {
                return new TextChange(selectedItem.FilterSpan, ((Item)selectedItem).InsertionText);
            }

            public override bool? IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
            {
                if (ch == '{' && completionItem.DisplayText.Contains("{"))
                {
                    return false;
                }

                if (ch == '(' && completionItem.DisplayText.Contains("("))
                {
                    return false;
                }

                return base.IsCommitCharacter(completionItem, ch, textTypedSoFar);
            }
        }
    }
}