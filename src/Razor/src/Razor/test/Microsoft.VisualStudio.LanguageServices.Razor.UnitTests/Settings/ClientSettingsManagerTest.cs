// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.Settings;

public class ClientSettingsManagerTest(ITestOutputHelper testOutput) : VisualStudioTestBase(testOutput)
{
    private readonly IEnumerable<IClientSettingsChangedTrigger> _clientSettingsChangeTriggers = [];

    [Fact]
    public void ChangeTriggersGetInitialized()
    {
        // Act
        var triggers = new TestChangeTrigger[]
        {
            new(),
            new(),
        };

        var manager = new ClientSettingsManager(triggers);

        // Assert
        Assert.All(triggers, (trigger) => Assert.True(trigger.Initialized));
    }

    [Fact]
    public void InitialSettingsAreDefault()
    {
        // Act
        var manager = new ClientSettingsManager(_clientSettingsChangeTriggers);

        // Assert
        Assert.Equal(ClientSettings.Default, manager.GetClientSettings());
    }

    [Fact]
    public void Update_TriggersChangedIfEditorSettingsAreDifferent()
    {
        // Arrange
        var manager = new ClientSettingsManager(_clientSettingsChangeTriggers);
        var called = false;
        manager.ClientSettingsChanged += (caller, args) => called = true;
        var settings = new ClientSpaceSettings(IndentWithTabs: true, IndentSize: 7);

        // Act
        manager.Update(settings);

        // Assert
        Assert.True(called);
        Assert.Equal(settings, manager.GetClientSettings().ClientSpaceSettings);
    }

    [Fact]
    public void Update_DoesNotTriggerChangedIfEditorSettingsAreSame()
    {
        // Arrange
        var manager = new ClientSettingsManager(_clientSettingsChangeTriggers);
        var called = false;
        manager.ClientSettingsChanged += (caller, args) => called = true;
        var originalSettings = manager.GetClientSettings();

        // Act
        manager.Update(ClientSpaceSettings.Default);

        // Assert
        Assert.False(called);
        Assert.Same(originalSettings, manager.GetClientSettings());
    }

    [Fact]
    public void Update_TriggersChangedIfAdvancedSettingsAreDifferent()
    {
        // Arrange
        var manager = new ClientSettingsManager(_clientSettingsChangeTriggers);
        var called = false;
        manager.ClientSettingsChanged += (caller, args) => called = true;
        var settings = new ClientAdvancedSettings(
            FormatOnType: false,
            AutoClosingTags: true,
            AutoInsertAttributeQuotes: true,
            ColorBackground: true,
            CodeBlockBraceOnNextLine: false,
            AttributeIndentStyle: AttributeIndentStyle.AlignWithFirst,
            CommitElementsWithSpace: false,
            SnippetSetting: SnippetSetting.All,
            LogLevel: LogLevel.None,
            FormatOnPaste: false,
            TaskListDescriptors: []);

        // Act
        manager.Update(settings);

        // Assert
        Assert.True(called);
        Assert.Equal(settings, manager.GetClientSettings().AdvancedSettings);
    }

    [Fact]
    public void Update_DoesNotTriggerChangedIfAdvancedSettingsAreSame()
    {
        // Arrange
        var manager = new ClientSettingsManager(_clientSettingsChangeTriggers);
        var called = false;
        manager.ClientSettingsChanged += (caller, args) => called = true;
        var originalSettings = manager.GetClientSettings();

        // Act
        manager.Update(ClientAdvancedSettings.Default);

        // Assert
        Assert.False(called);
        Assert.Same(originalSettings, manager.GetClientSettings());
    }

    [Fact]
    public void InitialSettingsStored()
    {
        var defaultSettings = ClientAdvancedSettings.Default;
        var expectedSettings = defaultSettings with
        {
            FormatOnType = !defaultSettings.FormatOnType
        };

        var manager = new ClientSettingsManager(_clientSettingsChangeTriggers, new AdvancedSettingsStorage(expectedSettings));

        Assert.Same(expectedSettings, manager.GetClientSettings().AdvancedSettings);
    }

    private class TestChangeTrigger : IClientSettingsChangedTrigger
    {
        public bool Initialized { get; private set; }

        public void Initialize(IClientSettingsManager clientSettingsManager)
        {
            Initialized = true;
        }
    }

    private sealed class AdvancedSettingsStorage(ClientAdvancedSettings settings) : IAdvancedSettingsStorage
    {
        public ClientAdvancedSettings GetAdvancedSettings() => settings;
        public Task OnChangedAsync(Action<ClientAdvancedSettings> changed) => Task.CompletedTask;
    }
}
