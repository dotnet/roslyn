// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Provides C# and VB analyzers a convenient access to common editorconfig options with fallback to IDE default values.
/// </summary>
internal readonly struct AnalyzerOptionsProvider(IOptionsReader options, string language, IdeAnalyzerOptions fallbackOptions)
{
    /// <summary>
    /// Document editorconfig options.
    /// </summary>
    private readonly IOptionsReader _options = options;

    /// <summary>
    /// Fallback options - the default options in Code Style layer.
    /// </summary>
    private readonly IdeAnalyzerOptions _fallbackOptions = fallbackOptions;

    private readonly string _language = language;

    public AnalyzerOptionsProvider(IOptionsReader options, string language, AnalyzerOptions fallbackOptions)
        : this(options, language, fallbackOptions.GetIdeOptions())
    {
    }

    // SimplifierOptions

    public CodeStyleOption2<bool> QualifyFieldAccess => GetOption(CodeStyleOptions2.QualifyFieldAccess, FallbackSimplifierOptions.QualifyFieldAccess);
    public CodeStyleOption2<bool> QualifyPropertyAccess => GetOption(CodeStyleOptions2.QualifyPropertyAccess, FallbackSimplifierOptions.QualifyPropertyAccess);
    public CodeStyleOption2<bool> QualifyMethodAccess => GetOption(CodeStyleOptions2.QualifyMethodAccess, FallbackSimplifierOptions.QualifyMethodAccess);
    public CodeStyleOption2<bool> QualifyEventAccess => GetOption(CodeStyleOptions2.QualifyEventAccess, FallbackSimplifierOptions.QualifyEventAccess);
    public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInMemberAccess => GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, FallbackSimplifierOptions.PreferPredefinedTypeKeywordInMemberAccess);
    public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInDeclaration => GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, FallbackSimplifierOptions.PreferPredefinedTypeKeywordInDeclaration);

    public SimplifierOptions GetSimplifierOptions(ISimplification simplification)
        => simplification.GetSimplifierOptions(_options, _fallbackOptions.CleanupOptions?.SimplifierOptions);

    // SyntaxFormattingOptions

    public SyntaxFormattingOptions GetSyntaxFormattingOptions(ISyntaxFormatting formatting)
        => formatting.GetFormattingOptions(_options, _fallbackOptions.CleanupOptions?.FormattingOptions);

    // CodeGenerationOptions

    public NamingStylePreferences NamingPreferences => GetOption(NamingStyleOptions.NamingPreferences, _fallbackOptions.GenerationOptions?.NamingStyle ?? NamingStylePreferences.Default);

    // CodeStyleOptions

    public CodeStyleOption2<bool> PreferObjectInitializer => GetOption(CodeStyleOptions2.PreferObjectInitializer, FallbackCodeStyleOptions.PreferObjectInitializer);
    public CodeStyleOption2<bool> PreferCollectionInitializer => GetOption(CodeStyleOptions2.PreferCollectionInitializer, FallbackCodeStyleOptions.PreferCollectionInitializer);
    public CodeStyleOption2<bool> PreferSimplifiedBooleanExpressions => GetOption(CodeStyleOptions2.PreferSimplifiedBooleanExpressions, FallbackCodeStyleOptions.PreferSimplifiedBooleanExpressions);
    public OperatorPlacementWhenWrappingPreference OperatorPlacementWhenWrapping => GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping, FallbackCodeStyleOptions.OperatorPlacementWhenWrapping);
    public CodeStyleOption2<bool> PreferCoalesceExpression => GetOption(CodeStyleOptions2.PreferCoalesceExpression, FallbackCodeStyleOptions.PreferCoalesceExpression);
    public CodeStyleOption2<bool> PreferNullPropagation => GetOption(CodeStyleOptions2.PreferNullPropagation, FallbackCodeStyleOptions.PreferNullPropagation);
    public CodeStyleOption2<bool> PreferExplicitTupleNames => GetOption(CodeStyleOptions2.PreferExplicitTupleNames, FallbackCodeStyleOptions.PreferExplicitTupleNames);
    public CodeStyleOption2<bool> PreferAutoProperties => GetOption(CodeStyleOptions2.PreferAutoProperties, FallbackCodeStyleOptions.PreferAutoProperties);
    public CodeStyleOption2<bool> PreferInferredTupleNames => GetOption(CodeStyleOptions2.PreferInferredTupleNames, FallbackCodeStyleOptions.PreferInferredTupleNames);
    public CodeStyleOption2<bool> PreferInferredAnonymousTypeMemberNames => GetOption(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, FallbackCodeStyleOptions.PreferInferredAnonymousTypeMemberNames);
    public CodeStyleOption2<bool> PreferIsNullCheckOverReferenceEqualityMethod => GetOption(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod, FallbackCodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod);
    public CodeStyleOption2<bool> PreferConditionalExpressionOverAssignment => GetOption(CodeStyleOptions2.PreferConditionalExpressionOverAssignment, FallbackCodeStyleOptions.PreferConditionalExpressionOverAssignment);
    public CodeStyleOption2<bool> PreferConditionalExpressionOverReturn => GetOption(CodeStyleOptions2.PreferConditionalExpressionOverReturn, FallbackCodeStyleOptions.PreferConditionalExpressionOverReturn);
    public CodeStyleOption2<bool> PreferCompoundAssignment => GetOption(CodeStyleOptions2.PreferCompoundAssignment, FallbackCodeStyleOptions.PreferCompoundAssignment);
    public CodeStyleOption2<bool> PreferSimplifiedInterpolation => GetOption(CodeStyleOptions2.PreferSimplifiedInterpolation, FallbackCodeStyleOptions.PreferSimplifiedInterpolation);
    public CodeStyleOption2<UnusedParametersPreference> UnusedParameters => GetOption(CodeStyleOptions2.UnusedParameters, FallbackCodeStyleOptions.UnusedParameters);
    public CodeStyleOption2<AccessibilityModifiersRequired> RequireAccessibilityModifiers => GetOption(CodeStyleOptions2.AccessibilityModifiersRequired, FallbackCodeStyleOptions.AccessibilityModifiersRequired);
    public CodeStyleOption2<bool> PreferReadonly => GetOption(CodeStyleOptions2.PreferReadonly, FallbackCodeStyleOptions.PreferReadonly);
    public CodeStyleOption2<ParenthesesPreference> ArithmeticBinaryParentheses => GetOption(CodeStyleOptions2.ArithmeticBinaryParentheses, FallbackCodeStyleOptions.ArithmeticBinaryParentheses);
    public CodeStyleOption2<ParenthesesPreference> OtherBinaryParentheses => GetOption(CodeStyleOptions2.OtherBinaryParentheses, FallbackCodeStyleOptions.OtherBinaryParentheses);
    public CodeStyleOption2<ParenthesesPreference> RelationalBinaryParentheses => GetOption(CodeStyleOptions2.RelationalBinaryParentheses, FallbackCodeStyleOptions.RelationalBinaryParentheses);
    public CodeStyleOption2<ParenthesesPreference> OtherParentheses => GetOption(CodeStyleOptions2.OtherParentheses, FallbackCodeStyleOptions.OtherParentheses);
    public CodeStyleOption2<ForEachExplicitCastInSourcePreference> ForEachExplicitCastInSource => GetOption(CodeStyleOptions2.ForEachExplicitCastInSource, FallbackCodeStyleOptions.ForEachExplicitCastInSource);
    public CodeStyleOption2<bool> PreferNamespaceAndFolderMatchStructure => GetOption(CodeStyleOptions2.PreferNamespaceAndFolderMatchStructure, FallbackCodeStyleOptions.PreferNamespaceAndFolderMatchStructure);
    public CodeStyleOption2<bool> AllowMultipleBlankLines => GetOption(CodeStyleOptions2.AllowMultipleBlankLines, FallbackCodeStyleOptions.AllowMultipleBlankLines);
    public CodeStyleOption2<bool> AllowStatementImmediatelyAfterBlock => GetOption(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, FallbackCodeStyleOptions.AllowStatementImmediatelyAfterBlock);
    public string RemoveUnnecessarySuppressionExclusions => GetOption(CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions, FallbackCodeStyleOptions.RemoveUnnecessarySuppressionExclusions);

    public string FileHeaderTemplate => GetOption(CodeStyleOptions2.FileHeaderTemplate, defaultValue: string.Empty); // no fallback IDE option

    private TValue GetOption<TValue>(Option2<TValue> option, TValue defaultValue)
        => _options.GetOption(option, defaultValue);

    private TValue GetOption<TValue>(PerLanguageOption2<TValue> option, TValue defaultValue)
        => _options.GetOption(option, _language, defaultValue);

    private IdeCodeStyleOptions FallbackCodeStyleOptions
        => _fallbackOptions.CodeStyleOptions ?? IdeCodeStyleOptions.CommonDefaults;

    private SimplifierOptions FallbackSimplifierOptions
        => _fallbackOptions.CleanupOptions?.SimplifierOptions ?? SimplifierOptions.CommonDefaults;

    internal IOptionsReader GetAnalyzerConfigOptions()
        => _options;

    internal IdeAnalyzerOptions GetFallbackOptions()
        => _fallbackOptions;
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
        => new(analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree).GetOptionsReader(), syntaxTree.Options.Language, analyzerOptions);

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

    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this SyntaxTreeAnalysisContext context)
        => context.Options.GetIdeOptions();

    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this OperationAnalysisContext context)
        => context.Options.GetIdeOptions();

    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this CodeBlockAnalysisContext context)
        => context.Options.GetIdeOptions();
}
