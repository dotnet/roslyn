// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Recommendations;

#pragma warning disable RS0030 // Do not used banned APIs: PerLanguageOption<T>
public static class RecommendationOptions
{
    public static PerLanguageOption<bool> HideAdvancedMembers { get; } = RecommendationOptions2.HideAdvancedMembers.ToPublicOption();
    public static PerLanguageOption<bool> FilterOutOfScopeLocals { get; } = RecommendationOptions2.FilterOutOfScopeLocals.ToPublicOption();
}
#pragma warning restore

internal static class RecommendationOptions2
{
    public static readonly PerLanguageOption2<bool> HideAdvancedMembers = new PerLanguageOption2<bool>(
        "RecommendationOptions_HideAdvancedMembers", RecommendationServiceOptions.Default.HideAdvancedMembers)
        .WithPublicOption("RecommendationOptions", "HideAdvancedMembers");

    public static readonly PerLanguageOption2<bool> FilterOutOfScopeLocals = new PerLanguageOption2<bool>(
        "RecommendationOptions_FilterOutOfScopeLocals", RecommendationServiceOptions.Default.FilterOutOfScopeLocals)
        .WithPublicOption("RecommendationOptions", "FilterOutOfScopeLocals");
}

[DataContract]
internal readonly record struct RecommendationServiceOptions
{
    public static readonly RecommendationServiceOptions Default = new();

    [DataMember] public bool HideAdvancedMembers { get; init; } = false;
    [DataMember] public bool FilterOutOfScopeLocals { get; init; } = true;

    public RecommendationServiceOptions()
    {
    }
}
