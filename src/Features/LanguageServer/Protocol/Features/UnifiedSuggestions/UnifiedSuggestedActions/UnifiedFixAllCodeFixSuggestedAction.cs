// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Similar to FixAllCodeFixSuggestedAction, but in a location that can be used by
    /// both local Roslyn and LSP.
    /// </summary>
    internal class UnifiedFixAllCodeFixSuggestedAction : UnifiedSuggestedAction, IFixAllCodeFixSuggestedAction
    {
        public Diagnostic Diagnostic { get; }

        public IFixAllState FixAllState { get; }

        public UnifiedFixAllCodeFixSuggestedAction(
            Workspace workspace,
            CodeAction codeAction,
            CodeActionPriority codeActionPriority,
            IFixAllState fixAllState,
            Diagnostic diagnostic)
            : base(workspace, codeAction, codeActionPriority)
        {
            Diagnostic = diagnostic;
            FixAllState = fixAllState;
        }
    }
}
