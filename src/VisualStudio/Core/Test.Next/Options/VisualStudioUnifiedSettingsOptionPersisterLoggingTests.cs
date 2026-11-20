// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public partial class VisualStudioUnifiedSettingsOptionPersisterTests
{
    private const string LoggingMoniker = "languages.dotnet.analysis.enableFileLoggingForDiagnostics";

    [Theory]
    [InlineData(false, "false")]
    [InlineData(true, "true")]
    public async Task LoggingBooleanStorageRoundTrips(bool value, string serialized)
    {
        var option = VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics;
        Assert.False((bool)option.Definition.DefaultValue!);
        var key = new OptionKey2(option);
        var storage = Assert.IsType<VisualStudioOptionStorage.UnifiedSettingsManagerStorage>(VisualStudioOptionStorage.Storages[option.Definition.ConfigName]);
        Assert.Equal(LoggingMoniker, storage.Key);
        var manager = new MockUnifiedSettingsManager();
        var persister = new VisualStudioUnifiedSettingsOptionPersister((_, _) => Assert.Fail("Persist must not refresh."), manager);
        var writes = 0;
        manager.SetValueImpl = (moniker, storedValue) =>
        {
            Assert.Equal(LoggingMoniker, moniker);
            Assert.Equal(serialized, storedValue);
            writes++;
        };
        await storage.PersistAsync(persister, key, value);
        Assert.Equal(1, writes);
        manager.GetValueImpl = (moniker, _) =>
        {
            Assert.Equal(LoggingMoniker, moniker);
            return new SettingRetrieval<object>(SettingRetrievalOutcome.Success, null, serialized);
        };
        Assert.True(storage.TryFetch(persister, key, out var fetched));
        Assert.Equal(value, fetched);
        Assert.Equal(new[] { "languages.*" }, manager.SubscribedMonikers);
    }

    [WpfFact]
    public async Task LoggingIncomingNativeUpdatesAndResetRefreshCachedFileEventSinkWithoutPersisting()
    {
        var composition = EditorTestCompositions.EditorFeatures.GetCompositionConfiguration();
        var factory = RuntimeComposition.CreateRuntimeComposition(composition).CreateExportProviderFactory(joinableTaskFactory: null);
        await using var exportProvider = factory.CreateExportProvider();
        var options = exportProvider.GetExportedValue<IGlobalOptionService>();
        var threadingContext = exportProvider.GetExportedValue<IThreadingContext>();
        var option = VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics;
        var key = new OptionKey2(option);
        var storage = Assert.IsType<VisualStudioOptionStorage.UnifiedSettingsManagerStorage>(VisualStudioOptionStorage.Storages[option.Definition.ConfigName]);
        Assert.False(options.GetOption(option));
        // Only inspect IsEnabled: never queue logging work or write a log file.
        var sink = new FileEventSink(options, threadingContext);
        Assert.False(sink.IsEnabled(FunctionId.Workspace_Project_GetCompilation));

        var serialized = "false";
        var reads = 0;
        var refreshes = 0;
        var manager = new MockUnifiedSettingsManager();
        manager.GetValueImpl = (moniker, _) =>
        {
            Assert.Equal(LoggingMoniker, moniker);
            reads++;
            return new SettingRetrieval<object>(SettingRetrievalOutcome.Success, null, serialized);
        };
        manager.SetValueImpl = (_, _) => Assert.Fail("Incoming changes must not persist again.");
        var persister = new VisualStudioUnifiedSettingsOptionPersister((changedKey, value) =>
        {
            Assert.Equal(key, changedKey);
            refreshes++;
            options.RefreshOption(changedKey, value);
        }, manager);

        manager.NotifyChanged(LoggingMoniker);
        Assert.Equal(0, reads);
        Assert.True(storage.TryFetch(persister, key, out var initial));
        Assert.Equal(false, initial);
        Assert.Equal(1, reads);
        manager.NotifyChanged("languages.dotnet.analysis.unrelated");
        Assert.Equal(0, refreshes);

        // Native registry/import migration and native reset both enter this same subscription path.
        // The native migration itself is deliberately not simulated or asserted by this test.
        serialized = "true";
        manager.NotifyChanged(LoggingMoniker);
        Assert.True(options.GetOption(option));
        Assert.True(sink.IsEnabled(FunctionId.Workspace_Project_GetCompilation));
        serialized = "false";
        manager.NotifyChanged(LoggingMoniker);
        Assert.False(options.GetOption(option));
        Assert.False(sink.IsEnabled(FunctionId.Workspace_Project_GetCompilation));
        Assert.Equal(3, reads);
        Assert.Equal(2, refreshes);
    }
}
