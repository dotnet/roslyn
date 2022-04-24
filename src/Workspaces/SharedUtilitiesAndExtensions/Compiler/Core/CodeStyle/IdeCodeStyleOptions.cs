// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal abstract class IdeCodeStyleOptions
{
    protected static readonly CodeStyleOption2<bool> s_trueWithSuggestionEnforcement = new(value: true, notification: NotificationOption2.Suggestion);
    protected static readonly CodeStyleOption2<bool> s_trueWithSilentEnforcement = new(value: true, notification: NotificationOption2.Silent);

    private static readonly CodeStyleOption2<UnusedParametersPreference> s_preferAllMethodsUnusedParametersPreference =
        new(UnusedParametersPreference.AllMethods, NotificationOption2.Suggestion);

    private static readonly CodeStyleOption2<AccessibilityModifiersRequired> s_requireAccessibilityModifiersDefault =
            new(AccessibilityModifiersRequired.ForNonInterfaceMembers, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<ParenthesesPreference> s_alwaysForClarityPreference =
        new(ParenthesesPreference.AlwaysForClarity, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<ParenthesesPreference> s_neverIfUnnecessaryPreference =
        new(ParenthesesPreference.NeverIfUnnecessary, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<ForEachExplicitCastInSourcePreference> s_forEachExplicitCastInSourceNonLegacyPreference =
        new(ForEachExplicitCastInSourcePreference.WhenStronglyTyped, NotificationOption2.Suggestion);

    [DataContract]
    internal sealed record class CommonOptions(
        CodeStyleOption2<bool>? PreferObjectInitializer = null,
        CodeStyleOption2<bool>? PreferCollectionInitializer = null,
        CodeStyleOption2<bool>? PreferSimplifiedBooleanExpressions = null,
        OperatorPlacementWhenWrappingPreference OperatorPlacementWhenWrapping = OperatorPlacementWhenWrappingPreference.BeginningOfLine,
        CodeStyleOption2<bool>? PreferCoalesceExpression = null,
        CodeStyleOption2<bool>? PreferNullPropagation = null,
        CodeStyleOption2<bool>? PreferExplicitTupleNames = null,
        CodeStyleOption2<bool>? PreferAutoProperties = null,
        CodeStyleOption2<bool>? PreferInferredTupleNames = null,
        CodeStyleOption2<bool>? PreferInferredAnonymousTypeMemberNames = null,
        CodeStyleOption2<bool>? PreferIsNullCheckOverReferenceEqualityMethod = null,
        CodeStyleOption2<bool>? PreferConditionalExpressionOverAssignment = null,
        CodeStyleOption2<bool>? PreferConditionalExpressionOverReturn = null,
        CodeStyleOption2<bool>? PreferCompoundAssignment = null,
        CodeStyleOption2<bool>? PreferSimplifiedInterpolation = null,
        CodeStyleOption2<UnusedParametersPreference>? UnusedParameters = null,
        CodeStyleOption2<AccessibilityModifiersRequired>? RequireAccessibilityModifiers = null,
        CodeStyleOption2<bool>? PreferReadonly = null,
        CodeStyleOption2<ParenthesesPreference>? ArithmeticBinaryParentheses = null,
        CodeStyleOption2<ParenthesesPreference>? OtherBinaryParentheses = null,
        CodeStyleOption2<ParenthesesPreference>? RelationalBinaryParentheses = null,
        CodeStyleOption2<ParenthesesPreference>? OtherParentheses = null,
        CodeStyleOption2<ForEachExplicitCastInSourcePreference>? ForEachExplicitCastInSource = null,
        CodeStyleOption2<bool>? PreferNamespaceAndFolderMatchStructure = null,
        CodeStyleOption2<bool>? AllowMultipleBlankLines = null,
        CodeStyleOption2<bool>? AllowStatementImmediatelyAfterBlock = null,
        string RemoveUnnecessarySuppressionExclusions = "")
    {
        [property: DataMember(Order = 0)] public CodeStyleOption2<bool> PreferObjectInitializer { get; init; } = PreferObjectInitializer ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 1)] public CodeStyleOption2<bool> PreferCollectionInitializer { get; init; } = PreferCollectionInitializer ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 2)] public CodeStyleOption2<bool> PreferSimplifiedBooleanExpressions { get; init; } = PreferSimplifiedBooleanExpressions ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 3)] public OperatorPlacementWhenWrappingPreference OperatorPlacementWhenWrapping { get; init; } = OperatorPlacementWhenWrapping;
        [property: DataMember(Order = 4)] public CodeStyleOption2<bool> PreferCoalesceExpression { get; init; } = PreferCoalesceExpression ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 5)] public CodeStyleOption2<bool> PreferNullPropagation { get; init; } = PreferNullPropagation ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 6)] public CodeStyleOption2<bool> PreferExplicitTupleNames { get; init; } = PreferExplicitTupleNames ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 7)] public CodeStyleOption2<bool> PreferAutoProperties { get; init; } = PreferAutoProperties ?? s_trueWithSilentEnforcement;
        [property: DataMember(Order = 8)] public CodeStyleOption2<bool> PreferInferredTupleNames { get; init; } = PreferInferredTupleNames ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 9)] public CodeStyleOption2<bool> PreferInferredAnonymousTypeMemberNames { get; init; } = PreferInferredAnonymousTypeMemberNames ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 10)] public CodeStyleOption2<bool> PreferIsNullCheckOverReferenceEqualityMethod { get; init; } = PreferIsNullCheckOverReferenceEqualityMethod ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 11)] public CodeStyleOption2<bool> PreferConditionalExpressionOverAssignment { get; init; } = PreferConditionalExpressionOverAssignment ?? s_trueWithSilentEnforcement;
        [property: DataMember(Order = 12)] public CodeStyleOption2<bool> PreferConditionalExpressionOverReturn { get; init; } = PreferConditionalExpressionOverReturn ?? s_trueWithSilentEnforcement;
        [property: DataMember(Order = 13)] public CodeStyleOption2<bool> PreferCompoundAssignment { get; init; } = PreferCompoundAssignment ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 14)] public CodeStyleOption2<bool> PreferSimplifiedInterpolation { get; init; } = PreferSimplifiedInterpolation ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 15)] public CodeStyleOption2<UnusedParametersPreference> UnusedParameters { get; init; } = UnusedParameters ?? s_preferAllMethodsUnusedParametersPreference;
        [property: DataMember(Order = 16)] public CodeStyleOption2<AccessibilityModifiersRequired> RequireAccessibilityModifiers { get; init; } = RequireAccessibilityModifiers ?? s_requireAccessibilityModifiersDefault;
        [property: DataMember(Order = 17)] public CodeStyleOption2<bool> PreferReadonly { get; init; } = PreferReadonly ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 18)] public CodeStyleOption2<ParenthesesPreference> ArithmeticBinaryParentheses { get; init; } = ArithmeticBinaryParentheses ?? s_alwaysForClarityPreference;
        [property: DataMember(Order = 19)] public CodeStyleOption2<ParenthesesPreference> OtherBinaryParentheses { get; init; } = OtherBinaryParentheses ?? s_alwaysForClarityPreference;
        [property: DataMember(Order = 20)] public CodeStyleOption2<ParenthesesPreference> RelationalBinaryParentheses { get; init; } = RelationalBinaryParentheses ?? s_alwaysForClarityPreference;
        [property: DataMember(Order = 21)] public CodeStyleOption2<ParenthesesPreference> OtherParentheses { get; init; } = OtherParentheses ?? s_neverIfUnnecessaryPreference;
        [property: DataMember(Order = 22)] public CodeStyleOption2<ForEachExplicitCastInSourcePreference> ForEachExplicitCastInSource { get; init; } = ForEachExplicitCastInSource ?? s_forEachExplicitCastInSourceNonLegacyPreference;
        [property: DataMember(Order = 23)] public CodeStyleOption2<bool> PreferNamespaceAndFolderMatchStructure { get; init; } = PreferNamespaceAndFolderMatchStructure ?? s_trueWithSuggestionEnforcement;
        [property: DataMember(Order = 24)] public CodeStyleOption2<bool> AllowMultipleBlankLines { get; init; } = AllowMultipleBlankLines ?? s_trueWithSilentEnforcement;
        [property: DataMember(Order = 25)] public CodeStyleOption2<bool> AllowStatementImmediatelyAfterBlock { get; init; } = AllowStatementImmediatelyAfterBlock ?? s_trueWithSilentEnforcement;
        [property: DataMember(Order = 26)] public string RemoveUnnecessarySuppressionExclusions { get; init; } = RemoveUnnecessarySuppressionExclusions;

        public static readonly CommonOptions Default = new();
    }

    [DataMember(Order = 0)]
    public readonly CommonOptions Common;

    protected const int BaseMemberCount = 1;

    protected IdeCodeStyleOptions(CommonOptions? common)
    {
        Common = common ?? CommonOptions.Default;
    }
}
