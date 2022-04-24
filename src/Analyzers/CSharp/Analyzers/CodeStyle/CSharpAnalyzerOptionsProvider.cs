// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Provides convenient access to C# editorconfig options with fallback to IDE default values.
/// </summary>
internal readonly struct CSharpAnalyzerOptionsProvider
{
    /// <summary>
    /// Document editorconfig options.
    /// </summary>
    private readonly AnalyzerConfigOptions _options;

    /// <summary>
    /// Fallback options - the default options in Code Style layer.
    /// </summary>
    private readonly IdeAnalyzerOptions _fallbackOptions;

    public CSharpAnalyzerOptionsProvider(AnalyzerConfigOptions options, IdeAnalyzerOptions fallbackOptions)
    {
        _options = options;
        _fallbackOptions = fallbackOptions;
    }

    public CSharpAnalyzerOptionsProvider(AnalyzerConfigOptions options, AnalyzerOptions fallbackOptions)
        : this(options, fallbackOptions.GetIdeOptions())
    {
    }

    // SimplifierOptions

    public CodeStyleOption2<bool> VarForBuiltInTypes => GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes, FallbackSimplifierOptions.VarForBuiltInTypes);
    public CodeStyleOption2<bool> VarWhenTypeIsApparent => GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, FallbackSimplifierOptions.VarWhenTypeIsApparent);
    public CodeStyleOption2<bool> VarElsewhere => GetOption(CSharpCodeStyleOptions.VarElsewhere, FallbackSimplifierOptions.VarElsewhere);
    public CodeStyleOption2<bool> PreferSimpleDefaultExpression => GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, FallbackSimplifierOptions.PreferSimpleDefaultExpression);
    public CodeStyleOption2<PreferBracesPreference> PreferBraces => GetOption(CSharpCodeStyleOptions.PreferBraces, FallbackSimplifierOptions.PreferBraces);

    // CodeStyleOptions

    public CodeStyleOption2<bool> ImplicitObjectCreationWhenTypeIsApparent => GetOption(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent, FallbackCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent);
    public CodeStyleOption2<bool> PreferNullCheckOverTypeCheck => GetOption(CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck, FallbackCodeStyleOptions.PreferNullCheckOverTypeCheck);
    public CodeStyleOption2<bool> PreferParameterNullChecking => GetOption(CSharpCodeStyleOptions.PreferParameterNullChecking, FallbackCodeStyleOptions.PreferParameterNullChecking);
    public CodeStyleOption2<bool> AllowEmbeddedStatementsOnSameLine => GetOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, FallbackCodeStyleOptions.AllowEmbeddedStatementsOnSameLine);
    public CodeStyleOption2<bool> AllowBlankLinesBetweenConsecutiveBraces => GetOption(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, FallbackCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces);
    public CodeStyleOption2<bool> AllowBlankLineAfterColonInConstructorInitializer => GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, FallbackCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer);
    public CodeStyleOption2<bool> PreferConditionalDelegateCall => GetOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall, FallbackCodeStyleOptions.PreferConditionalDelegateCall);
    public CodeStyleOption2<bool> PreferSwitchExpression => GetOption(CSharpCodeStyleOptions.PreferSwitchExpression, FallbackCodeStyleOptions.PreferSwitchExpression);
    public CodeStyleOption2<bool> PreferPatternMatching => GetOption(CSharpCodeStyleOptions.PreferPatternMatching, FallbackCodeStyleOptions.PreferPatternMatching);
    public CodeStyleOption2<bool> PreferPatternMatchingOverAsWithNullCheck => GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, FallbackCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck);
    public CodeStyleOption2<bool> PreferPatternMatchingOverIsWithCastCheck => GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, FallbackCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck);
    public CodeStyleOption2<bool> PreferNotPattern => GetOption(CSharpCodeStyleOptions.PreferNotPattern, FallbackCodeStyleOptions.PreferNotPattern);
    public CodeStyleOption2<bool> PreferExtendedPropertyPattern => GetOption(CSharpCodeStyleOptions.PreferExtendedPropertyPattern, FallbackCodeStyleOptions.PreferExtendedPropertyPattern);
    public CodeStyleOption2<bool> PreferThrowExpression => GetOption(CSharpCodeStyleOptions.PreferThrowExpression, FallbackCodeStyleOptions.PreferThrowExpression);
    public CodeStyleOption2<bool> PreferInlinedVariableDeclaration => GetOption(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, FallbackCodeStyleOptions.PreferInlinedVariableDeclaration);
    public CodeStyleOption2<bool> PreferDeconstructedVariableDeclaration => GetOption(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration, FallbackCodeStyleOptions.PreferDeconstructedVariableDeclaration);
    public CodeStyleOption2<bool> PreferIndexOperator => GetOption(CSharpCodeStyleOptions.PreferIndexOperator, FallbackCodeStyleOptions.PreferIndexOperator);
    public CodeStyleOption2<bool> PreferRangeOperator => GetOption(CSharpCodeStyleOptions.PreferRangeOperator, FallbackCodeStyleOptions.PreferRangeOperator);
    public CodeStyleOption2<string> PreferredModifierOrder => GetOption(CSharpCodeStyleOptions.PreferredModifierOrder, FallbackCodeStyleOptions.PreferredModifierOrder);
    public CodeStyleOption2<bool> PreferSimpleUsingStatement => GetOption(CSharpCodeStyleOptions.PreferSimpleUsingStatement, FallbackCodeStyleOptions.PreferSimpleUsingStatement);
    public CodeStyleOption2<bool> PreferLocalOverAnonymousFunction => GetOption(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, FallbackCodeStyleOptions.PreferLocalOverAnonymousFunction);
    public CodeStyleOption2<bool> PreferTupleSwap => GetOption(CSharpCodeStyleOptions.PreferTupleSwap, FallbackCodeStyleOptions.PreferTupleSwap);
    public CodeStyleOption2<UnusedValuePreference> UnusedValueExpressionStatement => GetOption(CSharpCodeStyleOptions.UnusedValueExpressionStatement, FallbackCodeStyleOptions.UnusedValueExpressionStatement);
    public CodeStyleOption2<UnusedValuePreference> UnusedValueAssignment => GetOption(CSharpCodeStyleOptions.UnusedValueAssignment, FallbackCodeStyleOptions.UnusedValueAssignment);
    public CodeStyleOption2<bool> PreferMethodGroupConversion => GetOption(CSharpCodeStyleOptions.PreferMethodGroupConversion, FallbackCodeStyleOptions.PreferMethodGroupConversion);
    public CodeStyleOption2<bool> PreferTopLevelStatements => GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements, FallbackCodeStyleOptions.PreferTopLevelStatements);

    // CodeGenerationOptions

    public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLambdas => GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, FallbackCodeStyleOptions.PreferExpressionBodiedLambdas);
    public CodeStyleOption2<bool> PreferStaticLocalFunction => GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction, FallbackCodeStyleOptions.PreferStaticLocalFunction);
    public CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations => GetOption(CSharpCodeStyleOptions.NamespaceDeclarations, FallbackCodeStyleOptions.NamespaceDeclarations);
    public CodeStyleOption2<AddImportPlacement> PreferredUsingDirectivePlacement => GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, FallbackCodeStyleOptions.PreferredUsingDirectivePlacement);

    private TValue GetOption<TValue>(Option2<TValue> option, TValue defaultValue)
        => _options.GetEditorConfigOption(option, defaultValue);

    private CSharpIdeCodeStyleOptions FallbackCodeStyleOptions
        => (CSharpIdeCodeStyleOptions?)_fallbackOptions.CodeStyleOptions ?? CSharpIdeCodeStyleOptions.Default;

    private CSharpSimplifierOptions FallbackSimplifierOptions
        => (CSharpSimplifierOptions?)_fallbackOptions.CleanupOptions?.SimplifierOptions ?? CSharpSimplifierOptions.Default;

    public static explicit operator CSharpAnalyzerOptionsProvider(AnalyzerOptionsProvider provider)
        => new(provider.GetAnalyzerConfigOptions(), provider.GetFallbackOptions());

    public static implicit operator AnalyzerOptionsProvider(CSharpAnalyzerOptionsProvider provider)
        => new(provider._options, provider._fallbackOptions);
}

internal static class CSharpAnalyzerOptionsProviders
{
    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this SemanticModelAnalysisContext context)
        => new(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.SemanticModel.SyntaxTree), context.Options);

    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this SyntaxNodeAnalysisContext context)
        => new(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree), context.Options);

    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this SyntaxTreeAnalysisContext context)
        => new(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree), context.Options);

    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this OperationAnalysisContext context)
        => new(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Operation.Syntax.SyntaxTree), context.Options);
}
