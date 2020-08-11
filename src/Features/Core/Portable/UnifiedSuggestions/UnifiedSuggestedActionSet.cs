// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Similar to SuggestedActionSet, but in a location that can be used
    /// by both local Roslyn and LSP.
    /// </summary>
    internal class UnifiedSuggestedActionSet : IEquatable<UnifiedSuggestedActionSet>
    {
        public string? CategoryName { get; }

        public IEnumerable<IUnifiedSuggestedAction> Actions { get; }

        public object? Title { get; }

        public UnifiedSuggestedActionSetPriority Priority { get; }

        public TextSpan? ApplicableToSpan { get; }

        public UnifiedSuggestedActionSet(
            string? categoryName,
            IEnumerable<IUnifiedSuggestedAction> actions,
            object? title,
            UnifiedSuggestedActionSetPriority priority,
            TextSpan? applicableToSpan)
        {
            CategoryName = categoryName;
            Actions = actions;
            Title = title;
            Priority = priority;
            ApplicableToSpan = applicableToSpan;
        }

        public override bool Equals(object? obj)
            => obj is UnifiedSuggestedActionSet actionSet && Equals(actionSet);

        public bool Equals(UnifiedSuggestedActionSet? otherSuggestedActionSet)
        {
            if (otherSuggestedActionSet == null)
            {
                return false;
            }

            if (this == otherSuggestedActionSet)
            {
                return true;
            }

            if (Title != otherSuggestedActionSet.Title || Priority != otherSuggestedActionSet.Priority ||
                CategoryName != otherSuggestedActionSet.CategoryName ||
                ApplicableToSpan != otherSuggestedActionSet.ApplicableToSpan)
            {
                return false;
            }

            if (!Actions.SequenceEqual(otherSuggestedActionSet.Actions))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(CategoryName != null ? CategoryName.GetHashCode() : 0,
                Hash.Combine(Actions.GetHashCode(),
                Hash.Combine(Title != null ? Title.GetHashCode() : 0,
                Hash.Combine(Priority.GetHashCode(), ApplicableToSpan.GetHashCode()))));
        }
    }
}
