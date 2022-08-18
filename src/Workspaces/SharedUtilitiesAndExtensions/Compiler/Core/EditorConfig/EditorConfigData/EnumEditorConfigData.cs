// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    internal class EnumEditorConfigData<T> : EditorConfigData<T> where T : Enum
    {
        private readonly BidirectionalMap<string, T> ValueToSettingName;
        //private readonly ImmutableDictionary<string, string>? ValueToValueDocumentation;
        private readonly T? DefaultValue;

        public EnumEditorConfigData(string settingName, string settingNameDocumentation, BidirectionalMap<string, T> valueToSettingName, bool allowsMultipleValues = false)
            : base(settingName, settingNameDocumentation, allowsMultipleValues)
        {
            ValueToSettingName = valueToSettingName;
        }

        public EnumEditorConfigData(string settingName, string settingNameDocumentation, BidirectionalMap<string, T> valueToSettingName, T defaultValue, bool allowsMultipleValues = false)
            : base(settingName, settingNameDocumentation, allowsMultipleValues)
        {
            ValueToSettingName = valueToSettingName;
            DefaultValue = defaultValue;
        }

        public override string GetSettingName() => SettingName;

        public override string GetSettingNameDocumentation() => SettingNameDocumentation;

        public override bool GetAllowsMultipleValues() => AllowsMultipleValues;

        public override ImmutableArray<string> GetAllSettingValues()
        {
            return ValueToSettingName.Keys.ToImmutableArray();
        }

        public override string? GetSettingValueDocumentation(string key)
        {
            throw new NotImplementedException();
        }

        public override string GetEditorConfigStringFromValue(T value)
        {
            return ValueToSettingName.TryGetKey(value, out var key) ? key : "";
        }

        public override Optional<T> GetValueFromEditorConfigString(string key)
        {
            return ValueToSettingName.TryGetValue(key, out var value) ? value : DefaultValue ?? default!;
        }
    }
}
