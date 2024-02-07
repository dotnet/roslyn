// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Provides C# analyzers a convenient access to editorconfig options with fallback to IDE default values.
/// </summary>
internal readonly struct CSharpAnalyzerOptionsProvider(IOptionsReader options, IdeAnalyzerOptions fallbackOptions)
{
    /// <summary>
    /// Document editorconfig options.
    /// </summary>
    private readonly IOptionsReader _options = options;

    /// <summary>
    /// Fallback options - the default options in Code Style layer.
    /// </summary>
    private readonly IdeAnalyzerOptions _fallbackOptions = fallbackOptions;

    public CSharpAnalyzerOptionsProvider(IOptionsReader options, AnalyzerOptions fallbackOptions)
        : this(options, fallbackOptions.GetIdeOptions())
    {
    }

    // SimplifierOptions

    public CodeStyleOption2<bool> VarForBuiltInTypes => GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes, FallbackSimplifierOptions.VarForBuiltInTypes);
    public CodeStyleOption2<bool> VarWhenTypeIsApparent => GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, FallbackSimplifierOptions.VarWhenTypeIsApparent);
    public CodeStyleOption2<bool> VarElsewhere => GetOption(CSharpCodeStyleOptions.VarElsewhere, FallbackSimplifierOptions.VarElsewhere);
    public CodeStyleOption2<bool> PreferSimpleDefaultExpression => GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, FallbackSimplifierOptions.PreferSimpleDefaultExpression);
    public CodeStyleOption2<bool> AllowEmbeddedStatementsOnSameLine => GetOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, FallbackSimplifierOptions.AllowEmbeddedStatementsOnSameLine);
    public CodeStyleOption2<bool> PreferThrowExpression => GetOption(CSharpCodeStyleOptions.PreferThrowExpression, FallbackSimplifierOptions.PreferThrowExpression);
    public CodeStyleOption2<PreferBracesPreference> PreferBraces => GetOption(CSharpCodeStyleOptions.PreferBraces, FallbackSimplifierOptions.PreferBraces);

    internal CSharpSimplifierOptions GetSimplifierOptions()
        => new(_options, FallbackSimplifierOptions);

    // SyntaxFormattingOptions

    public CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations => GetOption(CSharpCodeStyleOptions.NamespaceDeclarations, FallbackSyntaxFormattingOptions.NamespaceDeclarations);
    public CodeStyleOption2<bool> PreferTopLevelStatements => GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements, FallbackSyntaxFormattingOptions.PreferTopLevelStatements);

    // AddImportPlacementOptions

    public CodeStyleOption2<AddImportPlacement> UsingDirectivePlacement => GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, FallbackAddImportPlacementOptions.UsingDirectivePlacement);

    // CodeStyleOptions

    public CodeStyleOption2<bool> ImplicitObjectCreationWhenTypeIsApparent => GetOption(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent, FallbackCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent);
    public CodeStyleOption2<bool> PreferNullCheckOverTypeCheck => GetOption(CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck, FallbackCodeStyleOptions.PreferNullCheckOverTypeCheck);
    public CodeStyleOption2<bool> AllowBlankLinesBetweenConsecutiveBraces => GetOption(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, FallbackCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces);
    public CodeStyleOption2<bool> AllowBlankLineAfterColonInConstructorInitializer => GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, FallbackCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer);
    public CodeStyleOption2<bool> AllowBlankLineAfterTokenInArrowExpressionClause => GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, FallbackCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause);
    public CodeStyleOption2<bool> AllowBlankLineAfterTokenInConditionalExpression => GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, FallbackCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression);
    public CodeStyleOption2<bool> PreferConditionalDelegateCall => GetOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall, FallbackCodeStyleOptions.PreferConditionalDelegateCall);
    public CodeStyleOption2<bool> PreferSwitchExpression => GetOption(CSharpCodeStyleOptions.PreferSwitchExpression, FallbackCodeStyleOptions.PreferSwitchExpression);
    public CodeStyleOption2<bool> PreferPatternMatching => GetOption(CSharpCodeStyleOptions.PreferPatternMatching, FallbackCodeStyleOptions.PreferPatternMatching);
    public CodeStyleOption2<bool> PreferPatternMatchingOverAsWithNullCheck => GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, FallbackCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck);
    public CodeStyleOption2<bool> PreferPatternMatchingOverIsWithCastCheck => GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, FallbackCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck);
    public CodeStyleOption2<bool> PreferNotPattern => GetOption(CSharpCodeStyleOptions.PreferNotPattern, FallbackCodeStyleOptions.PreferNotPattern);
    public CodeStyleOption2<bool> PreferExtendedPropertyPattern => GetOption(CSharpCodeStyleOptions.PreferExtendedPropertyPattern, FallbackCodeStyleOptions.PreferExtendedPropertyPattern);
    public CodeStyleOption2<bool> PreferInlinedVariableDeclaration => GetOption(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, FallbackCodeStyleOptions.PreferInlinedVariableDeclaration);
    public CodeStyleOption2<bool> PreferDeconstructedVariableDeclaration => GetOption(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration, FallbackCodeStyleOptions.PreferDeconstructedVariableDeclaration);
    public CodeStyleOption2<bool> PreferIndexOperator => GetOption(CSharpCodeStyleOptions.PreferIndexOperator, FallbackCodeStyleOptions.PreferIndexOperator);
    public CodeStyleOption2<bool> PreferRangeOperator => GetOption(CSharpCodeStyleOptions.PreferRangeOperator, FallbackCodeStyleOptions.PreferRangeOperator);
    public CodeStyleOption2<bool> PreferUtf8StringLiterals => GetOption(CSharpCodeStyleOptions.PreferUtf8StringLiterals, FallbackCodeStyleOptions.PreferUtf8StringLiterals);
    public CodeStyleOption2<string> PreferredModifierOrder => GetOption(CSharpCodeStyleOptions.PreferredModifierOrder, FallbackCodeStyleOptions.PreferredModifierOrder);
    public CodeStyleOption2<bool> PreferSimpleUsingStatement => GetOption(CSharpCodeStyleOptions.PreferSimpleUsingStatement, FallbackCodeStyleOptions.PreferSimpleUsingStatement);
    public CodeStyleOption2<bool> PreferLocalOverAnonymousFunction => GetOption(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, FallbackCodeStyleOptions.PreferLocalOverAnonymousFunction);
    public CodeStyleOption2<bool> PreferTupleSwap => GetOption(CSharpCodeStyleOptions.PreferTupleSwap, FallbackCodeStyleOptions.PreferTupleSwap);
    public CodeStyleOption2<UnusedValuePreference> UnusedValueExpressionStatement => GetOption(CSharpCodeStyleOptions.UnusedValueExpressionStatement, FallbackCodeStyleOptions.UnusedValueExpressionStatement);
    public CodeStyleOption2<UnusedValuePreference> UnusedValueAssignment => GetOption(CSharpCodeStyleOptions.UnusedValueAssignment, FallbackCodeStyleOptions.UnusedValueAssignment);
    public CodeStyleOption2<bool> PreferMethodGroupConversion => GetOption(CSharpCodeStyleOptions.PreferMethodGroupConversion, FallbackCodeStyleOptions.PreferMethodGroupConversion);
    public CodeStyleOption2<bool> PreferPrimaryConstructors => GetOption(CSharpCodeStyleOptions.PreferPrimaryConstructors, FallbackCodeStyleOptions.PreferPrimaryConstructors);

    // CodeGenerationOptions

    internal CSharpCodeGenerationOptions GetCodeGenerationOptions()
        => new(_options, FallbackCodeGenerationOptions);

    public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLambdas => GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, FallbackCodeStyleOptions.PreferExpressionBodiedLambdas);
    public CodeStyleOption2<bool> PreferReadOnlyStruct => GetOption(CSharpCodeStyleOptions.PreferReadOnlyStruct, FallbackCodeStyleOptions.PreferReadOnlyStruct);
    public CodeStyleOption2<bool> PreferReadOnlyStructMember => GetOption(CSharpCodeStyleOptions.PreferReadOnlyStructMember, FallbackCodeStyleOptions.PreferReadOnlyStructMember);
    public CodeStyleOption2<bool> PreferStaticLocalFunction => GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction, FallbackCodeStyleOptions.PreferStaticLocalFunction);

    private TValue GetOption<TValue>(Option2<TValue> option, TValue defaultValue)
        => _options.GetOption(option, defaultValue);

    private CSharpIdeCodeStyleOptions FallbackCodeStyleOptions
        => (CSharpIdeCodeStyleOptions?)_fallbackOptions.CodeStyleOptions ?? CSharpIdeCodeStyleOptions.Default;

    private CSharpSimplifierOptions FallbackSimplifierOptions
        => (CSharpSimplifierOptions?)_fallbackOptions.CleanupOptions?.SimplifierOptions ?? CSharpSimplifierOptions.Default;

    private CSharpSyntaxFormattingOptions FallbackSyntaxFormattingOptions
        => (CSharpSyntaxFormattingOptions?)_fallbackOptions.CleanupOptions?.FormattingOptions ?? CSharpSyntaxFormattingOptions.Default;

    private AddImportPlacementOptions FallbackAddImportPlacementOptions
        => _fallbackOptions.CleanupOptions?.AddImportOptions ?? AddImportPlacementOptions.Default;

    private CSharpCodeGenerationOptions FallbackCodeGenerationOptions
        => (CSharpCodeGenerationOptions?)_fallbackOptions.GenerationOptions ?? CSharpCodeGenerationOptions.Default;

    public static explicit operator CSharpAnalyzerOptionsProvider(AnalyzerOptionsProvider provider)
        => new(provider.GetAnalyzerConfigOptions(), provider.GetFallbackOptions());

    public static implicit operator AnalyzerOptionsProvider(CSharpAnalyzerOptionsProvider provider)
        => new(provider._options, LanguageNames.CSharp, provider._fallbackOptions);
}

internal static class CSharpAnalyzerOptionsProviders
{
    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this AnalyzerOptions options, SyntaxTree syntaxTree)
        => new(options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree).GetOptionsReader(), options);

    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this SemanticModelAnalysisContext context)
        => GetCSharpAnalyzerOptions(context.Options, context.SemanticModel.SyntaxTree);

    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this SyntaxNodeAnalysisContext context)
        => GetCSharpAnalyzerOptions(context.Options, context.Node.SyntaxTree);

    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this SyntaxTreeAnalysisContext context)
        => GetCSharpAnalyzerOptions(context.Options, context.Tree);

    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this CodeBlockAnalysisContext context)
        => GetCSharpAnalyzerOptions(context.Options, context.SemanticModel.SyntaxTree);

    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this OperationAnalysisContext context)
        => GetCSharpAnalyzerOptions(context.Options, context.Operation.Syntax.SyntaxTree);

    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this SymbolStartAnalysisContext context, SyntaxTree syntaxTree)
        => GetCSharpAnalyzerOptions(context.Options, syntaxTree);
}
