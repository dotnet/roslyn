// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeGen;

/// <summary>
/// Debug information maintained for each closure.
/// </summary>
/// <remarks>
/// The information is emitted to PDB in Custom Debug Information record for a method containing the closure.
/// </remarks>
[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
internal readonly record struct ClosureDebugInfo(int SyntaxOffset, DebugId ClosureId)
{
    internal string GetDebuggerDisplay()
        => $"({ClosureId} @{SyntaxOffset})";
}
