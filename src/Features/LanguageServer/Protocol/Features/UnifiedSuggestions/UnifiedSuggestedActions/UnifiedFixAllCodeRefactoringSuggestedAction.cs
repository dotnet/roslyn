// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Similar to FixAllCodeRefactoringSuggestedAction, but in a location that can be used by
    /// both local Roslyn and LSP.
    /// </summary>
    internal class UnifiedFixAllCodeRefactoringSuggestedAction : UnifiedSuggestedAction, IFixAllCodeRefactoringSuggestedAction
    {
        public IFixAllState FixAllState { get; }

        public UnifiedFixAllCodeRefactoringSuggestedAction(
            Workspace workspace,
            CodeAction codeAction,
            CodeActionPriority codeActionPriority,
            IFixAllState fixAllState)
            : base(workspace, codeAction, codeActionPriority)
        {
            FixAllState = fixAllState;
        }
    }
}
