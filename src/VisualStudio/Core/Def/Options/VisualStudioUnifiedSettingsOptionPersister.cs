// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using UnifiedSettingsManager = Microsoft.VisualStudio.Utilities.UnifiedSettings.ISettingsManager;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class VisualStudioUnifiedSettingsOptionPersister : AbstractVisualStudioSettingsOptionPersister<UnifiedSettingsManager>
{
    public VisualStudioUnifiedSettingsOptionPersister(
        Action<OptionKey2, object?> refreshOption,
        UnifiedSettingsManager settingsManager)
        : base(refreshOption, settingsManager)
    {
        var settingsSubset = settingsManager.GetReader().SubscribeToChanges(
            OnSettingChanged, "languages.*");
    }

    private void OnSettingChanged(VisualStudio.Utilities.UnifiedSettings.SettingsUpdate update)
    {
        foreach (var key in update.ChangedSettingMonikers)
            this.RefreshIfTracked(key);
    }

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
    protected override bool TryGetValue<T>(string storageKey, out T value)
    {
        var retrieval = this.SettingsManager.GetReader().GetValue<T>(
            storageKey, VisualStudio.Utilities.UnifiedSettings.SettingReadOptions.NoRequirements);

        value = retrieval.Value!;
        return retrieval.Outcome == VisualStudio.Utilities.UnifiedSettings.SettingRetrievalOutcome.Success;
    }

    protected override Task SetValueAsync<T>(string storageKey, T value, bool isMachineLocal)
    {
        var writer = this.SettingsManager.GetWriter(nameof(VisualStudioUnifiedSettingsOptionPersister));

        var result = writer.EnqueueChange(storageKey, value);
        writer.RequestCommit(nameof(VisualStudioUnifiedSettingsOptionPersister));

        return Task.CompletedTask;
    }
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
}
