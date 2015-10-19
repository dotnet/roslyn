// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal class KeywordCompletionItemRules : CompletionItemRules
    {
        public static KeywordCompletionItemRules Instance { get; } = new KeywordCompletionItemRules();

        public override TextChange? GetTextChange(CompletionItem selectedItem, char? ch = default(char?), string textTypedSoFar = null)
        {
            var insertionText = selectedItem.DisplayText;
            if (ch == ' ' && textTypedSoFar != null)
            {
                if (insertionText.StartsWith(textTypedSoFar, StringComparison.OrdinalIgnoreCase))
                {
                    insertionText = insertionText.Substring(0, textTypedSoFar.Length - 1);
                }
            }

            return new TextChange(selectedItem.FilterSpan, insertionText);
        }
    }
}
