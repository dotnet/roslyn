// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// The kind of action that triggered completion to start.
    /// </summary>
    public enum CompletionTriggerKind
    {
        /// <summary>
        /// Completion was triggered via some other mechanism.
        /// </summary>
        [Obsolete("Do not use.", error: true)]
        Other = 0,

        /// <summary>
        /// Completion was triggered via an action inserting a character into the document.
        /// </summary>
        Insertion,

        /// <summary>
        /// Completion was triggered via an action deleting a character from the document.
        /// </summary>
        Deletion,

        /// <summary>
        /// Completion was triggered for snippets only.
        /// </summary>
        Snippets,

        /// <summary>
        /// Completion was trigger by a direct invocation of the completion feature 
        /// (ctrl-j in Visual Studio).
        /// </summary>
        Invoke,

        /// <summary>
        /// Completion was triggered with a request to commit if a unique item would be selected 
        /// (ctrl-space in Visual Studio).
        /// </summary>
        InvokeAndCommitIfUnique
    }
}
