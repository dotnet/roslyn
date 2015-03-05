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

        /// <summary>
        /// Returns true if the reason completion was triggered was to augment an existing list of
        /// completion items.
        /// </summary>
        public bool IsAugment { get; }

        /// <summary>
        ///  Returns true if completion was triggered by the debugger.
        /// </summary>
        internal bool IsDebugger { get; }

        /// <summary>
        /// Return true if completion is running in the Immediate Window.
        /// </summary>
        internal bool IsImmediateWindow { get; }

        private CompletionTriggerInfo(CompletionTriggerReason triggerReason, char? triggerCharacter, bool isAugment, bool isDebugger, bool isImmediateWindow)
            : this()
        {
            Contract.ThrowIfTrue(triggerReason == CompletionTriggerReason.TypeCharCommand && triggerCharacter == null);
            this.TriggerReason = triggerReason;
            this.TriggerCharacter = triggerCharacter;
            this.IsAugment = isAugment;
            this.IsDebugger = isDebugger;
            this.IsImmediateWindow = isImmediateWindow;
        }

        public static CompletionTriggerInfo CreateTypeCharTriggerInfo(char triggerCharacter, bool isAugment = false)
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.TypeCharCommand, triggerCharacter, isAugment, isDebugger: false, isImmediateWindow: false);
        }

        public static CompletionTriggerInfo CreateInvokeCompletionTriggerInfo(bool isAugment = false)
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.InvokeCompletionCommand, null, isAugment, isDebugger: false, isImmediateWindow: false);
        }

        public static CompletionTriggerInfo CreateBackspaceTriggerInfo(char? triggerCharacter, bool isAugment = false)
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.BackspaceOrDeleteCommand, triggerCharacter, isAugment, isDebugger: false, isImmediateWindow: false);
        }

        public static CompletionTriggerInfo CreateSnippetTriggerInfo()
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.Snippets, null, isAugment: false, isDebugger: false, isImmediateWindow: false);
        }

        internal CompletionTriggerInfo WithIsAugment(bool isAugment)
        {
            return this.IsAugment == isAugment
                ? this
                : new CompletionTriggerInfo(this.TriggerReason, this.TriggerCharacter, isAugment, this.IsDebugger, this.IsImmediateWindow);
        }

        internal CompletionTriggerInfo WithIsDebugger(bool isDebugger)
        {
            return this.IsDebugger == isDebugger
                ? this
                : new CompletionTriggerInfo(this.TriggerReason, this.TriggerCharacter, this.IsAugment, isDebugger, this.IsImmediateWindow);
        }

        internal CompletionTriggerInfo WithIsImmediateWindow(bool isImmediateWIndow)
        {
            return this.IsImmediateWindow == isImmediateWIndow
                ? this
                : new CompletionTriggerInfo(this.TriggerReason, this.TriggerCharacter, this.IsAugment, this.IsDebugger, isImmediateWIndow);
        }
    }
}
