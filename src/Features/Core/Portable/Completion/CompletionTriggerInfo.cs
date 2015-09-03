// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// Provides information about what triggered completion.
    /// </summary>
    internal struct CompletionTriggerInfo
    {
        /// <summary>
        /// Provides the reason that completion was triggered.
        /// </summary>
        public CompletionTriggerReason TriggerReason { get; }

        /// <summary>
        /// If the <see cref="TriggerReason"/> was <see
        /// cref="CompletionTriggerReason.TypeCharCommand"/> then this was the character that was
        /// typed or deleted by backspace.  Otherwise it is null.
        /// </summary>
        public char? TriggerCharacter { get; }

        private CompletionTriggerInfo(CompletionTriggerReason triggerReason, char? triggerCharacter)
            : this()
        {
            Contract.ThrowIfTrue(triggerReason == CompletionTriggerReason.TypeCharCommand && triggerCharacter == null);
            this.TriggerReason = triggerReason;
            this.TriggerCharacter = triggerCharacter;
        }

        public static CompletionTriggerInfo CreateTypeCharTriggerInfo(char triggerCharacter)
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.TypeCharCommand, triggerCharacter);
        }

        public static CompletionTriggerInfo CreateInvokeCompletionTriggerInfo()
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.InvokeCompletionCommand, null);
        }

        public static CompletionTriggerInfo CreateBackspaceTriggerInfo(char? triggerCharacter)
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.BackspaceOrDeleteCommand, triggerCharacter);
        }

        public static CompletionTriggerInfo CreateSnippetTriggerInfo()
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.Snippets, null);
        }
    }
}
