// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal partial class CompletionItemRules
    {
        public static CompletionItemRules Default = new CompletionItemRules();

        /// <summary>
        /// Returns true if the character is one that can commit the specified completion item. A
        /// character will be checked to see if it should filter an item.  If not, it will be checked
        /// to see if it should commit that item.  If it does neither, then completion will be
        /// dismissed.
        /// </summary>
        public virtual Result<bool> IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return Result<bool>.Default;
        }

        /// <summary>
        /// Returns true if the character typed should be used to filter the specified completion
        /// item.  A character will be checked to see if it should filter an item.  If not, it will be
        /// checked to see if it should commit that item.  If it does neither, then completion will
        /// be dismissed.
        /// </summary>
        public virtual Result<bool> IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return Result<bool>.Default;
        }

        /// <summary>
        /// Returns true if the enter key that was typed should also be sent through to the editor
        /// after committing the provided completion item.
        /// </summary>
        public virtual Result<bool> SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            return Result<bool>.Default;
        }
    }
}
