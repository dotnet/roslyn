// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Microsoft.VisualStudio.VCProjectEngine;

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

    private static void CheckStorageKeyAndType(string storageKey, [NotNull] Type? storageType)
    {
        Contract.ThrowIfFalse(storageKey.StartsWith("languages"), "Need to update SubscribeToChanges in constructor to listen to changes to this key");

        // Currently, these are the only types we expect.  This can be augmented in the future if we need to serialize
        // more kinds to unified settings backend.
        Contract.ThrowIfFalse(
            storageType == typeof(int) ||
            storageType == typeof(bool) ||
            storageType?.IsEnum is true);
    }

    internal override Optional<object?> TryReadOptionValue(OptionKey2 optionKey, string storageKey, Type storageType)
    {
        CheckStorageKeyAndType(storageKey, storageType);

        var retrieval = this.SettingsManager.GetReader().GetValue<string>(storageKey, SettingReadOptions.NoRequirements);
        return retrieval is { Outcome: SettingRetrievalOutcome.Success, Value: string stringValue } &&
               optionKey.Option.Definition.Serializer.TryParse(stringValue, out var value) ? new(value) : default;
    }

    public override Task PersistAsync(OptionKey2 optionKey, string storageKey, object? value)
    {
        var storageType = value?.GetType();
        CheckStorageKeyAndType(storageKey, storageType);

        var writer = this.SettingsManager.GetWriter(nameof(VisualStudioUnifiedSettingsOptionPersister));

        // In-memory representation was different than persisted representation (often a bool/enum), so
        // serialize it as per the option's serializer.
        //
        // Note, we persist as a lowercase value, as that's what the setting manager does for these modern keys. On
        // read, TryParse will handle lowercase enum values just fine due to it using `Enum.TryParse(str,
        // ignoreCase: true, out result)`
        writer.EnqueueChange(storageKey, optionKey.Option.Definition.Serializer.Serialize(value).ToLowerInvariant());
        writer.RequestCommit(nameof(VisualStudioUnifiedSettingsOptionPersister));

        return Task.CompletedTask;
    }
}
