// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Similar to SuggestedAction, but in a location that can be used by
    /// both local Roslyn and LSP.
    /// </summary>
    internal class UnifiedSuggestedAction : IUnifiedSuggestedAction
    {
        public Workspace Workspace { get; }

        public CodeAction OriginalCodeAction { get; }

        public CodeActionPriority CodeActionPriority { get; }

        public UnifiedSuggestedAction(Workspace workspace, CodeAction codeAction, CodeActionPriority codeActionPriority)
        {
            Workspace = workspace;
            OriginalCodeAction = codeAction;
            CodeActionPriority = codeActionPriority;
        }
    }
}
