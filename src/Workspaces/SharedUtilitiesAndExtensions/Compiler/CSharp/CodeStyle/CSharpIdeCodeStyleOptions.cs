// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle;

[DataContract]
internal sealed class CSharpIdeCodeStyleOptions : IdeCodeStyleOptions
{
    private static readonly ImmutableArray<SyntaxKind> s_preferredModifierOrderDefault = ImmutableArray.Create(
        SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword,
        SyntaxKind.StaticKeyword,
        SyntaxKind.ExternKeyword,
        SyntaxKind.NewKeyword,
        SyntaxKind.VirtualKeyword, SyntaxKind.AbstractKeyword, SyntaxKind.SealedKeyword, SyntaxKind.OverrideKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.VolatileKeyword,
        SyntaxKind.AsyncKeyword);

    private static readonly CodeStyleOption2<UnusedValuePreference> s_discardVariableWithSilentEnforcement =
        new(UnusedValuePreference.DiscardVariable, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<string> s_defaultModifierOrder =
        new(string.Join(",", s_preferredModifierOrderDefault.Select(SyntaxFacts.GetText)), NotificationOption2.Silent);

    public static readonly CodeStyleOption2<AddImportPlacement> s_outsideNamespacePlacementWithSilentEnforcement =
        new(AddImportPlacement.OutsideNamespace, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<ExpressionBodyPreference> s_whenPossibleWithSilentEnforcement =
        new(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent);

    public static readonly CSharpIdeCodeStyleOptions Default = new();

    [DataMember(Order = BaseMemberCount + 0)] public readonly CodeStyleOption2<bool> ImplicitObjectCreationWhenTypeIsApparent;
    [DataMember(Order = BaseMemberCount + 1)] public readonly CodeStyleOption2<bool> PreferNullCheckOverTypeCheck;

    [DataMember(Order = BaseMemberCount + 2)] public readonly CodeStyleOption2<bool> AllowBlankLinesBetweenConsecutiveBraces;
    [DataMember(Order = BaseMemberCount + 3)] public readonly CodeStyleOption2<bool> AllowBlankLineAfterColonInConstructorInitializer;
    [DataMember(Order = BaseMemberCount + 4)] public readonly CodeStyleOption2<bool> PreferConditionalDelegateCall;
    [DataMember(Order = BaseMemberCount + 5)] public readonly CodeStyleOption2<bool> PreferSwitchExpression;
    [DataMember(Order = BaseMemberCount + 6)] public readonly CodeStyleOption2<bool> PreferPatternMatching;
    [DataMember(Order = BaseMemberCount + 7)] public readonly CodeStyleOption2<bool> PreferPatternMatchingOverAsWithNullCheck;
    [DataMember(Order = BaseMemberCount + 8)] public readonly CodeStyleOption2<bool> PreferPatternMatchingOverIsWithCastCheck;
    [DataMember(Order = BaseMemberCount + 9)] public readonly CodeStyleOption2<bool> PreferNotPattern;
    [DataMember(Order = BaseMemberCount + 10)] public readonly CodeStyleOption2<bool> PreferExtendedPropertyPattern;
    [DataMember(Order = BaseMemberCount + 11)] public readonly CodeStyleOption2<bool> PreferInlinedVariableDeclaration;
    [DataMember(Order = BaseMemberCount + 12)] public readonly CodeStyleOption2<bool> PreferDeconstructedVariableDeclaration;
    [DataMember(Order = BaseMemberCount + 13)] public readonly CodeStyleOption2<bool> PreferIndexOperator;
    [DataMember(Order = BaseMemberCount + 14)] public readonly CodeStyleOption2<bool> PreferRangeOperator;
    [DataMember(Order = BaseMemberCount + 15)] public readonly CodeStyleOption2<string> PreferredModifierOrder;
    [DataMember(Order = BaseMemberCount + 16)] public readonly CodeStyleOption2<bool> PreferSimpleUsingStatement;
    [DataMember(Order = BaseMemberCount + 17)] public readonly CodeStyleOption2<bool> PreferLocalOverAnonymousFunction;
    [DataMember(Order = BaseMemberCount + 18)] public readonly CodeStyleOption2<bool> PreferTupleSwap;
    [DataMember(Order = BaseMemberCount + 29)] public readonly CodeStyleOption2<UnusedValuePreference> UnusedValueExpressionStatement;
    [DataMember(Order = BaseMemberCount + 20)] public readonly CodeStyleOption2<UnusedValuePreference> UnusedValueAssignment;
    [DataMember(Order = BaseMemberCount + 21)] public readonly CodeStyleOption2<bool> PreferMethodGroupConversion;

    // the following are also used in code generation features, consider sharing:
    [DataMember(Order = BaseMemberCount + 22)] public readonly CodeStyleOption2<bool> PreferStaticLocalFunction;
    [DataMember(Order = BaseMemberCount + 23)] public readonly CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLambdas;

#pragma warning disable IDE1006 // Record naming style
    public CSharpIdeCodeStyleOptions(
        CommonOptions? Common = null,
        CodeStyleOption2<bool>? ImplicitObjectCreationWhenTypeIsApparent = null,
        CodeStyleOption2<bool>? PreferNullCheckOverTypeCheck = null,
        CodeStyleOption2<bool>? AllowBlankLinesBetweenConsecutiveBraces = null,
        CodeStyleOption2<bool>? AllowBlankLineAfterColonInConstructorInitializer = null,
        CodeStyleOption2<bool>? PreferConditionalDelegateCall = null,
        CodeStyleOption2<bool>? PreferSwitchExpression = null,
        CodeStyleOption2<bool>? PreferPatternMatching = null,
        CodeStyleOption2<bool>? PreferPatternMatchingOverAsWithNullCheck = null,
        CodeStyleOption2<bool>? PreferPatternMatchingOverIsWithCastCheck = null,
        CodeStyleOption2<bool>? PreferNotPattern = null,
        CodeStyleOption2<bool>? PreferExtendedPropertyPattern = null,
        CodeStyleOption2<bool>? PreferInlinedVariableDeclaration = null,
        CodeStyleOption2<bool>? PreferDeconstructedVariableDeclaration = null,
        CodeStyleOption2<bool>? PreferIndexOperator = null,
        CodeStyleOption2<bool>? PreferRangeOperator = null,
        CodeStyleOption2<string>? PreferredModifierOrder = null,
        CodeStyleOption2<bool>? PreferSimpleUsingStatement = null,
        CodeStyleOption2<bool>? PreferLocalOverAnonymousFunction = null,
        CodeStyleOption2<bool>? PreferTupleSwap = null,
        CodeStyleOption2<UnusedValuePreference>? UnusedValueExpressionStatement = null,
        CodeStyleOption2<UnusedValuePreference>? UnusedValueAssignment = null,
        CodeStyleOption2<bool>? PreferMethodGroupConversion = null,
        CodeStyleOption2<ExpressionBodyPreference>? PreferExpressionBodiedLambdas = null,
        CodeStyleOption2<bool>? PreferStaticLocalFunction = null)
#pragma warning restore
        : base(Common)
    {
        this.ImplicitObjectCreationWhenTypeIsApparent = ImplicitObjectCreationWhenTypeIsApparent ?? s_trueWithSuggestionEnforcement;
        this.PreferNullCheckOverTypeCheck = PreferNullCheckOverTypeCheck ?? s_trueWithSuggestionEnforcement;
        this.AllowBlankLinesBetweenConsecutiveBraces = AllowBlankLinesBetweenConsecutiveBraces ?? s_trueWithSilentEnforcement;
        this.AllowBlankLineAfterColonInConstructorInitializer = AllowBlankLineAfterColonInConstructorInitializer ?? s_trueWithSilentEnforcement;
        this.PreferConditionalDelegateCall = PreferConditionalDelegateCall ?? s_trueWithSuggestionEnforcement;
        this.PreferSwitchExpression = PreferSwitchExpression ?? s_trueWithSuggestionEnforcement;
        this.PreferPatternMatching = PreferPatternMatching ?? s_trueWithSilentEnforcement;
        this.PreferPatternMatchingOverAsWithNullCheck = PreferPatternMatchingOverAsWithNullCheck ?? s_trueWithSuggestionEnforcement;
        this.PreferPatternMatchingOverIsWithCastCheck = PreferPatternMatchingOverIsWithCastCheck ?? s_trueWithSuggestionEnforcement;
        this.PreferNotPattern = PreferNotPattern ?? s_trueWithSuggestionEnforcement;
        this.PreferExtendedPropertyPattern = PreferExtendedPropertyPattern ?? s_trueWithSuggestionEnforcement;
        this.PreferInlinedVariableDeclaration = PreferInlinedVariableDeclaration ?? s_trueWithSuggestionEnforcement;
        this.PreferDeconstructedVariableDeclaration = PreferDeconstructedVariableDeclaration ?? s_trueWithSuggestionEnforcement;
        this.PreferIndexOperator = PreferIndexOperator ?? s_trueWithSuggestionEnforcement;
        this.PreferRangeOperator = PreferRangeOperator ?? s_trueWithSuggestionEnforcement;
        this.PreferredModifierOrder = PreferredModifierOrder ?? s_defaultModifierOrder;
        this.PreferSimpleUsingStatement = PreferSimpleUsingStatement ?? s_trueWithSuggestionEnforcement;
        this.PreferLocalOverAnonymousFunction = PreferLocalOverAnonymousFunction ?? s_trueWithSuggestionEnforcement;
        this.PreferTupleSwap = PreferTupleSwap ?? s_trueWithSuggestionEnforcement;
        this.UnusedValueExpressionStatement = UnusedValueExpressionStatement ?? s_discardVariableWithSilentEnforcement;
        this.UnusedValueAssignment = UnusedValueAssignment ?? s_discardVariableWithSilentEnforcement;
        this.PreferMethodGroupConversion = PreferMethodGroupConversion ?? s_trueWithSilentEnforcement;
        this.PreferExpressionBodiedLambdas = PreferExpressionBodiedLambdas ?? s_whenPossibleWithSilentEnforcement;
        this.PreferStaticLocalFunction = PreferStaticLocalFunction ?? s_trueWithSuggestionEnforcement;
    }
}
