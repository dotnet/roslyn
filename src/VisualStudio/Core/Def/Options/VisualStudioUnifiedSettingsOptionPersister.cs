// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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

    private static void CheckStorageKey(string storageKey)
    {
        Contract.ThrowIfFalse(storageKey.StartsWith("languages"), "Need to update SubscribeToChanges in constructor to listen to changes to this key");
    }

    internal override Optional<object?> TryReadOptionValue(OptionKey2 optionKey, string storageKey, Type storageType, object? defaultValue)
    {
        CheckStorageKey(storageKey);

        if (storageType == typeof(int))
        {
            var retrieval = this.SettingsManager.GetReader().GetValue<int>(storageKey, SettingReadOptions.NoRequirements);
            return retrieval.Outcome == SettingRetrievalOutcome.Success ? new(retrieval.Value) : new(defaultValue);
        }
        else if (storageType.IsEnum)
        {
            var retrieval = this.SettingsManager.GetReader().GetValue<string>(storageKey, SettingReadOptions.NoRequirements);
            return retrieval is { Outcome: SettingRetrievalOutcome.Success, Value: string stringValue } &&
                   optionKey.Option.Definition.Serializer.TryParse(stringValue, out var value) ? new(value) : new(defaultValue);
        }

        // Add more types to support here as needed.

        throw ExceptionUtilities.UnexpectedValue(storageType);
    }

    public override Task PersistAsync(OptionKey2 optionKey, string storageKey, object? value)
    {
        CheckStorageKey(storageKey);

        var writer = this.SettingsManager.GetWriter(nameof(VisualStudioUnifiedSettingsOptionPersister));

        var storageType = value?.GetType();
        if (storageType == typeof(int))
        {
            writer.EnqueueChange(storageKey, value!);
        }
        else if (storageType?.IsEnum is true)
        {
            // In-memory representation was different than persisted representation (often a bool/enum), so
            // serialize it as per the option's serializer.
            //
            // Note, we persist as a lowercase value, as that's what the setting manager does for these modern keys. On
            // read, TryParse will handle lowercase enum values just fine due to it using `Enum.TryParse(str,
            // ignoreCase: true, out result)`
            writer.EnqueueChange(storageKey, optionKey.Option.Definition.Serializer.Serialize(value).ToLowerInvariant());
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(storageType);
        }

        writer.RequestCommit(nameof(VisualStudioUnifiedSettingsOptionPersister));

        return Task.CompletedTask;
    }
}
