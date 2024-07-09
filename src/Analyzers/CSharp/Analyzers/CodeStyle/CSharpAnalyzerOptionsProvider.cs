// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Provides C# analyzers a convenient access to editorconfig options with fallback to IDE default values.
/// </summary>
internal readonly struct CSharpAnalyzerOptionsProvider(IOptionsReader options)
{
    private IOptionsReader Options => options;

    // SimplifierOptions

    public CodeStyleOption2<bool> VarForBuiltInTypes => GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes);
    public CodeStyleOption2<bool> VarWhenTypeIsApparent => GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent);
    public CodeStyleOption2<bool> VarElsewhere => GetOption(CSharpCodeStyleOptions.VarElsewhere);
    public CodeStyleOption2<bool> PreferSimpleDefaultExpression => GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression);
    public CodeStyleOption2<bool> AllowEmbeddedStatementsOnSameLine => GetOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine);
    public CodeStyleOption2<bool> PreferThrowExpression => GetOption(CSharpCodeStyleOptions.PreferThrowExpression);
    public CodeStyleOption2<PreferBracesPreference> PreferBraces => GetOption(CSharpCodeStyleOptions.PreferBraces);

    internal CSharpSimplifierOptions GetSimplifierOptions()
        => new(options, fallbackOptions: null);

    // SyntaxFormattingOptions

    public CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations => GetOption(CSharpCodeStyleOptions.NamespaceDeclarations);
    public CodeStyleOption2<bool> PreferTopLevelStatements => GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);

    // AddImportPlacementOptions

    public CodeStyleOption2<AddImportPlacement> UsingDirectivePlacement => GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement);

    // CodeStyleOptions

    public CodeStyleOption2<bool> ImplicitObjectCreationWhenTypeIsApparent => GetOption(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent);
    public CodeStyleOption2<bool> PreferNullCheckOverTypeCheck => GetOption(CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck);
    public CodeStyleOption2<bool> AllowBlankLinesBetweenConsecutiveBraces => GetOption(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces);
    public CodeStyleOption2<bool> AllowBlankLineAfterColonInConstructorInitializer => GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer);
    public CodeStyleOption2<bool> AllowBlankLineAfterTokenInArrowExpressionClause => GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause);
    public CodeStyleOption2<bool> AllowBlankLineAfterTokenInConditionalExpression => GetOption(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression);
    public CodeStyleOption2<bool> PreferConditionalDelegateCall => GetOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall);
    public CodeStyleOption2<bool> PreferSwitchExpression => GetOption(CSharpCodeStyleOptions.PreferSwitchExpression);
    public CodeStyleOption2<bool> PreferPatternMatching => GetOption(CSharpCodeStyleOptions.PreferPatternMatching);
    public CodeStyleOption2<bool> PreferPatternMatchingOverAsWithNullCheck => GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck);
    public CodeStyleOption2<bool> PreferPatternMatchingOverIsWithCastCheck => GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck);
    public CodeStyleOption2<bool> PreferNotPattern => GetOption(CSharpCodeStyleOptions.PreferNotPattern);
    public CodeStyleOption2<bool> PreferExtendedPropertyPattern => GetOption(CSharpCodeStyleOptions.PreferExtendedPropertyPattern);
    public CodeStyleOption2<bool> PreferInlinedVariableDeclaration => GetOption(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration);
    public CodeStyleOption2<bool> PreferDeconstructedVariableDeclaration => GetOption(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration);
    public CodeStyleOption2<bool> PreferIndexOperator => GetOption(CSharpCodeStyleOptions.PreferIndexOperator);
    public CodeStyleOption2<bool> PreferRangeOperator => GetOption(CSharpCodeStyleOptions.PreferRangeOperator);
    public CodeStyleOption2<bool> PreferUtf8StringLiterals => GetOption(CSharpCodeStyleOptions.PreferUtf8StringLiterals);
    public CodeStyleOption2<string> PreferredModifierOrder => GetOption(CSharpCodeStyleOptions.PreferredModifierOrder);
    public CodeStyleOption2<bool> PreferSimpleUsingStatement => GetOption(CSharpCodeStyleOptions.PreferSimpleUsingStatement);
    public CodeStyleOption2<bool> PreferLocalOverAnonymousFunction => GetOption(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction);
    public CodeStyleOption2<bool> PreferTupleSwap => GetOption(CSharpCodeStyleOptions.PreferTupleSwap);
    public CodeStyleOption2<UnusedValuePreference> UnusedValueExpressionStatement => GetOption(CSharpCodeStyleOptions.UnusedValueExpressionStatement);
    public CodeStyleOption2<UnusedValuePreference> UnusedValueAssignment => GetOption(CSharpCodeStyleOptions.UnusedValueAssignment);
    public CodeStyleOption2<bool> PreferMethodGroupConversion => GetOption(CSharpCodeStyleOptions.PreferMethodGroupConversion);
    public CodeStyleOption2<bool> PreferPrimaryConstructors => GetOption(CSharpCodeStyleOptions.PreferPrimaryConstructors);
    public CodeStyleOption2<bool> PreferSystemThreadingLock => GetOption(CSharpCodeStyleOptions.PreferSystemThreadingLock);

    // CodeGenerationOptions

    internal CSharpCodeGenerationOptions GetCodeGenerationOptions()
        => new(options, fallbackOptions: null);

    public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLambdas => GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas);
    public CodeStyleOption2<bool> PreferReadOnlyStruct => GetOption(CSharpCodeStyleOptions.PreferReadOnlyStruct);
    public CodeStyleOption2<bool> PreferReadOnlyStructMember => GetOption(CSharpCodeStyleOptions.PreferReadOnlyStructMember);
    public CodeStyleOption2<bool> PreferStaticLocalFunction => GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction);
    public CodeStyleOption2<bool> PreferStaticAnonymousFunction => GetOption(CSharpCodeStyleOptions.PreferStaticAnonymousFunction);

    private TValue GetOption<TValue>(Option2<TValue> option)
        => options.GetOption(option);

    public static explicit operator CSharpAnalyzerOptionsProvider(AnalyzerOptionsProvider provider)
        => new(provider.GetAnalyzerConfigOptions());

    public static implicit operator AnalyzerOptionsProvider(CSharpAnalyzerOptionsProvider provider)
        => new(provider.Options, LanguageNames.CSharp);
}

internal static class CSharpAnalyzerOptionsProviders
{
    public static CSharpAnalyzerOptionsProvider GetCSharpAnalyzerOptions(this AnalyzerOptions options, SyntaxTree syntaxTree)
        => new(options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree).GetOptionsReader());

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
