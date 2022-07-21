// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle;

[DataContract]
internal sealed class CSharpIdeCodeStyleOptions : IdeCodeStyleOptions, IEquatable<CSharpIdeCodeStyleOptions>
{
    private static readonly ImmutableArray<SyntaxKind> s_preferredModifierOrderDefault = ImmutableArray.Create(
        SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword,
        SyntaxKind.FileKeyword,
        SyntaxKind.StaticKeyword,
        SyntaxKind.ExternKeyword,
        SyntaxKind.NewKeyword,
        SyntaxKind.VirtualKeyword, SyntaxKind.AbstractKeyword, SyntaxKind.SealedKeyword, SyntaxKind.OverrideKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.RequiredKeyword,
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

    [DataMember] public CodeStyleOption2<bool> ImplicitObjectCreationWhenTypeIsApparent { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferNullCheckOverTypeCheck { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> AllowBlankLinesBetweenConsecutiveBraces { get; init; } = s_trueWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> AllowBlankLineAfterColonInConstructorInitializer { get; init; } = s_trueWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferConditionalDelegateCall { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferSwitchExpression { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferPatternMatching { get; init; } = s_trueWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferPatternMatchingOverAsWithNullCheck { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferPatternMatchingOverIsWithCastCheck { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferNotPattern { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferExtendedPropertyPattern { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferInlinedVariableDeclaration { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferDeconstructedVariableDeclaration { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferIndexOperator { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferRangeOperator { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferUtf8StringLiterals { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<string> PreferredModifierOrder { get; init; } = s_defaultModifierOrder;
    [DataMember] public CodeStyleOption2<bool> PreferSimpleUsingStatement { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferLocalOverAnonymousFunction { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferTupleSwap { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<UnusedValuePreference> UnusedValueExpressionStatement { get; init; } = s_discardVariableWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<UnusedValuePreference> UnusedValueAssignment { get; init; } = s_discardVariableWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferMethodGroupConversion { get; init; } = s_trueWithSilentEnforcement;

    // the following are also used in code generation features, consider sharing:
    [DataMember] public CodeStyleOption2<bool> PreferStaticLocalFunction { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLambdas { get; init; } = s_whenPossibleWithSilentEnforcement;

    public override bool Equals(object? obj)
        => Equals(obj as CSharpIdeCodeStyleOptions);

    public bool Equals([AllowNull] CSharpIdeCodeStyleOptions other)
        => other is not null &&
           Common.Equals(Common) &&
           ImplicitObjectCreationWhenTypeIsApparent.Equals(ImplicitObjectCreationWhenTypeIsApparent) &&
           PreferNullCheckOverTypeCheck.Equals(PreferNullCheckOverTypeCheck) &&
           AllowBlankLinesBetweenConsecutiveBraces.Equals(AllowBlankLinesBetweenConsecutiveBraces) &&
           AllowBlankLineAfterColonInConstructorInitializer.Equals(AllowBlankLineAfterColonInConstructorInitializer) &&
           PreferConditionalDelegateCall.Equals(PreferConditionalDelegateCall) &&
           PreferSwitchExpression.Equals(PreferSwitchExpression) &&
           PreferPatternMatching.Equals(PreferPatternMatching) &&
           PreferPatternMatchingOverAsWithNullCheck.Equals(PreferPatternMatchingOverAsWithNullCheck) &&
           PreferPatternMatchingOverIsWithCastCheck.Equals(PreferPatternMatchingOverIsWithCastCheck) &&
           PreferNotPattern.Equals(PreferNotPattern) &&
           PreferExtendedPropertyPattern.Equals(PreferExtendedPropertyPattern) &&
           PreferInlinedVariableDeclaration.Equals(PreferInlinedVariableDeclaration) &&
           PreferDeconstructedVariableDeclaration.Equals(PreferDeconstructedVariableDeclaration) &&
           PreferIndexOperator.Equals(PreferIndexOperator) &&
           PreferRangeOperator.Equals(PreferRangeOperator) &&
           PreferUtf8StringLiterals.Equals(PreferUtf8StringLiterals) &&
           PreferredModifierOrder.Equals(PreferredModifierOrder) &&
           PreferSimpleUsingStatement.Equals(PreferSimpleUsingStatement) &&
           PreferLocalOverAnonymousFunction.Equals(PreferLocalOverAnonymousFunction) &&
           PreferTupleSwap.Equals(PreferTupleSwap) &&
           UnusedValueExpressionStatement.Equals(UnusedValueExpressionStatement) &&
           UnusedValueAssignment.Equals(UnusedValueAssignment) &&
           PreferMethodGroupConversion.Equals(PreferMethodGroupConversion) &&
           PreferStaticLocalFunction.Equals(PreferStaticLocalFunction) &&
           PreferExpressionBodiedLambdas.Equals(PreferExpressionBodiedLambdas);

    public override int GetHashCode()
        => Hash.Combine(Common,
           Hash.Combine(ImplicitObjectCreationWhenTypeIsApparent,
           Hash.Combine(PreferNullCheckOverTypeCheck,
           Hash.Combine(AllowBlankLinesBetweenConsecutiveBraces,
           Hash.Combine(AllowBlankLineAfterColonInConstructorInitializer,
           Hash.Combine(PreferConditionalDelegateCall,
           Hash.Combine(PreferSwitchExpression,
           Hash.Combine(PreferPatternMatching,
           Hash.Combine(PreferPatternMatchingOverAsWithNullCheck,
           Hash.Combine(PreferPatternMatchingOverIsWithCastCheck,
           Hash.Combine(PreferNotPattern,
           Hash.Combine(PreferExtendedPropertyPattern,
           Hash.Combine(PreferInlinedVariableDeclaration,
           Hash.Combine(PreferDeconstructedVariableDeclaration,
           Hash.Combine(PreferIndexOperator,
           Hash.Combine(PreferRangeOperator,
           Hash.Combine(PreferUtf8StringLiterals,
           Hash.Combine(PreferredModifierOrder,
           Hash.Combine(PreferSimpleUsingStatement,
           Hash.Combine(PreferLocalOverAnonymousFunction,
           Hash.Combine(PreferTupleSwap,
           Hash.Combine(UnusedValueExpressionStatement,
           Hash.Combine(UnusedValueAssignment,
           Hash.Combine(PreferMethodGroupConversion,
           Hash.Combine(PreferStaticLocalFunction,
           Hash.Combine(PreferExpressionBodiedLambdas, 0))))))))))))))))))))))))));
}
