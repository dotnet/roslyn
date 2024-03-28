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
/// </remarks>
[Export(typeof(IEditAndContinueSessionTracker))]
[Export(typeof(EditAndContinueSessionState))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditAndContinueSessionState() : IEditAndContinueSessionTracker
{
    public bool IsSessionActive { get; set; }

    public ImmutableArray<DiagnosticData> ApplyChangesDiagnostics { get; set; } = [];
}
