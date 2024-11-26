// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.KeywordHighlighting;
using Microsoft.CodeAnalysis.LineSeparators;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.CodeAnalysis.StringCopyPaste;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.ValidateFormatString;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.UnifiedSettings
{
    public class CSharpAdvancedUnifiedSettingsTests : UnifiedSettingsTests
    {
        internal override ImmutableArray<(string unifiedSettingsPath, IOption2 roslynOption)> OnboardedOptions2
            => [("textEditor.csharp.advanced.analysis.analyzerDiagnosticsScope", SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption),
                ("textEditor.csharp.advanced.analysis.compilerDiagnosticsScope", SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption),
                ("textEditor.csharp.advanced.analysis.enableInlineDiagnostics", InlineDiagnosticsOptionsStorage.EnableInlineDiagnostics),
                ("textEditor.csharp.advanced.analysis.inlineDiagnosticsLocation", InlineDiagnosticsOptionsStorage.Location),
                ("textEditor.csharpAndVisualBasic.analysis.codeAnalysisInSeparateProcess", RemoteHostOptionsStorage.OOP64Bit),
                ("textEditor.csharpAndVisualBasic.analysis.reloadChangedAnalyzerReferences", WorkspaceConfigurationOptionsStorage.ReloadChangedAnalyzerReferences),
                ("textEditor.csharpAndVisualBasic.analysis.enableFileLoggingForDiagnostics", VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics),
                ("textEditor.csharpAndVisualBasic.analysis.skipAnalyzersForImplicitlyTriggeredBuilds", FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds),
                ("textEditor.csharpAndVisualBasic.analysis.offerRemoveUnusedReferences", FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds),
                ("textEditor.csharpAndVisualBasic.sourceGenerators.skipAnalyzersForImplicitlyTriggeredBuilds", WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution),
                ("textEditor.csharpAndVisualBasic.sourceGenerators.enableDiagnosticsInSourceGeneratedFiles", SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFiles),
                ("textEditor.csharpAndVisualBasic.goToDefinition.skipAnalyzersForImplicitlyTriggeredBuilds", MetadataAsSourceOptionsStorage.NavigateToSourceLinkAndEmbeddedSources),
                ("textEditor.csharpAndVisualBasic.goToDefinition.navigateToDecompiledSources", MetadataAsSourceOptionsStorage.NavigateToDecompiledSources),
                ("textEditor.csharpAndVisualBasic.goToDefinition.alwaysUseDefaultSymbolServer", MetadataAsSourceOptionsStorage.AlwaysUseDefaultSymbolServers),
                ("textEditor.csharpAndVisualBasic.rename.commitRenameAsynchronously", InlineRenameSessionOptionsStorage.CommitRenameAsynchronously),
                ("textEditor.csharpAndVisualBasic.rename.useInlineAdornment", InlineRenameUIOptionsStorage.UseInlineAdornment),
                ("textEditor.csharp.advanced.usingDirectives.sortSystemDirectivesFirst", GenerationOptions.PlaceSystemNamespaceFirst),
                ("textEditor.csharp.advanced.usingDirectives.separateImportDirectiveGroups", GenerationOptions.SeparateImportDirectiveGroups),
                ("textEditor.csharp.advanced.usingDirectives.searchReferenceAssemblies", SymbolSearchOptionsStorage.SearchReferenceAssemblies),
                ("textEditor.csharp.advanced.usingDirectives.unsupportedSearchNugetPackages", SymbolSearchOptionsStorage.SearchNuGetPackages),
                ("textEditor.csharp.advanced.usingDirectives.addImportsOnPaste", AddImportOnPasteOptionsStorage.AddImportsOnPaste),
                ("textEditor.csharp.advanced.highlighting.highlightReferences", ReferenceHighlightingOptionsStorage.ReferenceHighlighting),
                ("textEditor.csharp.advanced.highlighting.highlightKeywords", KeywordHighlightingOptionsStorage.KeywordHighlighting),
                ("textEditor.csharp.advanced.outlining.enterOutliningModeOnFileOpen", OutliningOptionsStorage.Outlining),
                ("textEditor.csharp.advanced.outlining.collapseRegionsWhenFirstOpened", BlockStructureOptionsStorage.CollapseRegionsWhenFirstOpened),
                ("textEditor.csharp.advanced.outlining.collapseImportsWhenFirstOpened", BlockStructureOptionsStorage.CollapseImportsWhenFirstOpened),
                ("textEditor.csharp.advanced.outlining.collapseMetadataImplementationsWhenFirstOpened", BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened),
                ("textEditor.csharp.advanced.outlining.collapseEmptyMetadataImplementationsWhenFirstOpened", BlockStructureOptionsStorage.CollapseMetadataSignatureFilesWhenFirstOpened),
                ("textEditor.csharp.advanced.outlining.displayLineSeparators", LineSeparatorsOptionsStorage.LineSeparator),
                ("textEditor.csharp.advanced.outlining.showOutliningForDeclarationLevelConstructs", BlockStructureOptionsStorage.ShowOutliningForDeclarationLevelConstructs),
                ("textEditor.csharp.advanced.outlining.showOutliningForCodeLevelConstructs", BlockStructureOptionsStorage.ShowOutliningForCodeLevelConstructs),
                ("textEditor.csharp.advanced.outlining.showOutliningForCommentsAndPreprocessorRegions", BlockStructureOptionsStorage.ShowOutliningForCommentsAndPreprocessorRegions),
                ("textEditor.csharp.advanced.outlining.collapseRegionsWhenCollapsingToDefinitions", BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions),
                ("textEditor.csharp.advanced.outlining.collapseLocalFunctionsWhenCollapsingToDefinitions", BlockStructureOptionsStorage.CollapseLocalFunctionsWhenCollapsingToDefinitions),
                ("textEditor.csharp.advanced.fading.fadeOutUnusedImports", FadingOptions.FadeOutUnusedImports),
                ("textEditor.csharp.advanced.fading.fadeOutUnreachableCode", FadingOptions.FadeOutUnreachableCode),
                ("textEditor.csharp.advanced.blockStructureGuides.showBlockStructureGuidesForDeclarationLevelConstructs", BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs),
                ("textEditor.csharp.advanced.blockStructureGuides.showBlockStructureGuidesForCodeLevelConstructs", BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs),
                ("textEditor.csharp.advanced.blockStructureGuides.showBlockStructureGuidesForCommentsAndPreprocessorRegions", BlockStructureOptionsStorage.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions),
                ("textEditor.csharp.advanced.comments.autoXmlDocCommentGeneration", DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration),
                ("textEditor.csharp.advanced.comments.splitComments", SplitCommentOptionsStorage.Enabled),
                ("textEditor.csharp.advanced.comments.insertBlockCommentStartString", BlockCommentEditingOptionsStorage.AutoInsertBlockCommentStartString),
                ("textEditor.csharp.advanced.editorHelp.splitStringLiteralOnReturn", SplitStringLiteralOptionsStorage.Enabled),
                ("textEditor.csharp.advanced.editorHelp.fixStringContentsOnPaste", StringCopyPasteOptionsStorage.AutomaticallyFixStringContentsOnPaste),
                ("textEditor.csharp.advanced.editorHelp.showRemarksInQuickInfo", QuickInfoOptionsStorage.ShowRemarksInQuickInfo),
                ("textEditor.csharp.advanced.editorHelp.showPreviewForRenameTracking", RenameTrackingOptionsStorage.RenameTrackingPreview),
                ("textEditor.csharp.advanced.editorHelp.UnsupportedReportInvalidPlaceholdersInStringDotFormatCalls", FormatStringValidationOptionStorage.ReportInvalidPlaceholdersInStringDotFormatCalls),
                ("textEditor.csharp.advanced.editorHelp.classifyReassignedVariables", ClassificationOptionsStorage.ClassifyReassignedVariables),
                ("textEditor.csharp.advanced.editorHelp.classifyObsoleteSymbols", ClassificationOptionsStorage.ClassifyObsoleteSymbols),
                ("textEditor.csharp.advanced.regularExpression.colorizeRegexPatterns", ClassificationOptionsStorage.ColorizeRegexPatterns),
                ("textEditor.csharp.advanced.regularExpression.unsupportedReportInvalidRegexPatterns", RegexOptionsStorage.ReportInvalidRegexPatterns),
                ("textEditor.csharp.advanced.regularExpression.highlightRelatedRegexComponents", HighlightingOptionsStorage.HighlightRelatedRegexComponentsUnderCursor),
                ("textEditor.csharp.advanced.regularExpression.provideRegexCompletions", CompletionOptionsStorage.ProvideRegexCompletions),
                ("textEditor.csharp.advanced.jsonStrings.colorizeJsonPatterns", ClassificationOptionsStorage.ColorizeJsonPatterns),
                ("textEditor.csharp.advanced.jsonStrings.unsupportedReportInvalidJsonPatterns", JsonDetectionOptionsStorage.ReportInvalidJsonPatterns),
                ("textEditor.csharp.advanced.jsonStrings.highlightRelatedJsonComponents", HighlightingOptionsStorage.HighlightRelatedJsonComponentsUnderCursor),
                ("textEditor.csharp.advanced.implementInterfaceOrAbstractClass.memberInsertionLocation", ImplementTypeOptionsStorage.InsertionBehavior),
                ("textEditor.csharp.advanced.implementInterfaceOrAbstractClass.propertyGenerationBehavior", ImplementTypeOptionsStorage.PropertyGenerationBehavior),
                ("textEditor.csharpAndVisualBasic.advanced.inlayHints.displayInlineHintsWhilePressingAltF1", InlineHintsViewOptionsStorage.DisplayAllHintsWhilePressingAltF1),
                ("textEditor.csharp.advanced.inlayHints.colorizeInlineHints", InlineHintsViewOptionsStorage.ColorHints),
                ("textEditor.csharp.advanced.inlayHints.enableInlayHintsForParameters", InlineHintsOptionsStorage.EnabledForParameters),
                ("textEditor.csharp.advanced.inlayHints.enableInlayHintsForLiteralParameters", InlineHintsOptionsStorage.ForLiteralParameters),
                ("textEditor.csharp.advanced.inlayHints.enableInlayHintsForObjectCreationParameters", InlineHintsOptionsStorage.ForObjectCreationParameters),
                ("textEditor.csharp.advanced.inlayHints.enableInlayHintsForOtherParameters", InlineHintsOptionsStorage.ForOtherParameters),
                ("textEditor.csharp.advanced.inlayHints.enableInlayHintsForIndexerParameters", InlineHintsOptionsStorage.ForIndexerParameters),
                ("textEditor.csharp.advanced.inlayHints.suppressInlayHintsForParametersThatMatchMethodIntent", InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent),
                ("textEditor.csharp.advanced.inlayHints.suppressInlayHintsForParametersThatDifferOnlyBySuffix", InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix),
                ("textEditor.csharp.advanced.inlayHints.suppressInlayHintsForParametersThatMatchArgumentName", InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName),
                ("textEditor.csharp.advanced.inlayHints.enableInlayHintsForTypes", InlineHintsOptionsStorage.EnabledForTypes),
                ("textEditor.csharp.advanced.inlayHints.enableInlayHintsForImplicitVariableTypes", InlineHintsOptionsStorage.ForImplicitVariableTypes),
                ("textEditor.csharp.advanced.inlayHints.enableInlayHintsForLambdaParameterTypes", InlineHintsOptionsStorage.ForLambdaParameterTypes),
                ("textEditor.csharp.advanced.inlayHints.enableInlayHintsForImplicitObjectCreation", InlineHintsOptionsStorage.ForImplicitObjectCreation),
                ("textEditor.csharp.advanced.inlayHints.enableInlayHintsForCollectionExpressions", InlineHintsOptionsStorage.ForCollectionExpressions),
                ("textEditor.csharp.advanced.inheritanceMargin.showInheritanceMargin", InheritanceMarginOptionsStorage.ShowInheritanceMargin),
                ("textEditor.csharp.advanced.inheritanceMargin.showGlobalImportsInInheritanceMargin", InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports),
                ("textEditor.csharp.advanced.stackTraceExplorer.openStackTraceExplorerOnFocus", StackTraceExplorerOptionsStorage.OpenOnFocus),
            ];

        internal override object[] GetEnumOptionValues(IOption2 option)
        {
            if (option.Equals(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption))
            {
                return [BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics,
                    BackgroundAnalysisScope.OpenFiles,
                    BackgroundAnalysisScope.FullSolution,
                    BackgroundAnalysisScope.Minimal];
            }

            if (option.Equals(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption))
            {
                return [CompilerDiagnosticsScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics,
                    CompilerDiagnosticsScope.OpenFiles,
                    CompilerDiagnosticsScope.FullSolution,
                    CompilerDiagnosticsScope.None];
            }

            return base.GetEnumOptionValues(option);
        }

        [Fact]
        public async Task AdvancedPageTest()
        {
            using var registrationFileStream = typeof(CSharpIntellisenseUnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.CSharp.UnitTests.csharpSettings.registration.json");
            using var reader = new StreamReader(registrationFileStream);
            var registrationFile = await reader.ReadToEndAsync().ConfigureAwait(false);
            var registrationJsonObject = JObject.Parse(registrationFile, new JsonLoadSettings() { CommentHandling = CommentHandling.Ignore });

            var optionPageId = registrationJsonObject.SelectToken("$.categories['textEditor.csharp.advanced'].legacyOptionPageId");
            Assert.Equal(Guids.CSharpOptionPageAdvancedIdString, optionPageId!.ToString());

            using var pkgdefFileStream = typeof(CSharpIntellisenseUnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.CSharp.UnitTests.PackageRegistration.pkgdef");
            using var pkgdefReader = new StreamReader(pkgdefFileStream);
            var pkgdefFile = await pkgdefReader.ReadToEndAsync().ConfigureAwait(false);
            TestUnifiedSettingsCategory(registrationJsonObject, categoryBasePath: "textEditor.csharp.advanced", languageName: LanguageNames.CSharp, pkgdefFile);
        }
    }
}
