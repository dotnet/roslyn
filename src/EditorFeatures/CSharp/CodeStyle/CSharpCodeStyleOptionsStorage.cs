// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle;

internal static class CSharpCodeStyleOptionsStorage
{
    [ExportLanguageService(typeof(ICodeStyleOptionsStorage), LanguageNames.CSharp), Shared]
    private sealed class Service : ICodeStyleOptionsStorage
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Service()
        {
        }

        public IdeCodeStyleOptions GetOptions(IGlobalOptionService globalOptions)
            => GetCSharpCodeStyleOptions(globalOptions);
    }

    public static CSharpIdeCodeStyleOptions GetCSharpCodeStyleOptions(this IGlobalOptionService globalOptions)
        => new()
        {
            Common = globalOptions.GetCommonCodeStyleOptions(LanguageNames.CSharp),
            ImplicitObjectCreationWhenTypeIsApparent = globalOptions.GetOption(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent),
            PreferNullCheckOverTypeCheck = globalOptions.GetOption(CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck),
            AllowBlankLinesBetweenConsecutiveBraces = globalOptions.GetOption(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces),
            AllowBlankLineAfterColonInConstructorInitializer = globalOptions.GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer),
            PreferConditionalDelegateCall = globalOptions.GetOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall),
            PreferSwitchExpression = globalOptions.GetOption(CSharpCodeStyleOptions.PreferSwitchExpression),
            PreferPatternMatching = globalOptions.GetOption(CSharpCodeStyleOptions.PreferPatternMatching),
            PreferPatternMatchingOverAsWithNullCheck = globalOptions.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck),
            PreferPatternMatchingOverIsWithCastCheck = globalOptions.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck),
            PreferNotPattern = globalOptions.GetOption(CSharpCodeStyleOptions.PreferNotPattern),
            PreferExtendedPropertyPattern = globalOptions.GetOption(CSharpCodeStyleOptions.PreferExtendedPropertyPattern),
            PreferInlinedVariableDeclaration = globalOptions.GetOption(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration),
            PreferDeconstructedVariableDeclaration = globalOptions.GetOption(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration),
            PreferIndexOperator = globalOptions.GetOption(CSharpCodeStyleOptions.PreferIndexOperator),
            PreferRangeOperator = globalOptions.GetOption(CSharpCodeStyleOptions.PreferRangeOperator),
            PreferUtf8StringLiterals = globalOptions.GetOption(CSharpCodeStyleOptions.PreferUtf8StringLiterals),
            PreferredModifierOrder = globalOptions.GetOption(CSharpCodeStyleOptions.PreferredModifierOrder),
            PreferSimpleUsingStatement = globalOptions.GetOption(CSharpCodeStyleOptions.PreferSimpleUsingStatement),
            PreferLocalOverAnonymousFunction = globalOptions.GetOption(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction),
            PreferTupleSwap = globalOptions.GetOption(CSharpCodeStyleOptions.PreferTupleSwap),
            UnusedValueExpressionStatement = globalOptions.GetOption(CSharpCodeStyleOptions.UnusedValueExpressionStatement),
            UnusedValueAssignment = globalOptions.GetOption(CSharpCodeStyleOptions.UnusedValueAssignment),
            PreferMethodGroupConversion = globalOptions.GetOption(CSharpCodeStyleOptions.PreferMethodGroupConversion),
            PreferExpressionBodiedLambdas = globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas),
            PreferStaticLocalFunction = globalOptions.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction)
        };
}
