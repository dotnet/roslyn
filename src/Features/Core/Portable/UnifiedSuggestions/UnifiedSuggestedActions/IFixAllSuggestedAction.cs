// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions
{
    /// <summary>
    /// Common interface used by both local Roslyn and LSP to implement
    /// their specific versions of FixAllSuggestedAction.
    /// </summary>
    internal interface IFixAllSuggestedAction
    {
        Diagnostic Diagnostic { get; }

        CodeAction OriginalCodeAction { get; }

        FixAllState? FixAllState { get; }
    }
}
