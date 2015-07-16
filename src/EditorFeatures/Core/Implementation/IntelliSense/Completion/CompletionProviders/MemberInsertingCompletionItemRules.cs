// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal class MemberInsertingCompletionItemRules : CompletionItemRules
    {
        public static MemberInsertingCompletionItemRules Instance { get; } = new MemberInsertingCompletionItemRules();

        public override Result<bool> IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            // Commit on tab, enter and (
            return ch == '(';
        }

        public override Result<bool> SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            return false;
        }
    }
}
