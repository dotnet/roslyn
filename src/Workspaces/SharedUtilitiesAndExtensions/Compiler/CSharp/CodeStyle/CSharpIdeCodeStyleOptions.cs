// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle;

[DataContract]
internal sealed record class CSharpIdeCodeStyleOptions : IdeCodeStyleOptions, IEquatable<CSharpIdeCodeStyleOptions>
{
    private static readonly ImmutableArray<SyntaxKind> s_preferredModifierOrderDefault =
    [
        SyntaxKind.PublicKeyword,
        SyntaxKind.PrivateKeyword,
        SyntaxKind.ProtectedKeyword,
        SyntaxKind.InternalKeyword,
        SyntaxKind.FileKeyword,
        SyntaxKind.StaticKeyword,
        SyntaxKind.ExternKeyword,
        SyntaxKind.NewKeyword,
        SyntaxKind.VirtualKeyword,
        SyntaxKind.AbstractKeyword,
        SyntaxKind.SealedKeyword,
        SyntaxKind.OverrideKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.RequiredKeyword,
        SyntaxKind.VolatileKeyword,
        SyntaxKind.AsyncKeyword,
    ];

    private static readonly CodeStyleOption2<UnusedValuePreference> s_discardVariableWithSilentEnforcement =
        new(UnusedValuePreference.DiscardVariable, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<UnusedValuePreference> s_discardVariableWithSuggestionEnforcement =
        new(UnusedValuePreference.DiscardVariable, NotificationOption2.Suggestion);

    private static readonly CodeStyleOption2<string> s_defaultModifierOrder =
        new(string.Join(",", s_preferredModifierOrderDefault.Select(SyntaxFacts.GetText)), NotificationOption2.Silent);

    private static readonly CodeStyleOption2<ExpressionBodyPreference> s_whenPossibleWithSilentEnforcement =
        new(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent);

    public static readonly CSharpIdeCodeStyleOptions Default = new();

    [DataMember] public CodeStyleOption2<bool> ImplicitObjectCreationWhenTypeIsApparent { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferNullCheckOverTypeCheck { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> AllowBlankLinesBetweenConsecutiveBraces { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> AllowBlankLineAfterColonInConstructorInitializer { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> AllowBlankLineAfterTokenInArrowExpressionClause { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> AllowBlankLineAfterTokenInConditionalExpression { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferConditionalDelegateCall { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferSwitchExpression { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferPatternMatching { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferPatternMatchingOverAsWithNullCheck { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferPatternMatchingOverIsWithCastCheck { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferNotPattern { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferExtendedPropertyPattern { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferInlinedVariableDeclaration { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferDeconstructedVariableDeclaration { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferIndexOperator { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferRangeOperator { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferUtf8StringLiterals { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<string> PreferredModifierOrder { get; init; } = s_defaultModifierOrder;
    [DataMember] public CodeStyleOption2<bool> PreferSimpleUsingStatement { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferLocalOverAnonymousFunction { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferTupleSwap { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<UnusedValuePreference> UnusedValueExpressionStatement { get; init; } = s_discardVariableWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<UnusedValuePreference> UnusedValueAssignment { get; init; } = s_discardVariableWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferMethodGroupConversion { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;

    // the following are also used in code generation features, consider sharing:
    [DataMember] public CodeStyleOption2<bool> PreferReadOnlyStruct { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferReadOnlyStructMember { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferStaticLocalFunction { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferStaticAnonymousFunction { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLambdas { get; init; } = s_whenPossibleWithSilentEnforcement;

    [DataMember] public CodeStyleOption2<bool> PreferPrimaryConstructors { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;

    public CSharpIdeCodeStyleOptions()
        : base()
    {
    }

    internal CSharpIdeCodeStyleOptions(IOptionsReader options, CSharpIdeCodeStyleOptions? fallbackOptions)
        : base(options, fallbackOptions ??= Default, LanguageNames.CSharp)
    {
        ImplicitObjectCreationWhenTypeIsApparent = options.GetOption(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent, fallbackOptions.ImplicitObjectCreationWhenTypeIsApparent);
        PreferNullCheckOverTypeCheck = options.GetOption(CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck, fallbackOptions.PreferNullCheckOverTypeCheck);
        AllowBlankLinesBetweenConsecutiveBraces = options.GetOption(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, fallbackOptions.AllowBlankLinesBetweenConsecutiveBraces);
        AllowBlankLineAfterColonInConstructorInitializer = options.GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, fallbackOptions.AllowBlankLineAfterColonInConstructorInitializer);
        AllowBlankLineAfterTokenInConditionalExpression = options.GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, fallbackOptions.AllowBlankLineAfterTokenInConditionalExpression);
        AllowBlankLineAfterTokenInArrowExpressionClause = options.GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, fallbackOptions.AllowBlankLineAfterTokenInArrowExpressionClause);
        PreferConditionalDelegateCall = options.GetOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall, fallbackOptions.PreferConditionalDelegateCall);
        PreferSwitchExpression = options.GetOption(CSharpCodeStyleOptions.PreferSwitchExpression, fallbackOptions.PreferSwitchExpression);
        PreferPatternMatching = options.GetOption(CSharpCodeStyleOptions.PreferPatternMatching, fallbackOptions.PreferPatternMatching);
        PreferPatternMatchingOverAsWithNullCheck = options.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, fallbackOptions.PreferPatternMatchingOverAsWithNullCheck);
        PreferPatternMatchingOverIsWithCastCheck = options.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, fallbackOptions.PreferPatternMatchingOverIsWithCastCheck);
        PreferNotPattern = options.GetOption(CSharpCodeStyleOptions.PreferNotPattern, fallbackOptions.PreferNotPattern);
        PreferExtendedPropertyPattern = options.GetOption(CSharpCodeStyleOptions.PreferExtendedPropertyPattern, fallbackOptions.PreferExtendedPropertyPattern);
        PreferInlinedVariableDeclaration = options.GetOption(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, fallbackOptions.PreferInlinedVariableDeclaration);
        PreferDeconstructedVariableDeclaration = options.GetOption(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration, fallbackOptions.PreferDeconstructedVariableDeclaration);
        PreferIndexOperator = options.GetOption(CSharpCodeStyleOptions.PreferIndexOperator, fallbackOptions.PreferIndexOperator);
        PreferRangeOperator = options.GetOption(CSharpCodeStyleOptions.PreferRangeOperator, fallbackOptions.PreferRangeOperator);
        PreferUtf8StringLiterals = options.GetOption(CSharpCodeStyleOptions.PreferUtf8StringLiterals, fallbackOptions.PreferUtf8StringLiterals);
        PreferredModifierOrder = options.GetOption(CSharpCodeStyleOptions.PreferredModifierOrder, fallbackOptions.PreferredModifierOrder);
        PreferSimpleUsingStatement = options.GetOption(CSharpCodeStyleOptions.PreferSimpleUsingStatement, fallbackOptions.PreferSimpleUsingStatement);
        PreferLocalOverAnonymousFunction = options.GetOption(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, fallbackOptions.PreferLocalOverAnonymousFunction);
        PreferTupleSwap = options.GetOption(CSharpCodeStyleOptions.PreferTupleSwap, fallbackOptions.PreferTupleSwap);
        UnusedValueExpressionStatement = options.GetOption(CSharpCodeStyleOptions.UnusedValueExpressionStatement, fallbackOptions.UnusedValueExpressionStatement);
        UnusedValueAssignment = options.GetOption(CSharpCodeStyleOptions.UnusedValueAssignment, fallbackOptions.UnusedValueAssignment);
        PreferMethodGroupConversion = options.GetOption(CSharpCodeStyleOptions.PreferMethodGroupConversion, fallbackOptions.PreferMethodGroupConversion);
        PreferExpressionBodiedLambdas = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, fallbackOptions.PreferExpressionBodiedLambdas);
        PreferReadOnlyStruct = options.GetOption(CSharpCodeStyleOptions.PreferReadOnlyStruct, fallbackOptions.PreferReadOnlyStruct);
        PreferReadOnlyStructMember = options.GetOption(CSharpCodeStyleOptions.PreferReadOnlyStructMember, fallbackOptions.PreferReadOnlyStructMember);
        PreferStaticLocalFunction = options.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction, fallbackOptions.PreferStaticLocalFunction);
        PreferStaticAnonymousFunction = options.GetOption(CSharpCodeStyleOptions.PreferStaticAnonymousFunction, fallbackOptions.PreferStaticAnonymousFunction);
        PreferPrimaryConstructors = options.GetOption(CSharpCodeStyleOptions.PreferPrimaryConstructors, fallbackOptions.PreferPrimaryConstructors);
    }
}
