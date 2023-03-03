// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Similar to SuggestedActionSet, but in a location that can be used
    /// by both local Roslyn and LSP.
    /// </summary>
    internal class UnifiedSuggestedActionSet
    {
        public Solution OriginalSolution { get; }

        public string? CategoryName { get; }

        public ImmutableArray<IUnifiedSuggestedAction> Actions { get; }

        public object? Title { get; }

        public CodeActionPriority Priority { get; }

        public TextSpan? ApplicableToSpan { get; }

        public UnifiedSuggestedActionSet(
            Solution originalSolution,
            string? categoryName,
            ImmutableArray<IUnifiedSuggestedAction> actions,
            object? title,
            CodeActionPriority priority,
            TextSpan? applicableToSpan)
        {
            OriginalSolution = originalSolution;
            CategoryName = categoryName;
            Actions = actions;
            Title = title;
            Priority = priority;
            ApplicableToSpan = applicableToSpan;
        }
    }
}
