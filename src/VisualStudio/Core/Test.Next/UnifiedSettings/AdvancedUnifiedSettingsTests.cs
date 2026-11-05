// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImportOnPaste;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using Microsoft.CodeAnalysis.Editor.Implementation.SplitComment;
using Microsoft.CodeAnalysis.Editor.InlineDiagnostics;
using Microsoft.CodeAnalysis.Editor.InlineHints;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration;
using Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.KeywordHighlighting;
using Microsoft.CodeAnalysis.LineSeparators;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.ReferenceHighlighting;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.CodeAnalysis.StringCopyPaste;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.ValidateFormatString;
using Microsoft.CodeAnalysis.VisualBasic.AutomaticInsertionOfAbstractOrInterfaceMembers;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;
using Xunit;
using static Microsoft.VisualStudio.LanguageServices.Options.VisualStudioOptionStorage;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

/// <summary>
/// Validates task-oriented registrations and their legacy migrations.
/// </summary>
public sealed class AdvancedUnifiedSettingsTests
{
    private static readonly (IOption2 Option, string Path)[] s_commonOptions =
    [
        (SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, "analysis.backgroundAnalysisScope"),
        (SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, "analysis.compilerDiagnosticsScope"),
        (InlineDiagnosticsOptionsStorage.EnableInlineDiagnostics, "analysis.enableInlineDiagnostics"),
        (InlineDiagnosticsOptionsStorage.Location, "analysis.inlineDiagnosticsLocation"),
        (GenerationOptions.PlaceSystemNamespaceFirst, "codeCleanup.imports.placeSystemNamespaceFirst"),
        (GenerationOptions.SeparateImportDirectiveGroups, "codeCleanup.imports.separateImportDirectiveGroups"),
        (SymbolSearchOptionsStorage.SearchReferenceAssemblies, "codeCleanup.imports.searchReferenceAssemblies"),
        (SymbolSearchOptionsStorage.SearchNuGetPackages, "codeCleanup.imports.searchNuGetPackages"),
        (AddImportOnPasteOptionsStorage.AddImportsOnPaste, "codeCleanup.imports.addImportsOnPaste"),
        (ReferenceHighlightingOptionsStorage.ReferenceHighlighting, "textEditor.highlighting.referenceHighlighting"),
        (KeywordHighlightingOptionsStorage.KeywordHighlighting, "textEditor.highlighting.keywordHighlighting"),
        (OutliningOptionsStorage.Outlining, "textEditor.outlining.onFileOpen"),
        (BlockStructureOptionsStorage.CollapseRegionsWhenFirstOpened, "textEditor.outlining.onFileOpen.collapseRegions"),
        (BlockStructureOptionsStorage.CollapseImportsWhenFirstOpened, "textEditor.outlining.onFileOpen.collapseImports"),
        (BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, "textEditor.outlining.onFileOpen.collapseSourceLinkEmbeddedDecompiledFiles"),
        (BlockStructureOptionsStorage.CollapseMetadataSignatureFilesWhenFirstOpened, "textEditor.outlining.onFileOpen.collapseMetadataSignatureFiles"),
        (LineSeparatorsOptionsStorage.LineSeparator, "textEditor.lineSeparator"),
        (BlockStructureOptionsStorage.ShowOutliningForDeclarationLevelConstructs, "textEditor.outlining.showOutliningForDeclarationLevelConstructs"),
        (BlockStructureOptionsStorage.ShowOutliningForCodeLevelConstructs, "textEditor.outlining.showOutliningForCodeLevelConstructs"),
        (BlockStructureOptionsStorage.ShowOutliningForCommentsAndPreprocessorRegions, "textEditor.outlining.showOutliningForCommentsAndPreprocessorRegions"),
        (BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, "textEditor.outlining.collapseRegionsWhenCollapsingToDefinitions"),
        (FadingOptions.FadeOutUnusedImports, "textEditor.highlighting.fadeOutUnusedImports"),
        (FadingOptions.FadeOutUnusedMembers, "textEditor.highlighting.fadeOutUnusedMembers"),
        (BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, "textEditor.structureGuides.showBlockStructureGuidesForDeclarationLevelConstructs"),
        (BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, "textEditor.structureGuides.showBlockStructureGuidesForCodeLevelConstructs"),
        (BlockStructureOptionsStorage.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, "textEditor.structureGuides.showBlockStructureGuidesForCommentsAndPreprocessorRegions"),
        (DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, "codeStyle.comments.autoXmlDocCommentGeneration"),
        (DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, "codeStyle.comments.generateSummaryTagOnSingleLine"),
        (DocumentationCommentOptionsStorage.GenerateOnlySummaryTag, "codeStyle.comments.generateOnlySummaryTag"),
        (SplitCommentOptionsStorage.Enabled, "codeStyle.comments.splitComments"),
        (QuickInfoOptionsStorage.ShowRemarksInQuickInfo, "intellisense.showRemarksInQuickInfo"),
        (RenameTrackingOptionsStorage.RenameTrackingPreview, "intellisense.refactoring.renameTrackingPreview"),
        (FormatStringValidationOptionStorage.ReportInvalidPlaceholdersInStringDotFormatCalls, "intellisense.embeddedLanguages.compositeFormatting.reportInvalidPlaceholders"),
        (ClassificationOptionsStorage.ClassifyReassignedVariables, "textEditor.highlighting.classifyReassignedVariables"),
        (ClassificationOptionsStorage.ClassifyObsoleteSymbols, "textEditor.highlighting.classifyObsoleteSymbols"),
        (ClassificationOptionsStorage.ColorizeRegexPatterns, "intellisense.embeddedLanguages.regularExpressions.colorize"),
        (RegexOptionsStorage.ReportInvalidRegexPatterns, "intellisense.embeddedLanguages.regularExpressions.reportInvalidPatterns"),
        (HighlightingOptionsStorage.HighlightRelatedRegexComponentsUnderCursor, "intellisense.embeddedLanguages.regularExpressions.highlightRelatedComponents"),
        (CompletionOptionsStorage.ProvideRegexCompletions, "intellisense.embeddedLanguages.regularExpressions.showCompletions"),
        (ClassificationOptionsStorage.ColorizeJsonPatterns, "intellisense.embeddedLanguages.json.colorize"),
        (JsonDetectionOptionsStorage.ReportInvalidJsonPatterns, "intellisense.embeddedLanguages.json.reportInvalidPatterns"),
        (HighlightingOptionsStorage.HighlightRelatedJsonComponentsUnderCursor, "intellisense.embeddedLanguages.json.highlightRelatedComponents"),
        (ImplementTypeOptionsStorage.InsertionBehavior, "intellisense.refactoring.implementType.insertionBehavior"),
        (ImplementTypeOptionsStorage.PropertyGenerationBehavior, "intellisense.refactoring.implementType.propertyGenerationBehavior"),
        (InlineHintsViewOptionsStorage.ColorHints, "intellisense.inlineHints.colorize"),
        (InlineHintsOptionsStorage.EnabledForParameters, "intellisense.inlineHints.parameters.show"),
        (InlineHintsOptionsStorage.ForLiteralParameters, "intellisense.inlineHints.parameters.forLiterals"),
        (InlineHintsOptionsStorage.ForObjectCreationParameters, "intellisense.inlineHints.parameters.forObjectCreation"),
        (InlineHintsOptionsStorage.ForOtherParameters, "intellisense.inlineHints.parameters.forOtherArguments"),
        (InlineHintsOptionsStorage.ForIndexerParameters, "intellisense.inlineHints.parameters.forIndexers"),
        (InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent, "intellisense.inlineHints.parameters.suppressWhenMatchingMethodIntent"),
        (InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix, "intellisense.inlineHints.parameters.suppressWhenDifferingOnlyBySuffix"),
        (InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName, "intellisense.inlineHints.parameters.suppressWhenMatchingArgumentName"),
        (InheritanceMarginOptionsStorage.ShowInheritanceMargin, "intellisense.browsingNavigation.inheritanceMargin.show"),
        (InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports, "intellisense.browsingNavigation.inheritanceMargin.includeGlobalImports"),
    ];

    private static readonly (IOption2 Option, string Path)[] s_csharpOnlyOptions =
    [
        (BlockStructureOptionsStorage.CollapseLocalFunctionsWhenCollapsingToDefinitions, "textEditor.outlining.collapseLocalFunctionsWhenCollapsingToDefinitions"),
        (FadingOptions.FadeOutUnreachableCode, "textEditor.highlighting.fadeOutUnreachableCode"),
        (BlockCommentEditingOptionsStorage.AutoInsertBlockCommentStartString, "codeStyle.comments.autoInsertBlockCommentStartString"),
        (StringCopyPasteOptionsStorage.AutomaticallyFixStringContentsOnPaste, "textEditor.automaticallyFixStringContentsOnPaste"),
        (CrefPasteOptionsStorage.FixCrefOnPaste, "textEditor.fixCrefOnPaste"),
        (InlineHintsOptionsStorage.EnabledForTypes, "intellisense.inlineHints.types.show"),
        (InlineHintsOptionsStorage.ForImplicitVariableTypes, "intellisense.inlineHints.types.forInferredVariables"),
        (InlineHintsOptionsStorage.ForLambdaParameterTypes, "intellisense.inlineHints.types.forLambdaParameters"),
        (InlineHintsOptionsStorage.ForImplicitObjectCreation, "intellisense.inlineHints.types.forImplicitObjectCreation"),
        (InlineHintsOptionsStorage.ForCollectionExpressions, "intellisense.inlineHints.types.forCollectionExpressions"),
    ];

    private static readonly (IOption2 Option, string Path)[] s_visualBasicOnlyOptions =
    [
        (EndConstructGenerationOptionsStorage.EndConstruct, "textEditor.endConstruct"),
        (LineCommitOptionsStorage.PrettyListing, "codeStyle.formatting.general.prettyListing"),
        (AutomaticInsertionOfAbstractOrInterfaceMembersOptionsStorage.AutomaticInsertionOfAbstractOrInterfaceMembers, "intellisense.refactoring.automaticInsertionOfAbstractOrInterfaceMembers"),
        (VisualStudioNavigationOptionsStorage.NavigateToObjectBrowser, "intellisense.browsingNavigation.navigateToObjectBrowser"),
    ];

    private static readonly (IOption2 Option, string Path, string Category, int Order, bool BothLanguages)[] s_globalOptions =
    [
        (FeatureOnOffOptions.OfferRemoveUnusedReferences, "codeCleanup.offerRemoveUnusedReferences", "codeCleanup", 0, true),
        (VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics, "analysis.enableFileLoggingForDiagnostics", "analysis", 50, true),
        (FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds, "analysis.skipAnalyzersForImplicitlyTriggeredBuilds", "analysis", 60, true),
        (WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution, "analysis.sourceGeneratorExecution", "analysis", 70, true),
        (MetadataAsSourceOptionsStorage.NavigateToSourceLinkAndEmbeddedSources, "intellisense.browsingNavigation.navigateToSourceLinkAndEmbeddedSources", "intellisense.browsingNavigation", 0, false),
        (MetadataAsSourceOptionsStorage.NavigateToDecompiledSources, "intellisense.browsingNavigation.navigateToDecompiledSources", "intellisense.browsingNavigation", 10, false),
        (MetadataAsSourceOptionsStorage.AlwaysUseDefaultSymbolServers, "intellisense.browsingNavigation.alwaysUseDefaultSymbolServers", "intellisense.browsingNavigation", 20, false),
        (InlineHintsViewOptionsStorage.DisplayAllHintsWhilePressingAltF1, "intellisense.inlineHints.showAllOnAltF1", "intellisense.inlineHints", 0, true),
        (InheritanceMarginOptionsStorage.InheritanceMarginCombinedWithIndicatorMargin, "intellisense.browsingNavigation.inheritanceMargin.combineWithIndicatorMargin", "intellisense.browsingNavigation", 50, true),
        (SplitStringLiteralOptionsStorage.Enabled, "textEditor.splitStringLiterals", "textEditor", 20, false),
        (StackTraceExplorerOptionsStorage.OpenOnFocus, "intellisense.browsingNavigation.openStackTraceExplorerOnFocus", "intellisense.browsingNavigation", 70, false),
    ];

    [Theory]
    [InlineData(LanguageNames.CSharp, "csharp", "csharpSettings.registration.json", 65)]
    [InlineData(LanguageNames.VisualBasic, "basic", "visualBasicSettings.registration.json", 59)]
    public void CompleteLanguageInventory(string language, string languageId, string manifest, int count)
    {
        using var document = Load(manifest);
        var properties = document.RootElement.GetProperty("properties");
        var prefix = $"languages.{languageId}.";
        var expected = s_commonOptions.Concat(language == LanguageNames.CSharp ? s_csharpOnlyOptions : s_visualBasicOnlyOptions).ToArray();
        Assert.Equal(count, expected.Length);
        Assert.Equal(count + (language == LanguageNames.CSharp ? 5 : 0), properties.EnumerateObject().Count(property => property.Value.TryGetProperty("transientScopesSupported", out _)));
        foreach (var (option, path) in expected)
        {
            var moniker = prefix + path;
            var setting = properties.GetProperty(moniker);
            VerifyOption(option, language, setting);
            var categories = document.RootElement.GetProperty("categories");
            var expectedCategory = categories.EnumerateObject().Select(category => category.Name)
                .Where(category => moniker.StartsWith(category + ".", StringComparison.Ordinal))
                .OrderByDescending(category => category.Length).First();
            Assert.Equal(expectedCategory, GetCategory(moniker, properties));
        }
    }

    private static string GetCategory(string moniker, JsonElement properties)
    {
        var setting = properties.GetProperty(moniker);
        if (setting.TryGetProperty("enableWhen", out var enableWhen))
        {
            Assert.False(setting.TryGetProperty("placements", out _));
            var expression = enableWhen.GetString()!;
            Assert.StartsWith("${config:", expression);
            var dependency = expression.Substring("${config:".Length, expression.IndexOf('}') - "${config:".Length);
            return GetCategory(dependency, properties);
        }

        return setting.TryGetProperty("placements", out var placements)
            ? Assert.Single(placements.EnumerateArray()).GetProperty("category").GetString()!
            : moniker[..moniker.LastIndexOf('.')];
    }

    [Theory]
    [InlineData("csharpSettings.registration.json", "languages.csharp.codeCleanup.imports.placeSystemNamespaceFirst", false)]
    [InlineData("visualBasicSettings.registration.json", "languages.basic.codeCleanup.imports.placeSystemNamespaceFirst", true)]
    [InlineData("csharpSettings.registration.json", "languages.csharp.textEditor.lineSeparator", false)]
    [InlineData("visualBasicSettings.registration.json", "languages.basic.textEditor.lineSeparator", true)]
    public void LanguageDefaultsMatchGeneralProfile(string manifest, string moniker, bool expectedDefault)
    {
        using var document = Load(manifest);
        var setting = document.RootElement.GetProperty("properties").GetProperty(moniker);
        Assert.Equal(expectedDefault, setting.GetProperty("default").GetBoolean());
    }

    [Fact]
    public void GlobalOptionsHaveOneDefinitionAndCorrectPlacements()
    {
        using var shared = Load("roslynSettings.registration.json");
        using var csharp = Load("csharpSettings.registration.json");
        using var basic = Load("visualBasicSettings.registration.json");
        var properties = shared.RootElement.GetProperty("properties");
        Assert.Equal(s_globalOptions.Length, properties.EnumerateObject().Count(property => property.Name.StartsWith("languages.dotnet.", StringComparison.Ordinal)));
        foreach (var (option, path, category, order, bothLanguages) in s_globalOptions)
        {
            var moniker = "languages.dotnet." + path;
            Assert.False(csharp.RootElement.GetProperty("properties").TryGetProperty(moniker, out _));
            Assert.False(basic.RootElement.GetProperty("properties").TryGetProperty(moniker, out _));
            var setting = properties.GetProperty(moniker);
            VerifyOption(option, language: null, setting);
            Assert.False(setting.TryGetProperty("enableWhen", out _));
            var placements = setting.GetProperty("placements").EnumerateArray().ToArray();
            Assert.Equal(bothLanguages ? 2 : 1, placements.Length);
            Assert.Equal("languages.csharp." + category, placements[0].GetProperty("category").GetString());
            foreach (var placement in placements)
            {
                Assert.Equal(order, placement.GetProperty("order").GetInt32());
                var placementCategory = placement.GetProperty("category").GetString()!;
                var languageDocument = placementCategory.StartsWith("languages.csharp.", StringComparison.Ordinal) ? csharp : basic;
                Assert.True(languageDocument.RootElement.GetProperty("categories").TryGetProperty(placementCategory, out _));
            }

            if (bothLanguages)
            {
                Assert.Equal("languages.basic." + category, placements[1].GetProperty("category").GetString());
                var message = Assert.Single(setting.GetProperty("messages").EnumerateArray());
                Assert.Equal("@Microsoft.VisualStudio.LanguageServices.ServicesVSResources:This_setting_is_shared_by_CSharp_and_Visual_Basic;{6cf2e545-6109-4730-8883-cf43d7aec3e1}", message.GetProperty("text").GetString());
                Assert.Equal("This setting is shared by C# and Visual Basic. Changing it affects both languages.", TestModel.Utilities.EvalResource(message.GetProperty("text").GetString()!));
                Assert.False(message.TryGetProperty("visibleWhen", out _));
            }
            else
            {
                Assert.False(setting.TryGetProperty("messages", out _));
            }
        }

        var sourceGeneratorExecution = properties.GetProperty("languages.dotnet.analysis.sourceGeneratorExecution");
        Assert.Equal("Source generator execution:", TestModel.Utilities.EvalResource(sourceGeneratorExecution.GetProperty("title").GetString()!));
        Assert.True(sourceGeneratorExecution.GetProperty("requiresRestart").GetBoolean());
    }

    [Theory]
    [InlineData("csharp", "csharpSettings.registration.json", 16)]
    [InlineData("basic", "visualBasicSettings.registration.json", 12)]
    public void DisabledControlsRetainTheirLegacyDependencies(string languageId, string manifest, int count)
    {
        using var document = Load(manifest);
        var properties = document.RootElement.GetProperty("properties");
        var prefix = $"languages.{languageId}.";
        var expected = new Dictionary<string, string>
        {
            ["analysis.inlineDiagnosticsLocation"] = "analysis.enableInlineDiagnostics",
            ["textEditor.outlining.onFileOpen.collapseRegions"] = "textEditor.outlining.onFileOpen",
            ["textEditor.outlining.onFileOpen.collapseImports"] = "textEditor.outlining.onFileOpen",
            ["textEditor.outlining.onFileOpen.collapseSourceLinkEmbeddedDecompiledFiles"] = "textEditor.outlining.onFileOpen",
            ["textEditor.outlining.onFileOpen.collapseMetadataSignatureFiles"] = "textEditor.outlining.onFileOpen",
        };
        foreach (var path in new[] { "forLiterals", "forObjectCreation", "forOtherArguments", "forIndexers", "suppressWhenMatchingMethodIntent", "suppressWhenDifferingOnlyBySuffix", "suppressWhenMatchingArgumentName" })
            expected.Add("intellisense.inlineHints.parameters." + path, "intellisense.inlineHints.parameters.show");
        if (languageId == "csharp")
        {
            foreach (var path in new[] { "forInferredVariables", "forLambdaParameters", "forImplicitObjectCreation", "forCollectionExpressions" })
                expected.Add("intellisense.inlineHints.types." + path, "intellisense.inlineHints.types.show");
        }

        Assert.Equal(count, expected.Count);
        Assert.Equal(count + (languageId == "csharp" ? 2 : 0), properties.EnumerateObject().Count(property => property.Value.TryGetProperty("transientScopesSupported", out _) && property.Value.TryGetProperty("enableWhen", out _)));
        foreach (var (path, dependency) in expected)
            Assert.Equal($"${{config:{prefix}{dependency}}}=='true'", properties.GetProperty(prefix + path).GetProperty("enableWhen").GetString());

        var checkbox = properties.GetProperty(prefix + "analysis.enableInlineDiagnostics");
        var location = properties.GetProperty(prefix + "analysis.inlineDiagnosticsLocation");
        Assert.Equal("Display diagnostics inline", TestModel.Utilities.EvalResource(checkbox.GetProperty("title").GetString()!));
        Assert.Equal("Inline diagnostic location", TestModel.Utilities.EvalResource(location.GetProperty("title").GetString()!));
        Assert.True(location.GetProperty("order").GetInt32() > checkbox.GetProperty("order").GetInt32());
    }

    [Theory]
    [InlineData("csharp", "csharpSettings.registration.json")]
    [InlineData("basic", "visualBasicSettings.registration.json")]
    public void EmbeddedLanguageLabelsIdentifyTheirLanguage(string languageId, string manifest)
    {
        using var document = Load(manifest);
        var properties = document.RootElement.GetProperty("properties");
        var prefix = $"languages.{languageId}.intellisense.embeddedLanguages.";
        foreach (var (path, label) in new[]
        {
            ("regularExpressions.showCompletions", "Show regular expression completion list"),
            ("regularExpressions.highlightRelatedComponents", "Highlight related regular expression components under cursor"),
            ("json.highlightRelatedComponents", "Highlight related JSON components under cursor"),
        })
        {
            Assert.Equal(label, TestModel.Utilities.EvalResource(properties.GetProperty(prefix + path).GetProperty("title").GetString()!));
        }
    }

    [Fact]
    public void LegacyMigrationsUseValidRuleShapesAndUniqueTargets()
    {
        var targets = new HashSet<(string Store, string Path)>();
        foreach (var manifest in new[] { "csharpSettings.registration.json", "visualBasicSettings.registration.json", "roslynSettings.registration.json" })
        {
            using var document = Load(manifest);
            VerifyUniqueProperties(document.RootElement);
            foreach (var property in document.RootElement.GetProperty("properties").EnumerateObject().Where(property => property.Value.TryGetProperty("migration", out _)))
            {
                var migration = property.Value.GetProperty("migration").EnumerateObject().Single();
                var input = GetInput(migration);
                Assert.True(targets.Add((input.GetProperty("store").GetString()!, input.GetProperty("path").GetString()!)), property.Name);
            }
        }

        Assert.Equal(162, targets.Count);
    }

    [Fact]
    public void EnumMappingsPreserveTheLegacyIntegerMeanings()
    {
        foreach (var (manifest, languageId) in new[] { ("csharpSettings.registration.json", "csharp"), ("visualBasicSettings.registration.json", "basic") })
        {
            using var document = Load(manifest);
            var properties = document.RootElement.GetProperty("properties");
            var prefix = $"languages.{languageId}.";
            VerifyMap(properties.GetProperty(prefix + "analysis.backgroundAnalysisScope"), ("none", 3), ("currentDocument", 0), ("openDocuments", 1), ("entireSolution", 2));
            VerifyMap(properties.GetProperty(prefix + "analysis.compilerDiagnosticsScope"), ("none", 0), ("currentDocument", 1), ("openDocuments", 2), ("entireSolution", 3));
            VerifyMap(properties.GetProperty(prefix + "analysis.inlineDiagnosticsLocation"), ("placedAtEndOfCode", 0), ("placedAtEndOfEditor", 1));
            VerifyMap(properties.GetProperty(prefix + "intellisense.refactoring.implementType.insertionBehavior"), ("withOtherMembersOfTheSameKind", 0), ("atTheEnd", 1));
            VerifyMap(properties.GetProperty(prefix + "intellisense.refactoring.implementType.propertyGenerationBehavior"), ("preferThrowingProperties", 0), ("preferAutoProperties", 1));
        }

        using var shared = Load("roslynSettings.registration.json");
        VerifyMap(shared.RootElement.GetProperty("properties").GetProperty("languages.dotnet.analysis.sourceGeneratorExecution"), ("automatic", 0), ("balanced", 1));
    }

    private static void VerifyMap(JsonElement setting, params (string Result, int Match)[] expected)
    {
        var actual = setting.GetProperty("migration").GetProperty("custom").GetProperty("map").EnumerateArray()
            .Where(map => map.GetProperty("result").GetString() != "@default")
            .Select(map => (map.GetProperty("result").GetString()!, map.GetProperty("matches")[0].GetInt32())).ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumMappingsDeclareFallbackAndResetWithoutLosingExplicitDefaultValues()
    {
        var count = 0;
        foreach (var manifest in new[] { "csharpSettings.registration.json", "visualBasicSettings.registration.json", "roslynSettings.registration.json" })
        {
            using var document = Load(manifest);
            foreach (var property in document.RootElement.GetProperty("properties").EnumerateObject().Where(property => property.Value.TryGetProperty("transientScopesSupported", out _)))
            {
                if (property.Value.GetProperty("type").GetString() != "string"
                    || !property.Value.GetProperty("migration").TryGetProperty("custom", out var rule)
                    || property.Value.GetProperty("default").GetString() == "automatic")
                {
                    continue;
                }

                var maps = rule.GetProperty("map").EnumerateArray().ToArray();
                var fallback = maps[^1];
                Assert.Equal("@default", fallback.GetProperty("result").GetString());
                Assert.Equal("@any", fallback.GetProperty("matches")[0].GetString());
                Assert.All(maps.Take(maps.Length - 1), map => Assert.Equal(JsonValueKind.Number, map.GetProperty("matches")[0].ValueKind));
                Assert.Equal(2, maps.Count(map => map.GetProperty("result").GetString() == "@default"));

                // Reset uses the first @default entry; an explicit default still needs its literal mapping.
                var defaultToken = property.Value.GetProperty("default").GetString();
                var literalDefault = Assert.Single(maps, map => map.GetProperty("result").GetString() == defaultToken);
                var defaultInteger = literalDefault.GetProperty("matches")[0].GetInt32();
                Assert.Equal("@default", maps[0].GetProperty("result").GetString());
                Assert.Equal(defaultInteger, maps[0].GetProperty("matches")[0].GetInt32());

                count++;
            }
        }

        Assert.Equal(11, count);
    }

    [Theory]
    [InlineData("csharpSettings.registration.json", "languages.csharp.intellisense.browsingNavigation.inheritanceMargin.show")]
    [InlineData("visualBasicSettings.registration.json", "languages.basic.intellisense.browsingNavigation.inheritanceMargin.show")]
    [InlineData("roslynSettings.registration.json", "languages.dotnet.codeCleanup.offerRemoveUnusedReferences")]
    public void NullableLegacySettingsUseBooleanCheckboxesAndPassMigration(string manifest, string moniker)
    {
        using var document = Load(manifest);
        var setting = document.RootElement.GetProperty("properties").GetProperty(moniker);
        Assert.Equal("boolean", setting.GetProperty("type").GetString());
        Assert.True(setting.GetProperty("default").GetBoolean());
        Assert.False(setting.TryGetProperty("enum", out _));
        Assert.False(setting.TryGetProperty("enumItemLabels", out _));
        var pass = setting.GetProperty("migration").GetProperty("pass");
        Assert.Equal("full", pass.GetProperty("mode").GetString());
        Assert.Equal("SettingsManager", pass.GetProperty("input").GetProperty("store").GetString());
        Assert.False(pass.GetProperty("input").TryGetProperty("bitmask", out _));

    }

    private static void VerifyOption(IOption2 option, string? language, JsonElement setting)
    {
        Assert.False(setting.GetProperty("transientScopesSupported").GetBoolean());
        VerifyResource(setting.GetProperty("title").GetString()!);
        var type = Nullable.GetUnderlyingType(option.Definition.Type) ?? option.Definition.Type;
        var migration = setting.GetProperty("migration").EnumerateObject().Single();
        var input = GetInput(migration);
        switch (Storages[option.Definition.ConfigName])
        {
            case RoamingProfileStorage roaming:
                var path = language == LanguageNames.VisualBasic && roaming.VisualBasicKey is not null
                    ? roaming.VisualBasicKey
                    : roaming.Key.Replace("%LANGUAGE%", language == LanguageNames.CSharp ? "CSharp" : "VisualBasic");
                Assert.Equal("SettingsManager", input.GetProperty("store").GetString());
                Assert.Equal(path, input.GetProperty("path").GetString());
                Assert.Equal("full", migration.Value.GetProperty("mode").GetString());
                break;
            case UnifiedSettingsManagerStorage unified:
                Assert.Equal("languages.dotnet.analysis.enableFileLoggingForDiagnostics", unified.Key);
                Assert.Equal("VsUserSettingsRegistry", input.GetProperty("store").GetString());
                Assert.Equal(@"Roslyn\Internal\Diagnostics\EnableFileLoggingForDiagnostics", input.GetProperty("path").GetString());
                Assert.Equal("full", migration.Value.GetProperty("mode").GetString());
                break;
            default:
                throw new InvalidOperationException(option.Definition.ConfigName);
        }

        if (type == typeof(bool))
        {
            Assert.Equal(option == VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics ? "custom" : "pass", migration.Name);
            Assert.Equal("boolean", setting.GetProperty("type").GetString());
            var expectedDefault = (bool)option.Definition.DefaultValue!;
            // The General profile overrides these shared option defaults for individual languages.
            if (option == GenerationOptions.PlaceSystemNamespaceFirst && language == LanguageNames.CSharp)
                expectedDefault = false;
            else if (option == LineSeparatorsOptionsStorage.LineSeparator && language == LanguageNames.VisualBasic)
                expectedDefault = true;

            Assert.Equal(expectedDefault, setting.GetProperty("default").GetBoolean());
        }
        else
        {
            Assert.True(type.IsEnum);
            Assert.Equal("custom", migration.Name);
            Assert.Equal("string", setting.GetProperty("type").GetString());
            var values = setting.GetProperty("enum").EnumerateArray().Select(value => value.GetString()!).ToArray();
            var labels = setting.GetProperty("enumItemLabels").EnumerateArray().Select(value => value.GetString()!).ToArray();
            Assert.Equal(values.Length, labels.Length);
            foreach (var label in labels)
                VerifyResource(label);
            var maps = migration.Value.GetProperty("map").EnumerateArray().Where(map => map.GetProperty("result").GetString() != "@default").ToArray();
            Assert.Equal(values.OrderBy(value => value), maps.Select(map => map.GetProperty("result").GetString()!).OrderBy(value => value));
            Assert.Equal(Enum.GetValues(type).Cast<object>().Select(Convert.ToInt32).Distinct().OrderBy(value => value), maps.Select(map => map.GetProperty("matches")[0].GetInt32()).Distinct().OrderBy(value => value));
            var defaultMap = Assert.Single(maps, map => map.GetProperty("matches")[0].GetInt32() == Convert.ToInt32(option.Definition.DefaultValue));
            Assert.Equal(defaultMap.GetProperty("result").GetString(), setting.GetProperty("default").GetString());
        }
    }

    private static void VerifyResource(string reference)
    {
        Assert.StartsWith("@", reference);
        var separator = reference.IndexOf(';');
        var namespaceSeparator = reference.IndexOf(':');
        Assert.True(namespaceSeparator > 1 && namespaceSeparator < separator, reference);
        Assert.False(string.IsNullOrEmpty(TestModel.Utilities.EvalResource(reference)), reference);
    }

    [Theory]
    [InlineData("csharpSettings.registration.json", 94)]
    [InlineData("visualBasicSettings.registration.json", 88)]
    [InlineData("roslynSettings.registration.json", 13)]
    public void DllResourcesUseTheirDeclaredNativeResourceTable(string manifest, int expectedReferences)
    {
        using var document = Load(manifest);
        var count = 0;
        foreach (var group in document.RootElement.EnumerateObject().Where(group => group.Name is "categories" or "properties"))
        {
            foreach (var entry in group.Value.EnumerateObject().Where(entry => group.Name == "categories" || entry.Value.TryGetProperty("transientScopesSupported", out _)))
                Verify(entry.Value);
        }

        Assert.Equal(expectedReferences, count);

        void Verify(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                        Verify(property.Value);
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                        Verify(item);
                    break;
                case JsonValueKind.String:
                    var value = element.GetString()!;
                    if (value.StartsWith("@", StringComparison.Ordinal) && value.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        VerifyResource(value);
                        count++;
                    }
                    break;
            }
        }
    }

    private static JsonElement GetInput(JsonProperty migration)
    {
        string[] expectedProperties = migration.Name switch
        {
            "pass" => ["input"],
            "custom" => ["inputs", "map"],
            "enumIntegerToString" => ["input", "map"],
            _ => throw new InvalidOperationException(migration.Name),
        };
        Assert.Equal(expectedProperties, migration.Value.EnumerateObject()
            .Where(property => property.Name != "mode")
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal));

        return migration.Name == "custom"
            ? migration.Value.GetProperty("inputs").EnumerateArray().Single()
            : migration.Value.GetProperty("input");
    }

    private static JsonDocument Load(string manifest)
    {
        using var stream = typeof(AdvancedUnifiedSettingsTests).Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests." + manifest);
        Assert.NotNull(stream);
        return JsonDocument.Parse(stream!, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
    }

    private static void VerifyUniqueProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                Assert.True(names.Add(property.Name), property.Name);
                VerifyUniqueProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                VerifyUniqueProperties(item);
        }
    }
}
