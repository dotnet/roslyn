// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Similar to SuggestedAction, but in a location that can be used by
    /// both local Roslyn and LSP.
    /// </summary>
    internal class UnifiedSuggestedAction : IUnifiedSuggestedAction, IEquatable<IUnifiedSuggestedAction>
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

        public bool Equals(IUnifiedSuggestedAction? other)
            => other is UnifiedSuggestedAction action && Equals(action);

        public override bool Equals(object? obj)
            => obj is UnifiedSuggestedAction action && Equals(action);

        internal bool Equals(UnifiedSuggestedAction otherSuggestedAction)
        {
            if (this == otherSuggestedAction)
            {
                return true;
            }

            return OriginalCodeAction.Title == otherSuggestedAction.OriginalCodeAction.Title;
        }

        public override int GetHashCode()
        {
            if (OriginalCodeAction.EquivalenceKey == null)
            {
                return base.GetHashCode();
            }

            return OriginalCodeAction.EquivalenceKey.GetHashCode();
        }
    }
}
