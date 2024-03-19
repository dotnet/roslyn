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
    /// </summary>
    None = 0,

    /// <summary>
    /// All changes are valid, can be applied.
    /// </summary>
    Ready = 1,

    /// <summary>
    /// Changes require restarting the application in order to be applied.
    /// </summary>
    RestartRequired = 2,

    /// <summary>
    /// Some changes are errors that block rebuild of the module.
    /// This means that the code is in a broken state that cannot be resolved by restarting the application.
    /// </summary>
    Blocked = 3
}
