// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Recommendations
{
#pragma warning disable RS0030 // Do not used banned APIs: PerLanguageOption<T>
    public static class RecommendationOptions
    {
        public static PerLanguageOption<bool> HideAdvancedMembers { get; } = (PerLanguageOption<bool>)RecommendationOptions2.HideAdvancedMembers;

        public static PerLanguageOption<bool> FilterOutOfScopeLocals { get; } = (PerLanguageOption<bool>)RecommendationOptions2.FilterOutOfScopeLocals;
    }
#pragma warning restore

    internal static class RecommendationOptions2
    {
        public static readonly PerLanguageOption2<bool> HideAdvancedMembers = new("RecommendationOptions", "HideAdvancedMembers", defaultValue: false);

        public static readonly PerLanguageOption2<bool> FilterOutOfScopeLocals = new("RecommendationOptions", "FilterOutOfScopeLocals", defaultValue: true);
    }

    internal readonly record struct RecommendationServiceOptions(
        bool FilterOutOfScopeLocals,
        bool HideAdvancedMembers)
    {
        public static RecommendationServiceOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static RecommendationServiceOptions From(OptionSet options, string language)
          => new(
              HideAdvancedMembers: options.GetOption(RecommendationOptions2.HideAdvancedMembers, language),
              FilterOutOfScopeLocals: options.GetOption(RecommendationOptions2.FilterOutOfScopeLocals, language));
    }
}
