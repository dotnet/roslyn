// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Snippets
    }
}
