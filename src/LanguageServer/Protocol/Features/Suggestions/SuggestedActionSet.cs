// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Suggestions;

/// <summary>
/// Similar to SuggestedActionSet, but in a location that can be used
/// by both local Roslyn and LSP.
/// </summary>
internal sealed class SuggestedActionSet(
    string? categoryName,
    ImmutableArray<SuggestedAction> actions,
    string? title,
    CodeActionPriority priority,
    TextSpan? applicableToSpan)
{
    public readonly string? CategoryName = categoryName;
    public readonly ImmutableArray<SuggestedAction> Actions = actions;
    public readonly string? Title = title;
    public readonly CodeActionPriority Priority = priority;
    public readonly TextSpan? ApplicableToSpan = applicableToSpan;
}
