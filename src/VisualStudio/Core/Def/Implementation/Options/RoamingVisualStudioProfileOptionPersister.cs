// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// Serializes settings marked with <see cref="RoamingProfileStorageLocation"/> to and from the user's roaming profile.
    /// </summary>
    [Export(typeof(IOptionPersister))]
    internal sealed class RoamingVisualStudioProfileOptionPersister : ForegroundThreadAffinitizedObject, IOptionPersister
    {
        // NOTE: This service is not public or intended for use by teams/individuals outside of Microsoft. Any data stored is subject to deletion without warning.
        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { };

        private readonly ISettingsManager _settingManager;
        private readonly IGlobalOptionService _globalOptionService;

        /// <summary>
        /// The list of options that have been been fetched from <see cref="_settingManager"/>, by key. We track this so
        /// if a later change happens, we know to refresh that value. This is synchronized with monitor locks on
        /// <see cref="_optionsToMonitorForChangesGate" />.
        /// </summary>
        private readonly Dictionary<string, List<OptionKey>> _optionsToMonitorForChanges = new Dictionary<string, List<OptionKey>>();
        private readonly object _optionsToMonitorForChangesGate = new object();

        /// <remarks>We make sure this code is from the UI by asking for all serializers on the UI thread in <see cref="HACK_AbstractCreateServicesOnUiThread"/>.</remarks>
        [ImportingConstructor]
        public RoamingVisualStudioProfileOptionPersister(IGlobalOptionService globalOptionService, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(assertIsForeground: true) // The GetService call requires being on the UI thread or else it will marshal and risk deadlock
        {
            Contract.ThrowIfNull(globalOptionService);

            _settingManager = (ISettingsManager)serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));
            _globalOptionService = globalOptionService;

            // While the settings persistence service should be available in all SKUs it is possible an ISO shell author has undefined the
            // contributing package. In that case persistence of settings won't work (we don't bother with a backup solution for persistence
            // as the scenario seems exceedingly unlikely), but we shouldn't crash the IDE.
            if (_settingManager != null)
            {
                var settingsSubset = _settingManager.GetSubset("*");
                settingsSubset.SettingChangedAsync += OnSettingChangedAsync;
            }
        }

        private System.Threading.Tasks.Task OnSettingChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            lock (_optionsToMonitorForChangesGate)
            {
                if (_optionsToMonitorForChanges.TryGetValue(args.PropertyName, out var optionsToRefresh))
                {
                    foreach (var optionToRefresh in optionsToRefresh)
                    {
                        if (TryFetch(optionToRefresh, out var optionValue))
                        {
                            _globalOptionService.RefreshOption(optionToRefresh, optionValue);
                        }
                    }
                }
            }

            return SpecializedTasks.EmptyTask;
        }

        public bool TryFetch(OptionKey optionKey, out object value)
        {
            if (_settingManager == null)
            {
                Debug.Fail("Manager field is unexpectedly null.");
                value = null;
                return false;
            }

            // Do we roam this at all?
            var roamingSerialization = optionKey.Option.StorageLocations.OfType<RoamingProfileStorageLocation>().SingleOrDefault();

            if (roamingSerialization == null)
            {
                value = null;
                return false;
            }

            var storageKey = roamingSerialization.GetKeyNameForLanguage(optionKey.Language);

            RecordObservedValueToWatchForChanges(optionKey, storageKey);

            value = _settingManager.GetValueOrDefault(storageKey, optionKey.Option.DefaultValue);

            // VS's ISettingsManager has some quirks around storing enums.  Specifically,
            // it *can* persist and retrieve enums, but only if you properly call 
            // GetValueOrDefault<EnumType>.  This is because it actually stores enums just
            // as ints and depends on the type parameter passed in to convert the integral
            // value back to an enum value.  Unfortunately, we call GetValueOrDefault<object>
            // and so we get the value back as boxed integer.
            //
            // Because of that, manually convert the integer to an enum here so we don't
            // crash later trying to cast a boxed integer to an enum value.
            if (optionKey.Option.Type.IsEnum)
            {
                if (value != null)
                {
                    value = Enum.ToObject(optionKey.Option.Type, value);
                }
            }
            else if (optionKey.Option.Type == typeof(CodeStyleOption<bool>))
            {
                return DeserializeCodeStyleOption<bool>(ref value);
            }
            else if (optionKey.Option.Type == typeof(CodeStyleOption<ExpressionBodyPreference>))
            {
                return DeserializeCodeStyleOption<ExpressionBodyPreference>(ref value);
            }
            else if (optionKey.Option.Type == typeof(NamingStylePreferences))
            {
                // We store these as strings, so deserialize
                if (value is string serializedValue)
                {
                    try
                    {
                        value = NamingStylePreferences.FromXElement(XElement.Parse(serializedValue));
                    }
                    catch (Exception)
                    {
                        value = null;
                        return false;
                    }
                }
                else
                {
                    value = null;
                    return false;
                }
            }
            else if (optionKey.Option.Type == typeof(bool) && value is int intValue)
            {
                // TypeScript used to store some booleans as integers. We now handle them properly for legacy sync scenarios.
                value = intValue != 0;
                return true;
            }
            else if (optionKey.Option.Type == typeof(bool) && value is long longValue)
            {
                // TypeScript used to store some booleans as integers. We now handle them properly for legacy sync scenarios.
                value = longValue != 0;
                return true;
            }
            else if (optionKey.Option.Type == typeof(bool?))
            {
                // code uses object to hold onto any value which will use boxing on value types.
                // see boxing on nullable types - https://msdn.microsoft.com/en-us/library/ms228597.aspx
                return (value is bool) || (value == null);
            }
            else if (value != null && optionKey.Option.Type != value.GetType())
            {
                // We got something back different than we expected, so fail to deserialize
                value = null;
                return false;
            }

            return true;
        }

        private bool DeserializeCodeStyleOption<T>(ref object value)
        {
            if (value is string serializedValue)
            {
                try
                {
                    value = CodeStyleOption<T>.FromXElement(XElement.Parse(serializedValue));
                    return true;
                }
                catch (Exception)
                {
                }
            }

            value = null;
            return false;
        }

        private void RecordObservedValueToWatchForChanges(OptionKey optionKey, string storageKey)
        {
            // We're about to fetch the value, so make sure that if it changes we'll know about it
            lock (_optionsToMonitorForChangesGate)
            {
                var optionKeysToMonitor = _optionsToMonitorForChanges.GetOrAdd(storageKey, _ => new List<OptionKey>());

                if (!optionKeysToMonitor.Contains(optionKey))
                {
                    optionKeysToMonitor.Add(optionKey);
                }
            }
        }

        public bool TryPersist(OptionKey optionKey, object value)
        {
            if (_settingManager == null)
            {
                Debug.Fail("Manager field is unexpectedly null.");
                return false;
            }

            // Do we roam this at all?
            var roamingSerialization = optionKey.Option.StorageLocations.OfType<RoamingProfileStorageLocation>().SingleOrDefault();

            if (roamingSerialization == null)
            {
                value = null;
                return false;
            }

            var storageKey = roamingSerialization.GetKeyNameForLanguage(optionKey.Language);

            RecordObservedValueToWatchForChanges(optionKey, storageKey);

            if (value is ICodeStyleOption codeStyleOption)
            {
                // We store these as strings, so serialize
                value = codeStyleOption.ToXElement().ToString();
            }
            else if (optionKey.Option.Type == typeof(NamingStylePreferences))
            {
                // We store these as strings, so serialize
                var valueToSerialize = value as NamingStylePreferences;

                if (value != null)
                {
                    value = valueToSerialize.CreateXElement().ToString();
                }
            }

            _settingManager.SetValueAsync(storageKey, value, isMachineLocal: false);
            return true;
        }
    }
}
