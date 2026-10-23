// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsProviderTests
{
    private static readonly JoinableTaskContext s_context = new();

    [Theory]
    [InlineData("symbolSpecifications")]
    [InlineData("namingStyles")]
    public async Task EmptyDefinitionNamesUseTheNeutralLocalizedPromptAsync(string moniker)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var result = await provider.GetValueAsync<IReadOnlyList<IDictionary<string, object>>>(moniker, CancellationToken.None);
        var rows = Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<IDictionary<string, object>>>.Success>(result).Value;
        rows[0]["name"] = " ";
        var content = rows.Select(item => (IReadOnlyDictionary<string, object>)item.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();

        var messages = provider.ValidateArrayItemProperty(moniker, 0, "name", content);
        Assert.Equal(1, messages.Count);
        Assert.Equal(ServicesVSResources.Enter_a_name, messages[0].Text);
        var failure = Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.SetValueAsync(moniker, rows, CancellationToken.None));
        Assert.Equal(string.Format(ServicesVSResources.Unable_to_update_naming_settings_0, ServicesVSResources.Enter_a_name), failure.ErrorMessage);
    }

    [Fact]
    public async Task InvalidArrayShapesUseLocalizedErrorsAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);

        var readFailure = Assert.IsType<ExternalSettingOperationResult<string>.Failure>(
            await provider.GetValueAsync<string>("rules", CancellationToken.None));
        Assert.Equal(string.Format(ServicesVSResources.Unable_to_update_naming_settings_0,
            ServicesVSResources.Naming_settings_must_be_requested_as_an_array_of_objects), readFailure.ErrorMessage);

        var writeFailure = Assert.IsType<ExternalSettingOperationResult.Failure>(
            await provider.SetValueAsync("rules", "not an array", CancellationToken.None));
        Assert.Equal(string.Format(ServicesVSResources.Unable_to_update_naming_settings_0,
            ServicesVSResources.Naming_settings_must_be_an_array_of_objects), writeFailure.ErrorMessage);
    }

    [Fact]
    public async Task RemovingAReferencedStyleUsesALocalizedValidationDetailAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var graph = manager.Reader.Graph;
        var styles = graph.NamingStyles.Where(item => !Equals(item["id"], graph.Rules[0]["namingStyleId"])).ToArray();

        var messages = provider.ValidateSetting("namingStyles", styles);

        Assert.Equal(1, messages.Count);
        var expected = string.Format(ServicesVSResources.Unable_to_update_naming_settings_0,
            ServicesVSResources.A_naming_rule_refers_to_an_unknown_symbol_specification_or_naming_style);
        Assert.Equal(expected, messages[0].Text);
        var failure = Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.SetValueAsync("namingStyles", styles, CancellationToken.None));
        Assert.Equal(expected, failure.ErrorMessage);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public void SynchronousValidationCanRunBeforeTheFirstAsynchronousRead(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = new NamingSettingsProvider(CreateCoordinator(manager, language), language);

        Assert.Equal(0, provider.ValidateSetting("rules", manager.Reader.Graph.Rules).Count);
        Assert.Null(manager.LastWriter);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task ResetGraphRemovesAllThreeCustomizedArraysAsync(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var defaults = NamingSettingsGraph.Default;
        var specification = defaults.SymbolSpecifications[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        specification["symbolKinds"] = new[] { "class", "struct" };
        var style = defaults.NamingStyles[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        style["prefix"] = "Custom";
        var rule = defaults.Rules[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        rule["severity"] = "warning";
        var customized = new NamingSettingsGraph(defaults.SymbolSpecifications.SetItem(0, specification), defaults.NamingStyles.SetItem(0, style), defaults.Rules.SetItem(0, rule));
        new NamingSettingsStore(manager, language).WriteGraph(customized, "Customize", Guid.NewGuid());
        Assert.Equal(3, manager.CustomizedArrays.Count);
        using var provider = new NamingSettingsProvider(CreateCoordinator(manager, language), language);
        var pendingRules = customized.Rules.Select(item => item.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
        pendingRules[0]["severity"] = "error";
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.SetValueAsync("rules", pendingRules, CancellationToken.None));

        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.ResetAsync(CancellationToken.None));

        Assert.Empty(manager.CustomizedArrays);
        Assert.True(defaults.ConfigurationEquals(manager.Reader.Graph));
        Assert.True(defaults.ConfigurationEquals(await provider.GetGraphAsync(CancellationToken.None)));
        var writer = manager.LastWriter;
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Same(writer, manager.LastWriter);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task EditConcurrentWithResetDoesNotRestorePreResetDefinitionsAsync(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var graph = NamingSettingsGraph.Default;
        var unusedStyle = graph.NamingStyles[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        unusedStyle["id"] = "unused_style";
        unusedStyle["name"] = "Unused style";
        unusedStyle.Remove("defaultName");
        manager.Reader.Graph = new(graph.SymbolSpecifications, graph.NamingStyles.Add(unusedStyle), graph.Rules);
        using var provider = new NamingSettingsProvider(CreateCoordinator(manager, language), language);
        await provider.RefreshCacheAsync(CancellationToken.None);
        var rules = graph.Rules.Select(item => item.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
        rules[0]["severity"] = "warning";
        var committing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var resume = new ManualResetEventSlim();
        manager.BeforeCommit = () =>
        {
            manager.BeforeCommit = null;
            committing.SetResult(true);
            resume.Wait();
        };

        var reset = Task.Run(() => provider.ResetAsync(CancellationToken.None));
        await committing.Task;
        var edit = provider.SetValueAsync("rules", rules, CancellationToken.None);
        resume.Set();

        Assert.IsType<ExternalSettingOperationResult.Success>(await reset);
        Assert.IsType<ExternalSettingOperationResult.Success>(await edit);
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.DoesNotContain(manager.Reader.Graph.NamingStyles, style => Equals(style["id"], "unused_style"));
        Assert.Equal("warning", manager.Reader.Graph.Rules[0]["severity"]);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task FailedResetPreservesBufferedEditsAsync(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock { CommitOutcome = SettingCommitOutcome.InternalError };
        using var provider = new NamingSettingsProvider(CreateCoordinator(manager, language), language);
        var rules = manager.Reader.Graph.Rules.Select(item => item.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
        rules[0]["severity"] = "warning";
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.SetValueAsync("rules", rules, CancellationToken.None));

        var failure = Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.ResetAsync(CancellationToken.None));

        Assert.Equal("warning", (await provider.GetGraphAsync(CancellationToken.None)).Rules[0]["severity"]);
        Assert.Equal(string.Format(ServicesVSResources.Unable_to_update_naming_settings_0,
            ServicesVSResources.Naming_preferences_could_not_be_reset_Try_again), failure.ErrorMessage);
        manager.CommitOutcome = SettingCommitOutcome.Success;
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Equal("warning", manager.Reader.Graph.Rules[0]["severity"]);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task ResetRecoversFromInvalidPersistedGraphAsync(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var invalidRules = manager.Reader.Graph.Rules.Select(item => item.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
        invalidRules[0]["namingStyleId"] = "missing-style";
        manager.Reader.Graph = new(manager.Reader.Graph.SymbolSpecifications, manager.Reader.Graph.NamingStyles, invalidRules.Cast<IReadOnlyDictionary<string, object>>().ToImmutableArray());
        using var provider = new NamingSettingsProvider(CreateCoordinator(manager, language), language);

        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.ResetAsync(CancellationToken.None));

        Assert.True(NamingSettingsGraph.Default.ConfigurationEquals(manager.Reader.Graph));
    }

    [Theory]
    [InlineData(LanguageNames.CSharp, false)]
    [InlineData(LanguageNames.CSharp, true)]
    [InlineData(LanguageNames.VisualBasic, false)]
    [InlineData(LanguageNames.VisualBasic, true)]
    public async Task LegacyPublicationCanBeRetriedUnlessNativeStateChangedAsync(string language, bool concurrentChange)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var legacy = NamingStylePreferences.Default;
        var failWrite = true;
        var refreshes = new List<NamingStylePreferences>();
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, language), () => legacy, value =>
        {
            if (failWrite)
                throw new InvalidOperationException("Injected legacy write failure.");
            legacy = value;
        }, s_context, refreshes.Add);
        using var provider = new NamingSettingsProvider(coordinator, language);
        var original = manager.Reader.Graph;
        var rules = original.Rules.Select(item => item.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
        rules[0]["severity"] = "error";
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.SetValueAsync("rules", rules, CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Equal("error", manager.Reader.Graph.Rules[0]["severity"]);
        Assert.Equal(NamingStylePreferences.Default, legacy);
        Assert.Empty(refreshes);
        var writer = manager.LastWriter;
        failWrite = false;
        if (concurrentChange)
        {
            var changedRule = original.Rules[0].ToDictionary(pair => pair.Key, pair => pair.Value);
            changedRule["severity"] = "warning";
            manager.Reader.Graph = new(original.SymbolSpecifications, original.NamingStyles, original.Rules.SetItem(0, changedRule));
            coordinator.NotifyChanged();
        }

        var result = await provider.CommitPendingChangesAsync(CancellationToken.None);

        if (concurrentChange)
        {
            Assert.IsType<ExternalSettingOperationResult.Failure>(result);
            Assert.Equal(NamingStylePreferences.Default, legacy);
            Assert.Empty(refreshes);
            Assert.Equal("error", (await provider.GetGraphAsync(CancellationToken.None)).Rules[0]["severity"]);
        }
        else
        {
            Assert.IsType<ExternalSettingOperationResult.Success>(result);
            Assert.Equal(manager.Reader.Graph.ToPreferences(), legacy);
            Assert.Equal(legacy, Assert.Single(refreshes));
            Assert.IsType<ExternalSettingOperationResult.Success>(await provider.CommitPendingChangesAsync(CancellationToken.None));
        }
        Assert.Same(writer, manager.LastWriter);
    }

    [Fact]
    public async Task IncomingNativeChangeCanBeValidatedImmediatelyDuringNotificationAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var coordinator = CreateCoordinator(manager);
        using var provider = new NamingSettingsProvider(coordinator);
        await provider.GetGraphAsync(CancellationToken.None);
        var rules = manager.Reader.Graph.Rules.Select(item => item.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
        rules[0]["severity"] = "warning";
        manager.Reader.Graph = new(manager.Reader.Graph.SymbolSpecifications, manager.Reader.Graph.NamingStyles, rules.Cast<IReadOnlyDictionary<string, object>>().ToImmutableArray());
        var validations = 0;
        provider.RevalidationRequired += (_, _) =>
        {
            Assert.Equal(0, provider.ValidateSetting("rules", manager.Reader.Graph.Rules).Count);
            validations++;
        };

        coordinator.NotifyChanged();

        Assert.Equal(1, validations);
        Assert.Equal("warning", (await provider.GetGraphAsync(CancellationToken.None)).Rules[0]["severity"]);
        Assert.Null(manager.LastWriter);
    }

    [Fact]
    public void ColdValidationReportsTheActualNativeReadFailure()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        manager.Reader.Outcome = SettingRetrievalOutcome.FailedValidation;
        using var provider = CreateProvider(manager);

        var messages = provider.ValidateSetting("rules", manager.Reader.Graph.Rules);

        Assert.Equal(1, messages.Count);
        Assert.Contains(nameof(SettingRetrievalOutcome.FailedValidation), messages[0].Text);
        Assert.Null(manager.LastWriter);
    }

    [Fact]
    public async Task ActualShellDictionaryContractIsEditableAndDetachedAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var result = await provider.GetValueAsync<IReadOnlyList<IDictionary<string, object>>>("rules", CancellationToken.None);
        var rules = Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<IDictionary<string, object>>>.Success>(result).Value;
        rules[0]["severity"] = "error";
        Assert.Equal("suggestion", (await provider.GetGraphAsync(CancellationToken.None)).Rules[0]["severity"]);
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.SetValueAsync("rules", rules, CancellationToken.None));
        rules[0]["severity"] = "warning";
        Assert.Equal("error", (await provider.GetGraphAsync(CancellationToken.None)).Rules[0]["severity"]);
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Equal(ReportDiagnostic.Error, manager.Reader.Graph.ToPreferences().Rules.NamingRules[0].EnforcementLevel);
    }

    [Fact]
    public async Task RefreshCancelsBufferedEditsWithoutWritingAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var rules = manager.Reader.Graph.Rules.Select(item => item.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
        rules[0]["severity"] = "warning";
        await provider.SetValueAsync("rules", rules, CancellationToken.None);
        await provider.RefreshCacheAsync(CancellationToken.None);
        await provider.CommitPendingChangesAsync(CancellationToken.None);

        Assert.Null(manager.LastWriter);
        Assert.Equal(NamingStylePreferences.Default, manager.Reader.Graph.ToPreferences());
    }

    [Fact]
    public async Task IncomingNativeEditDoesNotDiscardDraftOrPermitStaleCommitAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var coordinator = CreateCoordinator(manager);
        using var provider = new NamingSettingsProvider(coordinator);
        var rules = manager.Reader.Graph.Rules.Select(item => item.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
        rules[0]["severity"] = "warning";
        await provider.SetValueAsync("rules", rules, CancellationToken.None);
        manager.Reader.Graph = new([], [], []);
        coordinator.NotifyChanged();

        Assert.Equal("warning", (await provider.GetGraphAsync(CancellationToken.None)).Rules[0]["severity"]);
        Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Null(manager.LastWriter);
        await provider.RefreshCacheAsync(CancellationToken.None);
        Assert.Empty((await provider.GetGraphAsync(CancellationToken.None)).Rules);
    }

    [Fact]
    public async Task CorrectedGraphResolvesRegionErrorAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        manager.Reader.Outcome = SettingRetrievalOutcome.FailedValidation;
        Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.RefreshCacheAsync(CancellationToken.None));
        manager.Reader.Outcome = SettingRetrievalOutcome.Success;
        var resolved = 0;
        provider.ErrorConditionResolved += (_, _) => resolved++;

        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.RefreshCacheAsync(CancellationToken.None));
        Assert.Equal(1, resolved);
    }

    [Fact]
    public async Task RulesAreReadFromTheRegisteredJsonArraysAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);

        var result = await provider.GetValueAsync<IReadOnlyList<IReadOnlyDictionary<string, object>>>("rules", CancellationToken.None);

        var success = Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<IReadOnlyDictionary<string, object>>>.Success>(result);
        Assert.Equal(manager.Reader.Graph.Rules, success.Value);
        Assert.All(success.Value, rule => Assert.DoesNotContain("legacyId", rule.Keys));
    }

    [Fact]
    public async Task BufferedRuleEditPreservesBothDefinitionCollectionsAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        manager.Reader.Graph = NamingSettingsGraph.Default;
        using var provider = CreateProvider(manager);
        var original = manager.Reader.Graph;
        await provider.RefreshCacheAsync(CancellationToken.None);
        var rules = original.Rules.Select(item => item.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)).ToArray();
        rules[0]["severity"] = "warning";

        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.SetValueAsync("rules", rules, CancellationToken.None));
        Assert.Null(manager.LastWriter);
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.CommitPendingChangesAsync(CancellationToken.None));

        Assert.Equal(ReportDiagnostic.Warn, manager.Reader.Graph.ToPreferences().Rules.NamingRules[0].EnforcementLevel);
        Assert.Equal(JsonSerializer.Serialize(NamingSettingsStorageSerializer.SerializeSpecifications(original.SymbolSpecifications)), JsonSerializer.Serialize(NamingSettingsStorageSerializer.SerializeSpecifications(manager.Reader.Graph.SymbolSpecifications)));
        Assert.Equal(JsonSerializer.Serialize(NamingSettingsStorageSerializer.SerializeStyles(original.NamingStyles)), JsonSerializer.Serialize(NamingSettingsStorageSerializer.SerializeStyles(manager.Reader.Graph.NamingStyles)));
    }

    [Fact]
    public async Task PendingApprovalIsNotReportedAsAnAppliedEditAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock { CommitOutcome = SettingCommitOutcome.PendingApproval };
        using var provider = CreateProvider(manager);
        await provider.RefreshCacheAsync(CancellationToken.None);
        var rules = manager.Reader.Graph.Rules.Select(item => item.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)).ToArray();
        rules[0]["severity"] = "warning";
        await provider.SetValueAsync("rules", rules, CancellationToken.None);

        var failure = Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.CommitPendingChangesAsync(CancellationToken.None));

        Assert.Equal(string.Format(ServicesVSResources.Unable_to_update_naming_settings_0,
            ServicesVSResources.Naming_settings_could_not_be_saved_Refresh_and_try_again), failure.ErrorMessage);
        Assert.Equal(NamingStylePreferences.Default, manager.Reader.Graph.ToPreferences());
    }

    [Fact]
    public async Task LastRuleCannotSilentlyDiscardUnusedDefinitionsAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);

        var result = await provider.SetValueAsync("rules", Array.Empty<IReadOnlyDictionary<string, object>>(), CancellationToken.None);

        Assert.IsType<ExternalSettingOperationResult.Success>(result);
        Assert.Null(manager.LastWriter);
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.CommitPendingChangesAsync(CancellationToken.None));
        var preferences = manager.Reader.Graph.ToPreferences();
        Assert.Empty(preferences.Rules.NamingRules);
        Assert.True(NamingStylePreferences.Default.SymbolSpecifications.SequenceEqual(preferences.SymbolSpecifications));
        Assert.True(NamingStylePreferences.Default.NamingStyles.SequenceEqual(preferences.NamingStyles));
    }

    [Fact]
    public async Task DynamicReferenceChoicesUseReadableIdsAndDisplayNamesAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);

        var result = await provider.GetEnumChoicesAsync("rules.symbolSpecificationId", CancellationToken.None);

        var choices = Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>.Success>(result).Value;
        Assert.Equal(manager.Reader.Graph.SymbolSpecifications.Length, choices.Count);
        Assert.Equal("class", choices[0].Moniker);
        Assert.Equal(NamingStylePreferences.Default.SymbolSpecifications[0].Name, choices[0].Title);
    }

    [Fact]
    public async Task SeverityChoicesContainLocalizedTitlesInsteadOfResourceReferencesAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        using var provider = CreateProvider(manager);
        var result = await provider.GetEnumChoicesAsync("rules.severity", CancellationToken.None);

        var choices = Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>.Success>(result).Value;
        Assert.Equal(new[] { "silent", "suggestion", "warning", "error" }, choices.Select(choice => choice.Moniker));
        Assert.Equal(new[] { WorkspacesResources.Refactoring_Only, WorkspacesResources.Suggestion, WorkspacesResources.Warning, WorkspacesResources.Error }, choices.Select(choice => choice.Title));
        Assert.All(choices, choice => Assert.DoesNotContain("@", choice.Title));
    }

    [Fact]
    public async Task InvalidJsonProducesAnExplicitRegionErrorAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        manager.Reader.Graph = new NamingSettingsGraph([], manager.Reader.Graph.NamingStyles, manager.Reader.Graph.Rules);
        using var provider = CreateProvider(manager);

        var result = await provider.GetValueAsync<IReadOnlyList<IReadOnlyDictionary<string, object>>>("rules", CancellationToken.None);

        var failure = Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<IReadOnlyDictionary<string, object>>>.Failure>(result);
        Assert.Equal(ExternalSettingsErrorScope.EntireRegion, failure.Scope);
        Assert.Contains("Naming", failure.ErrorMessage);
    }

    [Fact]
    public async Task DisposalReleasesCoordinatorNotificationsAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var coordinator = CreateCoordinator(manager);
        var provider = new NamingSettingsProvider(coordinator);
        var notifications = 0;
        provider.SettingValuesChanged += (_, _) => notifications++;
        provider.Dispose();

        await coordinator.SynchronizeFromUnifiedSettingsAsync(CancellationToken.None);

        Assert.Equal(0, notifications);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task ImportedKindsRemainSelectableAndSurviveDefinitionEditsAsync(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var coordinator = CreateCoordinator(manager);
        using var provider = new NamingSettingsProvider(coordinator, language);
        await provider.GetGraphAsync(CancellationToken.None);
        var graph = manager.Reader.Graph;
        var specification = graph.SymbolSpecifications[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        specification["symbolKinds"] = new[] { "class", "pointer" };
        manager.Reader.Graph = new(graph.SymbolSpecifications.SetItem(0, specification), graph.NamingStyles, graph.Rules);
        var notifications = new List<string>();
        provider.EnumSettingChoicesChanged += (_, e) => notifications.Add(e.SettingMoniker);
        coordinator.NotifyChanged();
        Assert.Contains("symbolSpecifications.symbolKinds", notifications);
        var choices = Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>.Success>(await provider.GetEnumChoicesAsync("symbolSpecifications.symbolKinds", CancellationToken.None)).Value;
        Assert.Contains(choices, choice => choice.Moniker == "pointer");
        var items = Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<IDictionary<string, object>>>.Success>(await provider.GetValueAsync<IReadOnlyList<IDictionary<string, object>>>("symbolSpecifications", CancellationToken.None)).Value;
        items[0]["name"] = "Imported specification";
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.SetValueAsync("symbolSpecifications", items, CancellationToken.None));
        Assert.IsType<ExternalSettingOperationResult.Success>(await provider.CommitPendingChangesAsync(CancellationToken.None));
        Assert.Equal(new[] { "class", "pointer" }, NamingSettingsGraph.ReadStrings(manager.Reader.Graph.SymbolSpecifications[0], "symbolKinds"));
    }

    private static NamingSettingsProvider CreateProvider(NamingSettingsStoreTests.SettingsManagerMock manager)
        => new(CreateCoordinator(manager));

    private static NamingSettingsCoordinator CreateCoordinator(NamingSettingsStoreTests.SettingsManagerMock manager, string language = LanguageNames.CSharp)
    {
        var legacy = NamingStylePreferences.Default;
        return new(new NamingSettingsStore(manager, language), () => legacy, value => legacy = value, s_context);
    }
}
