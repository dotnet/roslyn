// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions;

/// <summary>
/// Similar to ISuggestedAction, but in a location that can be used by both local Roslyn and LSP.
/// </summary>
internal interface IUnifiedSuggestedAction
{
    object? Provider { get; }

    CodeAction OriginalCodeAction { get; }

    CodeActionPriority CodeActionPriority { get; }

    CodeRefactoringKind? CodeRefactoringKind { get; }
}
