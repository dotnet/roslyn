// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Microsoft.VisualStudio.Settings;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public sealed class VisualStudioSettingsOptionPersisterTests
{
    private sealed class MockSettingsSubset : ISettingsSubset
    {
        public event PropertyChangedAsyncEventHandler? SettingChangedAsync;

        public void TriggerSettingChanged(string storageName)
            => SettingChangedAsync?.Invoke(this, new PropertyChangedEventArgs(storageName));
    }

    private sealed class MockSettingsManager : ISettingsManager
    {
        public Func<string, Type, (GetValueResult, object?)>? GetValueImpl;
        public Action<string, object?>? SetValueImpl;

        public readonly MockSettingsSubset Subset = new();

        public ISettingsSubset GetSubset(string namePattern)
            => Subset;

        public GetValueResult TryGetValue<T>(string name, out T value)
        {
            var (result, objValue) = GetValueImpl!(name, typeof(T));
            value = (result == GetValueResult.Success) ? (T)objValue! : default!;
            return result;
        }

        public Task SetValueAsync(string name, object? value, bool isMachineLocal)
        {
            SetValueImpl?.Invoke(name, value);
            return Task.CompletedTask;
        }

        public ISettingsList GetOrCreateList(string name, bool isMachineLocal)
            => throw new NotImplementedException();

        public T GetValueOrDefault<T>(string name, T defaultValue = default!)
            => throw new NotImplementedException();

        public string[] NamesStartingWith(string prefix)
            => throw new NotImplementedException();

        public void SetOnlineStore(IAsyncStringStorage store)
            => throw new NotImplementedException();

        public void SetSharedStore(IAsyncStringStorage store)
            => throw new NotImplementedException();
    }

    public enum E
    {
        A,
        B
    }

    private static readonly ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> s_noFallbacks =
        ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>>.Empty;

    private static readonly NamingStylePreferences s_nonDefaultNamingStylePreferences = OptionsTestHelpers.GetNonDefaultNamingStylePreference();

    private static (object? value, object? storageValue) GetSomeOptionValue(Type optionType)
        => optionType == typeof(bool) ? (true, true) :
           optionType == typeof(string) ? ("str", "str") :
           optionType == typeof(int) ? (1, 1) :
           optionType == typeof(long) ? (1L, 1L) :
           optionType == typeof(bool?) ? (true, true) :
           optionType == typeof(int?) ? (1, 1) :
           optionType == typeof(long?) ? (1L, 1L) :
           optionType == typeof(E) ? (E.A, (int)E.A) :
           optionType == typeof(E?) ? (E.A, (int)E.A) :
           optionType == typeof(NamingStylePreferences) ? (s_nonDefaultNamingStylePreferences, s_nonDefaultNamingStylePreferences.CreateXElement().ToString()) :
           optionType == typeof(CodeStyleOption2<bool>) ? (new CodeStyleOption2<bool>(false, NotificationOption2.Warning), new CodeStyleOption2<bool>(false, NotificationOption2.Warning).ToXElement().ToString()) :
           optionType == typeof(ImmutableArray<bool>) ? (ImmutableArray.Create(true, false), new[] { true, false }) :
           optionType == typeof(ImmutableArray<int>) ? (ImmutableArray.Create(0, 1), new[] { 0, 1 }) :
           optionType == typeof(ImmutableArray<long>) ? (ImmutableArray.Create(0L, 1L), new[] { 0L, 1L }) :
           optionType == typeof(ImmutableArray<string>) ? (ImmutableArray.Create("a", "b"), new[] { "a", "b" }) :
           throw ExceptionUtilities.UnexpectedValue(optionType);

    private static bool IsDefaultImmutableArray(object array)
        => (bool)array.GetType().GetMethod("get_IsDefault").Invoke(array, [])!;

    [Fact]
    public void SettingsChangeEvent()
    {
        var exportProvider = VisualStudioTestCompositions.LanguageServices.ExportProviderFactory.CreateExportProvider();
        var fallbacks = exportProvider.GetExports<IVisualStudioStorageReadFallback, OptionNameMetadata>().ToImmutableDictionary(item => item.Metadata.ConfigName, item => item);

        var refreshedOptions = new List<(OptionKey2, object?)>();
        var settingsManager = new MockSettingsManager();
        var persister = new VisualStudioSettingsOptionPersister((optionKey, newValue) => refreshedOptions.Add((optionKey, newValue)), fallbacks, settingsManager);
        var optionKey = new OptionKey2(CSharpFormattingOptions2.NewLineBeforeOpenBrace);

        // one flag is set:

        settingsManager.GetValueImpl = (name, type) => name switch
        {
            "TextEditor.CSharp.Specific.NewLinesForBracesInAnonymousTypes" => (GetValueResult.Success, false),
            _ => (GetValueResult.Missing, null),
        };

        Assert.True(persister.TryFetch(optionKey, "TextEditor.CSharp.Specific.csharp_new_line_before_open_brace", out var value));
        Assert.Equal(value, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue & ~NewLineBeforeOpenBracePlacement.AnonymousTypes);

        // Settings manager receives an option update (e.g. roaming options have been retrieved from online storage) and 
        // triggers option change event.

        settingsManager.GetValueImpl = (name, type) => name switch
        {
            "TextEditor.CSharp.Specific.NewLinesForBracesInMethods" => (GetValueResult.Success, false),
            _ => (GetValueResult.Missing, null),
        };

        settingsManager.Subset.TriggerSettingChanged("TextEditor.CSharp.Specific.NewLinesForBracesInMethods");

        Assert.Equal((optionKey, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue & ~NewLineBeforeOpenBracePlacement.Methods), refreshedOptions.Single());
        refreshedOptions.Clear();

        // Settings manager receives another option update -- the primary option is set now

        settingsManager.GetValueImpl = (name, type) => name switch
        {
            "TextEditor.CSharp.Specific.csharp_new_line_before_open_brace" => (GetValueResult.Success, NewLineBeforeOpenBracePlacement.Accessors | NewLineBeforeOpenBracePlacement.LambdaExpressionBody),
            "TextEditor.CSharp.Specific.NewLinesForBracesInAnonymousTypes" => (GetValueResult.Success, false),
            "TextEditor.CSharp.Specific.NewLinesForBracesInMethods" => (GetValueResult.Success, true),
            _ => (GetValueResult.Missing, null),
        };

        settingsManager.Subset.TriggerSettingChanged("TextEditor.CSharp.Specific.csharp_new_line_before_open_brace");

        Assert.Equal((optionKey, NewLineBeforeOpenBracePlacement.Accessors | NewLineBeforeOpenBracePlacement.LambdaExpressionBody), refreshedOptions.Single());
        refreshedOptions.Clear();
    }

    [Theory, CombinatorialData]
    public void SettingsManagerReadOptionValue_Success(
        [CombinatorialValues(
            typeof(bool),
            typeof(string),
            typeof(int),
            typeof(long),
            typeof(bool?),
            typeof(int?),
            typeof(long?),
            typeof(E),
            typeof(E?),
            typeof(CodeStyleOption2<bool>),
            typeof(NamingStylePreferences),
            typeof(ImmutableArray<bool>),
            typeof(ImmutableArray<int>),
            typeof(ImmutableArray<long>),
            typeof(ImmutableArray<string>))]
        Type optionType)
    {
        var (optionValue, storageValue) = GetSomeOptionValue(optionType);
        var defaultValue = OptionsTestHelpers.GetDifferentValue(optionType, optionValue);

        var mockManager = new MockSettingsManager()
        {
            GetValueImpl = (_, _) => (GetValueResult.Success, storageValue)
        };

        var result = VisualStudioSettingsOptionPersister.TryReadOptionValue(mockManager, "key", optionType, defaultValue);
        Assert.True(result.HasValue);
        Assert.Equal(optionValue, result.Value);
    }

    [Theory, CombinatorialData]
    public void SettingsManagerReadOptionValue_Error(
        [CombinatorialValues(
            GetValueResult.Missing,
            GetValueResult.IncompatibleType,
            GetValueResult.ObsoleteFormat,
            GetValueResult.UnknownError,
            GetValueResult.Corrupt)]
        GetValueResult specializedTypeResult,
        [CombinatorialValues(
            typeof(bool),
            typeof(string),
            typeof(int),
            typeof(long),
            typeof(bool?),
            typeof(int?),
            typeof(long?),
            typeof(E),
            typeof(E?),
            typeof(CodeStyleOption2<bool>),
            typeof(NamingStylePreferences),
            typeof(ImmutableArray<bool>),
            typeof(ImmutableArray<int>),
            typeof(ImmutableArray<long>),
            typeof(ImmutableArray<string>))]
        Type optionType)
    {
        var (optionValue, storageValue) = GetSomeOptionValue(optionType);

        var mockManager = new MockSettingsManager()
        {
            GetValueImpl = (_, type) => (specializedTypeResult, storageValue)
        };

        var result = VisualStudioSettingsOptionPersister.TryReadOptionValue(mockManager, "key", optionType, optionValue);

        // we should not fall back to object if the manager tells us bool value is missing or invalid in any way:
        Assert.False(result.HasValue);
    }

    [Theory]
    [InlineData(typeof(ImmutableArray<bool>))]
    [InlineData(typeof(ImmutableArray<int>))]
    [InlineData(typeof(ImmutableArray<long>))]
    [InlineData(typeof(ImmutableArray<string>))]
    public void Roundtrip_DefaultImmutableArray(Type type)
    {
        var defaultArray = Activator.CreateInstance(type);
        var serializedValue = (object?)null;

        Optional<object?> newValue = default;

        var mockManager = new MockSettingsManager()
        {
            GetValueImpl = (_, _) => (GetValueResult.Success, serializedValue),
            SetValueImpl = (name, value) => newValue = value
        };

        // read
        var result = VisualStudioSettingsOptionPersister.TryReadOptionValue(mockManager, "key", type, defaultValue: null);
        Assert.True(result.HasValue);
        Assert.True(IsDefaultImmutableArray(result.Value!));

        // write
        var persister = new VisualStudioSettingsOptionPersister((_, _) => { }, s_noFallbacks, mockManager);
        persister.PersistAsync("key", defaultArray).Wait();

        Assert.True(newValue.HasValue);
        Assert.Equal(serializedValue, newValue.Value);
    }

    public static IEnumerable<object?[]> GetRoundtripTestCases()
    {
        var value1 = new CodeStyleOption2<int>(1, NotificationOption2.Warning);
        yield return new object?[] { value1, value1.ToXElement().ToString(), new CodeStyleOption2<int>(0, NotificationOption2.None), typeof(CodeStyleOption2<int>) };

        var value2 = s_nonDefaultNamingStylePreferences;
        yield return new object?[] { value2, value2.CreateXElement().ToString(), NamingStylePreferences.Empty, typeof(NamingStylePreferences) };

        yield return new object?[] { E.B, (int)E.B, E.B, typeof(E) };
        yield return new object?[] { null, null, E.B, typeof(E?) };
        yield return new object?[] { (E?)E.B, (int?)E.B, E.B, typeof(E?) };
    }

    [Theory]
    [MemberData(nameof(GetRoundtripTestCases))]
    public void Roundtrip(object? value, object? serializedValue, object defaultValue, Type type)
    {
        Optional<object?> newValue = default;

        var mockManager = new MockSettingsManager()
        {
            GetValueImpl = (_, _) => (GetValueResult.Success, serializedValue),
            SetValueImpl = (name, value) => newValue = value
        };

        // read
        var result = VisualStudioSettingsOptionPersister.TryReadOptionValue(mockManager, "key", type, defaultValue);
        Assert.True(result.HasValue);
        Assert.Equal(value, result.Value);

        // write
        var persister = new VisualStudioSettingsOptionPersister((_, _) => { }, s_noFallbacks, mockManager);
        persister.PersistAsync("key", value).Wait();

        Assert.True(newValue.HasValue);
        Assert.Equal(serializedValue, newValue.Value);
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
        var mockManager = new MockSettingsManager();

        var persister = new VisualStudioSettingsOptionPersister(
            (_, _) => { },
            ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>>.Empty,
            mockManager);

        // read
        var manager = (VisualStudioOptionStorage.SettingsManagerStorage)VisualStudioOptionStorage.Storages[key];
        IPerLanguageValuedOption option = key switch
        {
            "indent_size" => FormattingOptions2.IndentationSize,
            "indent_style" => FormattingOptions2.UseTabs,
            "tab_width" => FormattingOptions2.TabSize,
            "smart_indent" => FormattingOptions2.SmartIndent,
            _ => throw ExceptionUtilities.UnexpectedValue(key),
        };

        var optionKey = new OptionKey2(option, LanguageNames.CSharp);
        mockManager.GetValueImpl = (_, _) => (GetValueResult.Missing, null);
        Assert.False(manager.TryFetch(persister, optionKey, out _));

        mockManager.GetValueImpl = (_, _) => (GetValueResult.Success, null);
        Assert.False(manager.TryFetch(persister, optionKey, out _));

        mockManager.GetValueImpl = null;
        mockManager.SetValueImpl = (name, storedValue) =>
        {
            Assert.Equal(finalValue, storedValue);
        };
        await manager.PersistAsync(persister, optionKey, value);

        mockManager.SetValueImpl = null;
        mockManager.GetValueImpl = (name, type) => (GetValueResult.Success, finalValue);
        Assert.True(manager.TryFetch(persister, optionKey, out var fetchedValue));
        Assert.Equal(value, fetchedValue);
    }
}
