// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Represents light bulb menu item for code fixes.
    /// </summary>
    internal sealed class CodeFixSuggestedAction : SuggestedActionWithNestedFlavors, ICodeFixSuggestedAction, ITelemetryDiagnosticID<string>
    {
        public CodeFix CodeFix { get; }

        public CodeFixSuggestedAction(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            Solution originalSolution,
            ITextBuffer subjectBuffer,
            CodeFix fix,
            object provider,
            CodeAction action,
            SuggestedActionSet fixAllFlavors)
            : base(threadingContext,
                   sourceProvider,
                   workspace,
                   originalSolution,
                   subjectBuffer,
                   provider,
                   action,
                   fixAllFlavors)
        {
            CodeFix = fix;
        }

        public string GetDiagnosticID()
            => CodeFix.PrimaryDiagnostic.GetTelemetryDiagnosticID();

        protected override DiagnosticData GetDiagnostic()
            => CodeFix.GetPrimaryDiagnosticData();
    }
}
