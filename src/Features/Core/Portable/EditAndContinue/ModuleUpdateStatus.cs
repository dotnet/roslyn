// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Indicates the state of a manage module update.
/// </summary>
internal enum ModuleUpdateStatus
{
    /// <summary>
    /// No change made.
    /// Pending solution update is not created.
    /// Committed solution snapshot is advanced.
    /// </summary>
    None = 0,

    /// <summary>
    /// Changes can be applied (project might need rebuild in presence of transient errors).
    /// A pending solution update is created and has to be committed (commited solution snapshot is advanced at that point) or discarded.
    /// </summary>
    Ready = 1,

    /// <summary>
    /// Some changes are errors that block rebuild of the module.
    /// This means that the code is in a broken state that cannot be resolved by restarting the application.
    /// Pending solution update is not created.
    /// </summary>
    Blocked = 2
}
