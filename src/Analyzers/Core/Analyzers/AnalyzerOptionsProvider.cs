// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.CodeStyle;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Provides C# and VB analyzers a convenient access to common editorconfig options.
/// </summary>
internal readonly struct AnalyzerOptionsProvider(IOptionsReader options, string language)
{
    // SimplifierOptions

    public CodeStyleOption2<bool> QualifyFieldAccess => GetOption(CodeStyleOptions2.QualifyFieldAccess);
    public CodeStyleOption2<bool> QualifyPropertyAccess => GetOption(CodeStyleOptions2.QualifyPropertyAccess);
    public CodeStyleOption2<bool> QualifyMethodAccess => GetOption(CodeStyleOptions2.QualifyMethodAccess);
    public CodeStyleOption2<bool> QualifyEventAccess => GetOption(CodeStyleOptions2.QualifyEventAccess);
    public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInMemberAccess => GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess);
    public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInDeclaration => GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration);

    public SimplifierOptions GetSimplifierOptions(ISimplification simplification)
        => simplification.GetSimplifierOptions(options);

    // SyntaxFormattingOptions

    public SyntaxFormattingOptions GetSyntaxFormattingOptions(ISyntaxFormatting formatting)
        => formatting.GetFormattingOptions(options);

    // CodeGenerationOptions

    public NamingStylePreferences NamingPreferences => GetOption(NamingStyleOptions.NamingPreferences);

    // CodeStyleOptions

    public CodeStyleOption2<bool> PreferObjectInitializer => GetOption(CodeStyleOptions2.PreferObjectInitializer);
    public CodeStyleOption2<CollectionExpressionPreference> PreferCollectionExpression => GetOption(CodeStyleOptions2.PreferCollectionExpression);
    public CodeStyleOption2<bool> PreferCollectionInitializer => GetOption(CodeStyleOptions2.PreferCollectionInitializer);
    public CodeStyleOption2<bool> PreferSimplifiedBooleanExpressions => GetOption(CodeStyleOptions2.PreferSimplifiedBooleanExpressions);
    public OperatorPlacementWhenWrappingPreference OperatorPlacementWhenWrapping => GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping);
    public CodeStyleOption2<bool> PreferCoalesceExpression => GetOption(CodeStyleOptions2.PreferCoalesceExpression);
    public CodeStyleOption2<bool> PreferNullPropagation => GetOption(CodeStyleOptions2.PreferNullPropagation);
    public CodeStyleOption2<bool> PreferExplicitTupleNames => GetOption(CodeStyleOptions2.PreferExplicitTupleNames);
    public CodeStyleOption2<bool> PreferAutoProperties => GetOption(CodeStyleOptions2.PreferAutoProperties);
    public CodeStyleOption2<bool> PreferInferredTupleNames => GetOption(CodeStyleOptions2.PreferInferredTupleNames);
    public CodeStyleOption2<bool> PreferInferredAnonymousTypeMemberNames => GetOption(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames);
    public CodeStyleOption2<bool> PreferIsNullCheckOverReferenceEqualityMethod => GetOption(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod);
    public CodeStyleOption2<bool> PreferConditionalExpressionOverAssignment => GetOption(CodeStyleOptions2.PreferConditionalExpressionOverAssignment);
    public CodeStyleOption2<bool> PreferConditionalExpressionOverReturn => GetOption(CodeStyleOptions2.PreferConditionalExpressionOverReturn);
    public CodeStyleOption2<bool> PreferCompoundAssignment => GetOption(CodeStyleOptions2.PreferCompoundAssignment);
    public CodeStyleOption2<bool> PreferSimplifiedInterpolation => GetOption(CodeStyleOptions2.PreferSimplifiedInterpolation);
    public CodeStyleOption2<bool> PreferSystemHashCode => GetOption(CodeStyleOptions2.PreferSystemHashCode);
    public CodeStyleOption2<UnusedParametersPreference> UnusedParameters => GetOption(CodeStyleOptions2.UnusedParameters);
    public CodeStyleOption2<AccessibilityModifiersRequired> RequireAccessibilityModifiers => GetOption(CodeStyleOptions2.AccessibilityModifiersRequired);
    public CodeStyleOption2<bool> PreferReadonly => GetOption(CodeStyleOptions2.PreferReadonly);
    public CodeStyleOption2<ParenthesesPreference> ArithmeticBinaryParentheses => GetOption(CodeStyleOptions2.ArithmeticBinaryParentheses);
    public CodeStyleOption2<ParenthesesPreference> OtherBinaryParentheses => GetOption(CodeStyleOptions2.OtherBinaryParentheses);
    public CodeStyleOption2<ParenthesesPreference> RelationalBinaryParentheses => GetOption(CodeStyleOptions2.RelationalBinaryParentheses);
    public CodeStyleOption2<ParenthesesPreference> OtherParentheses => GetOption(CodeStyleOptions2.OtherParentheses);
    public CodeStyleOption2<ForEachExplicitCastInSourcePreference> ForEachExplicitCastInSource => GetOption(CodeStyleOptions2.ForEachExplicitCastInSource);
    public CodeStyleOption2<bool> PreferNamespaceAndFolderMatchStructure => GetOption(CodeStyleOptions2.PreferNamespaceAndFolderMatchStructure);
    public CodeStyleOption2<bool> AllowMultipleBlankLines => GetOption(CodeStyleOptions2.AllowMultipleBlankLines);
    public CodeStyleOption2<bool> AllowStatementImmediatelyAfterBlock => GetOption(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock);
    public string RemoveUnnecessarySuppressionExclusions => GetOption(CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions);

    public string FileHeaderTemplate => GetOption(CodeStyleOptions2.FileHeaderTemplate);

    public TValue GetOption<TValue>(PerLanguageOption2<TValue> option)
        => options.GetOption(option, language);

    private TValue GetOption<TValue>(Option2<TValue> option)
        => options.GetOption(option);

    internal IOptionsReader GetAnalyzerConfigOptions()
        => options;
}

internal static partial class AnalyzerOptionsProviders
{
    public static IdeAnalyzerOptions GetIdeOptions(this AnalyzerOptions options)
#if CODE_STYLE
        => IdeAnalyzerOptions.CommonDefault;
#else
        => (options is WorkspaceAnalyzerOptions workspaceOptions) ? workspaceOptions.IdeOptions : IdeAnalyzerOptions.CommonDefault;
#endif

    public static AnalyzerOptionsProvider GetAnalyzerOptions(this AnalyzerOptions analyzerOptions, SyntaxTree syntaxTree)
        => new(analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree).GetOptionsReader(), syntaxTree.Options.Language);

    public static AnalyzerOptionsProvider GetAnalyzerOptions(this SemanticModelAnalysisContext context)
        => GetAnalyzerOptions(context.Options, context.SemanticModel.SyntaxTree);

    public static AnalyzerOptionsProvider GetAnalyzerOptions(this SyntaxNodeAnalysisContext context)
        => GetAnalyzerOptions(context.Options, context.Node.SyntaxTree);

    public static AnalyzerOptionsProvider GetAnalyzerOptions(this SyntaxTreeAnalysisContext context)
        => GetAnalyzerOptions(context.Options, context.Tree);

    public static AnalyzerOptionsProvider GetAnalyzerOptions(this OperationAnalysisContext context)
        => GetAnalyzerOptions(context.Options, context.Operation.Syntax.SyntaxTree);

    public static AnalyzerOptionsProvider GetAnalyzerOptions(this CodeBlockAnalysisContext context)
        => GetAnalyzerOptions(context.Options, context.CodeBlock.SyntaxTree);

    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this SemanticModelAnalysisContext context)
        => context.Options.GetIdeOptions();

    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this SyntaxNodeAnalysisContext context)
        => context.Options.GetIdeOptions();
}
