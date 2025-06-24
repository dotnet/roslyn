// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
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
using Microsoft.VisualStudio.LanguageServices.DocumentOutline;
using Roslyn.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public class UnifiedSettingsTests
{
    #region CSharpTest
    /// <summary>
    /// Dictionary containing the option to unified setting path for C#.
    /// </summary>
    private static readonly ImmutableDictionary<IOption2, string> s_csharpUnifiedSettingsStorage = ImmutableDictionary<IOption2, string>.Empty.
        // Intellisense page
        Add(CompletionOptionsStorage.TriggerOnTypingLetters, "textEditor.csharp.intellisense.triggerCompletionOnTypingLetters").
        Add(CompletionOptionsStorage.TriggerOnDeletion, "textEditor.csharp.intellisense.triggerCompletionOnDeletion").
        Add(CompletionOptionsStorage.TriggerInArgumentLists, "textEditor.csharp.intellisense.triggerCompletionInArgumentLists").
        Add(CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, "textEditor.csharp.intellisense.highlightMatchingPortionsOfCompletionListItems").
        Add(CompletionViewOptionsStorage.ShowCompletionItemFilters, "textEditor.csharp.intellisense.showCompletionItemFilters").
        Add(CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon, "textEditor.csharp.intellisense.completeStatementOnSemicolon").
        Add(CompletionOptionsStorage.SnippetsBehavior, "textEditor.csharp.intellisense.snippetsBehavior").
        Add(CompletionOptionsStorage.EnterKeyBehavior, "textEditor.csharp.intellisense.returnKeyCompletionBehavior").
        Add(CompletionOptionsStorage.ShowNameSuggestions, "textEditor.csharp.intellisense.showNameCompletionSuggestions").
        Add(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, "textEditor.csharp.intellisense.showCompletionItemsFromUnimportedNamespaces").
        Add(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, "textEditor.csharp.intellisense.enableArgumentCompletionSnippets").
        Add(CompletionOptionsStorage.ShowNewSnippetExperienceUserOption, "textEditor.csharp.intellisense.showNewSnippetExperience").
        // Advanced page
        Add(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, "textEditor.csharp.advanced.analysis.analyzerDiagnosticsScope").
        Add(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, "textEditor.csharp.advanced.analysis.compilerDiagnosticsScope").
        Add(InlineDiagnosticsOptionsStorage.EnableInlineDiagnostics, "textEditor.csharp.advanced.analysis.enableInlineDiagnostics").
        Add(InlineDiagnosticsOptionsStorage.Location, "textEditor.csharp.advanced.analysis.inlineDiagnosticsLocation").
        Add(RemoteHostOptionsStorage.OOP64Bit, "textEditor.csharpAndVisualBasic.analysis.codeAnalysisInSeparateProcess").
        Add(WorkspaceConfigurationOptionsStorage.ReloadChangedAnalyzerReferences, "textEditor.csharpAndVisualBasic.analysis.reloadChangedAnalyzerReferences").
        Add(FeatureOnOffOptions.OfferRemoveUnusedReferences, "textEditor.csharpAndVisualBasic.analysis.offerRemoveUnusedReferences").
        Add(VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics, "textEditor.csharpAndVisualBasic.analysis.enableFileLoggingForDiagnostics").
        Add(FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds, "textEditor.csharpAndVisualBasic.analysis.skipAnalyzersForImplicitlyTriggeredBuilds").
        Add(WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution, "textEditor.csharpAndVisualBasic.sourceGenerators.sourceGeneratorExecution").
        Add(MetadataAsSourceOptionsStorage.NavigateToSourceLinkAndEmbeddedSources, "textEditor.csharpAndVisualBasic.goToDefinition.skipAnalyzersForImplicitlyTriggeredBuilds").
        Add(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources, "textEditor.csharpAndVisualBasic.goToDefinition.navigateToDecompiledSources").
        Add(MetadataAsSourceOptionsStorage.AlwaysUseDefaultSymbolServers, "textEditor.csharpAndVisualBasic.goToDefinition.alwaysUseDefaultSymbolServer").
        Add(SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFiles, "textEditor.csharpAndVisualBasic.sourceGenerators.enableDiagnosticsInSourceGeneratedFiles").
        Add(InlineRenameSessionOptionsStorage.CommitRenameAsynchronously, "textEditor.csharpAndVisualBasic.rename.commitRenameAsynchronously").
        Add(InlineRenameUIOptionsStorage.UseInlineAdornment, "textEditor.csharpAndVisualBasic.rename.useInlineAdornment").
        Add(GenerationOptions.PlaceSystemNamespaceFirst, "textEditor.csharp.advanced.usingDirectives.sortSystemDirectivesFirst").
        Add(GenerationOptions.SeparateImportDirectiveGroups, "textEditor.csharp.advanced.usingDirectives.separateImportDirectiveGroups").
        Add(SymbolSearchOptionsStorage.SearchReferenceAssemblies, "textEditor.csharp.advanced.usingDirectives.searchReferenceAssemblies").
        Add(SymbolSearchOptionsStorage.SearchNuGetPackages, "textEditor.csharp.advanced.usingDirectives.unsupportedSearchNugetPackages").
        Add(AddImportOnPasteOptionsStorage.AddImportsOnPaste, "textEditor.csharp.advanced.usingDirectives.addImportsOnPaste").
        Add(ReferenceHighlightingOptionsStorage.ReferenceHighlighting, "textEditor.csharp.advanced.highlighting.highlightReferences").
        Add(KeywordHighlightingOptionsStorage.KeywordHighlighting, "textEditor.csharp.advanced.highlighting.highlightKeywords").
        Add(OutliningOptionsStorage.Outlining, "textEditor.csharp.advanced.outlining.enterOutliningModeOnFileOpen").
        Add(BlockStructureOptionsStorage.CollapseRegionsWhenFirstOpened, "textEditor.csharp.advanced.outlining.collapseRegionsWhenFirstOpened").
        Add(BlockStructureOptionsStorage.CollapseImportsWhenFirstOpened, "textEditor.csharp.advanced.outlining.collapseImportsWhenFirstOpened").
        Add(BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, "textEditor.csharp.advanced.outlining.collapseMetadataImplementationsWhenFirstOpened").
        Add(BlockStructureOptionsStorage.CollapseMetadataSignatureFilesWhenFirstOpened, "textEditor.csharp.advanced.outlining.collapseEmptyMetadataImplementationsWhenFirstOpened").
        Add(LineSeparatorsOptionsStorage.LineSeparator, "textEditor.csharp.advanced.outlining.displayLineSeparators").
        Add(BlockStructureOptionsStorage.ShowOutliningForDeclarationLevelConstructs, "textEditor.csharp.advanced.outlining.showOutliningForDeclarationLevelConstructs").
        Add(BlockStructureOptionsStorage.ShowOutliningForCodeLevelConstructs, "textEditor.csharp.advanced.outlining.showOutliningForCodeLevelConstructs").
        Add(BlockStructureOptionsStorage.ShowOutliningForCommentsAndPreprocessorRegions, "textEditor.csharp.advanced.outlining.showOutliningForCommentsAndPreprocessorRegions").
        Add(BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, "textEditor.csharp.advanced.outlining.collapseRegionsWhenCollapsingToDefinitions").
        Add(BlockStructureOptionsStorage.CollapseLocalFunctionsWhenCollapsingToDefinitions, "textEditor.csharp.advanced.outlining.collapseLocalFunctionsWhenCollapsingToDefinitions").
        Add(FadingOptions.FadeOutUnusedImports, "textEditor.csharp.advanced.fading.fadeOutUnusedImports").
        Add(FadingOptions.FadeOutUnreachableCode, "textEditor.csharp.advanced.fading.fadeOutUnreachableCode").
        Add(BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, "BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs").
        Add(BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, "textEditor.csharp.advanced.blockStructureGuides.showBlockStructureGuidesForCodeLevelConstructs").
        Add(BlockStructureOptionsStorage.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, "textEditor.csharp.advanced.blockStructureGuides.showBlockStructureGuidesForCommentsAndPreprocessorRegions").
        Add(DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, "textEditor.csharp.advanced.comments.autoXmlDocCommentGeneration").
        Add(SplitCommentOptionsStorage.Enabled, "textEditor.csharp.advanced.comments.splitComments").
        Add(BlockCommentEditingOptionsStorage.AutoInsertBlockCommentStartString, "textEditor.csharp.advanced.comments.insertBlockCommentStartString").
        Add(StringCopyPasteOptionsStorage.AutomaticallyFixStringContentsOnPaste, "textEditor.csharp.advanced.editorHelp.fixStringContentsOnPaste").
        Add(SplitStringLiteralOptionsStorage.Enabled, "textEditor.csharp.advanced.editorHelp.splitStringLiteralOnReturn").
        Add(QuickInfoOptionsStorage.ShowRemarksInQuickInfo, "textEditor.csharp.advanced.editorHelp.showRemarksInQuickInfo").
        Add(RenameTrackingOptionsStorage.RenameTrackingPreview, "textEditor.csharp.advanced.editorHelp.showPreviewForRenameTracking").
        Add(FormatStringValidationOptionStorage.ReportInvalidPlaceholdersInStringDotFormatCalls, "textEditor.csharp.advanced.editorHelp.UnsupportedReportInvalidPlaceholdersInStringDotFormatCalls").
        Add(ClassificationOptionsStorage.ClassifyReassignedVariables, "textEditor.csharp.advanced.editorHelp.classifyReassignedVariables").
        Add(ClassificationOptionsStorage.ClassifyObsoleteSymbols, "textEditor.csharp.advanced.editorHelp.classifyObsoleteSymbols").
        Add(ClassificationOptionsStorage.ColorizeRegexPatterns, "textEditor.csharp.advanced.regularExpression.colorizeRegexPatterns").
        Add(RegexOptionsStorage.ReportInvalidRegexPatterns, "textEditor.csharp.advanced.regularExpression.unsupportedReportInvalidRegexPatterns").
        Add(HighlightingOptionsStorage.HighlightRelatedRegexComponentsUnderCursor, "textEditor.csharp.advanced.regularExpression.highlightRelatedRegexComponents").
        Add(CompletionOptionsStorage.ProvideRegexCompletions, "textEditor.csharp.advanced.regularExpression.provideRegexCompletions").
        Add(ClassificationOptionsStorage.ColorizeJsonPatterns, "textEditor.csharp.advanced.jsonStrings.colorizeJsonPatterns").
        Add(JsonDetectionOptionsStorage.ReportInvalidJsonPatterns, "textEditor.csharp.advanced.jsonStrings.unsupportedReportInvalidJsonPatterns").
        Add(HighlightingOptionsStorage.HighlightRelatedJsonComponentsUnderCursor, "textEditor.csharp.advanced.jsonStrings.highlightRelatedJsonComponents").
        Add(ColorSchemeOptionsStorage.ColorScheme, "textEditor.csharpAndVisualBasic.advanced.editorColorScheme.visualStudioColorSchemeName").
        Add(ImplementTypeOptionsStorage.InsertionBehavior, "textEditor.csharp.advanced.implementInterfaceOrAbstractClass.memberInsertionLocation").
        Add(ImplementTypeOptionsStorage.PropertyGenerationBehavior, "textEditor.csharp.advanced.implementInterfaceOrAbstractClass.propertyGenerationBehavior").
        Add(InlineHintsViewOptionsStorage.DisplayAllHintsWhilePressingAltF1, "textEditor.csharpAndVisualBasic.advanced.inlineHints.displayInlineHintsWhilePressingAltF1").
        Add(InlineHintsViewOptionsStorage.ColorHints, "textEditor.csharp.advanced.inlineHints.colorizeInlineHints").
        Add(InlineHintsOptionsStorage.EnabledForParameters, "textEditor.csharp.advanced.inlineHints.enableInlayHintsForParameters").
        Add(InlineHintsOptionsStorage.ForLiteralParameters, "textEditor.csharp.advanced.inlineHints.enableInlayHintsForLiteralParameters").
        Add(InlineHintsOptionsStorage.ForObjectCreationParameters, "textEditor.csharp.advanced.inlineHints.enableInlayHintsForObjectCreationParameters").
        Add(InlineHintsOptionsStorage.ForOtherParameters, "textEditor.csharp.advanced.inlineHints.enableInlayHintsForOtherParameters").
        Add(InlineHintsOptionsStorage.ForIndexerParameters, "textEditor.csharp.advanced.inlineHints.enableInlayHintsForIndexerParameters").
        Add(InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent, "textEditor.csharp.advanced.inlineHints.suppressInlayHintsForParametersThatMatchMethodIntent").
        Add(InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix, "textEditor.csharp.advanced.inlineHints.suppressInlayHintsForParametersThatDifferOnlyBySuffix").
        Add(InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName, "textEditor.csharp.advanced.inlineHints.suppressInlayHintsForParametersThatMatchArgumentName").
        Add(InlineHintsOptionsStorage.EnabledForTypes, "textEditor.csharp.advanced.inlineHints.enableInlayHintsForTypes").
        Add(InlineHintsOptionsStorage.ForImplicitVariableTypes, "textEditor.csharp.advanced.inlineHints.enableInlayHintsForImplicitVariableTypes").
        Add(InlineHintsOptionsStorage.ForLambdaParameterTypes, "textEditor.csharp.advanced.inlineHints.enableInlayHintsForLambdaParameterTypes").
        Add(InlineHintsOptionsStorage.ForImplicitObjectCreation, "textEditor.csharp.advanced.inlineHints.enableInlayHintsForImplicitObjectCreation").
        Add(InlineHintsOptionsStorage.ForCollectionExpressions, "textEditor.csharp.advanced.inlineHints.enableInlayHintsForCollectionExpressions").
        Add(InheritanceMarginOptionsStorage.ShowInheritanceMargin, "textEditor.csharp.advanced.inheritanceMargin.showInheritanceMargin").
        Add(InheritanceMarginOptionsStorage.InheritanceMarginCombinedWithIndicatorMargin, "textEditor.csharpAndVisualBasic.advanced.inheritanceMargin.combineInheritanceAndIndicatorMargins").
        Add(InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports, "InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports").
        Add(StackTraceExplorerOptionsStorage.OpenOnFocus, "textEditor.csharp.advanced.stackTraceExplorer.openStackTraceExplorerOnFocus").
        Add(DocumentOutlineOptionsStorage.EnableDocumentOutline, "textEditor.csharpAndVisualBasic.advanced.documentOutline.enableDocumentOutline");

    // TODO: add test data
    private static readonly ImmutableArray<(IOption2, UnifiedSettingBase)> s_csharpAdvancedExpectedSettings = [
        (SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, CreateEnumOption<BackgroundAnalysisScope>(
            SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption,
            title: "Run background code analysis for",
            order: 0,
            enumLabels: ["None", "Current document", "Open document", "Entire solution"],
            languageName: LanguageNames.CSharp)),
    ];

    /// <summary>
    /// Array containing the option to expected unified settings for C# intellisense page.
    /// </summary>
    private static readonly ImmutableArray<(IOption2, UnifiedSettingBase)> s_csharpIntellisenseExpectedSettings =
    [
        (CompletionOptionsStorage.TriggerOnTypingLetters, CreateBooleanOption(
            CompletionOptionsStorage.TriggerOnTypingLetters,
            title: "Show completion list after a character is typed",
            order: 0,
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.TriggerOnDeletion, CreateBooleanOption(
            CompletionOptionsStorage.TriggerOnDeletion,
            title: "Show completion list after a character is deleted",
            order: 1,
            customDefaultValue: false,
            enableWhenOptionAndValue: (enableWhenOption: CompletionOptionsStorage.TriggerOnTypingLetters, whenValue: true),
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.TriggerInArgumentLists, CreateBooleanOption(
            CompletionOptionsStorage.TriggerInArgumentLists,
            title: "Automatically show completion list in argument lists",
            order: 10,
            languageName: LanguageNames.CSharp)),
        (CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, CreateBooleanOption(
            CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems,
            "Highlight matching portions of completion list items",
            order: 20,
            languageName: LanguageNames.CSharp)),
        (CompletionViewOptionsStorage.ShowCompletionItemFilters, CreateBooleanOption(
            CompletionViewOptionsStorage.ShowCompletionItemFilters,
            title: "Show completion item filters",
            order: 30,
            languageName: LanguageNames.CSharp)),
        (CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon, CreateBooleanOption(
            CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon,
            title: "Automatically complete statement on semicolon",
            order: 40,
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.SnippetsBehavior, CreateEnumOption(
            CompletionOptionsStorage.SnippetsBehavior,
            "Snippets behavior",
            order: 50,
            customDefaultValue: SnippetsRule.AlwaysInclude,
            enumLabels: ["Never include snippets", "Always include snippets", "Include snippets when ?-Tab is typed after an identifier"],
            enumValues: [SnippetsRule.NeverInclude, SnippetsRule.AlwaysInclude, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab],
            customMaps: [new Map { Result = "neverInclude", Match = 1 }, new Map { Result = "alwaysInclude", Match = 2 }, new Map { Result = "alwaysInclude", Match = 0 }, new Map { Result = "includeAfterTypingIdentifierQuestionTab", Match = 3 }],
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.EnterKeyBehavior, CreateEnumOption(
            CompletionOptionsStorage.EnterKeyBehavior,
            "Enter key behavior",
            order: 60,
            customDefaultValue: EnterKeyRule.Never,
            enumLabels: ["Never add new line on enter", "Only add new line on enter after end of fully typed word", "Always add new line on enter"],
            enumValues: [EnterKeyRule.Never, EnterKeyRule.AfterFullyTypedWord, EnterKeyRule.Always],
            customMaps: [new Map { Result = "never", Match = 1 }, new Map { Result = "never", Match = 0 }, new Map { Result = "always", Match = 2}, new Map { Result = "afterFullyTypedWord", Match = 3 }],
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.ShowNameSuggestions, CreateBooleanOption(
            CompletionOptionsStorage.ShowNameSuggestions,
            title: "Show name suggestions",
            order: 70,
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, CreateBooleanOption(
            CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
            title: "Show items from unimported namespaces",
            order: 80,
            languageName: LanguageNames.CSharp)),
        (CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, CreateBooleanOption(
            CompletionViewOptionsStorage.EnableArgumentCompletionSnippets,
            title: "Tab twice to insert arguments",
            customDefaultValue: false,
            order: 90,
            languageName: LanguageNames.CSharp,
            message: "Experimental feature")),
        (CompletionOptionsStorage.ShowNewSnippetExperienceUserOption, CreateBooleanOption(
            CompletionOptionsStorage.ShowNewSnippetExperienceUserOption,
            title: "Show new snippet experience",
            customDefaultValue: false,
            order: 100,
            languageName: LanguageNames.CSharp,
            featureFlagAndExperimentValue: (CompletionOptionsStorage.ShowNewSnippetExperienceFeatureFlag, true),
            message: "Experimental feature")),
    ];

    [Fact]
    public async Task CSharpCategoriesTest()
    {
        using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.csharpSettings.registration.json");
        var jsonDocument = await JsonNode.ParseAsync(registrationFileStream!, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var categories = jsonDocument!.Root["categories"]!.AsObject();
        var propertyToCategory = categories.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Deserialize<Category>());
        Assert.Equal(22, propertyToCategory.Count);
        Assert.Equal("C#", propertyToCategory["textEditor.csharp"]!.Title);
        Assert.Equal("IntelliSense", propertyToCategory["textEditor.csharp.intellisense"]!.Title);
        Assert.Equal(Guids.CSharpOptionPageIntelliSenseIdString, propertyToCategory["textEditor.csharp.intellisense"]!.LegacyOptionPageId);

        Assert.Equal("Advanced", propertyToCategory["textEditor.csharp.advanced"]!.Title);
        Assert.Equal(Guids.CSharpOptionPageAdvancedIdString, propertyToCategory["textEditor.csharp.advanced"]!.LegacyOptionPageId);

        Assert.Equal("Analysis", propertyToCategory["textEditor.csharp.advanced.analysis"]!.Title);
        Assert.Equal("Source Generators", propertyToCategory["textEditor.csharp.advanced.sourceGenerators"]!.Title);
        Assert.Equal("Go To Definition", propertyToCategory["textEditor.csharp.advanced.goToDefinition"]!.Title);
        Assert.Equal("Rename", propertyToCategory["textEditor.csharp.advanced.rename"]!.Title);
        Assert.Equal("Using Directives", propertyToCategory["textEditor.csharp.advanced.usingDirectives"]!.Title);
        Assert.Equal("Highlighting", propertyToCategory["textEditor.csharp.advanced.highlighting"]!.Title);
        Assert.Equal("Outlining", propertyToCategory["textEditor.csharp.advanced.outlining"]!.Title);
        Assert.Equal("Fading", propertyToCategory["textEditor.csharp.advanced.fading"]!.Title);
        Assert.Equal("Block Structure Guides", propertyToCategory["textEditor.csharp.advanced.blockStructureGuides"]!.Title);
        Assert.Equal("Comments", propertyToCategory["textEditor.csharp.advanced.comments"]!.Title);
        Assert.Equal("Editor Help", propertyToCategory["textEditor.csharp.advanced.editorHelp"]!.Title);
        Assert.Equal("Regular Expressions", propertyToCategory["textEditor.csharp.advanced.regularExpressions"]!.Title);
        Assert.Equal("JSON strings", propertyToCategory["textEditor.csharp.advanced.jsonStrings"]!.Title);
        Assert.Equal("Editor Color Scheme", propertyToCategory["textEditor.csharp.advanced.editorColorScheme"]!.Title);
        Assert.Equal("Implement Interface or Abstract Class", propertyToCategory["textEditor.csharp.advanced.implementInterfaceOrAbstractClass"]!.Title);
        Assert.Equal("Inline Hints", propertyToCategory["textEditor.csharp.advanced.inlineHints"]!.Title);
        Assert.Equal("Inheritance Margin", propertyToCategory["textEditor.csharp.advanced.inheritanceMargin"]!.Title);
        Assert.Equal("Stack Trace Explorer", propertyToCategory["textEditor.csharp.advanced.stackTraceExplorer"]!.Title);
        Assert.Equal("Document Outline", propertyToCategory["textEditor.csharp.advanced.documentOutline"]!.Title);

        await VerifyTagAsync(jsonDocument.ToString(), "Roslyn.VisualStudio.Next.UnitTests.csharpPackageRegistration.pkgdef");
    }

    [Fact]
    public async Task CSharpIntellisensePageTest()
    {
        using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.csharpSettings.registration.json");
        var jsonDocument = await JsonNode.ParseAsync(registrationFileStream!, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        foreach (var (option, _) in s_csharpIntellisenseExpectedSettings)
        {
            Assert.True(s_csharpUnifiedSettingsStorage.ContainsKey(option));
        }

        VerifyProperties(jsonDocument!, ["textEditor.csharp.intellisense"], s_csharpIntellisenseExpectedSettings);
        await VerifyTagAsync(jsonDocument!.ToString(), "Roslyn.VisualStudio.Next.UnitTests.csharpPackageRegistration.pkgdef");
    }

    [Fact]
    public async Task CSharpAdvancedPageTest()
    {
        using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.csharpSettings.registration.json");
        var jsonDocument = await JsonNode.ParseAsync(registrationFileStream!, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        foreach (var (option, _) in s_csharpAdvancedExpectedSettings)
        {
            Assert.True(s_csharpUnifiedSettingsStorage.ContainsKey(option));
        }

        VerifyProperties(jsonDocument!, ["textEditor.csharp.advanced", "textEditor.csharpAndVisualBasic.advanced"], s_csharpAdvancedExpectedSettings);
        await VerifyTagAsync(jsonDocument!.ToString(), "Roslyn.VisualStudio.Next.UnitTests.csharpPackageRegistration.pkgdef");
    }

    #endregion

    #region VisualBasicTest
    /// <summary>
    /// Dictionary containing the option to unified setting path for VB.
    /// </summary>
    private static readonly ImmutableDictionary<IOption2, string> s_visualBasicUnifiedSettingsStorage = ImmutableDictionary<IOption2, string>.Empty.
        Add(CompletionOptionsStorage.TriggerOnTypingLetters, "textEditor.basic.intellisense.triggerCompletionOnTypingLetters").
        Add(CompletionOptionsStorage.TriggerOnDeletion, "textEditor.basic.intellisense.triggerCompletionOnDeletion").
        Add(CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, "textEditor.basic.intellisense.highlightMatchingPortionsOfCompletionListItems").
        Add(CompletionViewOptionsStorage.ShowCompletionItemFilters, "textEditor.basic.intellisense.showCompletionItemFilters").
        Add(CompletionOptionsStorage.SnippetsBehavior, "textEditor.basic.intellisense.snippetsBehavior").
        Add(CompletionOptionsStorage.EnterKeyBehavior, "textEditor.basic.intellisense.returnKeyCompletionBehavior").
        Add(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, "textEditor.basic.intellisense.showCompletionItemsFromUnimportedNamespaces").
        Add(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, "textEditor.basic.intellisense.enableArgumentCompletionSnippets");

    /// <summary>
    /// Array containing the option to expected unified settings for VB intellisense page.
    /// </summary>
    private static readonly ImmutableArray<(IOption2, UnifiedSettingBase)> s_visualBasicIntellisenseExpectedSettings =
    [
        (CompletionOptionsStorage.TriggerOnTypingLetters, CreateBooleanOption(
            CompletionOptionsStorage.TriggerOnTypingLetters,
            title: "Show completion list after a character is typed",
            order: 0,
            languageName: LanguageNames.VisualBasic)),
        (CompletionOptionsStorage.TriggerOnDeletion, CreateBooleanOption(
            CompletionOptionsStorage.TriggerOnDeletion,
            title: "Show completion list after a character is deleted",
            order: 1,
            customDefaultValue: true,
            languageName: LanguageNames.VisualBasic)),
        (CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, CreateBooleanOption(
            CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems,
            "Highlight matching portions of completion list items",
            order: 10,
            languageName: LanguageNames.VisualBasic)),
        (CompletionViewOptionsStorage.ShowCompletionItemFilters, CreateBooleanOption(
            CompletionViewOptionsStorage.ShowCompletionItemFilters,
            title: "Show completion item filters",
            order: 20,
            languageName: LanguageNames.VisualBasic)),
        (CompletionOptionsStorage.SnippetsBehavior, CreateEnumOption(
            CompletionOptionsStorage.SnippetsBehavior,
            "Snippets behavior",
            order: 30,
            customDefaultValue: SnippetsRule.IncludeAfterTypingIdentifierQuestionTab,
            enumLabels: ["Never include snippets", "Always include snippets", "Include snippets when ?-Tab is typed after an identifier"],
            enumValues: [SnippetsRule.NeverInclude, SnippetsRule.AlwaysInclude, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab],
            customMaps: [new Map { Result = "neverInclude", Match = 1 }, new Map { Result = "alwaysInclude", Match = 2 }, new Map { Result = "includeAfterTypingIdentifierQuestionTab", Match = 3 }, new Map { Result = "includeAfterTypingIdentifierQuestionTab", Match = 0 }],
            languageName: LanguageNames.VisualBasic)),
        (CompletionOptionsStorage.EnterKeyBehavior, CreateEnumOption(
            CompletionOptionsStorage.EnterKeyBehavior,
            "Enter key behavior",
            order: 40,
            customDefaultValue: EnterKeyRule.Always,
            enumLabels: ["Never add new line on enter", "Only add new line on enter after end of fully typed word", "Always add new line on enter"],
            enumValues: [EnterKeyRule.Never, EnterKeyRule.AfterFullyTypedWord, EnterKeyRule.Always],
            customMaps: [new Map { Result = "never", Match = 1}, new Map { Result = "always", Match = 2}, new Map { Result = "always", Match = 0}, new Map { Result = "afterFullyTypedWord", Match = 3 }],
            languageName: LanguageNames.VisualBasic)),
        (CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, CreateBooleanOption(
            CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
            title: "Show items from unimported namespaces",
            order: 50,
            languageName: LanguageNames.VisualBasic)),
        (CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, CreateBooleanOption(
            CompletionViewOptionsStorage.EnableArgumentCompletionSnippets,
            title: "Tab twice to insert arguments",
            customDefaultValue: false,
            order: 60,
            languageName: LanguageNames.VisualBasic,
            message: "Experimental feature")),
    ];

    [Fact]
    public async Task VisualBasicCategoriesTest()
    {
        using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.visualBasicSettings.registration.json");
        var jsonDocument = await JsonNode.ParseAsync(registrationFileStream!, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var categories = jsonDocument!.Root["categories"]!.AsObject();
        var propertyToCategory = categories.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Deserialize<Category>());
        Assert.Equal(2, propertyToCategory.Count);
        Assert.Equal("Visual Basic", propertyToCategory["textEditor.basic"]!.Title);
        Assert.Equal("IntelliSense", propertyToCategory["textEditor.basic.intellisense"]!.Title);
        Assert.Equal(Guids.VisualBasicOptionPageIntelliSenseIdString, propertyToCategory["textEditor.basic.intellisense"]!.LegacyOptionPageId);
        await VerifyTagAsync(jsonDocument.ToString(), "Roslyn.VisualStudio.Next.UnitTests.visualBasicPackageRegistration.pkgdef");
    }

    [Fact]
    public async Task VisualBasicIntellisenseTest()
    {
        using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.visualBasicSettings.registration.json");
        var jsonDocument = await JsonNode.ParseAsync(registrationFileStream!, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        foreach (var (option, _) in s_visualBasicIntellisenseExpectedSettings)
        {
            Assert.True(s_visualBasicUnifiedSettingsStorage.ContainsKey(option));
        }

        VerifyProperties(jsonDocument!, ["textEditor.basic.intellisense"], s_visualBasicIntellisenseExpectedSettings);
        await VerifyTagAsync(jsonDocument!.ToString(), "Roslyn.VisualStudio.Next.UnitTests.visualBasicPackageRegistration.pkgdef");
    }

    private static void VerifyProperties(JsonNode jsonDocument, string[] prefixes, ImmutableArray<(IOption2, UnifiedSettingBase)> expectedOptionToSettings)
    {
        var properties = jsonDocument.Root["properties"]!.AsObject()
            .Where(jsonObject => prefixes.Any(prefix => jsonObject.Key.StartsWith(prefix)))
            .SelectAsArray(jsonObject => jsonObject.Value);
        Assert.Equal(expectedOptionToSettings.Length, properties.Length);
        foreach (var (actualJson, (expectedOption, expectedSetting)) in properties.Zip(expectedOptionToSettings, (actual, expected) => (actual, expected)))
        {
            // We only have bool and enum option now.
            UnifiedSettingBase actualSetting = expectedOption.Definition.Type.IsEnum
                ? actualJson.Deserialize<UnifiedSettingsEnumOption>()!
                : actualJson.Deserialize<UnifiedSettingsOption<bool>>()!;
            Assert.Equal(expectedSetting, actualSetting);
        }
    }

    #endregion

    #region Helpers

    private static async Task VerifyTagAsync(string registrationFile, string pkgdefFileName)
    {
        using var pkgDefFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream(pkgdefFileName);
        using var streamReader = new StreamReader(pkgDefFileStream!);
        var pkgdefFile = await streamReader.ReadToEndAsync();

        var fileBytes = Encoding.ASCII.GetBytes(registrationFile);
        var expectedTags = BitConverter.ToInt64([.. XxHash128.Hash(fileBytes).Take(8)], 0).ToString("X16");
        var regex = new Regex("""
                              "CacheTag"=qword:\w{16}
                              """);
        var match = regex.Match(pkgdefFile, 0).Value;
        var actualTag = match[^16..];
        // Please change the CacheTag value in pkddefFile when you modify the registration file.
        Assert.Equal(expectedTags, actualTag);
    }

    private static UnifiedSettingsOption<bool> CreateBooleanOption(
        IOption2 onboardedOption,
        string title,
        int order,
        string languageName,
        bool? customDefaultValue = null,
        (IOption2 featureFlagOption, bool value)? featureFlagAndExperimentValue = null,
        (IOption2 enableWhenOption, object whenValue)? enableWhenOptionAndValue = null,
        string? message = null)
    {
        var migration = new Migration { Pass = new Pass { Input = new Input(onboardedOption, languageName) } };
        var type = onboardedOption.Definition.Type;
        // If the option's type is nullable type, like bool?, we use bool in the registration file.
        var underlyingType = Nullable.GetUnderlyingType(type);
        var nonNullableType = underlyingType ?? type;

        var alternativeDefault = featureFlagAndExperimentValue is not null
            ? new AlternativeDefault<bool>(featureFlagAndExperimentValue.Value.featureFlagOption, featureFlagAndExperimentValue.Value.value)
            : null;

        var enableWhen = enableWhenOptionAndValue is not null
            ? $"${{config:{GetUnifiedSettingsOptionValue(enableWhenOptionAndValue.Value.enableWhenOption, languageName)}}}=='{enableWhenOptionAndValue.Value.whenValue.ToString().ToCamelCase()}'"
            : null;

        var expectedDefault = customDefaultValue ?? onboardedOption.Definition.DefaultValue;
        // If the option default value is null, it means the option is in experiment mode and is hidden by a feature flag.
        // In Unified Settings it is not allowed and should be replaced by using the alternative default.
        // Like:
        //     "textEditor.csharp.intellisense.showNewSnippetExperience": {
        //         "type": "boolean",
        //         "default": false,
        //         "alternateDefault": {
        //             "flagName": "Roslyn.SnippetCompletion",
        //             "default": true
        //         }
        //      }
        // so please specify a non-null default value.
        Assert.NotNull(expectedDefault);

        return new UnifiedSettingsOption<bool>
        {
            Title = title,
            Type = nonNullableType.Name.ToCamelCase(),
            Order = order,
            EnableWhen = enableWhen,
            Migration = migration,
            AlternateDefault = alternativeDefault,
            Default = (bool)expectedDefault,
            Messages = message is null ? null : [new Message { Text = message }],
        };
    }

    private static UnifiedSettingsEnumOption CreateEnumOption<T>(
        IOption2 onboardedOption,
        string title,
        int order,
        string[] enumLabels,
        string languageName,
        T? customDefaultValue = default,
        T[]? enumValues = null,
        Map[]? customMaps = null,
        (IOption2 featureFlagOption, T value)? featureFlagAndExperimentValue = null,
        (IOption2 enableWhenOption, object whenValue)? enableWhenOptionAndValue = null) where T : Enum
    {
        var type = onboardedOption.Definition.Type;
        // If the option's type is nullable type, we use the original type in the registration file.
        var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;
        Assert.Equal(typeof(T), nonNullableType);

        var expectedEnumValues = enumValues ?? [.. Enum.GetValues(nonNullableType).Cast<T>()];
        var migration = new Migration
        {
            EnumIntegerToString = new EnumIntegerToString
            {
                Input = new Input(onboardedOption, languageName),
                Map = customMaps ?? [.. expectedEnumValues.Select(value => new Map { Result = value.ToString().ToCamelCase(), Match = Convert.ToInt32(value) })]
            }
        };

        var alternativeDefault = featureFlagAndExperimentValue is not null
            ? new AlternativeDefault<string>(featureFlagAndExperimentValue.Value.featureFlagOption, featureFlagAndExperimentValue.Value.value.ToString().ToCamelCase())
            : null;

        var enableWhen = enableWhenOptionAndValue is not null
            ? $"${{config:{GetUnifiedSettingsOptionValue(enableWhenOptionAndValue.Value.enableWhenOption, languageName)}}}=='{enableWhenOptionAndValue.Value.whenValue.ToString().ToCamelCase()}'"
            : null;

        var expectedDefault = customDefaultValue ?? onboardedOption.Definition.DefaultValue;
        Assert.NotNull(expectedDefault);

        return new UnifiedSettingsEnumOption
        {
            Title = title,
            Type = "string",
            Enum = [.. expectedEnumValues.Select(value => value.ToString().ToCamelCase())],
            EnumItemLabels = enumLabels,
            Order = order,
            EnableWhen = enableWhen,
            Migration = migration,
            AlternateDefault = alternativeDefault,
            Default = expectedDefault.ToString().ToCamelCase(),
        };
    }

    private static string GetUnifiedSettingsOptionValue(IOption2 option, string languageName)
    {
        return languageName switch
        {
            LanguageNames.CSharp => s_csharpUnifiedSettingsStorage[option],
            LanguageNames.VisualBasic => s_visualBasicUnifiedSettingsStorage[option],
            _ => throw ExceptionUtilities.UnexpectedValue(languageName)
        };
    }

    #endregion
}
