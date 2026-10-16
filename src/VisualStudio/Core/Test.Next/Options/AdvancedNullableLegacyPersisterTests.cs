// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Settings;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed partial class VisualStudioSettingsOptionPersisterTests
{
    public static IEnumerable<object[]> AdvancedNullableStorageKeys()
    {
        yield return ["CSharpInheritanceMargin", "TextEditor.CSharp.Specific.ShowInheritanceMargin"];
        yield return ["VisualBasicInheritanceMargin", "TextEditor.VisualBasic.Specific.ShowInheritanceMargin"];
        yield return ["OfferRemoveUnusedReferences", "TextEditor.OfferRemoveUnusedReferences"];
    }

    private static OptionKey2 GetAdvancedNullableOptionKey(string optionId)
        => optionId switch
        {
            "CSharpInheritanceMargin" => new OptionKey2(InheritanceMarginOptionsStorage.ShowInheritanceMargin, LanguageNames.CSharp),
            "VisualBasicInheritanceMargin" => new OptionKey2(InheritanceMarginOptionsStorage.ShowInheritanceMargin, LanguageNames.VisualBasic),
            "OfferRemoveUnusedReferences" => new OptionKey2(FeatureOnOffOptions.OfferRemoveUnusedReferences),
            _ => throw new ArgumentOutOfRangeException(nameof(optionId)),
        };

    [Theory]
    [MemberData(nameof(AdvancedNullableStorageKeys))]
    public void AdvancedNullableLegacyReadDistinguishesMissingFromNullWithoutWriting(string optionId, string storageKey)
    {
        var optionKey = GetAdvancedNullableOptionKey(optionId);
        var writes = 0;
        var settingsManager = new MockSettingsManager
        {
            GetValueImpl = (_, _) => (GetValueResult.Missing, null),
            SetValueImpl = (_, _) => writes++,
        };
        var persister = new VisualStudioSettingsOptionPersister((_, _) => { }, s_noFallbacks, settingsManager);

        Assert.Equal(typeof(bool?), optionKey.Option.Type);
        Assert.Equal(true, optionKey.Option.DefaultValue);
        Assert.False(persister.TryFetch(optionKey, storageKey, out _));
        Assert.False(persister.TryReadOptionValue(optionKey, storageKey, typeof(bool?)).HasValue);

        foreach (var storedValue in new bool?[] { null, false, true })
        {
            settingsManager.GetValueImpl = (name, type) =>
            {
                Assert.Equal(storageKey, name);
                Assert.Equal(typeof(bool?), type);
                return (GetValueResult.Success, storedValue);
            };

            var result = persister.TryReadOptionValue(optionKey, storageKey, typeof(bool?));
            Assert.True(result.HasValue);
            Assert.Equal(storedValue, result.Value);
        }

        Assert.Equal(0, writes);
    }

    [Theory]
    [MemberData(nameof(AdvancedNullableStorageKeys))]
    public async Task AdvancedLegacyNullAwaitsRealRoamingWrite(string optionId, string storageKey)
    {
        var optionKey = GetAdvancedNullableOptionKey(optionId);
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Optional<object?> writtenValue = default;
        var settingsManager = new MockSettingsManager
        {
            SetValueAsyncImpl = (name, value, isMachineLocal) =>
            {
                Assert.Equal(storageKey, name);
                Assert.False(isMachineLocal);
                writtenValue = new Optional<object?>(value);
                return completion.Task;
            },
        };
        var persister = new VisualStudioSettingsOptionPersister((_, _) => { }, s_noFallbacks, settingsManager);

        var write = persister.PersistAsync(optionKey, storageKey, value: null);
        Assert.True(writtenValue.HasValue);
        Assert.Null(writtenValue.Value);
        Assert.False(write.IsCompleted);
        completion.SetResult(null);
        await write;
    }

    [Theory]
    [MemberData(nameof(AdvancedNullableStorageKeys))]
    public async Task AdvancedLegacyNullDoesNotReportSuccessWhenWriteFails(string optionId, string storageKey)
    {
        var optionKey = GetAdvancedNullableOptionKey(optionId);
        var error = new InvalidOperationException("Legacy storage unavailable");
        var settingsManager = new MockSettingsManager
        {
            SetValueAsyncImpl = (_, _, _) => Task.FromException(error),
        };
        var persister = new VisualStudioSettingsOptionPersister((_, _) => { }, s_noFallbacks, settingsManager);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => persister.PersistAsync(optionKey, storageKey, value: null));
        Assert.Same(error, actual);
    }

    [Theory]
    [MemberData(nameof(AdvancedNullableStorageKeys))]
    public void AdvancedIncomingLegacyUpdatesPreserveExplicitNull(string optionId, string storageKey)
    {
        var optionKey = GetAdvancedNullableOptionKey(optionId);
        bool? storedValue = true;
        var refreshed = new List<(OptionKey2 Key, object? Value)>();
        var settingsManager = new MockSettingsManager
        {
            GetValueImpl = (_, _) => (GetValueResult.Success, storedValue),
            SetValueImpl = (_, _) => throw new InvalidOperationException("An incoming update must not persist a default"),
        };
        var persister = new VisualStudioSettingsOptionPersister((key, value) => refreshed.Add((key, value)), s_noFallbacks, settingsManager);
        Assert.True(persister.TryFetch(optionKey, storageKey, out _));

        foreach (var value in new bool?[] { null, false, true })
        {
            refreshed.Clear();
            storedValue = value;
            settingsManager.Subset.TriggerSettingChanged(storageKey);
            var change = Assert.Single(refreshed);
            Assert.Equal(optionKey, change.Key);
            Assert.Equal(value, change.Value);
        }
    }

    [Theory]
    [MemberData(nameof(AdvancedNullableStorageKeys))]
    public async Task AdvancedCheckboxWritesAndResetRefreshTrackedNullableOptions(string optionId, string storageKey)
    {
        var optionKey = GetAdvancedNullableOptionKey(optionId);
        bool? storedValue = null;
        var refreshed = new List<(OptionKey2 Key, object? Value)>();
        var settingsManager = new MockSettingsManager
        {
            GetValueImpl = (_, _) => (GetValueResult.Success, storedValue),
            SetValueImpl = (name, value) =>
            {
                Assert.Equal(storageKey, name);
                storedValue = Assert.IsType<bool>(value);
            },
        };
        var persister = new VisualStudioSettingsOptionPersister((key, value) => refreshed.Add((key, value)), s_noFallbacks, settingsManager);
        Assert.True(persister.TryFetch(optionKey, storageKey, out var initialValue));
        Assert.Null(initialValue);

        foreach (var value in new[] { false, true, false, (bool)optionKey.Option.DefaultValue! })
        {
            refreshed.Clear();
            await settingsManager.SetValueAsync(storageKey, value, isMachineLocal: false);
            settingsManager.Subset.TriggerSettingChanged(storageKey);
            Assert.Equal(value, storedValue);
            var change = Assert.Single(refreshed);
            Assert.Equal(optionKey, change.Key);
            Assert.Equal(value, change.Value);
        }
    }
}
