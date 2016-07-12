// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Settings;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal abstract class AbstractSettingsManagerOptionSerializer : ForegroundThreadAffinitizedObject, IOptionSerializer
    {
        // NOTE: This service is not public or intended for use by teams/individuals outside of Microsoft. Any data stored is subject to deletion without warning.
        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { };

        protected readonly ISettingsManager Manager;
        private readonly IOptionService _optionService;

        public AbstractSettingsManagerOptionSerializer(VisualStudioWorkspaceImpl workspace)
            : base(assertIsForeground: true) // The GetService call requires being on the UI thread or else it will marshal and risk deadlock
        {
            Contract.ThrowIfNull(workspace);

            _storageKeyToOptionMap = new Lazy<ImmutableDictionary<string, IOption>>(CreateStorageKeyToOptionMap, isThreadSafe: true);

            this.Manager = workspace.GetVsService<SVsSettingsPersistenceManager, ISettingsManager>();
            _optionService = workspace.Services.GetService<IOptionService>();

            // While the settings persistence service should be available in all SKUs it is possible an ISO shell author has undefined the
            // contributing package. In that case persistence of settings won't work (we don't bother with a backup solution for persistence
            // as the scenario seems exceedingly unlikely), but we shouldn't crash the IDE.
            if (this.Manager != null)
            {
                ISettingsSubset settingsSubset = this.Manager.GetSubset(SettingStorageRoot + "*");
                settingsSubset.SettingChangedAsync += OnSettingChangedAsync;
            }
        }

        protected abstract string SettingStorageRoot { get; }

        protected abstract bool SupportsOption(IOption option, string languageName);

        protected virtual string GetStorageKeyForOption(IOption option)
        {
            return SettingStorageRoot + option.Name;
        }

        protected static IEnumerable<KeyValuePair<string, IOption>> GetOptionInfoFromTypeFields(IEnumerable<Type> types, BindingFlags flags, Func<FieldInfo, KeyValuePair<string, IOption>> transform)
        {
            return GetOptionInfoFromTypeFields(types, flags, transform, filter: null);
        }

        private Task OnSettingChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            IOption option;
            if (this.StorageKeyToOptionMap.TryGetValue(args.PropertyName, out option))
            {
                this.SetChangedOption(_optionService, option, LanguageName);
            }

            return SpecializedTasks.Default<object>();
        }

        protected abstract string LanguageName { get; }

        private void SetChangedOption(IOptionService optionService, IOption option, string languageName)
        {
            OptionKey key = new OptionKey(option, option.IsPerLanguage ? languageName : null);

            object currentValue;
            if (this.TryFetch(key, out currentValue))
            {
                OptionSet optionSet = optionService.GetOptions();
                optionSet = optionSet.WithChangedOption(key, currentValue);

                optionService.SetOptions(optionSet);
            }
        }

        private Lazy<ImmutableDictionary<string, IOption>> _storageKeyToOptionMap;

        protected abstract ImmutableDictionary<string, IOption> CreateStorageKeyToOptionMap();

        protected ImmutableDictionary<string, IOption> StorageKeyToOptionMap
        {
            get
            {
                return _storageKeyToOptionMap.Value;
            }
        }

        protected KeyValuePair<string, IOption> GetOptionInfo(FieldInfo fieldInfo)
        {
            var value = (IOption)fieldInfo.GetValue(null);
            return new KeyValuePair<string, IOption>(GetStorageKeyForOption(value), value);
        }

        protected static IEnumerable<KeyValuePair<string, IOption>> GetOptionInfoFromTypeFields(IEnumerable<Type> types, BindingFlags flags, Func<FieldInfo, KeyValuePair<string, IOption>> transform, Predicate<FieldInfo> filter)
        {
            var values = new List<KeyValuePair<string, IOption>>();
            foreach (Type type in types)
            {
                FieldInfo[] fields = type.GetFields(flags) ?? Array.Empty<FieldInfo>();
                Func<FieldInfo, bool> localFilter = (fi) =>
                    {
                        if (!(fi.GetValue(null) is IOption))
                        {
                            return false;
                        }

                        return (filter != null) ? filter(fi) : true;
                    };

                values.AddRange(fields.Where(localFilter).Select(transform));
            }

            return values;
        }

        public virtual bool TryFetch(OptionKey optionKey, out object value)
        {
            value = null;

            if (this.Manager == null)
            {
                Debug.Fail("Manager field is unexpectedly null.");
                return false;
            }

            if (!SupportsOption(optionKey.Option, optionKey.Language))
            {
                value = null;
                return false;
            }

            var storageKey = GetStorageKeyForOption(optionKey.Option);
            value = this.Manager.GetValueOrDefault(storageKey, optionKey.Option.DefaultValue);

            // VS's ISettingsManager has some quirks around storing enums.  Specifically,
            // it *can* persist and retrieve enums, but only if you properly call 
            // GetValueOrDefault<EnumType>.  This is because it actually stores enums just
            // as ints and depends on the type parameter passed in to convert the integral
            // value back to an enum value.  Unfortunately, we call GetValueOrDefault<object>
            // and so we get the value back as boxed integer.
            //
            // Because of that, manually convert the integer to an enum here so we don't
            // crash later trying to cast a boxed integer to an enum value.
            if (value != null && optionKey.Option.Type.IsEnum)
            {
                value = Enum.ToObject(optionKey.Option.Type, value);
            }

            return true;
        }

        public virtual bool TryPersist(OptionKey optionKey, object value)
        {
            if (this.Manager == null)
            {
                Debug.Fail("Manager field is unexpectedly null.");
                return false;
            }

            if (!SupportsOption(optionKey.Option, optionKey.Language))
            {
                return false;
            }

            var storageKey = GetStorageKeyForOption(optionKey.Option);
            this.Manager.SetValueAsync(storageKey, value, isMachineLocal: false);
            return true;
        }
    }
}