// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions
{
    /// <summary>
    /// Common interface used by both local Roslyn and LSP to implement
    /// their specific versions of FixAllCodeRefactoringSuggestedAction.
    /// </summary>
    internal interface IFixAllCodeRefactoringSuggestedAction
    {
        CodeAction OriginalCodeAction { get; }

        IFixAllState FixAllState { get; }
    }
}
