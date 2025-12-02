// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

    protected override bool TryGetValue<T>(OptionKey2 optionKey, string storageKey, Type storageType, out T value)
    {
        var retrieval = this.SettingsManager.GetReader().GetValue<string>(
            storageKey, SettingReadOptions.NoRequirements);

        if (retrieval.Outcome != SettingRetrievalOutcome.Success ||
            retrieval.Value is null ||
            !optionKey.Option.Definition.Serializer.TryParse(retrieval.Value, out var untypedValue) ||
            untypedValue is not T typedValue)
        {
            value = default!;
            return false;
        }

        value = typedValue;
        return true;
    }

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
    protected override Task SetValueAsync(OptionKey2 optionKey, string storageKey, object? value, bool isMachineLocal)
    {
        // In-memory representation was different than persisted representation (often a bool/enum), so
        // serialize it as per the option's serializer.
        //
        // Note, we persist as a lowercase value, as that's what the setting manager does for these modern keys. On
        // read, TryParse will handle lowercase enum values just fine due to it using `Enum.TryParse(str,
        // ignoreCase: true, out result)`
        var serialized = optionKey.Option.Definition.Serializer.Serialize(value).ToLowerInvariant();
        var writer = this.SettingsManager.GetWriter(nameof(VisualStudioUnifiedSettingsOptionPersister));

        var result = writer.EnqueueChange(storageKey, serialized);
        writer.RequestCommit(nameof(VisualStudioUnifiedSettingsOptionPersister));

        return Task.CompletedTask;
    }
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
}
