// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    internal interface IUnifiedSuggestedAction
    {
        public Workspace Workspace { get; }
        public CodeAction CodeAction { get; }
    }
}
