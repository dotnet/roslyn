// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Similar to CodeFixSuggestionAction, but in a location that can be used by
    /// both local Roslyn and LSP.
    /// </summary>
    internal class UnifiedCodeFixSuggestedAction : UnifiedSuggestedAction, ICodeFixSuggestedAction
    {
        public CodeFix CodeFix { get; }

        public object Provider { get; }

        public UnifiedSuggestedActionSet? FixAllFlavors { get; }

        public UnifiedCodeFixSuggestedAction(
            Workspace workspace,
            CodeAction codeAction,
            CodeActionPriority codeActionPriority,
            CodeFix codeFix,
            object provider,
            UnifiedSuggestedActionSet? fixAllFlavors)
            : base(workspace, codeAction, codeActionPriority)
        {
            CodeFix = codeFix;
            Provider = provider;
            FixAllFlavors = fixAllFlavors;
        }
    }
}
