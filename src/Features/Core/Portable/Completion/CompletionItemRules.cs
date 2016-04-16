// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    internal partial class CompletionItemRules
    {
        public static CompletionItemRules DefaultRules = new CompletionItemRules();

        /// <summary>
        /// The text change that will be made when this item is committed.  The text change includes
        /// both the span of text to replace (respective to the original document text when this
        /// completion item was created) and the text to replace it with.  The span will be adjusted
        /// automatically by the completion engine to fit on the current text using "EdgeInclusive"
        /// semantics.
        /// </summary>
        public virtual TextChange? GetTextChange(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            return null;
        }

        /// <summary>
        /// Returns true if the character is one that can commit the specified completion item. A
        /// character will be checked to see if it should filter an item.  If not, it will be checked
        /// to see if it should commit that item.  If it does neither, then completion will be
        /// dismissed.
        /// </summary>
        public virtual bool? IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return null;
        }

        /// <summary>
        /// Returns true if the character typed should be used to filter the specified completion
        /// item.  A character will be checked to see if it should filter an item.  If not, it will be
        /// checked to see if it should commit that item.  If it does neither, then completion will
        /// be dismissed.
        /// </summary>
        public virtual bool? IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return null;
        }

        /// <summary>
        /// Returns true if the enter key that was typed should also be sent through to the editor
        /// after committing the provided completion item.
        /// </summary>
        public virtual bool? SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            return null;
        }
    }
}
