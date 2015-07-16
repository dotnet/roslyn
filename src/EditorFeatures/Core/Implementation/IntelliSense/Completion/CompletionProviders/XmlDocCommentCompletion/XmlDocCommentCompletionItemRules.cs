// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.CompletionProviders.XmlDocCommentCompletion
{
    internal class XmlDocCommentCompletionItemRules : CompletionItemRules
    {
        public static XmlDocCommentCompletionItemRules Instance = new XmlDocCommentCompletionItemRules();

        public override Result<bool> IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            if (ch == '!' || ch == '-' || ch == '[')
            {
                return true;
            }

            return base.IsFilterCharacter(completionItem, ch, textTypedSoFar);
        }

        public override Result<bool> SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            return false;
        }
    }
}
