// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Similar to UnifiedCodeFixSuggestionAction, but in a location that can be used by
    /// both local Roslyn and LSP.
    /// </summary>
    internal class UnifiedCodeFixSuggestedAction : UnifiedSuggestedAction
    {
        public CodeFix CodeFix { get; }

        public object Provider { get; }

        public UnifiedSuggestedActionSet? FixAllFlavors { get; }

        public UnifiedCodeFixSuggestedAction(
            Workspace workspace,
            CodeFix codeFix,
            object provider,
            CodeAction codeAction,
            UnifiedSuggestedActionSet? fixAllFlavors)
            : base(workspace, codeAction)
        {
            CodeFix = codeFix;
            Provider = provider;
            FixAllFlavors = fixAllFlavors;
        }
    }
}
