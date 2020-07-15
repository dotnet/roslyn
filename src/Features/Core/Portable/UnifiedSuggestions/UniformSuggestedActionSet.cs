// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    internal class UnifiedSuggestedActionSet
    {
        public string CategoryName { get; }

        public IEnumerable<IUnifiedSuggestedAction> Actions { get; }

        public object Title { get; }

        public UnifiedSuggestedActionSetPriority Priority { get; }

        public TextSpan? ApplicableToSpan { get; }

        public UnifiedSuggestedActionSet(
            string categoryName,
            IEnumerable<IUnifiedSuggestedAction> actions,
            object title = null,
            UnifiedSuggestedActionSetPriority priority = UnifiedSuggestedActionSetPriority.None,
            TextSpan? applicableToSpan = null)
        {
            CategoryName = categoryName;
            Actions = actions;
            Title = title;
            Priority = priority;
            ApplicableToSpan = applicableToSpan;
        }

    }
}
