// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Completion
{
    internal class CSharpCompletionRules : CompletionRules
    {
        public CSharpCompletionRules(AbstractCompletionService completionService)
            : base(completionService)
        {
        }

        protected override bool SendEnterThroughToEditorCore(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            // If the text doesn't match, no reason to even check the options
            if (completionItem.DisplayText != textTypedSoFar)
            {
                return false;
            }

            return options.GetOption(CSharpCompletionOptions.AddNewLineOnEnterAfterFullyTypedWord);
        }
    }
}
