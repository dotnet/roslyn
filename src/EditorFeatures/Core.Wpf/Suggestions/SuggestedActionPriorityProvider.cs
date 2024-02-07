// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

/// <param name="lowPriorityAnalyzers">
/// Set of de-prioritized analyzers that were moved down from 'Normal' to 'Low' priority bucket. Note that this set is
/// owned by the <see cref="SuggestedActionsSourceProvider.SuggestedActionsSource"/> and shared across priority buckets.
/// </param>
internal sealed class SuggestedActionPriorityProvider(
    CodeActionRequestPriority priority,
    ConcurrentSet<DiagnosticAnalyzer> lowPriorityAnalyzers)
    : ICodeActionRequestPriorityProvider
{
    public CodeActionRequestPriority? Priority { get; } = priority;

    public void AddDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer)
        => lowPriorityAnalyzers.Add(analyzer);

    public bool IsDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer)
        => lowPriorityAnalyzers.Contains(analyzer);
}
