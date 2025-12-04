// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class VisualStudioUnifiedSettingsOptionPersisterTests
{
    private sealed class MockUnifiedSettingsManager : ISettingsManager
    {
        public Func<string, Type, SettingRetrieval<object>>? GetValueImpl
        {
            set => _settingsReader.GetValueImpl = value;
        }

        public Action<string, object?>? SetValueImpl
        {
            set => _settingsWriter.SetValueImpl = value;
        }

        private readonly MockSettingsReader _settingsReader = new();
        private readonly MockSettingsWriter _settingsWriter = new();

        public ISettingsReader GetReader() => _settingsReader;

        public ISettingsWriter GetWriter(string callerName) => _settingsWriter;

        public ISettingsWriter GetWriter(string callerName, Guid eventSource) => _settingsWriter;
    }

    private sealed class MockSettingsReader : ISettingsReader
    {
        public Func<string, Type, SettingRetrieval<object>>? GetValueImpl;

        public SettingRetrieval<IReadOnlyList<T>> GetArray<T>(string moniker, SettingReadOptions readOptions = SettingReadOptions.RequireValidation) where T : notnull
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<T> GetArrayOrThrow<T>(string moniker) where T : notnull
        {
            throw new NotImplementedException();
        }

        public SettingRetrieval<T> GetValue<T>(string moniker, SettingReadOptions readOptions = SettingReadOptions.RequireValidation) where T : notnull
        {
            var retrieval = GetValueImpl!(moniker, null!);
            return new SettingRetrieval<T>(retrieval.Outcome, retrieval.Message, retrieval.Value is T ? (T)retrieval.Value : default);
        }

        public SettingRetrieval<object> GetValue(string moniker, Type targetType, SettingReadOptions readOptions = SettingReadOptions.RequireValidation)
        {
            throw new NotImplementedException();
        }

        public T GetValueOrThrow<T>(string moniker) where T : notnull
        {
            throw new NotImplementedException();
        }

        public object GetValueOrThrow(string moniker, Type targetType)
        {
            throw new NotImplementedException();
        }

        public IDisposable SubscribeToChanges(Action<SettingsUpdate> handler, params string[] monikerPatterns) => null!;
    }

    private sealed class MockSettingsWriter : ISettingsWriter
    {
        internal Action<string, object?>? SetValueImpl;

        public SettingCommitResult Commit(string changeDescription)
        {
            throw new NotImplementedException();
        }

        public SettingChangeResult EnqueueArrayChange<T>(string moniker, IReadOnlyList<T> value) where T : notnull
        {
            throw new NotImplementedException();
        }

        public SettingChangeResult EnqueueChange<T>(string moniker, T value) where T : notnull
        {
            SetValueImpl!(moniker, value);
            return new SettingChangeResult(SettingChangeOutcome.PendingCommit, true, null);
        }

        public SettingChangeResult EnqueueChange<T>(string moniker, T value, SettingWriteOptions options) where T : notnull
        {
            throw new NotImplementedException();
        }

        public SettingRetrieval<IReadOnlyList<T>> GetArray<T>(string moniker, SettingReadOptions readOptions = SettingReadOptions.RequireValidation) where T : notnull
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<T> GetArrayOrThrow<T>(string moniker) where T : notnull
        {
            throw new NotImplementedException();
        }

        public SettingRetrieval<T> GetValue<T>(string moniker, SettingReadOptions readOptions = SettingReadOptions.RequireValidation) where T : notnull
        {
            throw new NotImplementedException();
        }

        public SettingRetrieval<object> GetValue(string moniker, Type targetType, SettingReadOptions readOptions = SettingReadOptions.RequireValidation)
        {
            throw new NotImplementedException();
        }

        public T GetValueOrThrow<T>(string moniker) where T : notnull
        {
            throw new NotImplementedException();
        }

        public object GetValueOrThrow(string moniker, Type targetType)
        {
            throw new NotImplementedException();
        }

        public SettingCommitResult RequestCommit(string changeDescription)
        {
            return new SettingCommitResult(SettingCommitOutcome.Success, null);
        }

        public IDisposable SubscribeToChanges(Action<SettingsUpdate> handler, params string[] monikerPatterns)
        {
            throw new NotImplementedException();
        }
    }

    [Theory]
    [InlineData("indent_size", 2, "2")]
    [InlineData("indent_size", 4, "4")]
    [InlineData("indent_style", true, "tab")]
    [InlineData("indent_style", false, "space")]
    [InlineData("tab_width", 2, "2")]
    [InlineData("tab_width", 4, "4")]
    [InlineData("smart_indent", FormattingOptions2.IndentStyle.None, "none")]
    [InlineData("smart_indent", FormattingOptions2.IndentStyle.Block, "block")]
    [InlineData("smart_indent", FormattingOptions2.IndentStyle.Smart, "smart")]
    public async Task ValidateSettingManagerSettings(
        string key,
        object value,
        string finalValue)
    {
        var mockManager = new MockUnifiedSettingsManager();

        var persister = new VisualStudioUnifiedSettingsOptionPersister(
            (_, _) => { },
            mockManager);

        // read
        var manager = (VisualStudioOptionStorage.UnifiedSettingsManagerStorage)VisualStudioOptionStorage.Storages[key];
        IPerLanguageValuedOption option = key switch
        {
            "indent_size" => FormattingOptions2.IndentationSize,
            "indent_style" => FormattingOptions2.UseTabs,
            "tab_width" => FormattingOptions2.TabSize,
            "smart_indent" => FormattingOptions2.SmartIndent,
            _ => throw ExceptionUtilities.UnexpectedValue(key),
        };

        var optionKey = new OptionKey2(option, LanguageNames.CSharp);

        // Ensure that if the settings manager doesn't find the item, we are resilient.
        mockManager.GetValueImpl = (_, _) => new SettingRetrieval<object>(SettingRetrievalOutcome.NotRegistered, null, null);
        Assert.False(manager.TryFetch(persister, optionKey, out _));

        // Ensure that if the settings manager finds and return nothing, we are resilient.
        mockManager.GetValueImpl = (_, _) => new SettingRetrieval<object>(SettingRetrievalOutcome.Success, null, null);
        Assert.False(manager.TryFetch(persister, optionKey, out _));

        // Ensure that if the settings manager finds and return an unexpected type, we are resilient.
        mockManager.GetValueImpl = (_, _) => new SettingRetrieval<object>(SettingRetrievalOutcome.Success, null, true);
        Assert.False(manager.TryFetch(persister, optionKey, out _));

        // Ensure that We don't try to get when doing a store.  Also ensure that the value stored is the appropriate
        // string value of the original value passed in.
        mockManager.GetValueImpl = null;
        mockManager.SetValueImpl = (name, storedValue) =>
        {
            Assert.Equal(finalValue, storedValue);
        };
        await manager.PersistAsync(persister, optionKey, value);

        // Attempt to read the value back.  If the settings manager gets it back properly, ensure that we can read it
        // into the actual in-memory (non-string) value we expect.
        mockManager.SetValueImpl = null;
        mockManager.GetValueImpl = (name, type) => new SettingRetrieval<object>(SettingRetrievalOutcome.Success, null, finalValue);
        Assert.True(manager.TryFetch(persister, optionKey, out var fetchedValue));
        Assert.Equal(value, fetchedValue);
    }
}
