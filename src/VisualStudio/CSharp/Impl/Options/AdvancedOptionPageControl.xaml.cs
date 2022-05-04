// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using ICSharpCode.Decompiler.IL;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ColorSchemes;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.InlineDiagnostics;
using Microsoft.CodeAnalysis.Editor.InlineHints;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.ComponentModelHost;
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
            BindToOption(Run_background_code_analysis_for, SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp);
            BindToOption(DisplayDiagnosticsInline, InlineDiagnosticsOptions.EnableInlineDiagnostics, LanguageNames.CSharp);
            BindToOption(at_the_end_of_the_line_of_code, InlineDiagnosticsOptions.Location, InlineDiagnosticsLocations.PlacedAtEndOfCode, LanguageNames.CSharp);
            BindToOption(on_the_right_edge_of_the_editor_window, InlineDiagnosticsOptions.Location, InlineDiagnosticsLocations.PlacedAtEndOfEditor, LanguageNames.CSharp);

            BindToOption(Run_code_analysis_in_separate_process, RemoteHostOptions.OOP64Bit);
            BindToOption(Enable_file_logging_for_diagnostics, VisualStudioLoggingOptionsMetadata.EnableFileLoggingForDiagnostics);
            BindToOption(Skip_analyzers_for_implicitly_triggered_builds, FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds);
            BindToOption(Show_Remove_Unused_References_command_in_Solution_Explorer_experimental, FeatureOnOffOptions.OfferRemoveUnusedReferences, () =>
            {
                // If the option has not been set by the user, check if the option to remove unused references
                // is enabled from experimentation. If so, default to that.
                return optionStore.GetOption(FeatureOnOffOptions.OfferRemoveUnusedReferencesFeatureFlag);
            });

            // Go To Definition
            BindToOption(Enable_navigation_to_decompiled_sources, MetadataAsSourceOptionsStorage.NavigateToDecompiledSources);
            BindToOption(Always_use_default_symbol_servers_for_navigation, MetadataAsSourceOptionsStorage.AlwaysUseDefaultSymbolServers);
            BindToOption(Navigate_asynchronously_exerimental, FeatureOnOffOptions.NavigateAsynchronously);

            // Using Directives
            BindToOption(PlaceSystemNamespaceFirst, GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp);
            BindToOption(SeparateImportGroups, GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInReferenceAssemblies, SymbolSearchOptionsStorage.SearchReferenceAssemblies, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInNuGetPackages, SymbolSearchOptionsStorage.SearchNuGetPackages, LanguageNames.CSharp);
            BindToOption(AddUsingsOnPaste, FeatureOnOffOptions.AddImportsOnPaste, LanguageNames.CSharp, () =>
            {
                // This option used to be backed by an experimentation flag but is no longer.
                // Having the option still a bool? keeps us from running into storage related issues,
                // but if the option was stored as null we want it to respect this default
                return false;
            });

            // Quick Actions
            BindToOption(ComputeQuickActionsAsynchronouslyExperimental, SuggestionsOptions.Asynchronous, () =>
            {
                // If the option has not been set by the user, check if the option is disabled from experimentation.
                return !optionStore.GetOption(SuggestionsOptions.AsynchronousQuickActionsDisableFeatureFlag);
            });

            // Highlighting
            BindToOption(EnableHighlightReferences, FeatureOnOffOptions.ReferenceHighlighting, LanguageNames.CSharp);
            BindToOption(EnableHighlightKeywords, FeatureOnOffOptions.KeywordHighlighting, LanguageNames.CSharp);

            // Outlining
            BindToOption(EnterOutliningMode, FeatureOnOffOptions.Outlining, LanguageNames.CSharp);
            BindToOption(DisplayLineSeparators, FeatureOnOffOptions.LineSeparator, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_declaration_level_constructs, BlockStructureOptionsStorage.ShowOutliningForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_code_level_constructs, BlockStructureOptionsStorage.ShowOutliningForCodeLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_comments_and_preprocessor_regions, BlockStructureOptionsStorage.ShowOutliningForCommentsAndPreprocessorRegions, LanguageNames.CSharp);
            BindToOption(Collapse_regions_when_collapsing_to_definitions, BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp);

            // Fading
            BindToOption(Fade_out_unused_usings, IdeAnalyzerOptionsStorage.FadeOutUnusedImports, LanguageNames.CSharp);
            BindToOption(Fade_out_unreachable_code, IdeAnalyzerOptionsStorage.FadeOutUnreachableCode, LanguageNames.CSharp);

            // Block Structure Guides
            BindToOption(Show_guides_for_declaration_level_constructs, BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_guides_for_code_level_constructs, BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.CSharp);

            // Comments
            BindToOption(GenerateXmlDocCommentsForTripleSlash, DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, LanguageNames.CSharp);
            BindToOption(InsertSlashSlashAtTheStartOfNewLinesWhenWritingSingleLineComments, SplitStringLiteralOptions.Enabled, LanguageNames.CSharp);
            BindToOption(InsertAsteriskAtTheStartOfNewLinesWhenWritingBlockComments, FeatureOnOffOptions.AutoInsertBlockCommentStartString, LanguageNames.CSharp);

            // Editor Help
            BindToOption(ShowRemarksInQuickInfo, QuickInfoOptionsStorage.ShowRemarksInQuickInfo, LanguageNames.CSharp);
            BindToOption(RenameTrackingPreview, FeatureOnOffOptions.RenameTrackingPreview, LanguageNames.CSharp);
            BindToOption(Split_string_literals_on_enter, SplitStringLiteralOptions.Enabled, LanguageNames.CSharp);
            BindToOption(Fix_text_pasted_into_string_literals_experimental, FeatureOnOffOptions.AutomaticallyFixStringContentsOnPaste, LanguageNames.CSharp);
            BindToOption(Report_invalid_placeholders_in_string_dot_format_calls, IdeAnalyzerOptionsStorage.ReportInvalidPlaceholdersInStringDotFormatCalls, LanguageNames.CSharp);
            BindToOption(Underline_reassigned_variables, ClassificationOptionsStorage.ClassifyReassignedVariables, LanguageNames.CSharp);
            BindToOption(Enable_all_features_in_opened_files_from_source_generators, WorkspaceConfigurationOptionsStorage.EnableOpeningSourceGeneratedFilesInWorkspace, () =>
            {
                // If the option has not been set by the user, check if the option is enabled from experimentation.
                // If so, default to that.
                return optionStore.GetOption(WorkspaceConfigurationOptionsStorage.EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag);
            });

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
            BindToOption(Editor_color_scheme, ColorSchemeOptions.ColorScheme);

            // Extract Method
            BindToOption(DontPutOutOrRefOnStruct, ExtractMethodOptionsStorage.DontPutOutOrRefOnStruct, LanguageNames.CSharp);

            // Implement Interface or Abstract Class
            BindToOption(with_other_members_of_the_same_kind, ImplementTypeOptionsStorage.InsertionBehavior, ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind, LanguageNames.CSharp);
            BindToOption(at_the_end, ImplementTypeOptionsStorage.InsertionBehavior, ImplementTypeInsertionBehavior.AtTheEnd, LanguageNames.CSharp);
            BindToOption(prefer_throwing_properties, ImplementTypeOptionsStorage.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferThrowingProperties, LanguageNames.CSharp);
            BindToOption(prefer_auto_properties, ImplementTypeOptionsStorage.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferAutoProperties, LanguageNames.CSharp);

            // Inline Hints
            BindToOption(DisplayAllHintsWhilePressingAltF1, InlineHintsViewOptions.DisplayAllHintsWhilePressingAltF1);
            BindToOption(ColorHints, InlineHintsViewOptions.ColorHints, LanguageNames.CSharp);

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

            // Inheritance Margin
            // Leave the null converter here to make sure if the option value is get from the storage (if it is null), the feature will be enabled
            BindToOption(ShowInheritanceMargin, FeatureOnOffOptions.ShowInheritanceMargin, LanguageNames.CSharp, () => true);
            BindToOption(InheritanceMarginCombinedWithIndicatorMargin, FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin);
            BindToOption(IncludeGlobalImports, FeatureOnOffOptions.InheritanceMarginIncludeGlobalImports, LanguageNames.CSharp);

            // Stack Trace Explorer
            BindToOption(AutomaticallyOpenStackTraceExplorer, StackTraceExplorerOptionsMetadata.OpenOnFocus);
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
    }
}
