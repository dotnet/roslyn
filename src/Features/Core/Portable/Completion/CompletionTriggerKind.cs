// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Completion;

/// <summary>
/// The kind of action that triggered completion to start.
/// </summary>
public enum CompletionTriggerKind
{
    /// <summary>
    /// Completion was triggered via some other mechanism.
    /// </summary>
    [Obsolete("Use 'Invoke' instead.")]
    Other = 0,

    /// <summary>
    /// Completion was trigger by a direct invocation of the completion feature 
    /// (ctrl-j in Visual Studio).
    /// </summary>
    Invoke = 0,

    /// <summary>
    /// Completion was triggered via an action inserting a character into the document.
    /// </summary>
    Insertion = 1,

    /// <summary>
    /// Completion was triggered via an action deleting a character from the document.
    /// </summary>
    Deletion = 2,

    /// <summary>
    /// Completion was triggered for snippets only.
    /// </summary>
    Snippets = 3,

    /// <summary>
    /// Completion was triggered with a request to commit if a unique item would be selected 
    /// (ctrl-space in Visual Studio).
    /// </summary>
    InvokeAndCommitIfUnique = 4
}
