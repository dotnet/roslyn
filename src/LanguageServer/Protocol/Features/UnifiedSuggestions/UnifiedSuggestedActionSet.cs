// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions;

/// <summary>
/// Similar to SuggestedActionSet, but in a location that can be used
/// by both local Roslyn and LSP.
/// </summary>
internal sealed class UnifiedSuggestedActionSet(
    string? categoryName,
    ImmutableArray<UnifiedSuggestedAction> actions,
    string? title,
    CodeActionPriority priority,
    TextSpan? applicableToSpan)
{
    public string? CategoryName { get; } = categoryName;
    public ImmutableArray<UnifiedSuggestedAction> Actions { get; } = actions;
    public string? Title { get; } = title;
    public CodeActionPriority Priority { get; } = priority;
    public TextSpan? ApplicableToSpan { get; } = applicableToSpan;
}
