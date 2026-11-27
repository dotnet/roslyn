// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class VisualStudioOptionPersister(
    VisualStudioSettingsOptionPersister visualStudioSettingsOptionPersister,
    VisualStudioUnifiedSettingsOptionPersister visualStudioUnifiedSettingsOptionPersister,
    LocalUserRegistryOptionPersister localUserRegistryPersister,
    FeatureFlagPersister featureFlagPersister) : IOptionPersister
{
    private readonly VisualStudioSettingsOptionPersister _visualStudioSettingsOptionPersister = visualStudioSettingsOptionPersister;
    private readonly LocalUserRegistryOptionPersister _localUserRegistryPersister = localUserRegistryPersister;
    private readonly FeatureFlagPersister _featureFlagPersister = featureFlagPersister;

    public bool TryFetch(OptionKey2 optionKey, out object? value)
    {
        value = null;
        return VisualStudioOptionStorage.Storages.TryGetValue(optionKey.Option.Definition.ConfigName, out var storage) && TryFetch(storage, optionKey, out value);
    }

    private bool TryFetch(VisualStudioOptionStorage storage, OptionKey2 optionKey, out object? value)
        => storage switch
        {
            VisualStudioOptionStorage.RoamingProfileStorage roaming => roaming.TryFetch(_visualStudioSettingsOptionPersister, optionKey, out value),
            VisualStudioOptionStorage.UnifiedSettingsManagerStorage settingsManager => settingsManager.TryFetch(visualStudioUnifiedSettingsOptionPersister, optionKey, out value),
            VisualStudioOptionStorage.FeatureFlagStorage featureFlags => featureFlags.TryFetch(_featureFlagPersister, optionKey, out value),
            VisualStudioOptionStorage.LocalUserProfileStorage local => local.TryFetch(_localUserRegistryPersister, optionKey, out value),
            _ => throw ExceptionUtilities.UnexpectedValue(storage)
        };

    public bool TryPersist(OptionKey2 optionKey, object? value)
    {
        if (!VisualStudioOptionStorage.Storages.TryGetValue(optionKey.Option.Definition.ConfigName, out var storage))
        {
            return false;
        }

        // fire and forget:
        PersistAsync(storage, optionKey, value).ReportNonFatalErrorAsync();
        return true;
    }

    public Task PersistAsync(VisualStudioOptionStorage storage, OptionKey2 optionKey, object? value)
        => storage switch
        {
            VisualStudioOptionStorage.RoamingProfileStorage roaming => roaming.PersistAsync(_visualStudioSettingsOptionPersister, optionKey, value),
            VisualStudioOptionStorage.UnifiedSettingsManagerStorage settingsManager => settingsManager.PersistAsync(visualStudioUnifiedSettingsOptionPersister, optionKey, value),
            VisualStudioOptionStorage.FeatureFlagStorage featureFlags => featureFlags.PersistAsync(_featureFlagPersister, value),
            VisualStudioOptionStorage.LocalUserProfileStorage local => local.PersistAsync(_localUserRegistryPersister, optionKey, value),
            _ => throw ExceptionUtilities.UnexpectedValue(storage)
        };
}
