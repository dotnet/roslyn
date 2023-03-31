// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

internal sealed class SuggestedActionPriorityProvider : ICodeActionRequestPriorityProvider
{
    private readonly ConcurrentSet<DiagnosticAnalyzer> _lowPriorityAnalyzers;

    public SuggestedActionPriorityProvider(CodeActionRequestPriority priority, ConcurrentSet<DiagnosticAnalyzer> lowPriorityAnalyzers)
    {
        Priority = priority;
        _lowPriorityAnalyzers = lowPriorityAnalyzers;
    }

    public CodeActionRequestPriority Priority { get; }

    public void AddDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer)
        => _lowPriorityAnalyzers.Add(analyzer);

    public bool IsDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer)
        => _lowPriorityAnalyzers.Contains(analyzer);
}
