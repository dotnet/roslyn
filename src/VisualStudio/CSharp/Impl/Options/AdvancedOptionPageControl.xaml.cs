﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Fading;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.ValidateFormatString;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.ColorSchemes;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal partial class AdvancedOptionPageControl : AbstractOptionPageControl
    {
        private readonly ColorSchemeApplier _colorSchemeApplier;

        public AdvancedOptionPageControl(OptionStore optionStore, IComponentModel componentModel, IExperimentationService experimentationService) : base(optionStore)
        {
            _colorSchemeApplier = componentModel.GetService<ColorSchemeApplier>();

            InitializeComponent();

            BindToOption(Background_analysis_scope_active_file, SolutionCrawlerOptions.BackgroundAnalysisScopeOption, BackgroundAnalysisScope.ActiveFile, LanguageNames.CSharp);
            BindToOption(Background_analysis_scope_open_files, SolutionCrawlerOptions.BackgroundAnalysisScopeOption, BackgroundAnalysisScope.OpenFilesAndProjects, LanguageNames.CSharp);
            BindToOption(Background_analysis_scope_full_solution, SolutionCrawlerOptions.BackgroundAnalysisScopeOption, BackgroundAnalysisScope.FullSolution, LanguageNames.CSharp);
            BindToOption(Enable_navigation_to_decompiled_sources, FeatureOnOffOptions.NavigateToDecompiledSources);
            BindToOption(Use_64bit_analysis_process, RemoteHostOptions.OOP64Bit);
            BindToOption(Show_Remove_Unused_References_command_in_Solution_Explorer_experimental, FeatureOnOffOptions.OfferRemoveUnusedReferences, () =>
            {
                // If the option has not been set by the user, check if the option to remove unused references
                // is enabled from experimentation. If so, default to that. Otherwise default to disabled
                return experimentationService?.IsExperimentEnabled(WellKnownExperimentNames.RemoveUnusedReferences) ?? false;
            });

            BindToOption(PlaceSystemNamespaceFirst, GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp);
            BindToOption(SeparateImportGroups, GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInReferenceAssemblies, SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInNuGetPackages, SymbolSearchOptions.SuggestForTypesInNuGetPackages, LanguageNames.CSharp);
            BindToOption(AddUsingsOnPaste, FeatureOnOffOptions.AddImportsOnPaste, LanguageNames.CSharp, () =>
            {
                // If the option has not been set by the user, check if the option to enable imports on paste
                // is enabled from experimentation. If so, default to that. Otherwise default to disabled
                return experimentationService?.IsExperimentEnabled(WellKnownExperimentNames.ImportsOnPasteDefaultEnabled) ?? false;
            });

            BindToOption(Split_string_literals_on_enter, SplitStringLiteralOptions.Enabled, LanguageNames.CSharp);

            BindToOption(EnterOutliningMode, FeatureOnOffOptions.Outlining, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_declaration_level_constructs, BlockStructureOptions.ShowOutliningForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_code_level_constructs, BlockStructureOptions.ShowOutliningForCodeLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_comments_and_preprocessor_regions, BlockStructureOptions.ShowOutliningForCommentsAndPreprocessorRegions, LanguageNames.CSharp);
            BindToOption(Collapse_regions_when_collapsing_to_definitions, BlockStructureOptions.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp);

            BindToOption(Fade_out_unused_usings, FadingOptions.FadeOutUnusedImports, LanguageNames.CSharp);
            BindToOption(Fade_out_unreachable_code, FadingOptions.FadeOutUnreachableCode, LanguageNames.CSharp);

            BindToOption(Show_guides_for_declaration_level_constructs, BlockStructureOptions.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_guides_for_code_level_constructs, BlockStructureOptions.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.CSharp);

            BindToOption(GenerateXmlDocCommentsForTripleSlash, DocumentationCommentOptions.AutoXmlDocCommentGeneration, LanguageNames.CSharp);
            BindToOption(InsertSlashSlashAtTheStartOfNewLinesWhenWritingSingleLineComments, SplitStringLiteralOptions.Enabled, LanguageNames.CSharp);
            BindToOption(InsertAsteriskAtTheStartOfNewLinesWhenWritingBlockComments, FeatureOnOffOptions.AutoInsertBlockCommentStartString, LanguageNames.CSharp);

            BindToOption(ShowRemarksInQuickInfo, QuickInfoOptions.ShowRemarksInQuickInfo, LanguageNames.CSharp);
            BindToOption(DisplayLineSeparators, FeatureOnOffOptions.LineSeparator, LanguageNames.CSharp);
            BindToOption(EnableHighlightReferences, FeatureOnOffOptions.ReferenceHighlighting, LanguageNames.CSharp);
            BindToOption(EnableHighlightKeywords, FeatureOnOffOptions.KeywordHighlighting, LanguageNames.CSharp);
            BindToOption(RenameTrackingPreview, FeatureOnOffOptions.RenameTrackingPreview, LanguageNames.CSharp);

            BindToOption(DontPutOutOrRefOnStruct, ExtractMethodOptions.DontPutOutOrRefOnStruct, LanguageNames.CSharp);

            BindToOption(with_other_members_of_the_same_kind, ImplementTypeOptions.InsertionBehavior, ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind, LanguageNames.CSharp);
            BindToOption(at_the_end, ImplementTypeOptions.InsertionBehavior, ImplementTypeInsertionBehavior.AtTheEnd, LanguageNames.CSharp);

            BindToOption(prefer_throwing_properties, ImplementTypeOptions.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferThrowingProperties, LanguageNames.CSharp);
            BindToOption(prefer_auto_properties, ImplementTypeOptions.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferAutoProperties, LanguageNames.CSharp);

            BindToOption(Report_invalid_placeholders_in_string_dot_format_calls, ValidateFormatStringOption.ReportInvalidPlaceholdersInStringDotFormatCalls, LanguageNames.CSharp);

            BindToOption(Colorize_regular_expressions, RegularExpressionsOptions.ColorizeRegexPatterns, LanguageNames.CSharp);
            BindToOption(Report_invalid_regular_expressions, RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.CSharp);
            BindToOption(Highlight_related_components_under_cursor, RegularExpressionsOptions.HighlightRelatedRegexComponentsUnderCursor, LanguageNames.CSharp);
            BindToOption(Show_completion_list, RegularExpressionsOptions.ProvideRegexCompletions, LanguageNames.CSharp);

            BindToOption(Editor_color_scheme, ColorSchemeOptions.ColorScheme);

            BindToOption(DisplayAllHintsWhilePressingAltF1, InlineHintsOptions.DisplayAllHintsWhilePressingAltF1);
            BindToOption(ColorHints, InlineHintsOptions.ColorHints, LanguageNames.CSharp);

            BindToOption(DisplayInlineParameterNameHints, InlineHintsOptions.EnabledForParameters, LanguageNames.CSharp);
            BindToOption(ShowHintsForLiterals, InlineHintsOptions.ForLiteralParameters, LanguageNames.CSharp);
            BindToOption(ShowHintsForNewExpressions, InlineHintsOptions.ForObjectCreationParameters, LanguageNames.CSharp);
            BindToOption(ShowHintsForEverythingElse, InlineHintsOptions.ForOtherParameters, LanguageNames.CSharp);
            BindToOption(SuppressHintsWhenParameterNameMatchesTheMethodsIntent, InlineHintsOptions.SuppressForParametersThatMatchMethodIntent, LanguageNames.CSharp);
            BindToOption(SuppressHintsWhenParameterNamesDifferOnlyBySuffix, InlineHintsOptions.SuppressForParametersThatDifferOnlyBySuffix, LanguageNames.CSharp);

            BindToOption(DisplayInlineTypeHints, InlineHintsOptions.EnabledForTypes, LanguageNames.CSharp);
            BindToOption(ShowHintsForVariablesWithInferredTypes, InlineHintsOptions.ForImplicitVariableTypes, LanguageNames.CSharp);
            BindToOption(ShowHintsForLambdaParameterTypes, InlineHintsOptions.ForLambdaParameterTypes, LanguageNames.CSharp);
            BindToOption(ShowHintsForImplicitObjectCreation, InlineHintsOptions.ForImplicitObjectCreation, LanguageNames.CSharp);
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
            Enable_pull_diagnostics_experimental_requires_restart.IsChecked = OptionStore.GetOption(InternalDiagnosticsOptions.NormalDiagnosticMode) == DiagnosticMode.Pull;
            Enable_Razor_pull_diagnostics_experimental_requires_restart.IsChecked = OptionStore.GetOption(InternalDiagnosticsOptions.RazorDiagnosticMode) == DiagnosticMode.Pull;
        }

        private void Enable_pull_diagnostics_experimental_requires_restart_Checked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InternalDiagnosticsOptions.NormalDiagnosticMode, DiagnosticMode.Pull);
            UpdatePullDiagnosticsOptions();
        }

        private void Enable_pull_diagnostics_experimental_requires_restart_Unchecked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InternalDiagnosticsOptions.NormalDiagnosticMode, DiagnosticMode.Push);
            UpdatePullDiagnosticsOptions();
        }

        private void Enable_Razor_pull_diagnostics_experimental_requires_restart_Checked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InternalDiagnosticsOptions.RazorDiagnosticMode, DiagnosticMode.Pull);
            UpdatePullDiagnosticsOptions();
        }

        private void Enable_Razor_pull_diagnostics_experimental_requires_restart_Unchecked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InternalDiagnosticsOptions.RazorDiagnosticMode, DiagnosticMode.Push);
            UpdatePullDiagnosticsOptions();
        }

        private void UpdateInlineHintsOptions()
        {
            var enabledForParameters = this.OptionStore.GetOption(InlineHintsOptions.EnabledForParameters, LanguageNames.CSharp);
            ShowHintsForLiterals.IsEnabled = enabledForParameters;
            ShowHintsForNewExpressions.IsEnabled = enabledForParameters;
            ShowHintsForEverythingElse.IsEnabled = enabledForParameters;
            SuppressHintsWhenParameterNameMatchesTheMethodsIntent.IsEnabled = enabledForParameters;
            SuppressHintsWhenParameterNamesDifferOnlyBySuffix.IsEnabled = enabledForParameters;

            var enabledForTypes = this.OptionStore.GetOption(InlineHintsOptions.EnabledForTypes, LanguageNames.CSharp);
            ShowHintsForVariablesWithInferredTypes.IsEnabled = enabledForTypes;
            ShowHintsForLambdaParameterTypes.IsEnabled = enabledForTypes;
            ShowHintsForImplicitObjectCreation.IsEnabled = enabledForTypes;
        }

        private void DisplayInlineParameterNameHints_Checked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineHintsOptions.EnabledForParameters, LanguageNames.CSharp, true);
            UpdateInlineHintsOptions();
        }

        private void DisplayInlineParameterNameHints_Unchecked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineHintsOptions.EnabledForParameters, LanguageNames.CSharp, false);
            UpdateInlineHintsOptions();
        }

        private void DisplayInlineTypeHints_Checked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineHintsOptions.EnabledForTypes, LanguageNames.CSharp, true);
            UpdateInlineHintsOptions();
        }

        private void DisplayInlineTypeHints_Unchecked(object sender, RoutedEventArgs e)
        {
            this.OptionStore.SetOption(InlineHintsOptions.EnabledForTypes, LanguageNames.CSharp, false);
            UpdateInlineHintsOptions();
        }
    }
}
