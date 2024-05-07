// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
{
    internal static class CommonCodeCleanUpFixerDiagnosticIds
    {
        [Export]
        [FixId(IDEDiagnosticIds.AddThisOrMeQualificationDiagnosticId)]
        [Name(IDEDiagnosticIds.AddThisOrMeQualificationDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.AddThisOrMeQualificationDiagnosticId}")]
        [LocalizedName(typeof(AnalyzersResources), nameof(AnalyzersResources.Add_this_or_Me_qualification))]
        public static readonly FixIdDefinition? AddQualificationDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.RemoveThisOrMeQualificationDiagnosticId)]
        [Name(IDEDiagnosticIds.RemoveThisOrMeQualificationDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.RemoveThisOrMeQualificationDiagnosticId}")]
        [LocalizedName(typeof(AnalyzersResources), nameof(AnalyzersResources.Add_this_or_Me_qualification))]
        public static readonly FixIdDefinition? RemoveQualificationDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)]
        [Name(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)]
        [Order(After = IDEDiagnosticIds.AddBracesDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId}")]
        [LocalizedName(typeof(AnalyzersResources), nameof(AnalyzersResources.Add_accessibility_modifiers))]
        public static readonly FixIdDefinition? AddAccessibilityModifiersDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [Name(IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [Order(After = IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.OrderModifiersDiagnosticId}")]
        [LocalizedName(typeof(AnalyzersResources), nameof(AnalyzersResources.Order_modifiers))]
        public static readonly FixIdDefinition? OrderModifiersDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)]
        [Name(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)]
        [Order(After = IDEDiagnosticIds.AddThisOrMeQualificationDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId}")]
        [LocalizedName(typeof(AnalyzersResources), nameof(AnalyzersResources.Make_field_readonly))]
        public static readonly FixIdDefinition? MakeFieldReadonlyDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [Name(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [Order(After = IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Remove_unnecessary_casts))]
        public static readonly FixIdDefinition? RemoveUnnecessaryCastDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [Name(IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [Order(After = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseObjectInitializerDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_object_collection_initialization_preferences))]
        public static readonly FixIdDefinition? UseObjectInitializerDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId)]
        [Name(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId)]
        [Order(After = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseCollectionInitializerDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_object_collection_initialization_preferences))]
        public static readonly FixIdDefinition? UseCollectionInitializerDiagnosticId;

        [Export]
        [FixId(AbstractCodeCleanUpFixer.FormatDocumentFixId)]
        [Name(AbstractCodeCleanUpFixer.FormatDocumentFixId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://learn.microsoft.com/visualstudio/ide/reference/options-text-editor-csharp-formatting")]
        [ExportMetadata("EnableByDefault", true)]
        [LocalizedName(typeof(ServicesVSResources), nameof(ServicesVSResources.Format_document))]
        public static readonly FixIdDefinition? FormatDocument;

        [Export]
        [FixId(AbstractCodeCleanUpFixer.RemoveUnusedImportsFixId)]
        [Name(AbstractCodeCleanUpFixer.RemoveUnusedImportsFixId)]
        [Order(After = AbstractCodeCleanUpFixer.FormatDocumentFixId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://learn.microsoft.com/visualstudio/ide/reference/options-text-editor-csharp-advanced#using-directives")]
        [ExportMetadata("EnableByDefault", true)]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Remove_unnecessary_imports_or_usings))]
        public static readonly FixIdDefinition? RemoveUnusedImports;

        [Export]
        [FixId(AbstractCodeCleanUpFixer.SortImportsFixId)]
        [Name(AbstractCodeCleanUpFixer.SortImportsFixId)]
        [Order(After = AbstractCodeCleanUpFixer.RemoveUnusedImportsFixId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://learn.microsoft.com/visualstudio/ide/reference/options-text-editor-csharp-advanced#using-directives")]
        [ExportMetadata("EnableByDefault", true)]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Sort_Imports_or_usings))]
        public static readonly FixIdDefinition? SortImports;

        [Export]
        [FixId(IDEDiagnosticIds.FileHeaderMismatch)]
        [Name(IDEDiagnosticIds.FileHeaderMismatch)]
        [Order(After = AbstractCodeCleanUpFixer.SortImportsFixId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.FileHeaderMismatch}")]
        [ExportMetadata("EnableByDefault", true)]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_file_header_preferences))]
        public static readonly FixIdDefinition? FileHeaderMismatch;

        [Export]
        [FixId(IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId)]
        [Name(IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId)]
        [Order(After = AbstractCodeCleanUpFixer.SortImportsFixId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_using_directive_placement_preferences))]
        public static readonly FixIdDefinition? MoveMisplacedUsingDirectivesDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId)]
        [Name(IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_parentheses_preferences))]
        public static readonly FixIdDefinition? AddRequiredParenthesesDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.ConsecutiveStatementPlacementDiagnosticId)]
        [Name(IDEDiagnosticIds.ConsecutiveStatementPlacementDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")] // Experimental features, not documented
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_statement_after_block_preferences_experimental))]
        public static readonly FixIdDefinition? ConsecutiveStatementPlacementDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId)]
        [Name(IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_unused_value_preferences))]
        public static readonly FixIdDefinition? ExpressionValueIsUnusedDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.MatchFolderAndNamespaceDiagnosticId)]
        [Name(IDEDiagnosticIds.MatchFolderAndNamespaceDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")] // Features not documented tracked by https://github.com/dotnet/roslyn/issues/59103
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_namespace_matches_folder_preferences))]
        public static readonly FixIdDefinition? MatchFolderAndNamespaceDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.MultipleBlankLinesDiagnosticId)]
        [Name(IDEDiagnosticIds.MultipleBlankLinesDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")] // Experimental features, not documented
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_blank_line_preferences_experimental))]
        public static readonly FixIdDefinition? MultipleBlankLinesDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)]
        [Name(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_language_framework_type_preferences))]
        public static readonly FixIdDefinition? PreferBuiltInOrFrameworkTypeDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId)]
        [Name(IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_parentheses_preferences))]
        public static readonly FixIdDefinition? RemoveUnnecessaryParenthesesDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId)]
        [Name(IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Remove_unused_suppressions))]
        public static readonly FixIdDefinition? RemoveUnnecessarySuppressionDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId)]
        [Name(IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_simplify_boolean_expression_preferences))]
        public static readonly FixIdDefinition? SimplifyConditionalExpressionDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.SimplifyInterpolationId)]
        [Name(IDEDiagnosticIds.SimplifyInterpolationId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.SimplifyInterpolationId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_string_interpolation_preferences))]
        public static readonly FixIdDefinition? SimplifyInterpolationId;

        [Export]
        [FixId(IDEDiagnosticIds.UnusedParameterDiagnosticId)]
        [Name(IDEDiagnosticIds.UnusedParameterDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UnusedParameterDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Remove_unused_parameters))]
        public static readonly FixIdDefinition? UnusedParameterDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseAutoPropertyDiagnosticId)]
        [Name(IDEDiagnosticIds.UseAutoPropertyDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseAutoPropertyDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_auto_property_preferences))]
        public static readonly FixIdDefinition? UseAutoPropertyDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseCoalesceCompoundAssignmentDiagnosticId)]
        [Name(IDEDiagnosticIds.UseCoalesceCompoundAssignmentDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseCoalesceCompoundAssignmentDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_compound_assignment_preferences))]
        public static readonly FixIdDefinition? UseCoalesceCompoundAssignmentDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseCoalesceExpressionForTernaryConditionalCheckDiagnosticId)]
        [Name(IDEDiagnosticIds.UseCoalesceExpressionForTernaryConditionalCheckDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseCoalesceExpressionForTernaryConditionalCheckDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_coalesce_expression_preferences))]
        public static readonly FixIdDefinition? UseCoalesceExpressionForTernaryConditionalCheckDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId)]
        [Name(IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_compound_assignment_preferences))]
        public static readonly FixIdDefinition? UseCompoundAssignmentDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId)]
        [Name(IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_conditional_expression_preferences))]
        public static readonly FixIdDefinition? UseConditionalExpressionForAssignmentDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId)]
        [Name(IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_conditional_expression_preferences))]
        public static readonly FixIdDefinition? UseConditionalExpressionForReturnDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExplicitTupleNameDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExplicitTupleNameDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseExplicitTupleNameDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_tuple_name_preferences))]
        public static readonly FixIdDefinition? UseExplicitTupleNameDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseInferredMemberNameDiagnosticId)]
        [Name(IDEDiagnosticIds.UseInferredMemberNameDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseInferredMemberNameDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_inferred_anonymous_type_member_names_preferences))]
        public static readonly FixIdDefinition? UseInferredMemberNameDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseIsNullCheckDiagnosticId)]
        [Name(IDEDiagnosticIds.UseIsNullCheckDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseIsNullCheckDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_null_checking_preferences))]
        public static readonly FixIdDefinition? UseIsNullCheckDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseNullPropagationDiagnosticId)]
        [Name(IDEDiagnosticIds.UseNullPropagationDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.UseNullPropagationDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_null_propagation_preferences))]
        public static readonly FixIdDefinition? UseNullPropagationDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId)]
        [Name(IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId}")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_unused_value_preferences))]
        public static readonly FixIdDefinition? ValueAssignedIsUnusedDiagnosticId;

        [Export]
        [FixId(AbstractCodeCleanUpFixer.ApplyThirdPartyFixersId)]
        [Name(AbstractCodeCleanUpFixer.ApplyThirdPartyFixersId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://microsoft.com/")]
        [LocalizedName(typeof(ServicesVSResources), nameof(ServicesVSResources.Fix_analyzer_warnings_and_errors_set_in_EditorConfig))]
        public static readonly FixIdDefinition? ThirdPartyAnalyzers;

        [Export]
        [FixId(AbstractCodeCleanUpFixer.ApplyAllAnalyzerFixersId)]
        [Name(AbstractCodeCleanUpFixer.ApplyAllAnalyzerFixersId)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink($"https://microsoft.com/")]
        [LocalizedName(typeof(ServicesVSResources), nameof(ServicesVSResources.Fix_all_warnings_and_errors_set_in_EditorConfig))]
        public static readonly FixIdDefinition? AllAnalyzers;
    }
}
