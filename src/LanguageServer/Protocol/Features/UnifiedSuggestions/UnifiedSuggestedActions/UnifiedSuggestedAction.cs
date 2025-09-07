// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions;

/// <summary>
/// Similar to SuggestedAction, but in a location that can be used by
/// both local Roslyn and LSP.
/// </summary>
internal class UnifiedSuggestedAction(Workspace workspace, CodeAction codeAction, CodeActionPriority codeActionPriority) : IUnifiedSuggestedAction
{
    public Workspace Workspace { get; } = workspace;

    public CodeAction OriginalCodeAction { get; } = codeAction;

    public CodeActionPriority CodeActionPriority { get; } = codeActionPriority;
}
