// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Similar to FixAllSuggestedAction, but in a location that can be used by
    /// both local Roslyn and LSP.
    /// </summary>
    internal class UnifiedFixAllSuggestedAction : UnifiedSuggestedAction
    {
        public Diagnostic Diagnostic { get; }

        public FixAllState? FixAllState { get; }

        public UnifiedFixAllSuggestedAction(
            Workspace workspace,
            FixAllState? fixAllState,
            Diagnostic diagnostic,
            CodeAction codeAction)
            : base(workspace, codeAction)
        {
            Diagnostic = diagnostic;
            FixAllState = fixAllState;
        }
    }
}
