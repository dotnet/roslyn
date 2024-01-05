// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    internal static class CSharpCodeCleanUpFixerDiagnosticIds
    {
        [Export]
        [FixId(IDEDiagnosticIds.UseImplicitTypeDiagnosticId)]
        [Name(IDEDiagnosticIds.UseImplicitTypeDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseImplicitTypeDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_var_preferences))]
        public static readonly FixIdDefinition? UseImplicitTypeDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExplicitTypeDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExplicitTypeDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseExplicitTypeDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_var_preferences))]
        public static readonly FixIdDefinition? UseExplicitTypeDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.AddBracesDiagnosticId)]
        [Name(IDEDiagnosticIds.AddBracesDiagnosticId)]
        [Order(After = AbstractCodeCleanUpFixer.SortImportsFixId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.AddBracesDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Add_required_braces_for_single_line_control_statements))]
        public static readonly FixIdDefinition? AddBracesDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition? UseExpressionBodyForConstructorsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition? UseExpressionBodyForMethodsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition? UseExpressionBodyForConversionOperatorsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition? UseExpressionBodyForOperatorsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition? UseExpressionBodyForPropertiesDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition? UseExpressionBodyForIndexersDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition? UseExpressionBodyForAccessorsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.InlineDeclarationDiagnosticId)]
        [Name(IDEDiagnosticIds.InlineDeclarationDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseImplicitTypeDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.InlineDeclarationDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_inline_out_variable_preferences))]
        public static readonly FixIdDefinition? InlineDeclarationDiagnosticId;

        [Export]
        [FixId(CSharpRemoveUnusedVariableCodeFixProvider.CS0168)]
        [Name(CSharpRemoveUnusedVariableCodeFixProvider.CS0168)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://learn.microsoft.com/dotnet/csharp/misc/cs0168")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Remove_unused_variables))]
        public static readonly FixIdDefinition? CS0168;

        [Export]
        [FixId(CSharpRemoveUnusedVariableCodeFixProvider.CS0219)]
        [Name(CSharpRemoveUnusedVariableCodeFixProvider.CS0219)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://learn.microsoft.com/dotnet/csharp/misc/cs0168")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Remove_unused_variables))]
        public static readonly FixIdDefinition? CS0219;

        [Export]
        [FixId(IDEDiagnosticIds.ConsecutiveBracePlacementDiagnosticId)]
        [Name(IDEDiagnosticIds.ConsecutiveBracePlacementDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")] // Experimental features, not documented
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_blank_lines_between_consecutive_braces_preferences_experimental))]
        public static readonly FixIdDefinition? ConsecutiveBracePlacementDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.ConstructorInitializerPlacementDiagnosticId)]
        [Name(IDEDiagnosticIds.ConstructorInitializerPlacementDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")] // Experimental features, not documented
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_blank_line_after_colon_in_constructor_initializer_preferences_experimental))]
        public static readonly FixIdDefinition? ConstructorInitializerPlacementDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId)]
        [Name(IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_pattern_matching_preferences))]
        public static readonly FixIdDefinition? ConvertSwitchStatementToExpressionDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.EmbeddedStatementPlacementDiagnosticId)]
        [Name(IDEDiagnosticIds.EmbeddedStatementPlacementDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")] // Experimental features, not documented
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_embedded_statements_on_same_line_preferences_experimental))]
        public static readonly FixIdDefinition? EmbeddedStatementPlacementDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.InlineAsTypeCheckId)]
        [Name(IDEDiagnosticIds.InlineAsTypeCheckId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.InlineAsTypeCheckId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_pattern_matching_preferences))]
        public static readonly FixIdDefinition? InlineAsTypeCheckId;

        [Export]
        [FixId(IDEDiagnosticIds.InlineIsTypeCheckId)]
        [Name(IDEDiagnosticIds.InlineIsTypeCheckId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.InlineIsTypeCheckId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_pattern_matching_preferences))]
        public static readonly FixIdDefinition? InlineIsTypeCheckId;

        [Export]
        [FixId(IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId)]
        [Name(IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_conditional_delegate_call_preferences))]
        public static readonly FixIdDefinition? InvokeDelegateWithConditionalAccessId;

        [Export]
        [FixId(IDEDiagnosticIds.MakeLocalFunctionStaticDiagnosticId)]
        [Name(IDEDiagnosticIds.MakeLocalFunctionStaticDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.MakeLocalFunctionStaticDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_static_local_function_preferences))]
        public static readonly FixIdDefinition? MakeLocalFunctionStaticDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.MakeStructReadOnlyDiagnosticId)]
        [Name(IDEDiagnosticIds.MakeStructReadOnlyDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.MakeStructReadOnlyDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_readonly_struct_preferences))]
        public static readonly FixIdDefinition? MakeStructReadOnlyDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.RemoveUnnecessaryLambdaExpressionDiagnosticId)]
        [Name(IDEDiagnosticIds.RemoveUnnecessaryLambdaExpressionDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.RemoveUnnecessaryLambdaExpressionDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_method_group_conversion_preferences))]
        public static readonly FixIdDefinition? RemoveUnnecessaryLambdaExpressionDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.SimplifyPropertyPatternDiagnosticId)]
        [Name(IDEDiagnosticIds.SimplifyPropertyPatternDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.SimplifyPropertyPatternDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_pattern_matching_preferences))]
        public static readonly FixIdDefinition? SimplifyPropertyPatternDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseDeconstructionDiagnosticId)]
        [Name(IDEDiagnosticIds.UseDeconstructionDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseDeconstructionDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_deconstruct_preferences))]
        public static readonly FixIdDefinition? UseDeconstructionDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseDefaultLiteralDiagnosticId)]
        [Name(IDEDiagnosticIds.UseDefaultLiteralDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseDefaultLiteralDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_default_T_preferences))]
        public static readonly FixIdDefinition? UseDefaultLiteralDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition? UseExpressionBodyForLambdaExpressionsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForLocalFunctionsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForLocalFunctionsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseExpressionBodyForLocalFunctionsDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition? UseExpressionBodyForLocalFunctionsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId)]
        [Name(IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_namespace_preferences))]
        public static readonly FixIdDefinition? UseFileScopedNamespaceDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseImplicitObjectCreationDiagnosticId)]
        [Name(IDEDiagnosticIds.UseImplicitObjectCreationDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseImplicitObjectCreationDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_new_preferences))]
        public static readonly FixIdDefinition? UseImplicitObjectCreationDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseIndexOperatorDiagnosticId)]
        [Name(IDEDiagnosticIds.UseIndexOperatorDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseIndexOperatorDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_range_preferences))]
        public static readonly FixIdDefinition? UseIndexOperatorDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseLocalFunctionDiagnosticId)]
        [Name(IDEDiagnosticIds.UseLocalFunctionDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseLocalFunctionDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_local_over_anonymous_function_preferences))]
        public static readonly FixIdDefinition? UseLocalFunctionDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseNotPatternDiagnosticId)]
        [Name(IDEDiagnosticIds.UseNotPatternDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseNotPatternDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_pattern_matching_preferences))]
        public static readonly FixIdDefinition? UseNotPatternDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseNullCheckOverTypeCheckDiagnosticId)]
        [Name(IDEDiagnosticIds.UseNullCheckOverTypeCheckDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseNullCheckOverTypeCheckDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_pattern_matching_preferences))]
        public static readonly FixIdDefinition? UseNullCheckOverTypeCheckDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UsePatternCombinatorsDiagnosticId)]
        [Name(IDEDiagnosticIds.UsePatternCombinatorsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UsePatternCombinatorsDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_pattern_matching_preferences))]
        public static readonly FixIdDefinition? UsePatternCombinatorsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseRangeOperatorDiagnosticId)]
        [Name(IDEDiagnosticIds.UseRangeOperatorDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseRangeOperatorDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_range_preferences))]
        public static readonly FixIdDefinition? UseRangeOperatorDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId)]
        [Name(IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_using_statement_preferences))]
        public static readonly FixIdDefinition? UseSimpleUsingStatementDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseThrowExpressionDiagnosticId)]
        [Name(IDEDiagnosticIds.UseThrowExpressionDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseThrowExpressionDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_throw_expression_preferences))]
        public static readonly FixIdDefinition? UseThrowExpressionDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseTupleSwapDiagnosticId)]
        [Name(IDEDiagnosticIds.UseTupleSwapDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseTupleSwapDiagnosticId}")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_deconstruct_preferences))]
        public static readonly FixIdDefinition? UseTupleSwapDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId)]
        [Name(IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId}")]
        [LocalizedName(typeof(CSharpAnalyzersResources), nameof(CSharpAnalyzersResources.Remove_unnecessary_nullable_directive))]
        public static readonly FixIdDefinition? RemoveRedundantNullableDirectiveDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.RemoveUnnecessaryNullableDirectiveDiagnosticId)]
        [Name(IDEDiagnosticIds.RemoveUnnecessaryNullableDirectiveDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.RemoveUnnecessaryNullableDirectiveDiagnosticId}")]
        [LocalizedName(typeof(CSharpAnalyzersResources), nameof(CSharpAnalyzersResources.Remove_unnecessary_nullable_directive))]
        public static readonly FixIdDefinition? RemoveUnnecessaryNullableDirectiveDiagnosticId;
    }
}
