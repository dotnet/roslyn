' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Windows
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
Imports Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
Imports Microsoft.CodeAnalysis.Editor.InlineDiagnostics
Imports Microsoft.CodeAnalysis.Editor.InlineHints
Imports Microsoft.CodeAnalysis.Editor.Options
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Fading
Imports Microsoft.CodeAnalysis.ImplementType
Imports Microsoft.CodeAnalysis.InlineHints
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.CodeAnalysis.Remote
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.SymbolSearch
Imports Microsoft.CodeAnalysis.ValidateFormatString
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.ColorSchemes
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Friend Class AdvancedOptionPageControl
        Private ReadOnly _colorSchemeApplier As ColorSchemeApplier

        Public Sub New(optionStore As OptionStore, componentModel As IComponentModel)
            MyBase.New(optionStore)

            _colorSchemeApplier = componentModel.GetService(Of ColorSchemeApplier)()

            InitializeComponent()

            ' Keep this code in sync with the actual order options appear in Tools | Options

            ' Analysis
            BindToOption(Background_analysis_scope_active_file, SolutionCrawlerOptions.BackgroundAnalysisScopeOption, BackgroundAnalysisScope.ActiveFile, LanguageNames.VisualBasic)
            BindToOption(Background_analysis_scope_open_files, SolutionCrawlerOptions.BackgroundAnalysisScopeOption, BackgroundAnalysisScope.OpenFilesAndProjects, LanguageNames.VisualBasic)
            BindToOption(Background_analysis_scope_full_solution, SolutionCrawlerOptions.BackgroundAnalysisScopeOption, BackgroundAnalysisScope.FullSolution, LanguageNames.VisualBasic)
            BindToOption(DisplayDiagnosticsInline, InlineDiagnosticsOptions.EnableInlineDiagnostics, LanguageNames.VisualBasic)
            BindToOption(at_the_end_of_the_line_of_code, InlineDiagnosticsOptions.Location, InlineDiagnosticsLocations.PlacedAtEndOfCode, LanguageNames.VisualBasic)
            BindToOption(on_the_right_edge_of_the_editor_window, InlineDiagnosticsOptions.Location, InlineDiagnosticsLocations.PlacedAtEndOfEditor, LanguageNames.VisualBasic)
            BindToOption(Run_code_analysis_in_separate_process, RemoteHostOptions.OOP64Bit)
            BindToOption(Enable_file_logging_for_diagnostics, InternalDiagnosticsOptions.EnableFileLoggingForDiagnostics)
            BindToOption(Skip_analyzers_for_implicitly_triggered_builds, FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds)
            BindToOption(Show_Remove_Unused_References_command_in_Solution_Explorer_experimental, FeatureOnOffOptions.OfferRemoveUnusedReferences,
                         Function()
                             ' If the option has Not been set by the user, check if the option is enabled from experimentation.
                             Return optionStore.GetOption(FeatureOnOffOptions.OfferRemoveUnusedReferencesFeatureFlag)
                         End Function)

            ' Import directives
            BindToOption(PlaceSystemNamespaceFirst, GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.VisualBasic)
            BindToOption(SeparateImportGroups, GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.VisualBasic)
            BindToOption(SuggestForTypesInReferenceAssemblies, SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, LanguageNames.VisualBasic)
            BindToOption(SuggestForTypesInNuGetPackages, SymbolSearchOptions.SuggestForTypesInNuGetPackages, LanguageNames.VisualBasic)
            BindToOption(AddMissingImportsOnPaste, FeatureOnOffOptions.AddImportsOnPaste, LanguageNames.VisualBasic,
                         Function()
                             ' This option used to be backed by an experimentation flag but Is no longer.
                             ' Having the option still a bool? keeps us from running into storage related issues,
                             ' but if the option was stored as null we want it to respect this default
                             Return False
                         End Function)

            ' Quick Actions
            BindToOption(ComputeQuickActionsAsynchronouslyExperimental, SuggestionsOptions.Asynchronous,
                         Function()
                             ' If the option has Not been set by the user, check if the option is disabled from experimentation.
                             Return Not optionStore.GetOption(SuggestionsOptions.AsynchronousQuickActionsDisableFeatureFlag)
                         End Function)

            ' Highlighting
            BindToOption(EnableHighlightReferences, FeatureOnOffOptions.ReferenceHighlighting, LanguageNames.VisualBasic)
            BindToOption(EnableHighlightKeywords, FeatureOnOffOptions.KeywordHighlighting, LanguageNames.VisualBasic)

            ' Outlining
            BindToOption(EnableOutlining, FeatureOnOffOptions.Outlining, LanguageNames.VisualBasic)
            BindToOption(DisplayLineSeparators, FeatureOnOffOptions.LineSeparator, LanguageNames.VisualBasic)
            BindToOption(Show_outlining_for_declaration_level_constructs, BlockStructureOptions.Metadata.ShowOutliningForDeclarationLevelConstructs, LanguageNames.VisualBasic)
            BindToOption(Show_outlining_for_code_level_constructs, BlockStructureOptions.Metadata.ShowOutliningForCodeLevelConstructs, LanguageNames.VisualBasic)
            BindToOption(Show_outlining_for_comments_and_preprocessor_regions, BlockStructureOptions.Metadata.ShowOutliningForCommentsAndPreprocessorRegions, LanguageNames.VisualBasic)
            BindToOption(Collapse_regions_when_collapsing_to_definitions, BlockStructureOptions.Metadata.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.VisualBasic)

            ' Fading
            BindToOption(Fade_out_unused_imports, FadingOptions.FadeOutUnusedImports, LanguageNames.VisualBasic)

            ' Block structure guides
            BindToOption(Show_guides_for_declaration_level_constructs, BlockStructureOptions.Metadata.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.VisualBasic)
            BindToOption(Show_guides_for_code_level_constructs, BlockStructureOptions.Metadata.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.VisualBasic)

            ' Comments
            BindToOption(GenerateXmlDocCommentsForTripleApostrophes, DocumentationCommentOptions.Metadata.AutoXmlDocCommentGeneration, LanguageNames.VisualBasic)
            BindToOption(InsertApostropheAtTheStartOfNewLinesWhenWritingApostropheComments, SplitCommentOptions.Enabled, LanguageNames.VisualBasic)

            ' Editor help
            BindToOption(EnableEndConstruct, FeatureOnOffOptions.EndConstruct, LanguageNames.VisualBasic)
            BindToOption(EnableLineCommit, FeatureOnOffOptions.PrettyListing, LanguageNames.VisualBasic)
            BindToOption(AutomaticInsertionOfInterfaceAndMustOverrideMembers, FeatureOnOffOptions.AutomaticInsertionOfAbstractOrInterfaceMembers, LanguageNames.VisualBasic)
            BindToOption(RenameTrackingPreview, FeatureOnOffOptions.RenameTrackingPreview, LanguageNames.VisualBasic)
            BindToOption(ShowRemarksInQuickInfo, QuickInfoOptions.Metadata.ShowRemarksInQuickInfo, LanguageNames.VisualBasic)
            BindToOption(Report_invalid_placeholders_in_string_dot_format_calls, ValidateFormatStringOption.ReportInvalidPlaceholdersInStringDotFormatCalls, LanguageNames.VisualBasic)
            BindToOption(Underline_reassigned_variables, ClassificationOptions.Metadata.ClassifyReassignedVariables, LanguageNames.VisualBasic)

            ' Go To Definition
            BindToOption(NavigateToObjectBrowser, VisualStudioNavigationOptions.NavigateToObjectBrowser, LanguageNames.VisualBasic)
            BindToOption(Enable_all_features_in_opened_files_from_source_generators, WorkspaceConfigurationOptions.EnableOpeningSourceGeneratedFilesInWorkspace,
                         Function()
                             ' If the option has Not been set by the user, check if the option is enabled from experimentation.
                             Return optionStore.GetOption(WorkspaceConfigurationOptions.EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag)
                         End Function)

            ' Regular expressions
            BindToOption(Colorize_regular_expressions, ClassificationOptions.Metadata.ColorizeRegexPatterns, LanguageNames.VisualBasic)
            BindToOption(Report_invalid_regular_expressions, RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.VisualBasic)
            BindToOption(Highlight_related_components_under_cursor, RegularExpressionsOptions.HighlightRelatedRegexComponentsUnderCursor, LanguageNames.VisualBasic)
            BindToOption(Show_completion_list, CompletionOptions.Metadata.ProvideRegexCompletions, LanguageNames.VisualBasic)

            ' Editor color scheme
            BindToOption(Editor_color_scheme, ColorSchemeOptions.ColorScheme)

            ' Extract method
            BindToOption(DontPutOutOrRefOnStruct, ExtractMethodOptions.DontPutOutOrRefOnStruct, LanguageNames.VisualBasic)

            ' Implement Interface or Abstract Class
            BindToOption(with_other_members_of_the_same_kind, ImplementTypeOptions.Metadata.InsertionBehavior, ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind, LanguageNames.VisualBasic)
            BindToOption(at_the_end, ImplementTypeOptions.Metadata.InsertionBehavior, ImplementTypeInsertionBehavior.AtTheEnd, LanguageNames.VisualBasic)

            BindToOption(prefer_throwing_properties, ImplementTypeOptions.Metadata.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferThrowingProperties, LanguageNames.VisualBasic)
            BindToOption(prefer_auto_properties, ImplementTypeOptions.Metadata.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferAutoProperties, LanguageNames.VisualBasic)

            ' Inline hints
            BindToOption(DisplayAllHintsWhilePressingAltF1, InlineHintsViewOptions.DisplayAllHintsWhilePressingAltF1)
            BindToOption(ColorHints, InlineHintsViewOptions.ColorHints, LanguageNames.VisualBasic)

            BindToOption(DisplayInlineParameterNameHints, InlineParameterHintsOptions.Metadata.EnabledForParameters, LanguageNames.VisualBasic)
            BindToOption(ShowHintsForLiterals, InlineParameterHintsOptions.Metadata.ForLiteralParameters, LanguageNames.VisualBasic)
            BindToOption(ShowHintsForNewExpressions, InlineParameterHintsOptions.Metadata.ForObjectCreationParameters, LanguageNames.VisualBasic)
            BindToOption(ShowHintsForEverythingElse, InlineParameterHintsOptions.Metadata.ForOtherParameters, LanguageNames.VisualBasic)
            BindToOption(ShowHintsForIndexers, InlineParameterHintsOptions.Metadata.ForIndexerParameters, LanguageNames.VisualBasic)
            BindToOption(SuppressHintsWhenParameterNameMatchesTheMethodsIntent, InlineParameterHintsOptions.Metadata.SuppressForParametersThatMatchMethodIntent, LanguageNames.VisualBasic)
            BindToOption(SuppressHintsWhenParameterNamesDifferOnlyBySuffix, InlineParameterHintsOptions.Metadata.SuppressForParametersThatDifferOnlyBySuffix, LanguageNames.VisualBasic)
            BindToOption(SuppressHintsWhenParameterNamesMatchArgumentNames, InlineParameterHintsOptions.Metadata.SuppressForParametersThatMatchArgumentName, LanguageNames.VisualBasic)

            BindToOption(ShowInheritanceMargin, FeatureOnOffOptions.ShowInheritanceMargin, LanguageNames.VisualBasic,
                         Function()
                             ' Leave the null converter here to make sure if the option value is get from the storage (if it is null), the feature will be enabled
                             Return True
                         End Function)
            BindToOption(InheritanceMarginCombinedWithIndicatorMargin, FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin)
        End Sub

        ' Since this dialog is constructed once for the lifetime of the application and VS Theme can be changed after the application has started,
        ' we need to update the visibility of our combobox and warnings based on the current VS theme before being rendered.
        Friend Overrides Sub OnLoad()
            Dim isSupportedTheme = _colorSchemeApplier.IsSupportedTheme()
            Dim isCustomized = _colorSchemeApplier.IsThemeCustomized()

            Editor_color_scheme.Visibility = If(isSupportedTheme, Visibility.Visible, Visibility.Collapsed)
            Customized_Theme_Warning.Visibility = If(isSupportedTheme AndAlso isCustomized, Visibility.Visible, Visibility.Collapsed)
            Custom_VS_Theme_Warning.Visibility = If(isSupportedTheme, Visibility.Collapsed, Visibility.Visible)

            UpdateInlineHintsOptions()

            MyBase.OnLoad()
        End Sub

        Private Sub UpdateInlineHintsOptions()
            Dim enabledForParameters = Me.OptionStore.GetOption(InlineParameterHintsOptions.Metadata.EnabledForParameters, LanguageNames.VisualBasic) <> False
            ShowHintsForLiterals.IsEnabled = enabledForParameters
            ShowHintsForNewExpressions.IsEnabled = enabledForParameters
            ShowHintsForEverythingElse.IsEnabled = enabledForParameters
            ShowHintsForIndexers.IsEnabled = enabledForParameters
            SuppressHintsWhenParameterNameMatchesTheMethodsIntent.IsEnabled = enabledForParameters
            SuppressHintsWhenParameterNamesDifferOnlyBySuffix.IsEnabled = enabledForParameters
            SuppressHintsWhenParameterNamesMatchArgumentNames.IsEnabled = enabledForParameters
        End Sub

        Private Sub DisplayInlineParameterNameHints_Checked()
            Me.OptionStore.SetOption(InlineParameterHintsOptions.Metadata.EnabledForParameters, LanguageNames.VisualBasic, True)
            UpdateInlineHintsOptions()
        End Sub

        Private Sub DisplayInlineParameterNameHints_Unchecked()
            Me.OptionStore.SetOption(InlineParameterHintsOptions.Metadata.EnabledForParameters, LanguageNames.VisualBasic, False)
            UpdateInlineHintsOptions()
        End Sub
    End Class
End Namespace
