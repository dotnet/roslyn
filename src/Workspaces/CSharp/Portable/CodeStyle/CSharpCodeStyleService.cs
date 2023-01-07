// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle;

[ExportLanguageService(typeof(ICodeStyleService), LanguageNames.CSharp), Shared]
internal sealed class CSharpCodeStyleService : ICodeStyleService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCodeStyleService()
    {
    }

    public IdeCodeStyleOptions DefaultOptions
        => CSharpIdeCodeStyleOptions.Default;

    public IdeCodeStyleOptions GetIdeCodeStyleOptions(IOptionsReader options, IdeCodeStyleOptions? fallbackOptions)
        => options.GetCSharpCodeStyleOptions((CSharpIdeCodeStyleOptions?)fallbackOptions);
}

internal static class CSharpIdeCodeStyleOptionsProviders
{
    public static CSharpIdeCodeStyleOptions GetCSharpCodeStyleOptions(this IOptionsReader options, CSharpIdeCodeStyleOptions? fallbackOptions)
    {
        fallbackOptions ??= CSharpIdeCodeStyleOptions.Default;

        return new()
        {
            Common = options.GetCommonCodeStyleOptions(LanguageNames.CSharp, fallbackOptions.Common),
            ImplicitObjectCreationWhenTypeIsApparent = options.GetOption(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent, fallbackOptions.ImplicitObjectCreationWhenTypeIsApparent),
            PreferNullCheckOverTypeCheck = options.GetOption(CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck, fallbackOptions.PreferNullCheckOverTypeCheck),
            AllowBlankLinesBetweenConsecutiveBraces = options.GetOption(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, fallbackOptions.AllowBlankLinesBetweenConsecutiveBraces),
            AllowBlankLineAfterColonInConstructorInitializer = options.GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, fallbackOptions.AllowBlankLineAfterColonInConstructorInitializer),
            AllowBlankLineAfterTokenInConditionalExpression = options.GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, fallbackOptions.AllowBlankLineAfterTokenInConditionalExpression),
            AllowBlankLineAfterTokenInArrowExpressionClause = options.GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, fallbackOptions.AllowBlankLineAfterTokenInArrowExpressionClause),
            PreferConditionalDelegateCall = options.GetOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall, fallbackOptions.PreferConditionalDelegateCall),
            PreferSwitchExpression = options.GetOption(CSharpCodeStyleOptions.PreferSwitchExpression, fallbackOptions.PreferSwitchExpression),
            PreferPatternMatching = options.GetOption(CSharpCodeStyleOptions.PreferPatternMatching, fallbackOptions.PreferPatternMatching),
            PreferPatternMatchingOverAsWithNullCheck = options.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, fallbackOptions.PreferPatternMatchingOverAsWithNullCheck),
            PreferPatternMatchingOverIsWithCastCheck = options.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, fallbackOptions.PreferPatternMatchingOverIsWithCastCheck),
            PreferNotPattern = options.GetOption(CSharpCodeStyleOptions.PreferNotPattern, fallbackOptions.PreferNotPattern),
            PreferExtendedPropertyPattern = options.GetOption(CSharpCodeStyleOptions.PreferExtendedPropertyPattern, fallbackOptions.PreferExtendedPropertyPattern),
            PreferInlinedVariableDeclaration = options.GetOption(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, fallbackOptions.PreferInlinedVariableDeclaration),
            PreferDeconstructedVariableDeclaration = options.GetOption(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration, fallbackOptions.PreferDeconstructedVariableDeclaration),
            PreferIndexOperator = options.GetOption(CSharpCodeStyleOptions.PreferIndexOperator, fallbackOptions.PreferIndexOperator),
            PreferRangeOperator = options.GetOption(CSharpCodeStyleOptions.PreferRangeOperator, fallbackOptions.PreferRangeOperator),
            PreferUtf8StringLiterals = options.GetOption(CSharpCodeStyleOptions.PreferUtf8StringLiterals, fallbackOptions.PreferUtf8StringLiterals),
            PreferredModifierOrder = options.GetOption(CSharpCodeStyleOptions.PreferredModifierOrder, fallbackOptions.PreferredModifierOrder),
            PreferSimpleUsingStatement = options.GetOption(CSharpCodeStyleOptions.PreferSimpleUsingStatement, fallbackOptions.PreferSimpleUsingStatement),
            PreferLocalOverAnonymousFunction = options.GetOption(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, fallbackOptions.PreferLocalOverAnonymousFunction),
            PreferTupleSwap = options.GetOption(CSharpCodeStyleOptions.PreferTupleSwap, fallbackOptions.PreferTupleSwap),
            UnusedValueExpressionStatement = options.GetOption(CSharpCodeStyleOptions.UnusedValueExpressionStatement, fallbackOptions.UnusedValueExpressionStatement),
            UnusedValueAssignment = options.GetOption(CSharpCodeStyleOptions.UnusedValueAssignment, fallbackOptions.UnusedValueAssignment),
            PreferMethodGroupConversion = options.GetOption(CSharpCodeStyleOptions.PreferMethodGroupConversion, fallbackOptions.PreferMethodGroupConversion),
            PreferExpressionBodiedLambdas = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, fallbackOptions.PreferExpressionBodiedLambdas),
            PreferReadOnlyStruct = options.GetOption(CSharpCodeStyleOptions.PreferReadOnlyStruct, fallbackOptions.PreferReadOnlyStruct),
            PreferStaticLocalFunction = options.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction, fallbackOptions.PreferStaticLocalFunction)
        };
    }
}
