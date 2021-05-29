﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Similar to ISuggestedAction, but in a location that can be used by both local Roslyn and LSP.
    /// </summary>
    internal interface IUnifiedSuggestedAction
    {
        Workspace Workspace { get; }

        CodeAction OriginalCodeAction { get; }

        CodeActionPriority CodeActionPriority { get; }
    }
}
