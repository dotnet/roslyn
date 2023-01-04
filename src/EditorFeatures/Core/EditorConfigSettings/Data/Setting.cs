// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract class Setting
    {
        public IOptionWithGroup Option { get; }
        public OptionKey2 Key { get; }
        public OptionUpdater Updater { get; }
        public string Description { get; }

        public SettingLocation Location { get; private set; }

        protected Setting(IOptionWithGroup option, OptionKey2 optionKey, string description, OptionUpdater updater, SettingLocation location)
        {
            Option = option;
            Key = optionKey;
            Description = description;
            Updater = updater;
            Location = location;
        }

        public abstract Type Type { get; }
        protected abstract object UpdateValue(object settingValue);
        public abstract object? GetValue();

        public void SetValue(object value)
        {
            Location = Location with { LocationKind = LocationKind.EditorConfig };
            Updater.QueueUpdate(Option, UpdateValue(value));
        }

        public string Category => Option.Group.Description;
        public bool IsDefinedInEditorConfig => Location.LocationKind != LocationKind.VisualStudio;

        public static Setting<TValue> Create<TValue>(
            Option2<TValue> option,
            string description,
            TieredAnalyzerConfigOptions options,
            OptionUpdater updater)
            where TValue : notnull
        {
            var optionKey = new OptionKey2(option);
            options.GetInitialLocationAndValue<TValue>(option, out var initialLocation, out var initialValue);
            return new Setting<TValue>(option, optionKey, description, updater, initialLocation, initialValue);
        }

        public static Setting<TValue> Create<TValue>(
            PerLanguageOption2<TValue> option,
            string description,
            TieredAnalyzerConfigOptions options,
            OptionUpdater updater)
            where TValue : notnull
        {
            // TODO: Support for other languages https://github.com/dotnet/roslyn/issues/65859
            var optionKey = new OptionKey2(option, LanguageNames.CSharp);
            options.GetInitialLocationAndValue<TValue>(option, out var initialLocation, out var initialValue);
            return new Setting<TValue>(option, optionKey, description, updater, initialLocation, initialValue);
        }

        public static EnumFlagsSetting<TValue> CreateEnumFlags<TValue>(
            Option2<TValue> option,
            int flag,
            string description,
            StrongBox<TValue> valueStorage,
            Conversions<TValue, int> conversions,
            TieredAnalyzerConfigOptions options,
            OptionUpdater updater)
            where TValue : struct, Enum
        {
            var optionKey = new OptionKey2(option);
            options.GetInitialLocationAndValue<TValue>(option, out var initialLocation, out var initialValue);
            valueStorage.Value = initialValue;
            return new EnumFlagsSetting<TValue>(option, optionKey, description, updater, initialLocation, flag, valueStorage, conversions);
        }
    }
}
