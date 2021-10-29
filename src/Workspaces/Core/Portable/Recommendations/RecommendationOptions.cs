// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Recommendations
{
    public static class RecommendationOptions
    {
        public static PerLanguageOption<bool> HideAdvancedMembers { get; } = new PerLanguageOption<bool>(nameof(RecommendationOptions), nameof(HideAdvancedMembers), defaultValue: false);

        public static PerLanguageOption<bool> FilterOutOfScopeLocals { get; } = new PerLanguageOption<bool>(nameof(RecommendationOptions), nameof(FilterOutOfScopeLocals), defaultValue: true);
    }

    internal record struct RecommendationServiceOptions(
        bool FilterOutOfScopeLocals,
        bool HideAdvancedMembers)
    {
        public static RecommendationServiceOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static RecommendationServiceOptions From(OptionSet options, string language)
          => new(
              HideAdvancedMembers: options.GetOption(RecommendationOptions.HideAdvancedMembers, language),
              FilterOutOfScopeLocals: options.GetOption(RecommendationOptions.FilterOutOfScopeLocals, language));
    }
}
