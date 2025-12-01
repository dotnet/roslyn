// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class VisualStudioUnifiedSettingsOptionPersister : AbstractVisualStudioSettingsOptionPersister<ISettingsManager>
{
    public VisualStudioUnifiedSettingsOptionPersister(
        Action<OptionKey2, object?> refreshOption,
        ISettingsManager settingsManager)
        : base(refreshOption, settingsManager)
    {
        var settingsSubset = settingsManager.GetReader().SubscribeToChanges(
            OnSettingChanged, "languages.*");
    }

    private void OnSettingChanged(SettingsUpdate update)
    {
        foreach (var key in update.ChangedSettingMonikers)
            this.RefreshIfTracked(key);
    }

    public override bool TryFetch(OptionKey2 optionKey, string storageKey, out object? value)
    {
        if (!TryFetchWorker(optionKey, storageKey, typeof(string), out var innerValue) ||
            innerValue is not string innerStringValue)
        {
            value = null;
            return false;
        }

        return optionKey.Option.Definition.Serializer.TryParse(innerStringValue, out value);
    }

    public override Task PersistAsync(OptionKey2 optionKey, string storageKey, object? value)
    {
        // In-memory representation was different than persisted representation (often a bool/enum), so
        // serialize it as per the option's serializer.
        //
        // Note, we persist as a lowercase value, as that's what the setting manager does for these modern keys. On
        // read, TryParse will handle lowercase enum values just fine due to it using `Enum.TryParse(str,
        // ignoreCase: true, out result)`
        var serialized = optionKey.Option.Definition.Serializer.Serialize(value).ToLowerInvariant();
        return this.PersistWorkerAsync(storageKey, serialized);
    }

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
    protected override bool TryGetValue<T>(string storageKey, out T value)
    {
        Debug.Assert(typeof(T) == typeof(string));
        var retrieval = this.SettingsManager.GetReader().GetValue<T>(
            storageKey, SettingReadOptions.NoRequirements);

        value = retrieval.Value!;
        return retrieval.Outcome == SettingRetrievalOutcome.Success;
    }

    protected override Task SetValueAsync(string storageKey, object? value, bool isMachineLocal)
    {
        Debug.Assert(value?.GetType() == typeof(string));
        var writer = this.SettingsManager.GetWriter(nameof(VisualStudioUnifiedSettingsOptionPersister));

        var result = writer.EnqueueChange(storageKey, value);
        writer.RequestCommit(nameof(VisualStudioUnifiedSettingsOptionPersister));

        return Task.CompletedTask;
    }
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
}
