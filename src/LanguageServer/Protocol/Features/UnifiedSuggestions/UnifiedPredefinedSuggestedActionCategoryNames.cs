// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Equivalent to PredefinedSuggestedActionCategoryNames, but in a location that
    /// can be used by both local Roslyn and LSP.
    /// </summary>
    internal static class UnifiedPredefinedSuggestedActionCategoryNames
    {
        public const string Any = "Any";
        public const string CodeFix = "CodeFix";
        public const string ErrorFix = "ErrorFix";
        public const string StyleFix = "StyleFix";
        public const string Refactoring = "Refactoring";
    }
}
