// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Settings;

namespace Microsoft.VisualStudio.LanguageServices.Options;

/// <summary>
/// Serializes settings to and from VS Settings storage.
/// </summary>
internal sealed class VisualStudioSettingsOptionPersister : AbstractVisualStudioSettingsOptionPersister<ISettingsManager>
{
    private readonly ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> _readFallbacks;

    /// <remarks>
    /// We make sure this code is from the UI by asking for all <see cref="IOptionPersister"/> in <see cref="RoslynPackage.RegisterOnAfterPackageLoadedAsyncWork"/>
    /// </remarks>
    public VisualStudioSettingsOptionPersister(
        Action<OptionKey2, object?> refreshOption,
        ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> readFallbacks,
        ISettingsManager settingsManager)
        : base(refreshOption, settingsManager)
    {
        _readFallbacks = readFallbacks;

        var settingsSubset = settingsManager.GetSubset("*");
        settingsSubset.SettingChangedAsync += OnSettingChangedAsync;
    }

    private Task OnSettingChangedAsync(object sender, PropertyChangedEventArgs args)
    {
        Contract.ThrowIfNull(this.SettingsManager);

        RefreshIfTracked(args.PropertyName);
        return Task.CompletedTask;
    }

    public override bool TryFetch(OptionKey2 optionKey, string storageKey, Type optionType, out object? value)
    {
        if (base.TryFetch(optionKey, storageKey, optionType, out value))
            return true;

        if (_readFallbacks.TryGetValue(optionKey.Option.Definition.ConfigName, out var lazyReadFallback))
        {
            var fallbackResult = lazyReadFallback.Value.TryRead(
                optionKey.Language,
                (altStorageKey, altStorageType, altDefaultValue) => TryReadAndMonitorOptionValue(optionKey, storageKey, altStorageKey, altStorageType, altDefaultValue));

            if (fallbackResult.HasValue)
            {
                value = fallbackResult.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    protected override bool TryGetValue<T>(string storageKey, out T value)
        => this.SettingsManager.TryGetValue(storageKey, out value) == GetValueResult.Success;

    protected override Task SetValueAsync<T>(string storageKey, T value, bool isMachineLocal)
        => this.SettingsManager.SetValueAsync(storageKey, value, isMachineLocal: false);
}
