// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Editor.Implementation.Suggestions.SuggestedActionPriorityProvider;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

/// <param name="lowPriorityAnalyzersAndDiagnosticIds">
/// Information about de-prioritized analyzers that were moved down from 'Normal' to 'Low' priority bucket. Note that this data is
/// owned by the <see cref="SuggestedActionsSourceProvider.SuggestedActionsSource"/> and shared across priority buckets.
/// </param>
internal sealed class SuggestedActionPriorityProvider(
    CodeActionRequestPriority priority,
    LowPriorityAnalyzersAndDiagnosticIds lowPriorityAnalyzersAndDiagnosticIds)
    : ICodeActionRequestPriorityProvider
{
    public CodeActionRequestPriority? Priority { get; } = priority;

    public struct LowPriorityAnalyzersAndDiagnosticIds()
    {
        public ConcurrentSet<DiagnosticAnalyzer> Analyzers { get; } = new();
        public ConcurrentSet<string> SupportedDiagnosticIds { get; } = new();
    }

    public void AddDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer)
    {
        lowPriorityAnalyzersAndDiagnosticIds.Analyzers.Add(analyzer);

        foreach (var supportedDiagnostic in analyzer.SupportedDiagnostics)
            lowPriorityAnalyzersAndDiagnosticIds.SupportedDiagnosticIds.Add(supportedDiagnostic.Id);
    }

    public bool IsDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer)
        => lowPriorityAnalyzersAndDiagnosticIds.Analyzers.Contains(analyzer);

    public bool HasDeprioritizedAnalyzerSupportingDiagnosticId(ImmutableArray<string> diagnosticIds)
    {
        foreach (var diagnosticId in diagnosticIds)
        {
            if (lowPriorityAnalyzersAndDiagnosticIds.SupportedDiagnosticIds.Contains(diagnosticId))
                return true;
        }

        return false;
    }
}
