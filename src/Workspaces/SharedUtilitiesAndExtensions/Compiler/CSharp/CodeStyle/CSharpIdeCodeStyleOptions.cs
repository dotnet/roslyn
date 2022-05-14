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

    private static readonly CodeStyleOption2<UnusedValuePreference> s_discardVariableWithSuggestionEnforcement =
        new(UnusedValuePreference.DiscardVariable, NotificationOption2.Suggestion);

    private static readonly CodeStyleOption2<string> s_defaultModifierOrder =
        new(string.Join(",", s_preferredModifierOrderDefault.Select(SyntaxFacts.GetText)), NotificationOption2.Silent);

    public static readonly CodeStyleOption2<AddImportPlacement> s_outsideNamespacePlacementWithSilentEnforcement =
        new(AddImportPlacement.OutsideNamespace, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<ExpressionBodyPreference> s_whenPossibleWithSilentEnforcement =
        new(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent);

    public static readonly CSharpIdeCodeStyleOptions Default = new();

    [DataMember(Order = BaseMemberCount + 0)] public CodeStyleOption2<bool> ImplicitObjectCreationWhenTypeIsApparent { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 1)] public CodeStyleOption2<bool> PreferNullCheckOverTypeCheck { get; init; } = s_trueWithSuggestionEnforcement;

    [DataMember(Order = BaseMemberCount + 2)] public CodeStyleOption2<bool> AllowBlankLinesBetweenConsecutiveBraces { get; init; } = s_trueWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 3)] public CodeStyleOption2<bool> AllowBlankLineAfterColonInConstructorInitializer { get; init; } = s_trueWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 4)] public CodeStyleOption2<bool> PreferConditionalDelegateCall { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 5)] public CodeStyleOption2<bool> PreferSwitchExpression { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 6)] public CodeStyleOption2<bool> PreferPatternMatching { get; init; } = s_trueWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 7)] public CodeStyleOption2<bool> PreferPatternMatchingOverAsWithNullCheck { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 8)] public CodeStyleOption2<bool> PreferPatternMatchingOverIsWithCastCheck { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 9)] public CodeStyleOption2<bool> PreferNotPattern { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 10)] public CodeStyleOption2<bool> PreferExtendedPropertyPattern { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 11)] public CodeStyleOption2<bool> PreferInlinedVariableDeclaration { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 12)] public CodeStyleOption2<bool> PreferDeconstructedVariableDeclaration { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 13)] public CodeStyleOption2<bool> PreferIndexOperator { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 14)] public CodeStyleOption2<bool> PreferRangeOperator { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 15)] public CodeStyleOption2<bool> PreferUtf8StringLiterals { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 16)] public CodeStyleOption2<string> PreferredModifierOrder { get; init; } = s_defaultModifierOrder;
    [DataMember(Order = BaseMemberCount + 17)] public CodeStyleOption2<bool> PreferSimpleUsingStatement { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 18)] public CodeStyleOption2<bool> PreferLocalOverAnonymousFunction { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 19)] public CodeStyleOption2<bool> PreferTupleSwap { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 20)] public CodeStyleOption2<UnusedValuePreference> UnusedValueExpressionStatement { get; init; } = s_discardVariableWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 21)] public CodeStyleOption2<UnusedValuePreference> UnusedValueAssignment { get; init; } = s_discardVariableWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 22)] public CodeStyleOption2<bool> PreferMethodGroupConversion { get; init; } = s_trueWithSilentEnforcement;

    // the following are also used in code generation features, consider sharing:
    [DataMember(Order = BaseMemberCount + 23)] public CodeStyleOption2<bool> PreferStaticLocalFunction { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 24)] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLambdas { get; init; } = s_whenPossibleWithSilentEnforcement;
}
