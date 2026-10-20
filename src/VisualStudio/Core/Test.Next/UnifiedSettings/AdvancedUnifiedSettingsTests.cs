// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
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
using Microsoft.VisualStudio.LanguageServices.VisualBasic;
using Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;
using Xunit;
using static Microsoft.VisualStudio.LanguageServices.Options.VisualStudioOptionStorage;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

/// <summary>
/// Validates TextEditor-backed Advanced registrations and the unshipped non-TextEditor drafts.
/// </summary>
public sealed class AdvancedUnifiedSettingsTests
{
    private static readonly (IOption2 Option, string Path)[] s_commonOptions =
    [
        (SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, "analysis.backgroundAnalysisScope"),
        (SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, "analysis.compilerDiagnosticsScope"),
        (InlineDiagnosticsOptionsStorage.EnableInlineDiagnostics, "analysis.enableInlineDiagnostics"),
        (InlineDiagnosticsOptionsStorage.Location, "analysis.inlineDiagnosticsLocation"),
        (GenerationOptions.PlaceSystemNamespaceFirst, "imports.placeSystemNamespaceFirst"),
        (GenerationOptions.SeparateImportDirectiveGroups, "imports.separateImportDirectiveGroups"),
        (SymbolSearchOptionsStorage.SearchReferenceAssemblies, "imports.searchReferenceAssemblies"),
        (SymbolSearchOptionsStorage.SearchNuGetPackages, "imports.searchNuGetPackages"),
        (AddImportOnPasteOptionsStorage.AddImportsOnPaste, "imports.addImportsOnPaste"),
        (ReferenceHighlightingOptionsStorage.ReferenceHighlighting, "highlighting.referenceHighlighting"),
        (KeywordHighlightingOptionsStorage.KeywordHighlighting, "highlighting.keywordHighlighting"),
        (OutliningOptionsStorage.Outlining, "outlining.outlining"),
        (BlockStructureOptionsStorage.CollapseRegionsWhenFirstOpened, "outlining.collapseRegionsWhenFirstOpened"),
        (BlockStructureOptionsStorage.CollapseImportsWhenFirstOpened, "outlining.collapseImportsWhenFirstOpened"),
        (BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, "outlining.collapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened"),
        (BlockStructureOptionsStorage.CollapseMetadataSignatureFilesWhenFirstOpened, "outlining.collapseMetadataSignatureFilesWhenFirstOpened"),
        (LineSeparatorsOptionsStorage.LineSeparator, "outlining.lineSeparator"),
        (BlockStructureOptionsStorage.ShowOutliningForDeclarationLevelConstructs, "outlining.showOutliningForDeclarationLevelConstructs"),
        (BlockStructureOptionsStorage.ShowOutliningForCodeLevelConstructs, "outlining.showOutliningForCodeLevelConstructs"),
        (BlockStructureOptionsStorage.ShowOutliningForCommentsAndPreprocessorRegions, "outlining.showOutliningForCommentsAndPreprocessorRegions"),
        (BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, "outlining.collapseRegionsWhenCollapsingToDefinitions"),
        (FadingOptions.FadeOutUnusedImports, "fading.fadeOutUnusedImports"),
        (FadingOptions.FadeOutUnusedMembers, "fading.fadeOutUnusedMembers"),
        (BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, "structureGuides.showBlockStructureGuidesForDeclarationLevelConstructs"),
        (BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, "structureGuides.showBlockStructureGuidesForCodeLevelConstructs"),
        (BlockStructureOptionsStorage.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, "structureGuides.showBlockStructureGuidesForCommentsAndPreprocessorRegions"),
        (DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, "comments.autoXmlDocCommentGeneration"),
        (DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, "comments.generateSummaryTagOnSingleLine"),
        (DocumentationCommentOptionsStorage.GenerateOnlySummaryTag, "comments.generateOnlySummaryTag"),
        (SplitCommentOptionsStorage.Enabled, "comments.splitComments"),
        (QuickInfoOptionsStorage.ShowRemarksInQuickInfo, "editorHelp.showRemarksInQuickInfo"),
        (RenameTrackingOptionsStorage.RenameTrackingPreview, "editorHelp.renameTrackingPreview"),
        (FormatStringValidationOptionStorage.ReportInvalidPlaceholdersInStringDotFormatCalls, "editorHelp.reportInvalidPlaceholdersInStringDotFormatCalls"),
        (ClassificationOptionsStorage.ClassifyReassignedVariables, "editorHelp.classifyReassignedVariables"),
        (ClassificationOptionsStorage.ClassifyObsoleteSymbols, "editorHelp.classifyObsoleteSymbols"),
        (ClassificationOptionsStorage.ColorizeRegexPatterns, "regularExpressions.colorizeRegexPatterns"),
        (RegexOptionsStorage.ReportInvalidRegexPatterns, "regularExpressions.reportInvalidRegexPatterns"),
        (HighlightingOptionsStorage.HighlightRelatedRegexComponentsUnderCursor, "regularExpressions.highlightRelatedRegexComponentsUnderCursor"),
        (CompletionOptionsStorage.ProvideRegexCompletions, "regularExpressions.provideRegexCompletions"),
        (ClassificationOptionsStorage.ColorizeJsonPatterns, "json.colorizeJsonPatterns"),
        (JsonDetectionOptionsStorage.ReportInvalidJsonPatterns, "json.reportInvalidJsonPatterns"),
        (HighlightingOptionsStorage.HighlightRelatedJsonComponentsUnderCursor, "json.highlightRelatedJsonComponentsUnderCursor"),
        (ImplementTypeOptionsStorage.InsertionBehavior, "implementType.insertionBehavior"),
        (ImplementTypeOptionsStorage.PropertyGenerationBehavior, "implementType.propertyGenerationBehavior"),
        (InlineHintsViewOptionsStorage.ColorHints, "inlineHints.colorHints"),
        (InlineHintsOptionsStorage.EnabledForParameters, "inlineHints.enabledForParameters"),
        (InlineHintsOptionsStorage.ForLiteralParameters, "inlineHints.forLiteralParameters"),
        (InlineHintsOptionsStorage.ForObjectCreationParameters, "inlineHints.forObjectCreationParameters"),
        (InlineHintsOptionsStorage.ForOtherParameters, "inlineHints.forOtherParameters"),
        (InlineHintsOptionsStorage.ForIndexerParameters, "inlineHints.forIndexerParameters"),
        (InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent, "inlineHints.suppressForParametersThatMatchMethodIntent"),
        (InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix, "inlineHints.suppressForParametersThatDifferOnlyBySuffix"),
        (InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName, "inlineHints.suppressForParametersThatMatchArgumentName"),
        (InheritanceMarginOptionsStorage.ShowInheritanceMargin, "inheritanceMargin.showInheritanceMargin"),
        (InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports, "inheritanceMargin.inheritanceMarginIncludeGlobalImports"),
    ];

    private static readonly (IOption2 Option, string Path)[] s_csharpOnlyOptions =
    [
        (BlockStructureOptionsStorage.CollapseLocalFunctionsWhenCollapsingToDefinitions, "outlining.collapseLocalFunctionsWhenCollapsingToDefinitions"),
        (FadingOptions.FadeOutUnreachableCode, "fading.fadeOutUnreachableCode"),
        (BlockCommentEditingOptionsStorage.AutoInsertBlockCommentStartString, "comments.autoInsertBlockCommentStartString"),
        (StringCopyPasteOptionsStorage.AutomaticallyFixStringContentsOnPaste, "editorHelp.automaticallyFixStringContentsOnPaste"),
        (CrefPasteOptionsStorage.FixCrefOnPaste, "editorHelp.fixCrefOnPaste"),
        (InlineHintsOptionsStorage.EnabledForTypes, "inlineHints.enabledForTypes"),
        (InlineHintsOptionsStorage.ForImplicitVariableTypes, "inlineHints.forImplicitVariableTypes"),
        (InlineHintsOptionsStorage.ForLambdaParameterTypes, "inlineHints.forLambdaParameterTypes"),
        (InlineHintsOptionsStorage.ForImplicitObjectCreation, "inlineHints.forImplicitObjectCreation"),
        (InlineHintsOptionsStorage.ForCollectionExpressions, "inlineHints.forCollectionExpressions"),
    ];

    private static readonly (IOption2 Option, string Path)[] s_visualBasicOnlyOptions =
    [
        (EndConstructGenerationOptionsStorage.EndConstruct, "editorHelp.endConstruct"),
        (LineCommitOptionsStorage.PrettyListing, "editorHelp.prettyListing"),
        (AutomaticInsertionOfAbstractOrInterfaceMembersOptionsStorage.AutomaticInsertionOfAbstractOrInterfaceMembers, "editorHelp.automaticInsertionOfAbstractOrInterfaceMembers"),
        (VisualStudioNavigationOptionsStorage.NavigateToObjectBrowser, "navigation.navigateToObjectBrowser"),
    ];

    private static readonly (IOption2 Option, string Path, string Category, int Order, bool BothLanguages)[] s_globalOptions =
    [
        (FeatureOnOffOptions.OfferRemoveUnusedReferences, "offerRemoveUnusedReferences", "analysis", 40, true),
        (VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics, "enableFileLoggingForDiagnostics", "analysis", 50, true),
        (FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds, "skipAnalyzersForImplicitlyTriggeredBuilds", "analysis", 60, true),
        (WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution, "sourceGeneratorExecution", "sourceGenerators", 0, true),
        (MetadataAsSourceOptionsStorage.NavigateToSourceLinkAndEmbeddedSources, "navigateToSourceLinkAndEmbeddedSources", "navigation", 0, false),
        (MetadataAsSourceOptionsStorage.NavigateToDecompiledSources, "navigateToDecompiledSources", "navigation", 10, false),
        (MetadataAsSourceOptionsStorage.AlwaysUseDefaultSymbolServers, "alwaysUseDefaultSymbolServers", "navigation", 20, false),
        (InlineHintsViewOptionsStorage.DisplayAllHintsWhilePressingAltF1, "displayAllHintsWhilePressingAltF1", "inlineHints", 0, true),
        (InheritanceMarginOptionsStorage.InheritanceMarginCombinedWithIndicatorMargin, "inheritanceMarginCombinedWithIndicatorMargin", "inheritanceMargin", 10, true),
        (SplitStringLiteralOptionsStorage.Enabled, "splitStringLiterals", "editorHelp", 20, false),
        (StackTraceExplorerOptionsStorage.OpenOnFocus, "openStackTraceExplorerOnFocus", "stackTrace", 0, false),
    ];

    [Theory]
    [InlineData(LanguageNames.CSharp, "csharp", "csharpSettings.registration.json", 65)]
    [InlineData(LanguageNames.VisualBasic, "basic", "visualBasicSettings.registration.json", 59)]
    public void CompleteLanguageInventory(string language, string languageId, string manifest, int count)
    {
        using var document = Load(manifest);
        var properties = document.RootElement.GetProperty("properties");
        var prefix = $"languages.{languageId}.advanced.";
        var expected = s_commonOptions.Concat(language == LanguageNames.CSharp ? s_csharpOnlyOptions : s_visualBasicOnlyOptions).ToArray();
        Assert.Equal(count, expected.Length);
        Assert.Equal(count, properties.EnumerateObject().Count(property => property.Name.StartsWith(prefix, StringComparison.Ordinal)));
        foreach (var (option, path) in expected)
        {
            var setting = properties.GetProperty(prefix + path);
            VerifyOption(option, language, setting);
            Assert.False(setting.TryGetProperty("placements", out _));
            Assert.True(document.RootElement.GetProperty("categories").TryGetProperty(prefix + path[..path.LastIndexOf('.')], out _));
        }
    }

    [Fact]
    public void GlobalOptionsHaveOneDefinitionAndCorrectPlacements()
    {
        using var shared = Load("roslynSettings.registration.json");
        using var csharp = Load("csharpSettings.registration.json");
        using var basic = Load("visualBasicSettings.registration.json");
        var properties = shared.RootElement.GetProperty("properties");
        Assert.Equal(s_globalOptions.Length, properties.EnumerateObject().Count(property => property.Name.StartsWith("languages.dotnet.advanced.", StringComparison.Ordinal)));
        foreach (var (option, path, category, order, bothLanguages) in s_globalOptions)
        {
            var moniker = "languages.dotnet.advanced." + path;
            Assert.False(csharp.RootElement.GetProperty("properties").TryGetProperty(moniker, out _));
            Assert.False(basic.RootElement.GetProperty("properties").TryGetProperty(moniker, out _));
            var setting = properties.GetProperty(moniker);
            VerifyOption(option, language: null, setting);
            Assert.False(setting.TryGetProperty("enableWhen", out _));
            var placements = setting.GetProperty("placements").EnumerateArray().ToArray();
            Assert.Equal(bothLanguages ? 2 : 1, placements.Length);
            Assert.Equal("languages.csharp.advanced." + category, placements[0].GetProperty("category").GetString());
            foreach (var placement in placements)
            {
                Assert.Equal(order, placement.GetProperty("order").GetInt32());
                var placementCategory = placement.GetProperty("category").GetString()!;
                var languageDocument = placementCategory.StartsWith("languages.csharp.", StringComparison.Ordinal) ? csharp : basic;
                Assert.True(languageDocument.RootElement.GetProperty("categories").TryGetProperty(placementCategory, out _));
            }

            if (bothLanguages)
            {
                Assert.Equal("languages.basic.advanced." + category, placements[1].GetProperty("category").GetString());
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

        Assert.True(properties.GetProperty("languages.dotnet.advanced.sourceGeneratorExecution").GetProperty("requiresRestart").GetBoolean());
    }

    [Theory]
    [InlineData("roslynSettings.registration.json", 6)]
    [InlineData("roslynAdvancedSettings.registration.draft.json", 6)]
    public void SharedLanguageSettingsDisplayAnAlwaysVisibleLocalizedMessage(string manifest, int count)
    {
        using var document = LoadLive(manifest);
        var sharedSettings = document.RootElement.GetProperty("properties").EnumerateObject()
            .Where(property => property.Value.TryGetProperty("placements", out var placements) && placements.GetArrayLength() == 2).ToArray();
        Assert.Equal(count, sharedSettings.Length);
        foreach (var setting in sharedSettings)
        {
            var message = Assert.Single(setting.Value.GetProperty("messages").EnumerateArray());
            Assert.Equal("@Microsoft.VisualStudio.LanguageServices.ServicesVSResources:This_setting_is_shared_by_CSharp_and_Visual_Basic;{6cf2e545-6109-4730-8883-cf43d7aec3e1}", message.GetProperty("text").GetString());
            Assert.Equal("This setting is shared by C# and Visual Basic. Changing it affects both languages.", TestModel.Utilities.EvalResource(message.GetProperty("text").GetString()!));
            Assert.False(message.TryGetProperty("visibleWhen", out _));
        }
    }

    [Theory]
    [InlineData("csharp", "csharpSettings.registration.json", 16)]
    [InlineData("basic", "visualBasicSettings.registration.json", 12)]
    public void DisabledControlsRetainTheirLegacyDependencies(string languageId, string manifest, int count)
    {
        using var document = Load(manifest);
        var properties = document.RootElement.GetProperty("properties");
        var prefix = $"languages.{languageId}.advanced.";
        var expected = new Dictionary<string, string>
        {
            ["analysis.inlineDiagnosticsLocation"] = "analysis.enableInlineDiagnostics",
            ["outlining.collapseRegionsWhenFirstOpened"] = "outlining.outlining",
            ["outlining.collapseImportsWhenFirstOpened"] = "outlining.outlining",
            ["outlining.collapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened"] = "outlining.outlining",
            ["outlining.collapseMetadataSignatureFilesWhenFirstOpened"] = "outlining.outlining",
        };
        foreach (var path in new[] { "forLiteralParameters", "forObjectCreationParameters", "forOtherParameters", "forIndexerParameters", "suppressForParametersThatMatchMethodIntent", "suppressForParametersThatDifferOnlyBySuffix", "suppressForParametersThatMatchArgumentName" })
            expected.Add("inlineHints." + path, "inlineHints.enabledForParameters");
        if (languageId == "csharp")
        {
            foreach (var path in new[] { "forImplicitVariableTypes", "forLambdaParameterTypes", "forImplicitObjectCreation", "forCollectionExpressions" })
                expected.Add("inlineHints." + path, "inlineHints.enabledForTypes");
        }

        Assert.Equal(count, expected.Count);
        Assert.Equal(count, properties.EnumerateObject().Count(property => property.Name.StartsWith(prefix, StringComparison.Ordinal) && property.Value.TryGetProperty("enableWhen", out _)));
        foreach (var (path, dependency) in expected)
            Assert.Equal($"${{config:{prefix}{dependency}}}=='true'", properties.GetProperty(prefix + path).GetProperty("enableWhen").GetString());
    }

    [Theory]
    [InlineData("csharpSettings.registration.json", "csharp")]
    [InlineData("csharpAdvancedSettings.registration.draft.json", "csharp")]
    [InlineData("visualBasicSettings.registration.json", "basic")]
    [InlineData("visualBasicAdvancedSettings.registration.draft.json", "basic")]
    public void InlineDiagnosticsLocationHasItsOwnLabelAndDependsOnItsCheckbox(string manifest, string languageId)
    {
        using var document = LoadLive(manifest);
        var properties = document.RootElement.GetProperty("properties");
        var prefix = $"languages.{languageId}.advanced.analysis.";
        var checkbox = properties.GetProperty(prefix + "enableInlineDiagnostics");
        var location = properties.GetProperty(prefix + "inlineDiagnosticsLocation");
        Assert.Equal("Display diagnostics inline", TestModel.Utilities.EvalResource(checkbox.GetProperty("title").GetString()!));
        Assert.Equal("Location", TestModel.Utilities.EvalResource(location.GetProperty("title").GetString()!));
        Assert.Equal($"${{config:{prefix}enableInlineDiagnostics}}=='true'", location.GetProperty("enableWhen").GetString());
        Assert.True(location.GetProperty("order").GetInt32() > checkbox.GetProperty("order").GetInt32());
    }

    [Theory]
    [InlineData("roslynSettings.registration.json")]
    [InlineData("roslynAdvancedSettings.registration.draft.json")]
    public void SourceGeneratorExecutionDeclaresRestartWithoutIncludingItInTheTitle(string manifest)
    {
        using var document = LoadLive(manifest);
        var setting = document.RootElement.GetProperty("properties").GetProperty("languages.dotnet.advanced.sourceGeneratorExecution");
        Assert.Equal("Source generator execution:", TestModel.Utilities.EvalResource(setting.GetProperty("title").GetString()!));
        Assert.True(setting.GetProperty("requiresRestart").GetBoolean());
    }

    [Fact]
    public void AdvancedLegacyTargetsAreNotDuplicated()
    {
        var targets = new HashSet<(string Store, string Path)>();
        foreach (var manifest in new[] { "csharpSettings.registration.json", "visualBasicSettings.registration.json", "roslynSettings.registration.json" })
        {
            using var document = Load(manifest);
            VerifyUniqueProperties(document.RootElement);
            foreach (var property in document.RootElement.GetProperty("properties").EnumerateObject().Where(property => property.Name.Contains(".advanced.")))
            {
                var rule = property.Value.GetProperty("migration").EnumerateObject().Single().Value;
                var input = GetInput(rule);
                Assert.True(targets.Add((input.GetProperty("store").GetString()!, input.GetProperty("path").GetString()!)), property.Name);
            }
        }

        Assert.Equal(135, targets.Count);
    }

    [Fact]
    public void EnumMappingsPreserveTheLegacyIntegerMeanings()
    {
        foreach (var (manifest, languageId) in new[] { ("csharpSettings.registration.json", "csharp"), ("visualBasicSettings.registration.json", "basic") })
        {
            using var document = Load(manifest);
            var properties = document.RootElement.GetProperty("properties");
            var prefix = $"languages.{languageId}.advanced.";
            VerifyMap(properties.GetProperty(prefix + "analysis.backgroundAnalysisScope"), ("none", 3), ("currentDocument", 0), ("openDocuments", 1), ("entireSolution", 2));
            VerifyMap(properties.GetProperty(prefix + "analysis.compilerDiagnosticsScope"), ("none", 0), ("currentDocument", 1), ("openDocuments", 2), ("entireSolution", 3));
            VerifyMap(properties.GetProperty(prefix + "analysis.inlineDiagnosticsLocation"), ("placedAtEndOfCode", 0), ("placedAtEndOfEditor", 1));
            VerifyMap(properties.GetProperty(prefix + "implementType.insertionBehavior"), ("withOtherMembersOfTheSameKind", 0), ("atTheEnd", 1));
            VerifyMap(properties.GetProperty(prefix + "implementType.propertyGenerationBehavior"), ("preferThrowingProperties", 0), ("preferAutoProperties", 1));
        }

        using var shared = Load("roslynSettings.registration.json");
        VerifyMap(shared.RootElement.GetProperty("properties").GetProperty("languages.dotnet.advanced.sourceGeneratorExecution"), ("automatic", 0), ("balanced", 1));
    }

    private static void VerifyMap(JsonElement setting, params (string Result, int Match)[] expected)
    {
        var actual = setting.GetProperty("migration").GetProperty("custom").GetProperty("map").EnumerateArray()
            .Where(map => map.GetProperty("result").GetString() != "@default")
            .Select(map => (map.GetProperty("result").GetString()!, map.GetProperty("matches")[0].GetInt32())).ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumMappingsPreserveUnknownImportsAndReverseExplicitChoicesAndReset()
    {
        var count = 0;
        foreach (var manifest in new[] { "csharpSettings.registration.json", "visualBasicSettings.registration.json", "roslynSettings.registration.json" })
        {
            using var document = Load(manifest);
            using var live = LoadLive(manifest);
            foreach (var property in document.RootElement.GetProperty("properties").EnumerateObject().Where(property => property.Name.Contains(".advanced.")))
            {
                if (property.Value.GetProperty("type").GetString() != "string"
                    || !property.Value.GetProperty("migration").TryGetProperty("custom", out var rule)
                    || property.Value.GetProperty("default").GetString() == "automatic")
                {
                    continue;
                }

                Assert.True(live.RootElement.GetProperty("properties").TryGetProperty(property.Name, out var liveSetting));
                Assert.Equal(JsonSerializer.Serialize(property.Value), JsonSerializer.Serialize(liveSetting));
                var maps = rule.GetProperty("map").EnumerateArray().ToArray();
                var fallback = maps[^1];
                Assert.Equal("@default", fallback.GetProperty("result").GetString());
                Assert.Equal("@any", fallback.GetProperty("matches")[0].GetString());
                Assert.All(maps.Take(maps.Length - 1), map => Assert.Equal(JsonValueKind.Number, map.GetProperty("matches")[0].ValueKind));
                Assert.Equal(2, maps.Count(map => map.GetProperty("result").GetString() == "@default"));

                // MigrationRule.Reverse selects the FIRST @default output for reset. Retain the
                // duplicate literal output so an explicitly chosen default still reverses correctly.
                var defaultToken = property.Value.GetProperty("default").GetString();
                var literalDefault = Assert.Single(maps, map => map.GetProperty("result").GetString() == defaultToken);
                var defaultInteger = literalDefault.GetProperty("matches")[0].GetInt32();
                var reverseDefault = maps.First(map => map.GetProperty("result").GetString() == "@default");
                Assert.Equal("@default", maps[0].GetProperty("result").GetString());
                Assert.Equal(defaultInteger, reverseDefault.GetProperty("matches")[0].GetInt32());
                Assert.Equal(maps[0].GetRawText(), reverseDefault.GetRawText());

                foreach (var unknown in new[] { int.MinValue, -1, int.MaxValue })
                {
                    var match = maps.First(map => map.GetProperty("matches")[0] is var value
                        && (value.ValueKind == JsonValueKind.String || value.GetInt32() == unknown));
                    Assert.Equal("@default", match.GetProperty("result").GetString());
                    Assert.Equal(JsonValueKind.String, match.GetProperty("matches")[0].ValueKind);
                }

                // MigrationRuleWithKnownLegacyLocations.Apply takes the first matching input.
                // @default imports are non-customized; LegacySynchronizer ignores their migration
                // change source, so merely reading an unknown/missing integer does not normalize it.
                foreach (var map in maps.Where(map => map.GetProperty("result").GetString() != "@default"))
                {
                    var integer = map.GetProperty("matches")[0].GetInt32();
                    var token = map.GetProperty("result").GetString();
                    var forward = maps.First(candidate => candidate.GetProperty("matches")[0] is var input
                        && (input.ValueKind == JsonValueKind.String || input.GetInt32() == integer));
                    Assert.Equal(integer == defaultInteger ? "@default" : token, forward.GetProperty("result").GetString());
                    var reverseExplicit = maps.First(candidate => candidate.GetProperty("result").GetString() == token);
                    Assert.Equal(integer, reverseExplicit.GetProperty("matches")[0].GetInt32());
                }

                count++;
            }
        }

        Assert.Equal(11, count);
    }

    [Theory]
    [InlineData("csharpSettings.registration.json", "languages.csharp.advanced.inheritanceMargin.showInheritanceMargin")]
    [InlineData("visualBasicSettings.registration.json", "languages.basic.advanced.inheritanceMargin.showInheritanceMargin")]
    [InlineData("roslynSettings.registration.json", "languages.dotnet.advanced.offerRemoveUnusedReferences")]
    public void NullableLegacySettingsUseBooleanCheckboxesAndPassMigration(string manifest, string moniker)
    {
        using var draft = Load(manifest);
        using var live = LoadLive(manifest);
        var setting = live.RootElement.GetProperty("properties").GetProperty(moniker);
        Assert.Equal(JsonSerializer.Serialize(draft.RootElement.GetProperty("properties").GetProperty(moniker)), JsonSerializer.Serialize(setting));
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
        var input = GetInput(migration.Value);
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
                Assert.Equal("languages.dotnet.advanced.enableFileLoggingForDiagnostics", unified.Key);
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
            Assert.Equal((bool)option.Definition.DefaultValue!, setting.GetProperty("default").GetBoolean());
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
            foreach (var map in maps)
            {
                var result = map.GetProperty("result").GetString();
                var reverse = maps.First(candidate => candidate.GetProperty("result").GetString() == result);
                Assert.Equal(map.GetProperty("matches")[0].GetInt32(), reverse.GetProperty("matches")[0].GetInt32());
            }
        }
    }

    private static void VerifyResource(string reference)
    {
        Assert.StartsWith("@", reference);
        var separator = reference.IndexOf(';');
        var namespaceSeparator = reference.IndexOf(':');
        Assert.True(namespaceSeparator > 1 && namespaceSeparator < separator, reference);
        var resourceTable = reference[1..namespaceSeparator];
        var key = reference[(namespaceSeparator + 1)..separator];
        var assembly = reference[(separator + 1)..];
        var (resourceType, resourceManager) = assembly switch
        {
            @"..\Microsoft.CodeAnalysis.Workspaces.dll" => (typeof(WorkspacesResources), WorkspacesResources.ResourceManager),
            @"..\Microsoft.CodeAnalysis.EditorFeatures.dll" => (typeof(EditorFeaturesResources), EditorFeaturesResources.ResourceManager),
            @"..\Microsoft.VisualStudio.LanguageServices.VisualBasic.dll" => (typeof(BasicVSResources), BasicVSResources.ResourceManager),
            @"..\Microsoft.VisualStudio.LanguageServices.CSharp.dll" => (typeof(Microsoft.VisualStudio.LanguageServices.CSharp.CSharpVSResources), Microsoft.VisualStudio.LanguageServices.CSharp.CSharpVSResources.ResourceManager),
            @"..\Microsoft.VisualStudio.LanguageServices.dll" => (typeof(Microsoft.VisualStudio.LanguageServices.ServicesVSResources), Microsoft.VisualStudio.LanguageServices.ServicesVSResources.ResourceManager),
            _ => throw new ArgumentException("Unexpected Advanced resource assembly", nameof(reference)),
        };
        Assert.Equal(resourceType.FullName, resourceTable);
        Assert.Contains(resourceTable + ".resources", resourceType.Assembly.GetManifestResourceNames());
        var value = resourceManager.GetString(key, CultureInfo.GetCultureInfo("en"));
        Assert.False(string.IsNullOrEmpty(value), reference);
    }

    [Theory]
    [InlineData("csharpSettings.registration.json", 95)]
    [InlineData("visualBasicSettings.registration.json", 88)]
    [InlineData("roslynSettings.registration.json", 13)]
    [InlineData("csharpAdvancedSettings.registration.draft.json", 95)]
    [InlineData("visualBasicAdvancedSettings.registration.draft.json", 88)]
    [InlineData("roslynAdvancedSettings.registration.draft.json", 13)]
    public void AdvancedDllResourcesUseTheirDeclaredNativeResourceTable(string manifest, int expectedReferences)
    {
        using var document = LoadLive(manifest);
        var count = 0;
        foreach (var group in document.RootElement.EnumerateObject().Where(group => group.Name is "categories" or "properties"))
        {
            foreach (var entry in group.Value.EnumerateObject().Where(entry => entry.Name.Contains(".advanced.") || entry.Name.EndsWith(".advanced", StringComparison.Ordinal)))
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

    [Theory]
    [InlineData("@Display_diagnostics_inline;..\\Microsoft.VisualStudio.LanguageServices.dll")]
    [InlineData("@Wrong.Resource.Table:Display_diagnostics_inline;..\\Microsoft.VisualStudio.LanguageServices.dll")]
    public void AdvancedResourceValidationRejectsMissingOrIncorrectTable(string reference)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => VerifyResource(reference));

    [Fact]
    public void LiveRegistrationsUseVerifiedContinuousMigrationModes()
    {
        var count = 0;
        foreach (var manifest in new[] { "csharpSettings.registration.json", "visualBasicSettings.registration.json", "roslynSettings.registration.json" })
        {
            using var document = LoadLive(manifest);
            using var draft = Load(manifest);
            var properties = document.RootElement.GetProperty("properties");
            foreach (var property in properties.EnumerateObject().Where(property => property.Name.Contains(".advanced.")))
            {
                var rule = property.Value.GetProperty("migration").EnumerateObject().Single().Value;
                var logging = property.Name == "languages.dotnet.advanced.enableFileLoggingForDiagnostics";
                Assert.Equal(logging ? "VsUserSettingsRegistry" : "SettingsManager", GetInput(rule).GetProperty("store").GetString());
                if (logging || property.Name == "languages.dotnet.advanced.openStackTraceExplorerOnFocus")
                {
                    Assert.Equal(logging ? @"Roslyn\Internal\Diagnostics\EnableFileLoggingForDiagnostics" : "StackTraceExplorer.Options.OpenOnFocus", GetInput(rule).GetProperty("path").GetString());
                    Assert.Equal("full", rule.GetProperty("mode").GetString());
                    Assert.Equal("boolean", property.Value.GetProperty("type").GetString());
                    Assert.False(property.Value.GetProperty("default").GetBoolean());
                    Assert.False((bool)(logging ? VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics : StackTraceExplorerOptionsStorage.OpenOnFocus).Definition.DefaultValue!);
                }
                else
                {
                    Assert.StartsWith("TextEditor.", GetInput(rule).GetProperty("path").GetString());
                    Assert.Equal("full", rule.GetProperty("mode").GetString());
                }
                Assert.Contains(property.Value.GetProperty("type").GetString(), new[] { "boolean", "string" });
                Assert.Equal(JsonSerializer.Serialize(draft.RootElement.GetProperty("properties").GetProperty(property.Name)), JsonSerializer.Serialize(property.Value));
                count++;
            }
        }

        Assert.Equal(135, count);
    }

    private static JsonElement GetInput(JsonElement rule)
        => rule.TryGetProperty("input", out var input) ? input : rule.GetProperty("inputs").EnumerateArray().Single();

    private static JsonDocument Load(string manifest)
        => LoadLive(manifest switch
        {
            "csharpSettings.registration.json" => "csharpAdvancedSettings.registration.draft.json",
            "visualBasicSettings.registration.json" => "visualBasicAdvancedSettings.registration.draft.json",
            "roslynSettings.registration.json" => "roslynAdvancedSettings.registration.draft.json",
            _ => throw new ArgumentException(nameof(manifest)),
        });

    private static JsonDocument LoadLive(string manifest)
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
