// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

/// <summary>
/// Suggested action for fix all occurrences code fix.  Note: this is only used
/// as a 'flavor' inside CodeFixSuggestionAction.
/// </summary>
internal sealed partial class FixAllCodeFixSuggestedAction(
    IThreadingContext threadingContext,
    SuggestedActionsSourceProvider sourceProvider,
    Solution originalSolution,
    ITextBuffer subjectBuffer,
    IRefactorOrFixAllState fixAllState,
    Diagnostic diagnostic,
    CodeAction originalCodeAction)
    : AbstractFixAllSuggestedAction(threadingContext,
        sourceProvider,
        originalSolution,
        subjectBuffer,
        fixAllState,
        originalCodeAction,
        new FixAllCodeAction(fixAllState)), ITelemetryDiagnosticID<string>, IFixAllCodeFixSuggestedAction
{
    public Diagnostic Diagnostic { get; } = diagnostic;

    public string GetDiagnosticID()
        => Diagnostic.GetTelemetryDiagnosticID();
}
