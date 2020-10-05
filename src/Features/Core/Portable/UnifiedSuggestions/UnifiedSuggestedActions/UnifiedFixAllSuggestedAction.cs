// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Similar to FixAllSuggestedAction, but in a location that can be used by
    /// both local Roslyn and LSP.
    /// </summary>
    internal class UnifiedFixAllSuggestedAction : UnifiedSuggestedAction, IFixAllSuggestedAction
    {
        public Diagnostic Diagnostic { get; }

        public FixAllState? FixAllState { get; }

        public UnifiedFixAllSuggestedAction(
            Workspace workspace,
            CodeAction codeAction,
            CodeActionPriority codeActionPriority,
            FixAllState? fixAllState,
            Diagnostic diagnostic)
            : base(workspace, codeAction, codeActionPriority)
        {
            Diagnostic = diagnostic;
            FixAllState = fixAllState;
        }
    }
}
