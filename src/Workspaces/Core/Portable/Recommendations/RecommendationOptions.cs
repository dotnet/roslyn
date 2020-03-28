// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Recommendations
{
    public static class RecommendationOptions
    {
        public static PerLanguageOption<bool> HideAdvancedMembers { get; } = new PerLanguageOption<bool>(nameof(RecommendationOptions), nameof(HideAdvancedMembers), defaultValue: false);

        public static PerLanguageOption<bool> FilterOutOfScopeLocals { get; } = new PerLanguageOption<bool>(nameof(RecommendationOptions), nameof(FilterOutOfScopeLocals), defaultValue: true);
    }
}
