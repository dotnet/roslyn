﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    internal class EnumEditorConfigData<T> : EditorConfigData<T> where T : Enum
    {
        private readonly BidirectionalMap<string, T> ValueToSettingName;
        private readonly Dictionary<string, string>? ValuesDocumentation;
        private readonly T? DefaultValue;
        private readonly bool Nullable;

        public EnumEditorConfigData(string settingName, string settingNameDocumentation, BidirectionalMap<string, T> valueToSettingName, Dictionary<string, string>? valuesDocumentation = null, bool allowsMultipleValues = false, bool nullable = false)
            : base(settingName, settingNameDocumentation, allowsMultipleValues)
        {
            ValueToSettingName = valueToSettingName;
            ValuesDocumentation = valuesDocumentation;
            Nullable = nullable;
        }
        public EnumEditorConfigData(string settingName, string settingNameDocumentation, BidirectionalMap<string, T> valueToSettingName, T defaultValue, Dictionary<string, string>? valuesDocumentation = null, bool allowsMultipleValues = false, bool nullable = false)
            : base(settingName, settingNameDocumentation, allowsMultipleValues)
        {
            ValueToSettingName = valueToSettingName;
            ValuesDocumentation = valuesDocumentation;
            DefaultValue = defaultValue;
            Nullable = nullable;
        }

        public override string GetSettingName() => SettingName;

        public override string GetSettingNameDocumentation() => SettingNameDocumentation;

        public override bool GetAllowsMultipleValues() => AllowsMultipleValues;

        public override ImmutableArray<string> GetAllSettingValues()
        {
            return ValueToSettingName.Keys.ToImmutableArray();
        }

        public override string[] GetAllSettingValuesDocumentation()
        {
            if (ValuesDocumentation != null)
            {
                return ValuesDocumentation.Values.ToArray();
            }

            return Array.Empty<string>();
        }

        public override string? GetSettingValueDocumentation(string key)
        {
            if (ValuesDocumentation != null)
            {
                return ValuesDocumentation.TryGetValue(key, out var value) ? value : null;
            }

            return null;
        }

        public override string GetEditorConfigStringFromValue(T value)
        {
            return ValueToSettingName.TryGetKey(value, out var key) ? key : "";
        }

        public override Optional<T> GetValueFromEditorConfigString(string key)
        {
            if (Nullable)
            {
                return ValueToSettingName.TryGetValue(key.Trim(), out var value1) ? value1 : new Optional<T>();
            }
            return ValueToSettingName.TryGetValue(key.Trim(), out var value2) ? value2 : DefaultValue ?? ValueToSettingName.GetValueOrDefault(key)!;
        }

        public override bool IsValueValid(string value)
        {
            return ValueToSettingName.TryGetValue(value.Trim(), out var _);
        }
    }
}
