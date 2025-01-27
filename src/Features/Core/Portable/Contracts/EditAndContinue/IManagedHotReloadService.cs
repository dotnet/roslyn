// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

/// <summary>
/// Service for providing helper functionality to a language service regarding hot reload and Edit and Continue operations.
/// This is currently exported through MEF.
/// </summary>
internal interface IManagedHotReloadService
{
    /// <summary>
    /// Retrieves a list of active statements for the debugging session.
    /// Shall only be called while the debugger is stopped (break mode).
    /// Returns empty array if no debugger is present.
    /// </summary>
    /// <param name="cancellation">Cancellation token.</param>
    /// <returns>
    /// Returns all the active statements in the session. Each <see cref="ManagedActiveStatementDebugInfo"/> has an unique <see cref="ManagedActiveStatementDebugInfo.ActiveInstruction"/>.
    /// For example, if an instruction is active in two different threads, only one active statement will be reported for it.
    /// </returns>
    ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellation);

    /// <summary>
    /// Check for Edit and Continue availability on all instances with specified <paramref name="module"/>.
    /// If no debugger is present, this will check if there are any agents available for hot reload or whether
    /// the user has disabled Edit and Continue or hot reload.
    /// </summary>
    /// <param name="module">Target module version identifier. This is only used when under a debugging session.</param>
    /// <param name="cancellation">Cancellation token.</param>
    /// <returns>
    /// Returns first status that's not <see cref="ManagedHotReloadAvailabilityStatus.Available"/>, if any.
    /// Otherwise, if there is at least one instance of the module loaded in a debugging session or there are active hot reload agents, returns <see cref="ManagedHotReloadAvailabilityStatus.Available"/>.
    /// Otherwise, returns <see cref="ManagedHotReloadAvailabilityStatus.ModuleNotLoaded"/>.
    /// </returns>
    ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellation);

    /// <summary>
    /// Notifies the debugger that a document has changed, which may affect the given module when that change is applied.
    /// Calls ISymUnmanagedEncUpdate.InitializeForEnc on SymReader for the given module.
    /// No-op if no debugger is present.
    /// </summary>
    /// <param name="module">Module version identifier.</param>
    /// <param name="cancellation">Cancellation token.</param>
    ValueTask PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellation);

    /// <summary>
    /// Get capabilities string for the set of hot reload edits supported by the runtime.
    /// </summary>
    /// <param name="cancellation">Cancellation token.</param>
    /// <returns>
    /// Returns an array of identifiers. If different agents have different capabilities, it's up to the manager 
    /// to merge them and present unified set of capabilities to the language service.
    /// The merging policy is entirely dependent on how the manager applies changes to multiple runtimes.
    /// </returns>
    ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellation);
}
