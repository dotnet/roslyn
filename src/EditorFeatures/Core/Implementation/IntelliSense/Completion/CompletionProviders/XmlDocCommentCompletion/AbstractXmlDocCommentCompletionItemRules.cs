// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.CompletionProviders.XmlDocCommentCompletion
{
    internal abstract class AbstractXmlDocCommentCompletionItemRules : CompletionItemRules
    {
        public override bool? IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            if (ch == '!' || ch == '-' || ch == '[')
            {
                return true;
            }

            return base.IsFilterCharacter(completionItem, ch, textTypedSoFar);
        }

        public override bool? SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            return false;
        }
    }
}
