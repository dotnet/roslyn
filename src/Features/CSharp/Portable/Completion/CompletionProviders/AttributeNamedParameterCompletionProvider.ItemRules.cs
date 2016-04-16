// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class AttributeNamedParameterCompletionProvider
    {
        private class ItemRules : CompletionItemRules
        {
            public static ItemRules Instance { get; } = new ItemRules();

            public override TextChange? GetTextChange(CompletionItem selectedItem, char? ch = default(char?), string textTypedSoFar = null)
            {
                var displayText = selectedItem.DisplayText;

                if (ch != null)
                {
                    // If user types a space, do not complete the " =" (space and equals) at the end of a named parameter. The
                    // typed space character will be passed through to the editor, and they can then type the '='.
                    if (ch == ' ' && displayText.EndsWith(SpaceEqualsString, StringComparison.Ordinal))
                    {
                        return new TextChange(selectedItem.FilterSpan, displayText.Remove(displayText.Length - SpaceEqualsString.Length));
                    }

                    // If the user types '=', do not complete the '=' at the end of the named parameter because the typed '=' 
                    // will be passed through to the editor.
                    if (ch == '=' && displayText.EndsWith(EqualsString, StringComparison.Ordinal))
                    {
                        return new TextChange(selectedItem.FilterSpan, displayText.Remove(displayText.Length - EqualsString.Length));
                    }

                    // If the user types ':', do not complete the ':' at the end of the named parameter because the typed ':' 
                    // will be passed through to the editor.
                    if (ch == ':' && displayText.EndsWith(ColonString, StringComparison.Ordinal))
                    {
                        return new TextChange(selectedItem.FilterSpan, displayText.Remove(displayText.Length - ColonString.Length));
                    }
                }

                return new TextChange(selectedItem.FilterSpan, displayText);
            }
        }
    }
}