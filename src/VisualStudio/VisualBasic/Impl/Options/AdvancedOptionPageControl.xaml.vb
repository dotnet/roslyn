' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Windows
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.AddImportOnPaste
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.ColorSchemes
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.DocumentHighlighting
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
Imports Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
Imports Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
Imports Microsoft.CodeAnalysis.Editor.InlineDiagnostics
Imports Microsoft.CodeAnalysis.Editor.InlineHints
Imports Microsoft.CodeAnalysis.Editor.InlineRename
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices
Imports Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.ImplementType
Imports Microsoft.CodeAnalysis.InheritanceMargin
Imports Microsoft.CodeAnalysis.InlineHints
Imports Microsoft.CodeAnalysis.InlineRename
Imports Microsoft.CodeAnalysis.KeywordHighlighting
Imports Microsoft.CodeAnalysis.LineSeparators
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.CodeAnalysis.ReferenceHighlighting
Imports Microsoft.CodeAnalysis.Remote
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.SymbolSearch
Imports Microsoft.CodeAnalysis.ValidateFormatString
Imports Microsoft.CodeAnalysis.VisualBasic.AutomaticInsertionOfAbstractOrInterfaceMembers
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.DocumentOutline
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Friend Class AdvancedOptionPageControl
        Private ReadOnly _threadingContext As IThreadingContext
        Private ReadOnly _colorSchemeApplier As ColorSchemeApplier

        Public Sub New(optionStore As OptionStore, componentModel As IComponentModel)
            MyBase.New(optionStore)

            _threadingContext = componentModel.GetService(Of IThreadingContext)()
            _colorSchemeApplier = componentModel.GetService(Of ColorSchemeApplier)()

            InitializeComponent()

            ' Keep this code in sync with the actual order options appear in Tools | Options

            ' Analysis
            BindToOption(Run_background_code_analysis_for, SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, label:=Run_background_code_analysis_for_label)

            BindToOption(Show_compiler_errors_and_warnings_for, SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, LanguageNames.VisualBasic)
            BindToOption(DisplayDiagnosticsInline, InlineDiagnosticsOptionsStorage.EnableInlineDiagnostics, LanguageNames.VisualBasic)
            BindToOption(at_the_end_of_the_line_of_code, InlineDiagnosticsOptionsStorage.Location, InlineDiagnosticsLocations.PlacedAtEndOfCode, LanguageNames.VisualBasic)
            BindToOption(on_the_right_edge_of_the_editor_window, InlineDiagnosticsOptionsStorage.Location, InlineDiagnosticsLocations.PlacedAtEndOfEditor, LanguageNames.VisualBasic)
            BindToOption(Run_code_analysis_in_separate_process, RemoteHostOptionsStorage.OOP64Bit)

            BindToOption(Enable_file_logging_for_diagnostics, VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics)
            BindToOption(Skip_analyzers_for_implicitly_triggered_builds, FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds)
            BindToOption(Show_Remove_Unused_References_command_in_Solution_Explorer_experimental, FeatureOnOffOptions.OfferRemoveUnusedReferences,
                         Function()
                             ' If the option has not been set by the user, check if the option is enabled from experimentation.
                             Return optionStore.GetOption(FeatureOnOffOptions.OfferRemoveUnusedReferencesFeatureFlag)
                         End Function)

            ' Source Generators
            BindToOption(Automatic_Run_generators_after_any_change, WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution, SourceGeneratorExecutionPreference.Automatic,
                         Function()
                             ' If the option hasn't been set by the user, then check the feature flag.  If the feature flag has set
                             ' us to only run when builds complete, then we're not in automatic mode.  So we `!` the result.
                             Return Not optionStore.GetOption(WorkspaceConfigurationOptionsStorage.SourceGeneratorExecutionBalancedFeatureFlag)
                         End Function)
            BindToOption(Balanced_Run_generators_after_saving_or_building, WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution, SourceGeneratorExecutionPreference.Balanced,
                         Function()
                             ' If the option hasn't been set by the user, then check the feature flag.  If the feature flag has set
                             ' us to only run when builds complete, then we're in `Balanced_Run_generators_after_saving_or_building` mode and directly return it.
                             Return optionStore.GetOption(WorkspaceConfigurationOptionsStorage.SourceGeneratorExecutionBalancedFeatureFlag)
                         End Function)
            BindToOption(Analyze_source_generated_files, SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFiles,
                         Function()
                             ' If the option has not been set by the user, check if the option is enabled from experimentation.
                             Return optionStore.GetOption(SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFilesFeatureFlag)
                         End Function)

            ' Rename
            BindToOption(Rename_asynchronously_exerimental, InlineRenameSessionOptionsStorage.RenameAsynchronously)
            BindToOption(Rename_UI_setting, InlineRenameUIOptionsStorage.UseInlineAdornment, label:=Rename_UI_setting_label)

            ' Import directives
            BindToOption(PlaceSystemNamespaceFirst, GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.VisualBasic)
            BindToOption(SeparateImportGroups, GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.VisualBasic)
            BindToOption(SuggestForTypesInReferenceAssemblies, SymbolSearchOptionsStorage.SearchReferenceAssemblies, LanguageNames.VisualBasic)
            BindToOption(SuggestForTypesInNuGetPackages, SymbolSearchOptionsStorage.SearchNuGetPackages, LanguageNames.VisualBasic)
            BindToOption(AddMissingImportsOnPaste, AddImportOnPasteOptionsStorage.AddImportsOnPaste, LanguageNames.VisualBasic)

            ' Highlighting
            BindToOption(EnableHighlightReferences, ReferenceHighlightingOptionsStorage.ReferenceHighlighting, LanguageNames.VisualBasic)
            BindToOption(EnableHighlightKeywords, KeywordHighlightingOptionsStorage.KeywordHighlighting, LanguageNames.VisualBasic)

            ' Outlining
            BindToOption(EnableOutlining, OutliningOptionsStorage.Outlining, LanguageNames.VisualBasic)
            BindToOption(Collapse_regions_on_file_open, BlockStructureOptionsStorage.CollapseRegionsWhenFirstOpened, LanguageNames.VisualBasic)
            BindToOption(Collapse_imports_on_file_open, BlockStructureOptionsStorage.CollapseImportsWhenFirstOpened, LanguageNames.VisualBasic)
            BindToOption(Collapse_sourcelink_embedded_decompiled_files_on_open, BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, LanguageNames.VisualBasic)
            BindToOption(Collapse_metadata_signature_files_on_open, BlockStructureOptionsStorage.CollapseMetadataSignatureFilesWhenFirstOpened, LanguageNames.VisualBasic)
            BindToOption(DisplayLineSeparators, LineSeparatorsOptionsStorage.LineSeparator, LanguageNames.VisualBasic)
            BindToOption(Show_outlining_for_declaration_level_constructs, BlockStructureOptionsStorage.ShowOutliningForDeclarationLevelConstructs, LanguageNames.VisualBasic)
            BindToOption(Show_outlining_for_code_level_constructs, BlockStructureOptionsStorage.ShowOutliningForCodeLevelConstructs, LanguageNames.VisualBasic)
            BindToOption(Show_outlining_for_comments_and_preprocessor_regions, BlockStructureOptionsStorage.ShowOutliningForCommentsAndPreprocessorRegions, LanguageNames.VisualBasic)
            BindToOption(Collapse_regions_when_collapsing_to_definitions, BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.VisualBasic)

            ' Fading
            BindToOption(Fade_out_unused_imports, FadingOptions.FadeOutUnusedImports, LanguageNames.VisualBasic)

            ' Block structure guides
            BindToOption(Show_guides_for_declaration_level_constructs, BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.VisualBasic)
            BindToOption(Show_guides_for_code_level_constructs, BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.VisualBasic)
            BindToOption(Show_guides_for_comments_and_preprocessor_regions, BlockStructureOptionsStorage.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, LanguageNames.VisualBasic)

            ' Comments
            BindToOption(GenerateXmlDocCommentsForTripleApostrophes, DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, LanguageNames.VisualBasic)
            BindToOption(InsertApostropheAtTheStartOfNewLinesWhenWritingApostropheComments, SplitCommentOptionsStorage.Enabled, LanguageNames.VisualBasic)

            ' Editor help
            BindToOption(EnableEndConstruct, EndConstructGenerationOptionsStorage.EndConstruct, LanguageNames.VisualBasic)
            BindToOption(EnableLineCommit, LineCommitOptionsStorage.PrettyListing, LanguageNames.VisualBasic)
            BindToOption(AutomaticInsertionOfInterfaceAndMustOverrideMembers, AutomaticInsertionOfAbstractOrInterfaceMembersOptionsStorage.AutomaticInsertionOfAbstractOrInterfaceMembers, LanguageNames.VisualBasic)
            BindToOption(RenameTrackingPreview, RenameTrackingOptionsStorage.RenameTrackingPreview, LanguageNames.VisualBasic)
            BindToOption(ShowRemarksInQuickInfo, QuickInfoOptionsStorage.ShowRemarksInQuickInfo, LanguageNames.VisualBasic)
            BindToOption(Report_invalid_placeholders_in_string_dot_format_calls, FormatStringValidationOptionStorage.ReportInvalidPlaceholdersInStringDotFormatCalls, LanguageNames.VisualBasic)
            BindToOption(Underline_reassigned_variables, ClassificationOptionsStorage.ClassifyReassignedVariables, LanguageNames.VisualBasic)
            BindToOption(Strike_out_obsolete_symbols, ClassificationOptionsStorage.ClassifyObsoleteSymbols, LanguageNames.VisualBasic)

            ' Go To Definition
            BindToOption(NavigateToObjectBrowser, VisualStudioNavigationOptionsStorage.NavigateToObjectBrowser, LanguageNames.VisualBasic)

            ' Regular expressions
            BindToOption(Colorize_regular_expressions, ClassificationOptionsStorage.ColorizeRegexPatterns, LanguageNames.VisualBasic)
            BindToOption(Report_invalid_regular_expressions, RegexOptionsStorage.ReportInvalidRegexPatterns, LanguageNames.VisualBasic)
            BindToOption(Highlight_related_regular_expression_components_under_cursor, HighlightingOptionsStorage.HighlightRelatedRegexComponentsUnderCursor, LanguageNames.VisualBasic)
            BindToOption(Show_completion_list, CompletionOptionsStorage.ProvideRegexCompletions, LanguageNames.VisualBasic)

            BindToOption(Colorize_JSON_strings, ClassificationOptionsStorage.ColorizeJsonPatterns, LanguageNames.VisualBasic)
            BindToOption(Report_invalid_JSON_strings, JsonDetectionOptionsStorage.ReportInvalidJsonPatterns, LanguageNames.VisualBasic)
            BindToOption(Highlight_related_JSON_components_under_cursor, HighlightingOptionsStorage.HighlightRelatedJsonComponentsUnderCursor, LanguageNames.VisualBasic)

            ' Editor color scheme
            BindToOption(Editor_color_scheme, ColorSchemeOptionsStorage.ColorScheme)

            ' Implement Interface or Abstract Class
            BindToOption(with_other_members_of_the_same_kind, ImplementTypeOptionsStorage.InsertionBehavior, ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind, LanguageNames.VisualBasic)
            BindToOption(at_the_end, ImplementTypeOptionsStorage.InsertionBehavior, ImplementTypeInsertionBehavior.AtTheEnd, LanguageNames.VisualBasic)

            BindToOption(prefer_throwing_properties, ImplementTypeOptionsStorage.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferThrowingProperties, LanguageNames.VisualBasic)
            BindToOption(prefer_auto_properties, ImplementTypeOptionsStorage.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferAutoProperties, LanguageNames.VisualBasic)

            ' Inline hints
            BindToOption(DisplayAllHintsWhilePressingAltF1, InlineHintsViewOptionsStorage.DisplayAllHintsWhilePressingAltF1)
            BindToOption(ColorHints, InlineHintsViewOptionsStorage.ColorHints, LanguageNames.VisualBasic)

            BindToOption(DisplayInlineParameterNameHints, InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.VisualBasic)
            BindToOption(ShowHintsForLiterals, InlineHintsOptionsStorage.ForLiteralParameters, LanguageNames.VisualBasic)
            BindToOption(ShowHintsForNewExpressions, InlineHintsOptionsStorage.ForObjectCreationParameters, LanguageNames.VisualBasic)
            BindToOption(ShowHintsForEverythingElse, InlineHintsOptionsStorage.ForOtherParameters, LanguageNames.VisualBasic)
            BindToOption(ShowHintsForIndexers, InlineHintsOptionsStorage.ForIndexerParameters, LanguageNames.VisualBasic)
            BindToOption(SuppressHintsWhenParameterNameMatchesTheMethodsIntent, InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent, LanguageNames.VisualBasic)
            BindToOption(SuppressHintsWhenParameterNamesDifferOnlyBySuffix, InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix, LanguageNames.VisualBasic)
            BindToOption(SuppressHintsWhenParameterNamesMatchArgumentNames, InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName, LanguageNames.VisualBasic)

            ' Leave the null converter here to make sure if the option value is get from the storage (if it is null), the feature will be enabled
            BindToOption(ShowInheritanceMargin, InheritanceMarginOptionsStorage.ShowInheritanceMargin, LanguageNames.VisualBasic, Function() True)
            BindToOption(InheritanceMarginCombinedWithIndicatorMargin, InheritanceMarginOptionsStorage.InheritanceMarginCombinedWithIndicatorMargin)
            BindToOption(IncludeGlobalImports, InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports, LanguageNames.VisualBasic)

            ' Document Outline
            BindToOption(EnableDocumentOutline, DocumentOutlineOptionsStorage.EnableDocumentOutline,
                Function()
                    ' If the option has Not been set by the user, check if the option is disabled from experimentation.
                    ' If so, default to reflect that.
                    Return Not optionStore.GetOption(DocumentOutlineOptionsStorage.DisableDocumentOutlineFeatureFlag)
                End Function)
        End Sub

        ' Since this dialog is constructed once for the lifetime of the application and VS Theme can be changed after the application has started,
        ' we need to update the visibility of our combobox and warnings based on the current VS theme before being rendered.
        Friend Overrides Sub OnLoad()
            Dim cancellationToken = _threadingContext.DisposalToken
            Dim values = _threadingContext.JoinableTaskFactory.Run(
                Async Function()
                    Return (isSupportedTheme:=Await _colorSchemeApplier.IsSupportedThemeAsync(cancellationToken).ConfigureAwait(False),
                            isCustomized:=Await _colorSchemeApplier.IsThemeCustomizedAsync(cancellationToken).ConfigureAwait(False))
                End Function)

            Dim isSupportedTheme = values.isSupportedTheme
            Dim isCustomized = values.isCustomized
            Editor_color_scheme.IsEnabled = isSupportedTheme
            Customized_Theme_Warning.Visibility = If(isSupportedTheme AndAlso isCustomized, Visibility.Visible, Visibility.Collapsed)
            Custom_VS_Theme_Warning.Visibility = If(isSupportedTheme, Visibility.Collapsed, Visibility.Visible)

            UpdateInlineHintsOptions()

            MyBase.OnLoad()
        End Sub

        Private Sub UpdateInlineHintsOptions()
            Dim enabledForParameters = Me.OptionStore.GetOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.VisualBasic) <> False
            ShowHintsForLiterals.IsEnabled = enabledForParameters
            ShowHintsForNewExpressions.IsEnabled = enabledForParameters
            ShowHintsForEverythingElse.IsEnabled = enabledForParameters
            ShowHintsForIndexers.IsEnabled = enabledForParameters
            SuppressHintsWhenParameterNameMatchesTheMethodsIntent.IsEnabled = enabledForParameters
            SuppressHintsWhenParameterNamesDifferOnlyBySuffix.IsEnabled = enabledForParameters
            SuppressHintsWhenParameterNamesMatchArgumentNames.IsEnabled = enabledForParameters
        End Sub

        Private Sub DisplayInlineParameterNameHints_Checked()
            Me.OptionStore.SetOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.VisualBasic, True)
            UpdateInlineHintsOptions()
        End Sub

        Private Sub DisplayInlineParameterNameHints_Unchecked()
            Me.OptionStore.SetOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.VisualBasic, False)
            UpdateInlineHintsOptions()
        End Sub

        Private Sub EnableOutlining_Checked(sender As Object, e As RoutedEventArgs)
            Collapse_regions_on_file_open.IsEnabled = True
            Collapse_imports_on_file_open.IsEnabled = True
            Collapse_sourcelink_embedded_decompiled_files_on_open.IsEnabled = True
            Collapse_metadata_signature_files_on_open.IsEnabled = True
        End Sub

        Private Sub EnableOutlining_Unchecked(sender As Object, e As RoutedEventArgs)
            Collapse_regions_on_file_open.IsEnabled = False
            Collapse_imports_on_file_open.IsEnabled = False
            Collapse_sourcelink_embedded_decompiled_files_on_open.IsEnabled = False
            Collapse_metadata_signature_files_on_open.IsEnabled = False
        End Sub
    End Class
End Namespace
