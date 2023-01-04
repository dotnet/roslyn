// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.AddImport;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal interface ICodeStyleOptionsStorage : ILanguageService
{
    IdeCodeStyleOptions GetOptions(IGlobalOptionService globalOptions);
}

internal static class CodeStyleOptionsStorage
{
    public static IdeCodeStyleOptions GetCodeStyleOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeStyleOptionsStorage>().GetOptions(globalOptions);

    public static IdeCodeStyleOptions.CommonOptions GetCommonCodeStyleOptions(this IGlobalOptionService globalOptions, string language)
        => new()
        {
            PreferObjectInitializer = globalOptions.GetOption(CodeStyleOptions2.PreferObjectInitializer, language),
            PreferCollectionInitializer = globalOptions.GetOption(CodeStyleOptions2.PreferCollectionInitializer, language),
            PreferSimplifiedBooleanExpressions = globalOptions.GetOption(CodeStyleOptions2.PreferSimplifiedBooleanExpressions, language),
            OperatorPlacementWhenWrapping = globalOptions.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping),
            PreferCoalesceExpression = globalOptions.GetOption(CodeStyleOptions2.PreferCoalesceExpression, language),
            PreferNullPropagation = globalOptions.GetOption(CodeStyleOptions2.PreferNullPropagation, language),
            PreferExplicitTupleNames = globalOptions.GetOption(CodeStyleOptions2.PreferExplicitTupleNames, language),
            PreferAutoProperties = globalOptions.GetOption(CodeStyleOptions2.PreferAutoProperties, language),
            PreferInferredTupleNames = globalOptions.GetOption(CodeStyleOptions2.PreferInferredTupleNames, language),
            PreferInferredAnonymousTypeMemberNames = globalOptions.GetOption(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, language),
            PreferIsNullCheckOverReferenceEqualityMethod = globalOptions.GetOption(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod, language),
            PreferConditionalExpressionOverAssignment = globalOptions.GetOption(CodeStyleOptions2.PreferConditionalExpressionOverAssignment, language),
            PreferConditionalExpressionOverReturn = globalOptions.GetOption(CodeStyleOptions2.PreferConditionalExpressionOverReturn, language),
            PreferCompoundAssignment = globalOptions.GetOption(CodeStyleOptions2.PreferCompoundAssignment, language),
            PreferSimplifiedInterpolation = globalOptions.GetOption(CodeStyleOptions2.PreferSimplifiedInterpolation, language),
            UnusedParameters = globalOptions.GetOption(CodeStyleOptions2.UnusedParameters, language),
            AccessibilityModifiersRequired = globalOptions.GetOption(CodeStyleOptions2.AccessibilityModifiersRequired, language),
            PreferReadonly = globalOptions.GetOption(CodeStyleOptions2.PreferReadonly, language),
            ArithmeticBinaryParentheses = globalOptions.GetOption(CodeStyleOptions2.ArithmeticBinaryParentheses, language),
            OtherBinaryParentheses = globalOptions.GetOption(CodeStyleOptions2.OtherBinaryParentheses, language),
            RelationalBinaryParentheses = globalOptions.GetOption(CodeStyleOptions2.RelationalBinaryParentheses, language),
            OtherParentheses = globalOptions.GetOption(CodeStyleOptions2.OtherParentheses, language),
            ForEachExplicitCastInSource = globalOptions.GetOption(CodeStyleOptions2.ForEachExplicitCastInSource),
            PreferNamespaceAndFolderMatchStructure = globalOptions.GetOption(CodeStyleOptions2.PreferNamespaceAndFolderMatchStructure, language),
            AllowMultipleBlankLines = globalOptions.GetOption(CodeStyleOptions2.AllowMultipleBlankLines, language),
            AllowStatementImmediatelyAfterBlock = globalOptions.GetOption(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, language),
            RemoveUnnecessarySuppressionExclusions = globalOptions.GetOption(CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions)
        };
}
