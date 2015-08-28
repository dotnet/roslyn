// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Completion
{
    internal enum CompletionTriggerReason
    {
        /// <summary>
        /// Completion was triggered through the 'Invoke Completion' command
        /// </summary>
        InvokeCompletionCommand,

        /// <summary>
        /// Completion was triggered through the 'Type Char' command.
        /// </summary>
        TypeCharCommand,

        /// <summary>
        /// Completion was triggered through the 'Backspace' command or the 'Delete' command.
        /// </summary>
        BackspaceOrDeleteCommand,

        /// <summary>
        /// Completion was triggered to show the list of Snippets.
        /// </summary>
        Snippets
    }
}
