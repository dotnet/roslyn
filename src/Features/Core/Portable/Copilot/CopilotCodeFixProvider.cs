// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.Copilot;

/// <summary>
/// Special code fix provider which provides fixes for all Copilot diagnostics with any custom diagnostic id.
/// </summary>
internal abstract class CopilotCodeFixProvider : CodeFixProvider
{
    // Special code fix provider which provides fixes for all Copilot diagnostics with any custom diagnostic id
    public override ImmutableArray<string> FixableDiagnosticIds => [];

    // We always want fixes for Copilot diagnostics to be low priority.
    protected override CodeActionRequestPriority ComputeRequestPriority() => CodeActionRequestPriority.Low;

    // Copilot code fixes do not support FixAll.
    public override FixAllProvider? GetFixAllProvider() => null;
}
