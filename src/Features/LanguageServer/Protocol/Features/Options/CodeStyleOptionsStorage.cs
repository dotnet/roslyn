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
    public static IdeCodeStyleOptions GetCodeStyleOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeStyleOptionsStorage>().GetOptions(globalOptions);

    public static IdeCodeStyleOptions.CommonOptions GetCommonCodeStyleOptions(this IGlobalOptionService globalOptions, string language)
        => new(
            globalOptions.GetOption(CodeStyleOptions2.PreferObjectInitializer, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferCollectionInitializer, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferSimplifiedBooleanExpressions, language),
            globalOptions.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferCoalesceExpression, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferNullPropagation, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferExplicitTupleNames, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferAutoProperties, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferInferredTupleNames, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferConditionalExpressionOverAssignment, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferConditionalExpressionOverReturn, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferCompoundAssignment, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferSimplifiedInterpolation, language),
            globalOptions.GetOption(CodeStyleOptions2.UnusedParameters, language),
            globalOptions.GetOption(CodeStyleOptions2.RequireAccessibilityModifiers, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferReadonly, language),
            globalOptions.GetOption(CodeStyleOptions2.ArithmeticBinaryParentheses, language),
            globalOptions.GetOption(CodeStyleOptions2.OtherBinaryParentheses, language),
            globalOptions.GetOption(CodeStyleOptions2.RelationalBinaryParentheses, language),
            globalOptions.GetOption(CodeStyleOptions2.OtherParentheses, language),
            globalOptions.GetOption(CodeStyleOptions2.ForEachExplicitCastInSource, language),
            globalOptions.GetOption(CodeStyleOptions2.PreferNamespaceAndFolderMatchStructure, language),
            globalOptions.GetOption(CodeStyleOptions2.AllowMultipleBlankLines, language),
            globalOptions.GetOption(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, language),
            globalOptions.GetOption(CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions));
}
