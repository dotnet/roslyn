// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.InlineDiagnostics;
using Microsoft.CodeAnalysis.Editor.InlineHints;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Fading;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.ValidateFormatString;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.ColorSchemes;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal partial class AdvancedOptionPageControl : AbstractOptionPageControl
    {
        private readonly ColorSchemeApplier _colorSchemeApplier;

        public AdvancedOptionPageControl(OptionStore optionStore, IComponentModel componentModel) : base(optionStore)
        {
            _colorSchemeApplier = componentModel.GetService<ColorSchemeApplier>();

            InitializeComponent();

            BindToOption(Background_analysis_scope_active_file, SolutionCrawlerOptions.BackgroundAnalysisScopeOption, BackgroundAnalysisScope.ActiveFile, LanguageNames.CSharp);
            BindToOption(Background_analysis_scope_open_files, SolutionCrawlerOptions.BackgroundAnalysisScopeOption, BackgroundAnalysisScope.OpenFilesAndProjects, LanguageNames.CSharp);
            BindToOption(Background_analysis_scope_full_solution, SolutionCrawlerOptions.BackgroundAnalysisScopeOption, BackgroundAnalysisScope.FullSolution, LanguageNames.CSharp);
            BindToOption(DisplayDiagnosticsInline, InlineDiagnosticsOptions.EnableInlineDiagnostics, LanguageNames.CSharp);
            BindToOption(at_the_end_of_the_line_of_code, InlineDiagnosticsOptions.Location, InlineDiagnosticsLocations.PlacedAtEndOfCode, LanguageNames.CSharp);
            BindToOption(on_the_right_edge_of_the_editor_window, InlineDiagnosticsOptions.Location, InlineDiagnosticsLocations.PlacedAtEndOfEditor, LanguageNames.CSharp);
            BindToOption(Enable_navigation_to_decompiled_sources, FeatureOnOffOptions.NavigateToDecompiledSources);
            BindToOption(Run_code_analysis_in_separate_process, RemoteHostOptions.OOP64Bit);
            BindToOption(Enable_file_logging_for_diagnostics, InternalDiagnosticsOptions.EnableFileLoggingForDiagnostics);
            BindToOption(Skip_analyzers_for_implicitly_triggered_builds, FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds);
            BindToOption(Show_Remove_Unused_References_command_in_Solution_Explorer_experimental, FeatureOnOffOptions.OfferRemoveUnusedReferences, () =>
            {
                // If the option has not been set by the user, check if the option to remove unused references
                // is enabled from experimentation. If so, default to that.
                return optionStore.GetOption(FeatureOnOffOptions.OfferRemoveUnusedReferencesFeatureFlag);
            });

            BindToOption(PlaceSystemNamespaceFirst, GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp);
            BindToOption(SeparateImportGroups, GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInReferenceAssemblies, SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInNuGetPackages, SymbolSearchOptions.SuggestForTypesInNuGetPackages, LanguageNames.CSharp);
            BindToOption(AddUsingsOnPaste, FeatureOnOffOptions.AddImportsOnPaste, LanguageNames.CSharp, () =>
            {
                // This option used to be backed by an experimentation flag but is no longer.
                // Having the option still a bool? keeps us from running into storage related issues,
                // but if the option was stored as null we want it to respect this default
                return false;
            });

            BindToOption(Split_string_literals_on_enter, SplitStringLiteralOptions.Enabled, LanguageNames.CSharp);

            BindToOption(EnterOutliningMode, FeatureOnOffOptions.Outlining, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_declaration_level_constructs, BlockStructureOptions.Metadata.ShowOutliningForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_code_level_constructs, BlockStructureOptions.Metadata.ShowOutliningForCodeLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_comments_and_preprocessor_regions, BlockStructureOptions.Metadata.ShowOutliningForCommentsAndPreprocessorRegions, LanguageNames.CSharp);
            BindToOption(Collapse_regions_when_collapsing_to_definitions, BlockStructureOptions.Metadata.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp);

            BindToOption(Fade_out_unused_usings, FadingOptions.FadeOutUnusedImports, LanguageNames.CSharp);
            BindToOption(Fade_out_unreachable_code, FadingOptions.FadeOutUnreachableCode, LanguageNames.CSharp);

            BindToOption(Show_guides_for_declaration_level_constructs, BlockStructureOptions.Metadata.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_guides_for_code_level_constructs, BlockStructureOptions.Metadata.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.CSharp);

            BindToOption(GenerateXmlDocCommentsForTripleSlash, DocumentationCommentOptions.Metadata.AutoXmlDocCommentGeneration, LanguageNames.CSharp);
            BindToOption(InsertSlashSlashAtTheStartOfNewLinesWhenWritingSingleLineComments, SplitStringLiteralOptions.Enabled, LanguageNames.CSharp);
            BindToOption(InsertAsteriskAtTheStartOfNewLinesWhenWritingBlockComments, FeatureOnOffOptions.AutoInsertBlockCommentStartString, LanguageNames.CSharp);

            BindToOption(ShowRemarksInQuickInfo, QuickInfoOptions.Metadata.ShowRemarksInQuickInfo, LanguageNames.CSharp);
            BindToOption(DisplayLineSeparators, FeatureOnOffOptions.LineSeparator, LanguageNames.CSharp);

            // Quick Actions
            BindToOption(ComputeQuickActionsAsynchronouslyExperimental, SuggestionsOptions.Asynchronous, () =>
            {
                // If the option has not been set by the user, check if the option is disabled from experimentation.
                return !optionStore.GetOption(SuggestionsOptions.AsynchronousQuickActionsDisableFeatureFlag);
            });

            // Highlighting
            BindToOption(EnableHighlightReferences, FeatureOnOffOptions.ReferenceHighlighting, LanguageNames.CSharp);
            BindToOption(EnableHighlightKeywords, FeatureOnOffOptions.KeywordHighlighting, LanguageNames.CSharp);

            BindToOption(RenameTrackingPreview, FeatureOnOffOptions.RenameTrackingPreview, LanguageNames.CSharp);
            BindToOption(Underline_reassigned_variables, ClassificationOptions.Metadata.ClassifyReassignedVariables, LanguageNames.CSharp);
            BindToOption(Enable_all_features_in_opened_files_from_source_generators, SourceGeneratedFileManager.Options.EnableOpeningInWorkspace, () =>
            {
                // If the option has not been set by the user, check if the option is enabled from experimentation.
                // If so, default to that.
                return optionStore.GetOption(SourceGeneratedFileManager.Options.EnableOpeningInWorkspaceFeatureFlag);
            });

            BindToOption(DontPutOutOrRefOnStruct, ExtractMethodOptions.Metadata.DontPutOutOrRefOnStruct, LanguageNames.CSharp);

            BindToOption(with_other_members_of_the_same_kind, ImplementTypeOptions.Metadata.InsertionBehavior, ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind, LanguageNames.CSharp);
            BindToOption(at_the_end, ImplementTypeOptions.Metadata.InsertionBehavior, ImplementTypeInsertionBehavior.AtTheEnd, LanguageNames.CSharp);

            BindToOption(prefer_throwing_properties, ImplementTypeOptions.Metadata.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferThrowingProperties, LanguageNames.CSharp);
            BindToOption(prefer_auto_properties, ImplementTypeOptions.Metadata.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferAutoProperties, LanguageNames.CSharp);

            BindToOption(Report_invalid_placeholders_in_string_dot_format_calls, ValidateFormatStringOption.ReportInvalidPlaceholdersInStringDotFormatCalls, LanguageNames.CSharp);

            BindToOption(Colorize_regular_expressions, ClassificationOptions.Metadata.ColorizeRegexPatterns, LanguageNames.CSharp);
            BindToOption(Report_invalid_regular_expressions, RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.CSharp);
            BindToOption(Highlight_related_components_under_cursor, RegularExpressionsOptions.HighlightRelatedRegexComponentsUnderCursor, LanguageNames.CSharp);
            BindToOption(Show_completion_list, CompletionOptions.Metadata.ProvideRegexCompletions, LanguageNames.CSharp);

            BindToOption(Editor_color_scheme, ColorSchemeOptions.ColorScheme);

            BindToOption(DisplayAllHintsWhilePressingAltF1, InlineHintsViewOptions.DisplayAllHintsWhilePressingAltF1);
            BindToOption(ColorHints, InlineHintsViewOptions.ColorHints, LanguageNames.CSharp);

            BindToOption(DisplayInlineParameterNameHints, InlineParameterHintsOptions.Metadata.EnabledForParameters, LanguageNames.CSharp);
            BindToOption(ShowHintsForLiterals, InlineParameterHintsOptions.Metadata.ForLiteralParameters, LanguageNames.CSharp);
            BindToOption(ShowHintsForNewExpressions, InlineParameterHintsOptions.Metadata.ForObjectCreationParameters, LanguageNames.CSharp);
            BindToOption(ShowHintsForEverythingElse, InlineParameterHintsOptions.Metadata.ForOtherParameters, LanguageNames.CSharp);
            BindToOption(ShowHintsForIndexers, InlineParameterHintsOptions.Metadata.ForIndexerParameters, LanguageNames.CSharp);
            BindToOption(SuppressHintsWhenParameterNameMatchesTheMethodsIntent, InlineParameterHintsOptions.Metadata.SuppressForParametersThatMatchMethodIntent, LanguageNames.CSharp);
            BindToOption(SuppressHintsWhenParameterNamesDifferOnlyBySuffix, InlineParameterHintsOptions.Metadata.SuppressForParametersThatDifferOnlyBySuffix, LanguageNames.CSharp);
            BindToOption(SuppressHintsWhenParameterNamesMatchArgumentNames, InlineParameterHintsOptions.Metadata.SuppressForParametersThatMatchArgumentName, LanguageNames.CSharp);

            BindToOption(DisplayInlineTypeHints, InlineTypeHintsOptions.Metadata.EnabledForTypes, LanguageNames.CSharp);
            BindToOption(ShowHintsForVariablesWithInferredTypes, InlineTypeHintsOptions.Metadata.ForImplicitVariableTypes, LanguageNames.CSharp);
            BindToOption(ShowHintsForLambdaParameterTypes, InlineTypeHintsOptions.Metadata.ForLambdaParameterTypes, LanguageNames.CSharp);
            BindToOption(ShowHintsForImplicitObjectCreation, InlineTypeHintsOptions.Metadata.ForImplicitObjectCreation, LanguageNames.CSharp);

            // Leave the null converter here to make sure if the option value is get from the storage (if it is null), the feature will be enabled
            BindToOption(ShowInheritanceMargin, FeatureOnOffOptions.ShowInheritanceMargin, LanguageNames.CSharp, () => true);
            BindToOption(InheritanceMarginCombinedWithIndicatorMargin, FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin);

            BindToOption(AutomaticallyOpenStackTraceExplorer, StackTraceExplorerOptions.OpenOnFocus);
        }

        // Since this dialog is constructed once for the lifetime of the application and VS Theme can be changed after the application has started,
        // we need to update the visibility of our combobox and warnings based on the current VS theme before being rendered.
        internal override void OnLoad()
        {
            var isSupportedTheme = _colorSchemeApplier.IsSupportedTheme();
            var isThemeCustomized = _colorSchemeApplier.IsThemeCustomized();

            Editor_color_scheme.Visibility = isSupportedTheme ? Visibility.Visible : Visibility.Collapsed;
            Customized_Theme_Warning.Visibility = isSupportedTheme && isThemeCustomized ? Visibility.Visible : Visibility.Collapsed;
            Custom_VS_Theme_Warning.Visibility = isSupportedTheme ? Visibility.Collapsed : Visibility.Visible;

            UpdatePullDiagnosticsOptions();
            UpdateInlineHintsOptions();

            base.OnLoad();
        }

        private void UpdatePullDiagnosticsOptions()
        {
            var normalPullDiagnosticsOption = OptionStore.GetOption(InternalDiagnosticsOptions.NormalDiagnosticMode);
            Enable_pull_diagnostics_experimental_requires_restart.IsChecked = GetCheckboxValueForDiagnosticMode(normalPullDiagnosticsOption);

            static bool? GetCheckboxValueForDiagnosticMode(DiagnosticMode mode)
            {
                return mode switch
                {
                    DiagnosticMode.Push => false,
                    DiagnosticMode.Pull => true,
                    DiagnosticMode.Default => null,
                    _ => throw new System.ArgumentException("unknown diagnostic mode"),
                };
            }
        }

        private void Enable_pull_diagnostics_experimental_requires_restart_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Three state is only valid for the initial option state (default).  If changed we only
            // allow the checkbox to be on or off.
            Enable_pull_diagnostics_experimental_requires_restart.IsThreeState = false;
            var checkboxValue = Enable_pull_diagnostics_experimental_requires_restart.IsChecked;
            var newDiagnosticMode = GetDiagnosticModeForCheckboxValue(checkboxValue);
            if (checkboxValue != null)
            {
                // Update the actual value of the feature flag to ensure CPS is informed of the new feature flag value.
                this.OptionStore.SetOption(DiagnosticOptions.LspPullDiagnosticsFeatureFlag, checkboxValue.Value);
            }

            // Update the workspace option.
            this.OptionStore.SetOption(InternalDiagnosticsOptions.NormalDiagnosticMode, newDiagnosticMode);

            UpdatePullDiagnosticsOptions();

            static DiagnosticMode GetDiagnosticModeForCheckboxValue(bool? checkboxValue)
            {
                return checkboxValue switch
                {
                    true => DiagnosticMode.Pull,
                    false => DiagnosticMode.Push,
                    null => DiagnosticMode.Default
                };
            }
        }

        private void Enable_pull_diagnostics_experimental_requires_restart_Indeterminate(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InternalDiagnosticsOptions.NormalDiagnosticMode, DiagnosticMode.Default);
            UpdatePullDiagnosticsOptions();
        }

        private void UpdateInlineHintsOptions()
        {
            var enabledForParameters = this.OptionStore.GetOption(InlineParameterHintsOptions.Metadata.EnabledForParameters, LanguageNames.CSharp);
            ShowHintsForLiterals.IsEnabled = enabledForParameters;
            ShowHintsForNewExpressions.IsEnabled = enabledForParameters;
            ShowHintsForEverythingElse.IsEnabled = enabledForParameters;
            ShowHintsForIndexers.IsEnabled = enabledForParameters;
            SuppressHintsWhenParameterNameMatchesTheMethodsIntent.IsEnabled = enabledForParameters;
            SuppressHintsWhenParameterNamesDifferOnlyBySuffix.IsEnabled = enabledForParameters;
            SuppressHintsWhenParameterNamesMatchArgumentNames.IsEnabled = enabledForParameters;

            var enabledForTypes = this.OptionStore.GetOption(InlineTypeHintsOptions.Metadata.EnabledForTypes, LanguageNames.CSharp);
            ShowHintsForVariablesWithInferredTypes.IsEnabled = enabledForTypes;
            ShowHintsForLambdaParameterTypes.IsEnabled = enabledForTypes;
            ShowHintsForImplicitObjectCreation.IsEnabled = enabledForTypes;
        }

        private void DisplayInlineParameterNameHints_Checked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineParameterHintsOptions.Metadata.EnabledForParameters, LanguageNames.CSharp, true);
            UpdateInlineHintsOptions();
        }

        private void DisplayInlineParameterNameHints_Unchecked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineParameterHintsOptions.Metadata.EnabledForParameters, LanguageNames.CSharp, false);
            UpdateInlineHintsOptions();
        }

        private void DisplayInlineTypeHints_Checked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineTypeHintsOptions.Metadata.EnabledForTypes, LanguageNames.CSharp, true);
            UpdateInlineHintsOptions();
        }

        private void DisplayInlineTypeHints_Unchecked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineTypeHintsOptions.Metadata.EnabledForTypes, LanguageNames.CSharp, false);
            UpdateInlineHintsOptions();
        }
    }
}
