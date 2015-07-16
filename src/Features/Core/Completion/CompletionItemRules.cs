// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal partial class CompletionItemRules
    {
        public static CompletionItemRules Default = new CompletionItemRules();

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
