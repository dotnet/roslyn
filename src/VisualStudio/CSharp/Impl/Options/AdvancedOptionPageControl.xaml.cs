// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImportOnPaste;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.ColorSchemes;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using Microsoft.CodeAnalysis.Editor.Implementation.SplitComment;
using Microsoft.CodeAnalysis.Editor.InlineDiagnostics;
using Microsoft.CodeAnalysis.Editor.InlineHints;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.KeywordHighlighting;
using Microsoft.CodeAnalysis.LineSeparators;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.CodeAnalysis.StringCopyPaste;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.DocumentOutline;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal partial class AdvancedOptionPageControl : AbstractOptionPageControl
    {
        private readonly IThreadingContext _threadingContext;
        private readonly ColorSchemeApplier _colorSchemeApplier;

        public AdvancedOptionPageControl(OptionStore optionStore, IComponentModel componentModel) : base(optionStore)
        {
            _threadingContext = componentModel.GetService<IThreadingContext>();
            _colorSchemeApplier = componentModel.GetService<ColorSchemeApplier>();

            InitializeComponent();

            // Analysis
            BindToOption(Run_background_code_analysis_for, SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, label: Run_background_code_analysis_for_label);
            BindToOption(Show_compiler_errors_and_warnings_for, SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, LanguageNames.CSharp, label: Show_compiler_errors_and_warnings_for_label);
            BindToOption(DisplayDiagnosticsInline, InlineDiagnosticsOptionsStorage.EnableInlineDiagnostics, LanguageNames.CSharp);
            BindToOption(at_the_end_of_the_line_of_code, InlineDiagnosticsOptionsStorage.Location, InlineDiagnosticsLocations.PlacedAtEndOfCode, LanguageNames.CSharp);
            BindToOption(on_the_right_edge_of_the_editor_window, InlineDiagnosticsOptionsStorage.Location, InlineDiagnosticsLocations.PlacedAtEndOfEditor, LanguageNames.CSharp);

            BindToOption(Run_code_analysis_in_separate_process, RemoteHostOptionsStorage.OOP64Bit);

            BindToOption(Enable_file_logging_for_diagnostics, VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics);
            BindToOption(Skip_analyzers_for_implicitly_triggered_builds, FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds);
            BindToOption(Show_Remove_Unused_References_command_in_Solution_Explorer_experimental, FeatureOnOffOptions.OfferRemoveUnusedReferences, () =>
            {
                // If the option has not been set by the user, check if the option to remove unused references
                // is enabled from experimentation. If so, default to that.
                return optionStore.GetOption(FeatureOnOffOptions.OfferRemoveUnusedReferencesFeatureFlag);
            });

            // Source Generators
            BindToOption(Automatic_Run_generators_after_any_change, WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution, SourceGeneratorExecutionPreference.Automatic, () =>
            {
                // If the option hasn't been set by the user, then check the feature flag.  If the feature flag has set
                // us to only run when builds complete, then we're not in automatic mode.  So we `!` the result.
                return !optionStore.GetOption(WorkspaceConfigurationOptionsStorage.SourceGeneratorExecutionBalancedFeatureFlag);
            });
            BindToOption(Balanced_Run_generators_after_saving_or_building, WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution, SourceGeneratorExecutionPreference.Balanced, () =>
            {
                // If the option hasn't been set by the user, then check the feature flag.  If the feature flag has set
                // us to only run when builds complete, then we're in `Balanced_Run_generators_after_saving_or_building` mode and directly return it.
                return optionStore.GetOption(WorkspaceConfigurationOptionsStorage.SourceGeneratorExecutionBalancedFeatureFlag);
            });
            BindToOption(Analyze_source_generated_files, SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFiles, () =>
            {
                // If the option has not been set by the user, check if the option is enabled from experimentation. If so, default to that.
                return optionStore.GetOption(SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFilesFeatureFlag);
            });

            // Go To Definition
            BindToOption(Enable_navigation_to_sourcelink_and_embedded_sources, MetadataAsSourceOptionsStorage.NavigateToSourceLinkAndEmbeddedSources);
            BindToOption(Enable_navigation_to_decompiled_sources, MetadataAsSourceOptionsStorage.NavigateToDecompiledSources);
            BindToOption(Always_use_default_symbol_servers_for_navigation, MetadataAsSourceOptionsStorage.AlwaysUseDefaultSymbolServers);

            // Rename
            BindToOption(Rename_asynchronously_exerimental, InlineRenameSessionOptionsStorage.RenameAsynchronously);
            BindToOption(Rename_UI_setting, InlineRenameUIOptionsStorage.UseInlineAdornment, label: Rename_UI_setting_label);

            // Using Directives
            BindToOption(PlaceSystemNamespaceFirst, GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp);
            BindToOption(SeparateImportGroups, GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInReferenceAssemblies, SymbolSearchOptionsStorage.SearchReferenceAssemblies, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInNuGetPackages, SymbolSearchOptionsStorage.SearchNuGetPackages, LanguageNames.CSharp);
            BindToOption(AddUsingsOnPaste, AddImportOnPasteOptionsStorage.AddImportsOnPaste, LanguageNames.CSharp);

            // Highlighting
            BindToOption(EnableHighlightReferences, ReferenceHighlightingOptionsStorage.ReferenceHighlighting, LanguageNames.CSharp);
            BindToOption(EnableHighlightKeywords, KeywordHighlightingOptionsStorage.KeywordHighlighting, LanguageNames.CSharp);

            // Outlining
            BindToOption(EnterOutliningMode, OutliningOptionsStorage.Outlining, LanguageNames.CSharp);
            BindToOption(Collapse_regions_on_file_open, BlockStructureOptionsStorage.CollapseRegionsWhenFirstOpened, LanguageNames.CSharp);
            BindToOption(Collapse_usings_on_file_open, BlockStructureOptionsStorage.CollapseImportsWhenFirstOpened, LanguageNames.CSharp);
            BindToOption(Collapse_sourcelink_embedded_decompiled_files_on_open, BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, LanguageNames.CSharp);
            BindToOption(Collapse_metadata_signature_files_on_open, BlockStructureOptionsStorage.CollapseMetadataSignatureFilesWhenFirstOpened, LanguageNames.CSharp);
            BindToOption(DisplayLineSeparators, LineSeparatorsOptionsStorage.LineSeparator, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_declaration_level_constructs, BlockStructureOptionsStorage.ShowOutliningForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_code_level_constructs, BlockStructureOptionsStorage.ShowOutliningForCodeLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_comments_and_preprocessor_regions, BlockStructureOptionsStorage.ShowOutliningForCommentsAndPreprocessorRegions, LanguageNames.CSharp);
            BindToOption(Collapse_regions_when_collapsing_to_definitions, BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp);
            BindToOption(Collapse_local_functions_when_collapsing_to_definitions, BlockStructureOptionsStorage.CollapseLocalFunctionsWhenCollapsingToDefinitions, LanguageNames.CSharp);

            // Fading
            BindToOption(Fade_out_unused_usings, FadingOptions.FadeOutUnusedImports, LanguageNames.CSharp);
            BindToOption(Fade_out_unreachable_code, FadingOptions.FadeOutUnreachableCode, LanguageNames.CSharp);

            // Block Structure Guides
            BindToOption(Show_guides_for_declaration_level_constructs, BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_guides_for_code_level_constructs, BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_guides_for_comments_and_preprocessor_regions, BlockStructureOptionsStorage.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, LanguageNames.CSharp);

            // Comments
            BindToOption(GenerateXmlDocCommentsForTripleSlash, DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, LanguageNames.CSharp);
            BindToOption(InsertSlashSlashAtTheStartOfNewLinesWhenWritingSingleLineComments, SplitCommentOptionsStorage.Enabled, LanguageNames.CSharp);
            BindToOption(InsertAsteriskAtTheStartOfNewLinesWhenWritingBlockComments, BlockCommentEditingOptionsStorage.AutoInsertBlockCommentStartString, LanguageNames.CSharp);

            // Editor Help
            BindToOption(ShowRemarksInQuickInfo, QuickInfoOptionsStorage.ShowRemarksInQuickInfo, LanguageNames.CSharp);
            BindToOption(RenameTrackingPreview, RenameTrackingOptionsStorage.RenameTrackingPreview, LanguageNames.CSharp);
            BindToOption(Split_string_literals_on_enter, SplitStringLiteralOptionsStorage.Enabled);
            BindToOption(Fix_text_pasted_into_string_literals_experimental, StringCopyPasteOptionsStorage.AutomaticallyFixStringContentsOnPaste, LanguageNames.CSharp);
            BindToOption(Report_invalid_placeholders_in_string_dot_format_calls, IdeAnalyzerOptionsStorage.ReportInvalidPlaceholdersInStringDotFormatCalls, LanguageNames.CSharp);
            BindToOption(Underline_reassigned_variables, ClassificationOptionsStorage.ClassifyReassignedVariables, LanguageNames.CSharp);
            BindToOption(Strike_out_obsolete_symbols, ClassificationOptionsStorage.ClassifyObsoleteSymbols, LanguageNames.CSharp);

            // Regular Expressions
            BindToOption(Colorize_regular_expressions, ClassificationOptionsStorage.ColorizeRegexPatterns, LanguageNames.CSharp);
            BindToOption(Report_invalid_regular_expressions, IdeAnalyzerOptionsStorage.ReportInvalidRegexPatterns, LanguageNames.CSharp);
            BindToOption(Highlight_related_regular_expression_components_under_cursor, HighlightingOptionsStorage.HighlightRelatedRegexComponentsUnderCursor, LanguageNames.CSharp);
            BindToOption(Show_completion_list, CompletionOptionsStorage.ProvideRegexCompletions, LanguageNames.CSharp);

            // Json
            BindToOption(Colorize_JSON_strings, ClassificationOptionsStorage.ColorizeJsonPatterns, LanguageNames.CSharp);
            BindToOption(Report_invalid_JSON_strings, IdeAnalyzerOptionsStorage.ReportInvalidJsonPatterns, LanguageNames.CSharp);
            BindToOption(Highlight_related_JSON_components_under_cursor, HighlightingOptionsStorage.HighlightRelatedJsonComponentsUnderCursor, LanguageNames.CSharp);

            // Classifications
            BindToOption(Editor_color_scheme, ColorSchemeOptionsStorage.ColorScheme);

            // Extract Method
            BindToOption(DontPutOutOrRefOnStruct, ExtractMethodOptionsStorage.DoNotPutOutOrRefOnStruct, LanguageNames.CSharp);

            // Implement Interface or Abstract Class
            BindToOption(with_other_members_of_the_same_kind, ImplementTypeOptionsStorage.InsertionBehavior, ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind, LanguageNames.CSharp);
            BindToOption(at_the_end, ImplementTypeOptionsStorage.InsertionBehavior, ImplementTypeInsertionBehavior.AtTheEnd, LanguageNames.CSharp);
            BindToOption(prefer_throwing_properties, ImplementTypeOptionsStorage.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferThrowingProperties, LanguageNames.CSharp);
            BindToOption(prefer_auto_properties, ImplementTypeOptionsStorage.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferAutoProperties, LanguageNames.CSharp);

            // Inline Hints
            BindToOption(DisplayAllHintsWhilePressingAltF1, InlineHintsViewOptionsStorage.DisplayAllHintsWhilePressingAltF1);
            BindToOption(ColorHints, InlineHintsViewOptionsStorage.ColorHints, LanguageNames.CSharp);

            BindToOption(DisplayInlineParameterNameHints, InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.CSharp);
            BindToOption(ShowHintsForLiterals, InlineHintsOptionsStorage.ForLiteralParameters, LanguageNames.CSharp);
            BindToOption(ShowHintsForNewExpressions, InlineHintsOptionsStorage.ForObjectCreationParameters, LanguageNames.CSharp);
            BindToOption(ShowHintsForEverythingElse, InlineHintsOptionsStorage.ForOtherParameters, LanguageNames.CSharp);
            BindToOption(ShowHintsForIndexers, InlineHintsOptionsStorage.ForIndexerParameters, LanguageNames.CSharp);
            BindToOption(SuppressHintsWhenParameterNameMatchesTheMethodsIntent, InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent, LanguageNames.CSharp);
            BindToOption(SuppressHintsWhenParameterNamesDifferOnlyBySuffix, InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix, LanguageNames.CSharp);
            BindToOption(SuppressHintsWhenParameterNamesMatchArgumentNames, InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName, LanguageNames.CSharp);

            BindToOption(DisplayInlineTypeHints, InlineHintsOptionsStorage.EnabledForTypes, LanguageNames.CSharp);
            BindToOption(ShowHintsForVariablesWithInferredTypes, InlineHintsOptionsStorage.ForImplicitVariableTypes, LanguageNames.CSharp);
            BindToOption(ShowHintsForLambdaParameterTypes, InlineHintsOptionsStorage.ForLambdaParameterTypes, LanguageNames.CSharp);
            BindToOption(ShowHintsForImplicitObjectCreation, InlineHintsOptionsStorage.ForImplicitObjectCreation, LanguageNames.CSharp);
            BindToOption(ShowHintsForCollectionExpressions, InlineHintsOptionsStorage.ForCollectionExpressions, LanguageNames.CSharp);

            // Inheritance Margin
            // Leave the null converter here to make sure if the option value is get from the storage (if it is null), the feature will be enabled
            BindToOption(ShowInheritanceMargin, InheritanceMarginOptionsStorage.ShowInheritanceMargin, LanguageNames.CSharp, () => true);
            BindToOption(InheritanceMarginCombinedWithIndicatorMargin, InheritanceMarginOptionsStorage.InheritanceMarginCombinedWithIndicatorMargin);
            BindToOption(IncludeGlobalImports, InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports, LanguageNames.CSharp);

            // Stack Trace Explorer
            BindToOption(AutomaticallyOpenStackTraceExplorer, StackTraceExplorerOptionsStorage.OpenOnFocus);

            // Document Outline
            BindToOption(EnableDocumentOutline, DocumentOutlineOptionsStorage.EnableDocumentOutline, () =>
            {
                // If the option has not been set by the user, check if the option is disabled from experimentation. If
                // so, default to reflect that.
                return !optionStore.GetOption(DocumentOutlineOptionsStorage.DisableDocumentOutlineFeatureFlag);
            });
        }

        // Since this dialog is constructed once for the lifetime of the application and VS Theme can be changed after the application has started,
        // we need to update the visibility of our combobox and warnings based on the current VS theme before being rendered.
        internal override void OnLoad()
        {
            var cancellationToken = _threadingContext.DisposalToken;
            var (isSupportedTheme, isThemeCustomized) = _threadingContext.JoinableTaskFactory.Run(async () =>
                (await _colorSchemeApplier.IsSupportedThemeAsync(cancellationToken).ConfigureAwait(false),
                 await _colorSchemeApplier.IsThemeCustomizedAsync(cancellationToken).ConfigureAwait(false)));

            Editor_color_scheme.Visibility = isSupportedTheme ? Visibility.Visible : Visibility.Collapsed;
            Customized_Theme_Warning.Visibility = isSupportedTheme && isThemeCustomized ? Visibility.Visible : Visibility.Collapsed;
            Custom_VS_Theme_Warning.Visibility = isSupportedTheme ? Visibility.Collapsed : Visibility.Visible;

            UpdateInlineHintsOptions();

            base.OnLoad();
        }

        private void UpdateInlineHintsOptions()
        {
            var enabledForParameters = this.OptionStore.GetOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.CSharp);
            ShowHintsForLiterals.IsEnabled = enabledForParameters;
            ShowHintsForNewExpressions.IsEnabled = enabledForParameters;
            ShowHintsForEverythingElse.IsEnabled = enabledForParameters;
            ShowHintsForIndexers.IsEnabled = enabledForParameters;
            SuppressHintsWhenParameterNameMatchesTheMethodsIntent.IsEnabled = enabledForParameters;
            SuppressHintsWhenParameterNamesDifferOnlyBySuffix.IsEnabled = enabledForParameters;
            SuppressHintsWhenParameterNamesMatchArgumentNames.IsEnabled = enabledForParameters;

            var enabledForTypes = this.OptionStore.GetOption(InlineHintsOptionsStorage.EnabledForTypes, LanguageNames.CSharp);
            ShowHintsForVariablesWithInferredTypes.IsEnabled = enabledForTypes;
            ShowHintsForLambdaParameterTypes.IsEnabled = enabledForTypes;
            ShowHintsForImplicitObjectCreation.IsEnabled = enabledForTypes;
            ShowHintsForCollectionExpressions.IsEnabled = enabledForTypes;
        }

        private void DisplayInlineParameterNameHints_Checked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.CSharp, true);
            UpdateInlineHintsOptions();
        }

        private void DisplayInlineParameterNameHints_Unchecked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.CSharp, false);
            UpdateInlineHintsOptions();
        }

        private void DisplayInlineTypeHints_Checked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineHintsOptionsStorage.EnabledForTypes, LanguageNames.CSharp, true);
            UpdateInlineHintsOptions();
        }

        private void DisplayInlineTypeHints_Unchecked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineHintsOptionsStorage.EnabledForTypes, LanguageNames.CSharp, false);
            UpdateInlineHintsOptions();
        }

        private void EnterOutliningMode_Checked(object sender, RoutedEventArgs e)
        {
            Collapse_regions_on_file_open.IsEnabled = true;
            Collapse_usings_on_file_open.IsEnabled = true;
            Collapse_metadata_signature_files_on_open.IsEnabled = true;
            Collapse_sourcelink_embedded_decompiled_files_on_open.IsEnabled = true;
        }

        private void EnterOutliningMode_Unchecked(object sender, RoutedEventArgs e)
        {
            Collapse_regions_on_file_open.IsEnabled = false;
            Collapse_usings_on_file_open.IsEnabled = false;
            Collapse_metadata_signature_files_on_open.IsEnabled = false;
            Collapse_sourcelink_embedded_decompiled_files_on_open.IsEnabled = false;
        }
    }
}
