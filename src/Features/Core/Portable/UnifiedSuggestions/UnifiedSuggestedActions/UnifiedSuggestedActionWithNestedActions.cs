// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    internal class UnifiedSuggestedActionWithNestedActions : UnifiedSuggestedAction
    {
        public object? Provider { get; }

        public ImmutableArray<UnifiedSuggestedActionSet> NestedActionSets { get; }

        public UnifiedSuggestedActionWithNestedActions(
            Workspace workspace,
            object? provider,
            CodeAction codeAction,
            ImmutableArray<UnifiedSuggestedActionSet> nestedActionSets)
            : base(workspace, codeAction)
        {
            Provider = provider;
            NestedActionSets = nestedActionSets;
        }

        public UnifiedSuggestedActionWithNestedActions(
            Workspace workspace,
            object provider,
            CodeAction codeAction,
            UnifiedSuggestedActionSet nestedActionSets)
            : this(workspace, provider, codeAction, ImmutableArray.Create(nestedActionSets))
        {
        }
    }
}
