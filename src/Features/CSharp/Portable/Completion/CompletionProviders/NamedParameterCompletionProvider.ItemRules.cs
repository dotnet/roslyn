// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class NamedParameterCompletionProvider
    {
        private class ItemRules : CompletionItemRules
        {
            public static ItemRules Instance { get; } = new ItemRules();

            public override TextChange? GetTextChange(CompletionItem selectedItem, char? ch = default(char?), string textTypedSoFar = null)
            {
                return new TextChange(
                    selectedItem.FilterSpan,
                    selectedItem.DisplayText.Substring(0, selectedItem.DisplayText.Length - ColonString.Length));
            }
        }
    }
}
