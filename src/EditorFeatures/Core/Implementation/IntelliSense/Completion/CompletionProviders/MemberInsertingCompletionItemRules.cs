// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal class MemberInsertingCompletionItemRules : CompletionItemRules
    {
        public static MemberInsertingCompletionItemRules Instance { get; } = new MemberInsertingCompletionItemRules();

        public override TextChange? GetTextChange(CompletionItem selectedItem, char? ch = default(char?), string textTypedSoFar = null)
        {
            return default(TextChange);
        }

        public override bool? IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            // Commit on tab, enter and (
            return ch == '(';
        }

        public override bool? SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            return false;
        }
    }
}
