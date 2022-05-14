// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal abstract class IdeCodeStyleOptions
{
    protected static readonly CodeStyleOption2<bool> s_trueWithSuggestionEnforcement =
        new(value: true, notification: NotificationOption2.Suggestion);

    protected static readonly CodeStyleOption2<bool> s_trueWithSilentEnforcement =
        new(value: true, notification: NotificationOption2.Silent);

    private static readonly CodeStyleOption2<UnusedParametersPreference> s_preferAllMethodsUnusedParametersPreference =
        new(UnusedParametersPreference.AllMethods, NotificationOption2.Suggestion);

    private static readonly CodeStyleOption2<AccessibilityModifiersRequired> s_accessibilityModifiersRequiredDefault =
        new(SyntaxFormattingOptions.CommonOptions.Default.AccessibilityModifiersRequired, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<ParenthesesPreference> s_alwaysForClarityPreference =
        new(ParenthesesPreference.AlwaysForClarity, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<ParenthesesPreference> s_neverIfUnnecessaryPreference =
        new(ParenthesesPreference.NeverIfUnnecessary, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<ForEachExplicitCastInSourcePreference> s_forEachExplicitCastInSourceNonLegacyPreference =
        new(ForEachExplicitCastInSourcePreference.WhenStronglyTyped, NotificationOption2.Suggestion);

    [DataContract]
    internal sealed class CommonOptions
    {
        public static readonly CommonOptions Default = new();

        [DataMember(Order = 0)] public CodeStyleOption2<bool> PreferObjectInitializer { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 1)] public CodeStyleOption2<bool> PreferCollectionInitializer { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 2)] public CodeStyleOption2<bool> PreferSimplifiedBooleanExpressions { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 3)] public OperatorPlacementWhenWrappingPreference OperatorPlacementWhenWrapping { get; init; } = OperatorPlacementWhenWrappingPreference.BeginningOfLine;
        [DataMember(Order = 4)] public CodeStyleOption2<bool> PreferCoalesceExpression { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 5)] public CodeStyleOption2<bool> PreferNullPropagation { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 6)] public CodeStyleOption2<bool> PreferExplicitTupleNames { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 7)] public CodeStyleOption2<bool> PreferAutoProperties { get; init; } = s_trueWithSilentEnforcement;
        [DataMember(Order = 8)] public CodeStyleOption2<bool> PreferInferredTupleNames { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 9)] public CodeStyleOption2<bool> PreferInferredAnonymousTypeMemberNames { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 10)] public CodeStyleOption2<bool> PreferIsNullCheckOverReferenceEqualityMethod { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 11)] public CodeStyleOption2<bool> PreferConditionalExpressionOverAssignment { get; init; } = s_trueWithSilentEnforcement;
        [DataMember(Order = 12)] public CodeStyleOption2<bool> PreferConditionalExpressionOverReturn { get; init; } = s_trueWithSilentEnforcement;
        [DataMember(Order = 13)] public CodeStyleOption2<bool> PreferCompoundAssignment { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 14)] public CodeStyleOption2<bool> PreferSimplifiedInterpolation { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 15)] public CodeStyleOption2<UnusedParametersPreference> UnusedParameters { get; init; } = s_preferAllMethodsUnusedParametersPreference;
        [DataMember(Order = 16)] public CodeStyleOption2<AccessibilityModifiersRequired> AccessibilityModifiersRequired { get; init; } = s_accessibilityModifiersRequiredDefault;
        [DataMember(Order = 17)] public CodeStyleOption2<bool> PreferReadonly { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 18)] public CodeStyleOption2<ParenthesesPreference> ArithmeticBinaryParentheses { get; init; } = s_alwaysForClarityPreference;
        [DataMember(Order = 19)] public CodeStyleOption2<ParenthesesPreference> OtherBinaryParentheses { get; init; } = s_alwaysForClarityPreference;
        [DataMember(Order = 20)] public CodeStyleOption2<ParenthesesPreference> RelationalBinaryParentheses { get; init; } = s_alwaysForClarityPreference;
        [DataMember(Order = 21)] public CodeStyleOption2<ParenthesesPreference> OtherParentheses { get; init; } = s_neverIfUnnecessaryPreference;
        [DataMember(Order = 22)] public CodeStyleOption2<ForEachExplicitCastInSourcePreference> ForEachExplicitCastInSource { get; init; } = s_forEachExplicitCastInSourceNonLegacyPreference;
        [DataMember(Order = 23)] public CodeStyleOption2<bool> PreferNamespaceAndFolderMatchStructure { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember(Order = 24)] public CodeStyleOption2<bool> AllowMultipleBlankLines { get; init; } = s_trueWithSilentEnforcement;
        [DataMember(Order = 25)] public CodeStyleOption2<bool> AllowStatementImmediatelyAfterBlock { get; init; } = s_trueWithSilentEnforcement;
        [DataMember(Order = 26)] public string RemoveUnnecessarySuppressionExclusions { get; init; } = "";
    }

    [DataMember(Order = 0)]
    public CommonOptions Common { get; init; } = CommonOptions.Default;

    protected const int BaseMemberCount = 1;

#if !CODE_STYLE
    public static IdeCodeStyleOptions GetDefault(HostLanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeStyleService>().DefaultOptions;
#endif
}
