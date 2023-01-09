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

namespace Microsoft.VisualStudio.LanguageServices.Options
{
    /// <summary>
    /// Serializes settings to and from VS Settings storage.
    /// </summary>
    internal sealed class VisualStudioSettingsOptionPersister
    {
        // NOTE: This service is not public or intended for use by teams/individuals outside of Microsoft. Any data stored is subject to deletion without warning.
        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { };

        private readonly ISettingsManager? _settingManager;
        private readonly ILegacyGlobalOptionService _legacyGlobalOptions;
        private readonly ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> _readFallbacks;

        /// <summary>
        /// Options that have been been fetched from <see cref="_settingManager"/>, by key. We track this so
        /// if a later change happens, we know to refresh that value.
        /// </summary>
        private ImmutableDictionary<string, (OptionKey2 optionKey, Type storageType)> _optionsToMonitorForChanges
            = ImmutableDictionary<string, (OptionKey2 optionKey, Type storageType)>.Empty;

        /// <remarks>
        /// We make sure this code is from the UI by asking for all <see cref="IOptionPersister"/> in <see cref="RoslynPackage.InitializeAsync"/>
        /// </remarks>
        public VisualStudioSettingsOptionPersister(ILegacyGlobalOptionService globalOptionService, ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> readFallbacks, ISettingsManager? settingsManager)
        {
            Contract.ThrowIfNull(globalOptionService);

            _settingManager = settingsManager;
            _legacyGlobalOptions = globalOptionService;
            _readFallbacks = readFallbacks;

            // While the settings persistence service should be available in all SKUs it is possible an ISO shell author has undefined the
            // contributing package. In that case persistence of settings won't work (we don't bother with a backup solution for persistence
            // as the scenario seems exceedingly unlikely), but we shouldn't crash the IDE.
            if (_settingManager != null)
            {
                var settingsSubset = _settingManager.GetSubset("*");
                settingsSubset.SettingChangedAsync += OnSettingChangedAsync;
            }
        }

        private Task OnSettingChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            var storageKey = args.PropertyName;
            if (_optionsToMonitorForChanges.TryGetValue(storageKey, out var entry))
            {
                var optionValue = TryReadOptionValue(entry.optionKey, storageKey, entry.storageType);
                if (optionValue.HasValue && _legacyGlobalOptions.GlobalOptions.RefreshOption(entry.optionKey, optionValue.Value))
                {
                    // We may be updating the values of internally defined public options.
                    // Update solution snapshots of all workspaces to reflect the new values.
                    _legacyGlobalOptions.UpdateRegisteredWorkspaces();
                }
            }

            return Task.CompletedTask;
        }

        private void RecordObservedValueToWatchForChanges(OptionKey2 optionKey, string storageKey, Type storageType)
        {
            ImmutableInterlocked.GetOrAdd(ref _optionsToMonitorForChanges, storageKey, _ => (optionKey, storageType));
        }

        public bool TryFetch(OptionKey2 optionKey, string storageKey, out object? value)
        {
            var result = TryReadOptionValue(optionKey, storageKey, optionKey.Option.Type);
            if (result.HasValue)
            {
                value = result.Value;
                return true;
            }

            if (_readFallbacks.TryGetValue(optionKey.Option.Definition.ConfigName, out var lazyReadFallback))
            {
                var fallbackResult = lazyReadFallback.Value.TryRead(optionKey.Language, (storageKey, storageType) => TryReadOptionValue(optionKey, storageKey, storageType));
                if (fallbackResult.HasValue)
                {
                    value = fallbackResult.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public Optional<object?> TryReadOptionValue(OptionKey2 optionKey, string storageKey, Type storageType)
        {
            Contract.ThrowIfNull(_settingManager);

            RecordObservedValueToWatchForChanges(optionKey, storageKey, storageType);

            if (storageType == typeof(bool) && _settingManager.TryGetValue(storageKey, out bool boolValue) == GetValueResult.Success)
            {
                return boolValue;
            }

            if (storageType == typeof(bool?) && _settingManager.TryGetValue(storageKey, out bool? nullableBoolValue) == GetValueResult.Success)
            {
                return nullableBoolValue;
            }

            if (storageType == typeof(int) && _settingManager.TryGetValue(storageKey, out int intValue) == GetValueResult.Success)
            {
                return intValue;
            }

            if (storageType.IsEnum && _settingManager.TryGetValue(storageKey, out int enumValue) == GetValueResult.Success)
            {
                return Enum.ToObject(storageType, enumValue);
            }

            if (storageType == typeof(NamingStylePreferences) || typeof(ICodeStyleOption).IsAssignableFrom(storageType))
            {
                if (_settingManager.TryGetValue(storageKey, out string stringValue) == GetValueResult.Success)
                {
                    try
                    {
                        if (storageType == typeof(NamingStylePreferences))
                        {
                            return NamingStylePreferences.FromXElement(XElement.Parse(stringValue));
                        }
                        else
                        {
                            var fromXElement = storageType.GetMethod(nameof(CodeStyleOption<object>.FromXElement), BindingFlags.Public | BindingFlags.Static);
                            return fromXElement.Invoke(null, new object[] { XElement.Parse(stringValue) });
                        }
                    }
                    catch
                    {
                        return default;
                    }
                }
            }

            if (storageType == typeof(ImmutableArray<string>) && _settingManager.TryGetValue(storageKey, out string[] stringArray) == GetValueResult.Success)
            {
                return stringArray.ToImmutableArray();
            }

            if (_settingManager.TryGetValue(storageKey, out object? value) == GetValueResult.Success &&
                (value is null || value.GetType() == storageType))
            {
                return value;
            }

            return default;
        }

        public bool TryPersist(OptionKey2 optionKey, string storageKey, object? value)
        {
            if (_settingManager == null)
            {
                Debug.Fail("Manager field is unexpectedly null.");
                return false;
            }

            RecordObservedValueToWatchForChanges(optionKey, storageKey, optionKey.Option.Type);

            if (value is ICodeStyleOption codeStyleOption)
            {
                // We store these as strings, so serialize
                value = codeStyleOption.ToXElement().ToString();
            }
            else if (optionKey.Option.Type == typeof(NamingStylePreferences))
            {
                // We store these as strings, so serialize
                if (value is NamingStylePreferences valueToSerialize)
                {
                    value = valueToSerialize.CreateXElement().ToString();
                }
            }

            _settingManager.SetValueAsync(storageKey, value, isMachineLocal: false);
            return true;
        }
    }
}
