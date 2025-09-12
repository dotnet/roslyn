// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions;

/// <summary>
/// Similar to SuggestedActionWithNestedActions, but in a location that can be used by
/// both local Roslyn and LSP.
/// </summary>
internal sealed class UnifiedSuggestedActionWithNestedActions(
    CodeAction codeAction,
    CodeActionPriority codeActionPriority,
    object provider,
    ImmutableArray<UnifiedSuggestedActionSet> nestedActionSets)
    : UnifiedSuggestedAction(codeAction, codeActionPriority, provider, codeRefactoringKind: null, diagnostics: [])
{
    public ImmutableArray<UnifiedSuggestedActionSet> NestedActionSets { get; } = nestedActionSets;
}
