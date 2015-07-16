// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class ObjectCreationCompletionItemRules : CompletionItemRules
    {
        public static ObjectCreationCompletionItemRules Instance { get; } = new ObjectCreationCompletionItemRules();

        public override Result<bool> IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            // TODO(cyrusn): We could just allow the standard list of completion characters.
            // However, i'd like to see what the experience is like really filtering down to the set
            // of things that is allowable.
            return ch == ' ' || ch == '(' || ch == '{' || ch == '[';
        }
    }
}
