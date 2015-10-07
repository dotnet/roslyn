// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem
{
    internal partial class LoadDirectiveCompletionProvider
    {
        private class ItemRules : CompletionItemRules
        {
            public static ItemRules Instance = new ItemRules();

            public override TextChange? GetTextChange(CompletionItem selectedItem, char? ch = default(char?), string textTypedSoFar = null)
            {
                // When we commit "\\" when the user types \ we have to adjust for the fact that the
                // controller will automatically append \ after we commit.  Because of that, we don't
                // want to actually commit "\\" as we'll end up with "\\\".  So instead we just commit
                // "\" and know that controller will append "\" and give us "\\".
                if (selectedItem.DisplayText == NetworkPath && ch == '\\')
                {
                    return new TextChange(selectedItem.FilterSpan, "\\");
                }

                return base.GetTextChange(selectedItem, ch, textTypedSoFar);
            }

            public override bool? IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
            {
                return PathCompletionUtilities.IsCommitcharacter(completionItem, ch, textTypedSoFar);
            }

            public override bool? IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
            {
                // If they've typed '\\', then we do not consider \ to be a filter character.  We want to
                // just commit at this point.
                if (textTypedSoFar == NetworkPath)
                {
                    return false;
                }

                return PathCompletionUtilities.IsFilterCharacter(completionItem, ch, textTypedSoFar);
            }

            public override bool? SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
            {
                return PathCompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
            }
        }
    }
}