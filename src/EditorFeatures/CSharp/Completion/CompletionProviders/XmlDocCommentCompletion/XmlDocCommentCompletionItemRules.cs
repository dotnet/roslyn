// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.CompletionProviders.XmlDocCommentCompletion;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders.XmlDocCommentCompletion
{
    internal class XmlDocCommentCompletionItemRules : AbstractXmlDocCommentCompletionItemRules
    {
        public static XmlDocCommentCompletionItemRules Instance { get; } = new XmlDocCommentCompletionItemRules();

        public override bool? IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            if ((ch == '"' || ch == ' ')
                && completionItem.DisplayText.Contains(ch))
            {
                return false;
            }

            if (ch == '>' || ch == '\t')
            {
                return true;
            }

            return base.IsCommitCharacter(completionItem, ch, textTypedSoFar);
        }
    }
}
