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
/// Validates shipping Advanced registrations and their legacy migrations.
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
        Assert.Equal(count + (language == LanguageNames.CSharp ? 5 : 0), properties.EnumerateObject().Count(property => property.Name.StartsWith(prefix, StringComparison.Ordinal)));
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

        var sourceGeneratorExecution = properties.GetProperty("languages.dotnet.advanced.sourceGeneratorExecution");
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
        Assert.Equal(count + (languageId == "csharp" ? 2 : 0), properties.EnumerateObject().Count(property => property.Name.StartsWith(prefix, StringComparison.Ordinal) && property.Value.TryGetProperty("enableWhen", out _)));
        foreach (var (path, dependency) in expected)
            Assert.Equal($"${{config:{prefix}{dependency}}}=='true'", properties.GetProperty(prefix + path).GetProperty("enableWhen").GetString());

        var checkbox = properties.GetProperty(prefix + "analysis.enableInlineDiagnostics");
        var location = properties.GetProperty(prefix + "analysis.inlineDiagnosticsLocation");
        Assert.Equal("Display diagnostics inline", TestModel.Utilities.EvalResource(checkbox.GetProperty("title").GetString()!));
        Assert.Equal("Location", TestModel.Utilities.EvalResource(location.GetProperty("title").GetString()!));
        Assert.True(location.GetProperty("order").GetInt32() > checkbox.GetProperty("order").GetInt32());
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
    public void EnumMappingsDeclareFallbackAndResetWithoutLosingExplicitDefaultValues()
    {
        var count = 0;
        foreach (var manifest in new[] { "csharpSettings.registration.json", "visualBasicSettings.registration.json", "roslynSettings.registration.json" })
        {
            using var document = Load(manifest);
            foreach (var property in document.RootElement.GetProperty("properties").EnumerateObject().Where(property => property.Name.Contains(".advanced.")))
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
    [InlineData("csharpSettings.registration.json", "languages.csharp.advanced.inheritanceMargin.showInheritanceMargin")]
    [InlineData("visualBasicSettings.registration.json", "languages.basic.advanced.inheritanceMargin.showInheritanceMargin")]
    [InlineData("roslynSettings.registration.json", "languages.dotnet.advanced.offerRemoveUnusedReferences")]
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
    [InlineData("csharpSettings.registration.json", 95)]
    [InlineData("visualBasicSettings.registration.json", 88)]
    [InlineData("roslynSettings.registration.json", 13)]
    public void AdvancedDllResourcesUseTheirDeclaredNativeResourceTable(string manifest, int expectedReferences)
    {
        using var document = Load(manifest);
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
