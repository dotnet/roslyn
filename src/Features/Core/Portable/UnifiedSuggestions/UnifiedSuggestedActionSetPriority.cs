// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Equivalent to SuggestedActionSetPriority, but in a location that can be used
    /// by both local Roslyn and LSP.
    /// </summary>
    internal enum UnifiedSuggestedActionSetPriority
    {
        Lowest = 0, // Corresponds to SuggestedActionSetPriority.None
        Low = 1,
        Medium = 2,
        High = 3
    }
}
