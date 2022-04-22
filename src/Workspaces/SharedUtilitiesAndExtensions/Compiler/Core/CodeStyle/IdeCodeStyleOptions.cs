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
    internal sealed class CommonOptions
    {
        [DataMember(Order = 0)] public readonly CodeStyleOption2<bool> PreferObjectInitializer;
        [DataMember(Order = 1)] public readonly CodeStyleOption2<bool> PreferCollectionInitializer;
        [DataMember(Order = 2)] public readonly CodeStyleOption2<bool> PreferSimplifiedBooleanExpressions;
        [DataMember(Order = 3)] public readonly OperatorPlacementWhenWrappingPreference OperatorPlacementWhenWrapping;
        [DataMember(Order = 4)] public readonly CodeStyleOption2<bool> PreferCoalesceExpression;
        [DataMember(Order = 5)] public readonly CodeStyleOption2<bool> PreferNullPropagation;
        [DataMember(Order = 6)] public readonly CodeStyleOption2<bool> PreferExplicitTupleNames;
        [DataMember(Order = 7)] public readonly CodeStyleOption2<bool> PreferAutoProperties;
        [DataMember(Order = 8)] public readonly CodeStyleOption2<bool> PreferInferredTupleNames;
        [DataMember(Order = 9)] public readonly CodeStyleOption2<bool> PreferInferredAnonymousTypeMemberNames;
        [DataMember(Order = 10)] public readonly CodeStyleOption2<bool> PreferIsNullCheckOverReferenceEqualityMethod;
        [DataMember(Order = 11)] public readonly CodeStyleOption2<bool> PreferConditionalExpressionOverAssignment;
        [DataMember(Order = 12)] public readonly CodeStyleOption2<bool> PreferConditionalExpressionOverReturn;
        [DataMember(Order = 13)] public readonly CodeStyleOption2<bool> PreferCompoundAssignment;
        [DataMember(Order = 14)] public readonly CodeStyleOption2<bool> PreferSimplifiedInterpolation;
        [DataMember(Order = 15)] public readonly CodeStyleOption2<UnusedParametersPreference> UnusedParameters;
        [DataMember(Order = 16)] public readonly CodeStyleOption2<AccessibilityModifiersRequired> RequireAccessibilityModifiers;
        [DataMember(Order = 17)] public readonly CodeStyleOption2<bool> PreferReadonly;
        [DataMember(Order = 18)] public readonly CodeStyleOption2<ParenthesesPreference> ArithmeticBinaryParentheses;
        [DataMember(Order = 19)] public readonly CodeStyleOption2<ParenthesesPreference> OtherBinaryParentheses;
        [DataMember(Order = 20)] public readonly CodeStyleOption2<ParenthesesPreference> RelationalBinaryParentheses;
        [DataMember(Order = 21)] public readonly CodeStyleOption2<ParenthesesPreference> OtherParentheses;
        [DataMember(Order = 22)] public readonly CodeStyleOption2<ForEachExplicitCastInSourcePreference> ForEachExplicitCastInSource;
        [DataMember(Order = 23)] public readonly CodeStyleOption2<bool> PreferNamespaceAndFolderMatchStructure;
        [DataMember(Order = 24)] public readonly CodeStyleOption2<bool> AllowMultipleBlankLines;
        [DataMember(Order = 25)] public readonly CodeStyleOption2<bool> AllowStatementImmediatelyAfterBlock;

        public CommonOptions(
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
            CodeStyleOption2<bool>? AllowStatementImmediatelyAfterBlock = null)
        {
            this.PreferObjectInitializer = PreferObjectInitializer ?? s_trueWithSuggestionEnforcement;
            this.PreferCollectionInitializer = PreferCollectionInitializer ?? s_trueWithSuggestionEnforcement;
            this.PreferSimplifiedBooleanExpressions = PreferSimplifiedBooleanExpressions ?? s_trueWithSuggestionEnforcement;
            this.OperatorPlacementWhenWrapping = OperatorPlacementWhenWrapping;
            this.PreferCoalesceExpression = PreferCoalesceExpression ?? s_trueWithSuggestionEnforcement;
            this.PreferNullPropagation = PreferNullPropagation ?? s_trueWithSuggestionEnforcement;
            this.PreferExplicitTupleNames = PreferExplicitTupleNames ?? s_trueWithSuggestionEnforcement;
            this.PreferAutoProperties = PreferAutoProperties ?? s_trueWithSilentEnforcement;
            this.PreferInferredTupleNames = PreferInferredTupleNames ?? s_trueWithSuggestionEnforcement;
            this.PreferInferredAnonymousTypeMemberNames = PreferInferredAnonymousTypeMemberNames ?? s_trueWithSuggestionEnforcement;
            this.PreferIsNullCheckOverReferenceEqualityMethod = PreferIsNullCheckOverReferenceEqualityMethod ?? s_trueWithSuggestionEnforcement;
            this.PreferConditionalExpressionOverAssignment = PreferConditionalExpressionOverAssignment ?? s_trueWithSilentEnforcement;
            this.PreferConditionalExpressionOverReturn = PreferConditionalExpressionOverReturn ?? s_trueWithSilentEnforcement;
            this.PreferCompoundAssignment = PreferCompoundAssignment ?? s_trueWithSuggestionEnforcement;
            this.PreferSimplifiedInterpolation = PreferSimplifiedInterpolation ?? s_trueWithSuggestionEnforcement;
            this.UnusedParameters = UnusedParameters ?? s_preferAllMethodsUnusedParametersPreference;
            this.RequireAccessibilityModifiers = RequireAccessibilityModifiers ?? s_requireAccessibilityModifiersDefault;
            this.PreferReadonly = PreferReadonly ?? s_trueWithSuggestionEnforcement;
            this.ArithmeticBinaryParentheses = ArithmeticBinaryParentheses ?? s_alwaysForClarityPreference;
            this.OtherBinaryParentheses = OtherBinaryParentheses ?? s_alwaysForClarityPreference;
            this.RelationalBinaryParentheses = RelationalBinaryParentheses ?? s_alwaysForClarityPreference;
            this.OtherParentheses = OtherParentheses ?? s_neverIfUnnecessaryPreference;
            this.ForEachExplicitCastInSource = ForEachExplicitCastInSource ?? s_forEachExplicitCastInSourceNonLegacyPreference;
            this.PreferNamespaceAndFolderMatchStructure = PreferNamespaceAndFolderMatchStructure ?? s_trueWithSuggestionEnforcement;
            this.AllowMultipleBlankLines = AllowMultipleBlankLines ?? s_trueWithSilentEnforcement;
            this.AllowStatementImmediatelyAfterBlock = AllowStatementImmediatelyAfterBlock ?? s_trueWithSilentEnforcement;
        }

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
