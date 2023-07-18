// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Settings;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class VisualStudioOptionPersister : IOptionPersister
{
    private readonly VisualStudioSettingsOptionPersister _visualStudioSettingsOptionPersister;
    private readonly LocalUserRegistryOptionPersister _localUserRegistryPersister;
    private readonly FeatureFlagPersister _featureFlagPersister;

    public VisualStudioOptionPersister(
        VisualStudioSettingsOptionPersister visualStudioSettingsOptionPersister,
        LocalUserRegistryOptionPersister localUserRegistryPersister,
        FeatureFlagPersister featureFlagPersister)
    {
        _visualStudioSettingsOptionPersister = visualStudioSettingsOptionPersister;
        _localUserRegistryPersister = localUserRegistryPersister;
        _featureFlagPersister = featureFlagPersister;
    }

    public bool TryFetch(OptionKey2 optionKey, out object? value)
    {
        value = null;
        return VisualStudioOptionStorage.Storages.TryGetValue(optionKey.Option.Definition.ConfigName, out var storage) && TryFetch(storage, optionKey, out value);
    }

    public bool TryFetch(VisualStudioOptionStorage storage, OptionKey2 optionKey, out object? value)
        => storage switch
        {
            VisualStudioOptionStorage.RoamingProfileStorage roaming => roaming.TryFetch(_visualStudioSettingsOptionPersister, optionKey, out value),
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
            VisualStudioOptionStorage.FeatureFlagStorage featureFlags => featureFlags.PersistAsync(_featureFlagPersister, value),
            VisualStudioOptionStorage.LocalUserProfileStorage local => local.PersistAsync(_localUserRegistryPersister, optionKey, value),
            _ => throw ExceptionUtilities.UnexpectedValue(storage)
        };
}
