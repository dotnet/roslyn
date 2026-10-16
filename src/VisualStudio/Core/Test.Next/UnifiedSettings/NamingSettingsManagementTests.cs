// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsManagementTests
{
    private static readonly JoinableTaskContext s_context = new();

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task DefinitionArraysAreDetachedAndNoOpEditsPreserveLocalizedDefaultNames(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager, language);
        var specifications = await ReadAsync(provider, "symbolSpecifications");
        Assert.Equal(manager.Reader.Graph.ToPreferences().SymbolSpecifications[0].Name, specifications[0]["name"]);
        ((string[])specifications[0]["symbolKinds"])[0] = "struct";
        Assert.Equal("class", NamingSettingsGraph.ReadStrings(manager.Reader.Graph.SymbolSpecifications[0], "symbolKinds")[0]);
        specifications = await ReadAsync(provider, "symbolSpecifications");
        AssertSuccess(await provider.SetValueAsync("symbolSpecifications", specifications, CancellationToken.None));
        AssertSuccess(await provider.SetValueAsync("namingStyles", await ReadAsync(provider, "namingStyles"), CancellationToken.None));
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.True(NamingSettingsGraph.Default.ConfigurationEquals(manager.Reader.Graph));
        Assert.All(manager.Reader.Graph.SymbolSpecifications.Concat(manager.Reader.Graph.NamingStyles), item => Assert.Equal("", item["name"]));
    }

    [Theory]
    [InlineData("namingStyles", 0)]
    [InlineData("symbolSpecifications", 1)]
    public async Task ReferencedDefinitionsCannotBeDeleted(string moniker, int index)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var items = await ReadAsync(provider, moniker);
        var remaining = items.Where((_, itemIndex) => itemIndex != index).ToArray();
        Assert.Equal(1, provider.ValidateSetting(moniker, remaining).Count);
        Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.SetValueAsync(moniker, remaining, CancellationToken.None));
        Assert.Null(manager.LastWriter);
        Assert.True(NamingSettingsGraph.Default.ConfigurationEquals(await provider.GetGraphAsync(CancellationToken.None)));
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task BufferedEditsPreserveIdentitiesAndRefreshReferenceLabelsWhileCancelDiscardsThem(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager, language);
        var styles = await ReadAsync(provider, "namingStyles");
        styles[0]["name"] = "Custom style";
        styles[0]["prefix"] = "Custom";
        AssertSuccess(await provider.SetValueAsync("namingStyles", styles, CancellationToken.None));
        styles[0]["prefix"] = "Mutated caller";
        var graph = await provider.GetGraphAsync(CancellationToken.None);
        Assert.Equal("Custom", graph.NamingStyles[0]["prefix"]);
        Assert.Equal(NamingSettingsGraph.Default.NamingStyles[0]["id"], graph.NamingStyles[0]["id"]);
        Assert.Equal(NamingSettingsGraph.Default.ToPreferences().NamingStyles[0].ID, graph.ToPreferences().NamingStyles[0].ID);
        var choices = Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>.Success>(await provider.GetEnumChoicesAsync("rules.namingStyleId", CancellationToken.None)).Value;
        Assert.Equal("Custom style", choices[0].Title);
        Assert.Null(manager.LastWriter);
        await provider.RefreshCacheAsync(CancellationToken.None);
        Assert.True(NamingSettingsGraph.Default.ConfigurationEquals(await provider.GetGraphAsync(CancellationToken.None)));
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task NewDefinitionsAndTheirRuleCommitAsOneValidatedGraph(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager, language);
        var specifications = (await ReadAsync(provider, "symbolSpecifications")).ToList();
        specifications.Add(new Dictionary<string, object>
        {
            ["name"] = "Custom specification",
            ["symbolKinds"] = new[] { "class", "interface" },
            ["accessibilities"] = new[] { "public", "internal" },
            ["requiredModifiers"] = new[] { "abstract" },
        });
        AssertSuccess(await provider.SetValueAsync("symbolSpecifications", specifications, CancellationToken.None));
        var styles = (await ReadAsync(provider, "namingStyles")).ToList();
        styles.Add(new Dictionary<string, object>
        {
            ["name"] = "Custom style",
            ["prefix"] = "T",
            ["suffix"] = "",
            ["wordSeparator"] = "_",
            ["capitalization"] = "camelCase",
        });
        AssertSuccess(await provider.SetValueAsync("namingStyles", styles, CancellationToken.None));
        var graph = await provider.GetGraphAsync(CancellationToken.None);
        var rules = (await ReadAsync(provider, "rules")).ToList();
        rules.Add(new Dictionary<string, object>
        {
            ["symbolSpecificationId"] = graph.SymbolSpecifications.Last()["id"],
            ["namingStyleId"] = graph.NamingStyles.Last()["id"],
            ["severity"] = "warning",
        });
        AssertSuccess(await provider.SetValueAsync("rules", rules, CancellationToken.None));
        Assert.Null(manager.LastWriter);
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Equal(1, manager.LastWriter!.CommitCount);
        Assert.Equal(3, manager.LastWriter.QueuedMonikers.Count);
        Assert.Equal("custom_specification", manager.Reader.Graph.SymbolSpecifications.Last()["id"]);
        Assert.Equal("custom_style", manager.Reader.Graph.NamingStyles.Last()["id"]);
        Assert.Equal(ReportDiagnostic.Warn, manager.Reader.Graph.ToPreferences().Rules.NamingRules.Last().EnforcementLevel);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp, false)]
    [InlineData(LanguageNames.CSharp, true)]
    [InlineData(LanguageNames.VisualBasic, false)]
    [InlineData(LanguageNames.VisualBasic, true)]
    public async Task EmptyConfigurationCanBeBuiltAndSavedIncrementally(string language, bool styleFirst)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager, language);
        manager.Reader.Graph = new([], [], []);
        foreach (var moniker in styleFirst ? new[] { "namingStyles", "symbolSpecifications" } : new[] { "symbolSpecifications", "namingStyles" })
        {
            var definition = moniker == "namingStyles"
                ? new Dictionary<string, object>
                {
                    ["name"] = "Custom style",
                    ["prefix"] = "T",
                    ["suffix"] = "",
                    ["wordSeparator"] = "_",
                    ["capitalization"] = "camelCase",
                }
                : new Dictionary<string, object>
                {
                    ["name"] = "Custom specification",
                    ["symbolKinds"] = new[] { "class" },
                    ["accessibilities"] = new[] { "public" },
                    ["requiredModifiers"] = Array.Empty<string>(),
                };
            AssertSuccess(await provider.SetValueAsync(moniker, new[] { definition }, CancellationToken.None));
            Assert.Single(await ReadAsync(provider, moniker));
            AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
            Assert.Empty(manager.Reader.Graph.Rules);
        }

        var graph = await provider.GetGraphAsync(CancellationToken.None);
        var rule = new Dictionary<string, object>
        {
            ["symbolSpecificationId"] = graph.SymbolSpecifications.Single()["id"],
            ["namingStyleId"] = graph.NamingStyles.Single()["id"],
            ["severity"] = "warning",
        };
        AssertSuccess(await provider.SetValueAsync("rules", new[] { rule }, CancellationToken.None));
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        var preferences = manager.Reader.Graph.ToPreferences();
        Assert.Equal("Custom specification", Assert.Single(preferences.SymbolSpecifications).Name);
        Assert.Equal("Custom style", Assert.Single(preferences.NamingStyles).Name);
        Assert.Equal(ReportDiagnostic.Warn, Assert.Single(preferences.Rules.NamingRules).EnforcementLevel);
    }

    [Fact]
    public async Task UnusedDefinitionCanBeRemovedWithoutChangingRules()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var specifications = (await ReadAsync(provider, "symbolSpecifications")).Skip(1).ToArray();
        AssertSuccess(await provider.SetValueAsync("symbolSpecifications", specifications, CancellationToken.None));
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Equal(NamingSettingsGraph.Default.SymbolSpecifications.Length - 1, manager.Reader.Graph.SymbolSpecifications.Length);
        Assert.Equal(JsonSerializer.Serialize(NamingSettingsGraph.Default.Rules), JsonSerializer.Serialize(manager.Reader.Graph.Rules));
    }

    [Theory]
    [InlineData("id")]
    [InlineData("legacyId")]
    public async Task CompatibilityIdentitiesCannotBeEdited(string property)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var styles = await ReadAsync(provider, "namingStyles");
        styles[0][property] = property == "id" ? "changed_id" : Guid.NewGuid().ToString();
        Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.SetValueAsync("namingStyles", styles, CancellationToken.None));
        Assert.Null(manager.LastWriter);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp, "internal", "static", "localFunction")]
    [InlineData(LanguageNames.VisualBasic, "Friend", "Shared", "module")]
    public async Task DefinitionChoicesUseLanguageSpecificLabels(string language, string accessibility, string modifier, string kind)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager, language);
        async Task<IReadOnlyList<EnumChoice>> Choices(string moniker)
            => Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>.Success>(await provider.GetEnumChoicesAsync(moniker, CancellationToken.None)).Value;
        Assert.Equal(accessibility, (await Choices("symbolSpecifications.accessibilities")).Single(choice => choice.Moniker == "internal").Title);
        Assert.Equal(modifier, (await Choices("symbolSpecifications.requiredModifiers")).Single(choice => choice.Moniker == "static").Title);
        Assert.Contains(await Choices("symbolSpecifications.symbolKinds"), choice => choice.Moniker == kind);
        Assert.Equal(5, (await Choices("namingStyles.capitalization")).Count);
    }

    [Theory]
    [InlineData("namingStyles", "name", "")]
    [InlineData("symbolSpecifications", "name", " ")]
    [InlineData("namingStyles", "capitalization", "unknown")]
    [InlineData("symbolSpecifications", "symbolKinds", "unknown")]
    public async Task InvalidDefinitionEditsLeaveTheGraphUnchanged(string moniker, string property, string value)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var items = await ReadAsync(provider, moniker);
        items[0][property] = property == "symbolKinds" ? new[] { value } : value;
        Assert.Equal(1, provider.ValidateSetting(moniker, items).Count);
        Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.SetValueAsync(moniker, items, CancellationToken.None));
        Assert.True(NamingSettingsGraph.Default.ConfigurationEquals(await provider.GetGraphAsync(CancellationToken.None)));
        Assert.Null(manager.LastWriter);
    }

    [Fact]
    public async Task NewDefinitionIdentitySurvivesSubsequentRenameAndCommit()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var styles = (await ReadAsync(provider, "namingStyles")).ToList();
        var added = new Dictionary<string, object>(styles[0]);
        added.Remove("id");
        added.Remove("legacyId");
        added["name"] = "New style";
        styles.Add(added);
        AssertSuccess(await provider.SetValueAsync("namingStyles", styles, CancellationToken.None));
        var graph = await provider.GetGraphAsync(CancellationToken.None);
        var id = graph.NamingStyles.Last()["id"];
        var guid = graph.ToPreferences().NamingStyles.Last().ID;
        styles = (await ReadAsync(provider, "namingStyles")).ToList();
        styles.Last()["name"] = "Renamed style";
        AssertSuccess(await provider.SetValueAsync("namingStyles", styles, CancellationToken.None));
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Equal(id, manager.Reader.Graph.NamingStyles.Last()["id"]);
        Assert.Equal(guid, manager.Reader.Graph.ToPreferences().NamingStyles.Last().ID);
        Assert.Equal("Renamed style", manager.Reader.Graph.ToPreferences().NamingStyles.Last().Name);
    }

    [Fact]
    public async Task RenamingBothDefinitionsPreservesRuleReferencesAndRefreshesChoices()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var references = manager.Reader.Graph.ToPreferences().Rules.NamingRules[0];
        var notifications = new List<string>();
        provider.EnumSettingChoicesChanged += (_, e) => notifications.Add(e.SettingMoniker);
        var specifications = await ReadAsync(provider, "symbolSpecifications");
        specifications[1]["name"] = "Renamed specification";
        AssertSuccess(await provider.SetValueAsync("symbolSpecifications", specifications, CancellationToken.None));
        var styles = await ReadAsync(provider, "namingStyles");
        styles[1]["name"] = "Renamed style";
        AssertSuccess(await provider.SetValueAsync("namingStyles", styles, CancellationToken.None));
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        var rule = manager.Reader.Graph.ToPreferences().Rules.NamingRules[0];
        Assert.Equal(references.SymbolSpecification.ID, rule.SymbolSpecification.ID);
        Assert.Equal(references.NamingStyle.ID, rule.NamingStyle.ID);
        Assert.Equal("Renamed style", rule.NamingStyle.Name);
        Assert.Contains("rules.symbolSpecificationId", notifications);
        Assert.Contains("rules.namingStyleId", notifications);
    }

    [Fact]
    public async Task FailedCommitRetainsEditsToBothDefinitionTablesForRetry()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock { CommitOutcome = SettingCommitOutcome.PendingApproval };
        using var provider = CreateProvider(manager);
        var specifications = await ReadAsync(provider, "symbolSpecifications");
        specifications[0]["name"] = "Edited specification";
        AssertSuccess(await provider.SetValueAsync("symbolSpecifications", specifications, CancellationToken.None));
        var styles = await ReadAsync(provider, "namingStyles");
        styles[0]["prefix"] = "Edited";
        AssertSuccess(await provider.SetValueAsync("namingStyles", styles, CancellationToken.None));
        Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.True(NamingSettingsGraph.Default.ConfigurationEquals(manager.Reader.Graph));
        manager.CommitOutcome = SettingCommitOutcome.Success;
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Equal("Edited specification", manager.Reader.Graph.SymbolSpecifications[0]["name"]);
        Assert.Equal("Edited", manager.Reader.Graph.NamingStyles[0]["prefix"]);
    }

    [Fact]
    public async Task AnEditArrivingDuringPublicationIsNotDiscarded()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var legacy = NamingSettingsGraph.Default.ToPreferences();
        NamingSettingsProvider? provider = null;
        var editDuringPublication = true;
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, LanguageNames.CSharp), () => legacy, value =>
        {
            legacy = value;
            if (editDuringPublication)
            {
                editDuringPublication = false;
                var styles = ReadAsync(provider!, "namingStyles").GetAwaiter().GetResult();
                styles[0]["suffix"] = "Later";
                AssertSuccess(provider!.SetValueAsync("namingStyles", styles, CancellationToken.None).GetAwaiter().GetResult());
            }
        }, s_context);
        using var ownedProvider = provider = new NamingSettingsProvider(coordinator);
        var initial = await ReadAsync(provider, "namingStyles");
        initial[0]["prefix"] = "First";
        AssertSuccess(await provider.SetValueAsync("namingStyles", initial, CancellationToken.None));
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Equal("", manager.Reader.Graph.NamingStyles[0]["suffix"]);
        Assert.Equal("Later", (await provider.GetGraphAsync(CancellationToken.None)).NamingStyles[0]["suffix"]);
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Equal("Later", manager.Reader.Graph.NamingStyles[0]["suffix"]);
    }

    private static NamingSettingsProvider CreateProvider(NamingSettingsStoreTests.SettingsManagerMock manager, string language = LanguageNames.CSharp)
    {
        manager.Reader.Graph = NamingSettingsGraph.Default;
        var legacy = NamingSettingsGraph.Default.ToPreferences();
        return new(new NamingSettingsCoordinator(new NamingSettingsStore(manager, language), () => legacy, value => legacy = value, s_context), language);
    }

    [Theory]
    [InlineData("symbolSpecifications")]
    [InlineData("namingStyles")]
    public async Task PropertyValidationUsesTheShellsDeclaredFieldsWithoutLosingHiddenIdentities(string moniker)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var rows = (await ReadAsync(provider, moniker)).Select(item =>
            (IReadOnlyDictionary<string, object>)item.Where(pair => pair.Key is not ("id" or "legacyId" or "defaultName"))
                .ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
        for (var index = 0; index < rows.Length; index++)
        {
            foreach (var property in rows[index].Keys)
                Assert.Equal(0, provider.ValidateArrayItemProperty(moniker, index, property, rows).Count);
        }
        var renamed = rows[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        renamed["name"] = "Renamed definition";
        rows[0] = renamed;
        Assert.Equal(0, provider.ValidateArrayItemProperty(moniker, 0, "name", rows).Count);
        renamed["name"] = " ";
        Assert.Equal(1, provider.ValidateArrayItemProperty(moniker, 0, "name", rows).Count);
        Assert.Null(manager.LastWriter);
        Assert.True(NamingSettingsGraph.Default.ConfigurationEquals(await provider.GetGraphAsync(CancellationToken.None)));
    }

    [Theory]
    [InlineData("symbolSpecifications")]
    [InlineData("namingStyles")]
    public async Task PropertyValidationTreatsTheAppendedDialogRowAsANewDefinition(string moniker)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var rows = (await ReadAsync(provider, moniker)).Select(item =>
            (IReadOnlyDictionary<string, object>)item.Where(pair => pair.Key is not ("id" or "legacyId" or "defaultName"))
                .ToDictionary(pair => pair.Key, pair => pair.Value)).ToList();
        var added = rows[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        added["name"] = "New definition";
        rows.Add(added);
        Assert.Equal(0, provider.ValidateArrayItemProperty(moniker, rows.Count - 1, "name", rows).Count);
        Assert.Null(manager.LastWriter);
    }

    private static async Task<IReadOnlyList<IDictionary<string, object>>> ReadAsync(NamingSettingsProvider provider, string moniker)
        => Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<IDictionary<string, object>>>.Success>(await provider.GetValueAsync<IReadOnlyList<IDictionary<string, object>>>(moniker, CancellationToken.None)).Value;

    [Theory]
    [InlineData(LanguageNames.CSharp, "namingStyles", "capitalization")]
    [InlineData(LanguageNames.VisualBasic, "namingStyles", "capitalization")]
    [InlineData(LanguageNames.CSharp, "symbolSpecifications", "symbolKinds")]
    [InlineData(LanguageNames.VisualBasic, "symbolSpecifications", "symbolKinds")]
    public async Task IncompleteNewRowsShowErrorsOnlyOnTheInvalidField(string language, string moniker, string enumProperty)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager, language);
        var rows = (await ReadAsync(provider, moniker)).Select(item =>
            (IReadOnlyDictionary<string, object>)item.Where(pair => pair.Key != "id").ToDictionary(pair => pair.Key, pair => pair.Value)).ToList();
        var added = rows[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        added["name"] = "";
        rows.Add(added);
        var writeRows = (await ReadAsync(provider, moniker)).ToList();
        writeRows.Add(added);
        var index = rows.Count - 1;
        foreach (var property in added.Keys)
            Assert.Equal(property == "name" ? 1 : 0, provider.ValidateArrayItemProperty(moniker, index, property, rows).Count);
        Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.SetValueAsync(moniker, writeRows, CancellationToken.None));

        added["name"] = "New definition";
        added[enumProperty] = moniker == "namingStyles" ? "unknown" : new[] { "unknown" };
        foreach (var property in added.Keys)
            Assert.Equal(property == enumProperty ? 1 : 0, provider.ValidateArrayItemProperty(moniker, index, property, rows).Count);
        Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.SetValueAsync(moniker, writeRows, CancellationToken.None));
        added[enumProperty] = rows[0][enumProperty];
        foreach (var property in added.Keys)
            Assert.Equal(0, provider.ValidateArrayItemProperty(moniker, index, property, rows).Count);
        AssertSuccess(await provider.SetValueAsync(moniker, writeRows, CancellationToken.None));
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Equal(1, manager.LastWriter!.CommitCount);
    }

    [Theory]
    [InlineData("symbolSpecifications")]
    [InlineData("namingStyles")]
    public async Task RevertingABuiltInNameRestoresDefaultStorageInsteadOfKeepingAnOverride(string moniker)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var rows = await ReadAsync(provider, moniker);
        var defaultName = rows[0]["name"];
        rows[0]["name"] = "Edited name";
        AssertSuccess(await provider.SetValueAsync(moniker, rows, CancellationToken.None));
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.EndsWith("." + moniker, Assert.Single(manager.CustomizedArrays).Key);

        rows = await ReadAsync(provider, moniker);
        rows[0]["name"] = defaultName;
        AssertSuccess(await provider.SetValueAsync(moniker, rows, CancellationToken.None));
        AssertSuccess(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Empty(manager.CustomizedArrays);
        Assert.True(NamingSettingsGraph.Default.ConfigurationEquals(manager.Reader.Graph));
    }

    private static void AssertSuccess(ExternalSettingOperationResult result) => Assert.IsType<ExternalSettingOperationResult.Success>(result);
}
