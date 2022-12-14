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
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Settings;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices;

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
        return VisualStudioOptionStorage.TryGetStorage(optionKey.Option.OptionDefinition.ConfigName, out var storage) && TryFetch(storage, optionKey, out value);
    }

    public bool TryFetch(VisualStudioOptionStorage storage, OptionKey2 optionKey, out object? value)
        => storage switch
        {
            VisualStudioOptionStorage.RoamingProfileStorage roaming => roaming.TryFetch(_visualStudioSettingsOptionPersister, optionKey, out value),
            VisualStudioOptionStorage.FeatureFlagStorage featureFlags => featureFlags.TryFetch(_featureFlagPersister, optionKey, out value),
            VisualStudioOptionStorage.LocalUserProfileStorage local => local.TryFetch(_localUserRegistryPersister, optionKey, out value),
            VisualStudioOptionStorage.CompositeStorage composite => composite.TryFetch(this, optionKey, out value),
            _ => throw ExceptionUtilities.UnexpectedValue(storage)
        };

    public bool TryPersist(OptionKey2 optionKey, object? value)
        => VisualStudioOptionStorage.TryGetStorage(optionKey.Option.OptionDefinition.ConfigName, out var storage) && TryPersist(storage, optionKey, value);

    public bool TryPersist(VisualStudioOptionStorage storage, OptionKey2 optionKey, object? value)
        => storage switch
        {
            VisualStudioOptionStorage.RoamingProfileStorage roaming => roaming.TryPersist(_visualStudioSettingsOptionPersister, optionKey, value),
            VisualStudioOptionStorage.FeatureFlagStorage featureFlags => featureFlags.TryPersist(_featureFlagPersister, value),
            VisualStudioOptionStorage.LocalUserProfileStorage local => local.TryPersist(_localUserRegistryPersister, optionKey, value),
            VisualStudioOptionStorage.CompositeStorage composite => composite.TryPersist(this, optionKey, value),
            _ => throw ExceptionUtilities.UnexpectedValue(storage)
        };
}
