// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Settings;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed partial class VisualStudioSettingsOptionPersisterTests
{
    [Fact]
    public void AdvancedNullableLegacyReadDistinguishesMissingFromNullWithoutWriting()
    {
        var optionKey = new OptionKey2(InheritanceMarginOptionsStorage.ShowInheritanceMargin, LanguageNames.CSharp);
        const string storageKey = "TextEditor.CSharp.Specific.ShowInheritanceMargin";
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

    [Fact]
    public void AdvancedIncomingLegacyUpdatesPreserveExplicitNull()
    {
        var optionKey = new OptionKey2(InheritanceMarginOptionsStorage.ShowInheritanceMargin, LanguageNames.CSharp);
        const string storageKey = "TextEditor.CSharp.Specific.ShowInheritanceMargin";
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
}
