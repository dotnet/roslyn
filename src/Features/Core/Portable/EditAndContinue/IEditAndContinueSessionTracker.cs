// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Exposes EnC and Hot Reload session state to in-proc components.
/// </summary>
internal interface IEditAndContinueSessionTracker
{
    /// <summary>
    /// True while Hot Reload or EnC session is active.
    /// </summary>
    bool IsSessionActive { get; }

    /// <summary>
    /// Diagnostics reported by the last <see cref="IEditAndContinueService.EmitSolutionUpdateAsync"/> call.
    /// Includes emit errors and issues reported by the debugger when applying changes.
    /// Does not include rude edits, which are reported by <see cref="IEditAndContinueService.GetDocumentDiagnosticsAsync"/>.
    /// </summary>
    ImmutableArray<DiagnosticData> ApplyChangesDiagnostics { get; }
}
