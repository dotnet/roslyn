// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

/// <summary>
/// Severity of a rude edit made by the user.
/// </summary>
internal enum ManagedHotReloadDiagnosticSeverity
{
    /// <summary>
    /// Diagnostic for a warning.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Diagnostic for a rude edit. 
    /// This is a less severe diagnostic and can be generally addressed by restarting the application.
    /// </summary>
    RestartRequired = 2,

    /// <summary>
    /// Diagnostic for a compiler error.
    /// This means we can't do anything until the error is fixed.
    /// </summary>
    Error = 3
}
