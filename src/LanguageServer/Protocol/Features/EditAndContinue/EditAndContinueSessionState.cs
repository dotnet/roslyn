// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Manages in-proc EnC and Hot Reload session state that other components need to keep track of.
/// </summary>
/// <remarks>
/// Separated from the in-proc EnC language service to allow access from lower-layer components.
/// 
/// <see cref="IEditAndContinueSessionTracker"/> provides read-only access,
/// <see cref="EditAndContinueSessionState"/> provides read-write access, which is only used by the EnC language service.
/// </remarks>
[Export(typeof(IEditAndContinueSessionTracker))]
[Export(typeof(EditAndContinueSessionState))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditAndContinueSessionState() : IEditAndContinueSessionTracker
{
    /// <summary>
    /// Set to true when EnC or Hot Reload session becomes active (e.g. F5/Ctrl+F5), to false when it ends.
    /// </summary>
    public bool IsSessionActive { get; set; }

    /// <summary>
    /// Updated when the user attempts to apply changes.
    /// Includes EnC emit diagnostics and debuggee state related diagnostics.
    /// </summary>
    public ImmutableArray<DiagnosticData> ApplyChangesDiagnostics { get; set; } = [];
}
