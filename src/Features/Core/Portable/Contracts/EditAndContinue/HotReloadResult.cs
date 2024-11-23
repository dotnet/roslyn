// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

/// <summary>
/// Result for a hot reload apply operation.
/// </summary>
internal enum HotReloadResult
{
    /// <summary>
    /// Successfully applied the changes.
    /// </summary>
    Applied = 0,

    /// <summary>
    /// No changes were found.
    /// </summary>
    NoChanges = 1,

    /// <summary>
    /// Rude edits were found. 
    /// Changes can be applied by restarting the session.
    /// </summary>
    RestartRequired = 2,

    /// <summary>
    /// Edits with a compiler error were found.
    /// This assumes that the agents do not support restart and any rude edits were treated as errors.
    /// </summary>
    ErrorEdits = 3,

    /// <summary>
    /// An internal error was found while applying code updates. This will generally be propagated through an exception.
    /// </summary>
    ApplyUpdateFailure = 4
}
