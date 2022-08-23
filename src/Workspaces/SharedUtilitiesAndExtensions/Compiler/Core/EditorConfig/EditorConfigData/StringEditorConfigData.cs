// Licensed to the .NET Foundation under one or more agreements.
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
    internal class StringEditorConfigData : EditorConfigData<string>
    {
        private readonly BidirectionalMap<string, string>? ValueToSettingName;
        private readonly Dictionary<string, string>? ValuesDocumentation;
        private readonly string DefaultEditorConfigString;
        private readonly string DefaultValue;

        public StringEditorConfigData(string settingName, string settingNameDocumentation, string defaultEditorConfigString, string defaultValue, BidirectionalMap<string, string>? valueToSettingName = null, Dictionary<string, string>? valuesDocumentation = null)
            : base(settingName, settingNameDocumentation)
        {
            ValueToSettingName = valueToSettingName;
            DefaultEditorConfigString = defaultEditorConfigString;
            ValuesDocumentation = valuesDocumentation;
            DefaultValue = defaultValue;
        }

        public override string GetSettingName() => SettingName;

        public override string GetSettingNameDocumentation() => SettingNameDocumentation;

        public override bool GetAllowsMultipleValues() => AllowsMultipleValues;

        public override ImmutableArray<string> GetAllSettingValues()
        {
            return ValueToSettingName != null ? ValueToSettingName.Keys.ToImmutableArray() : ImmutableArray<string>.Empty;
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

        public override string GetEditorConfigStringFromValue(string value)
        {
            if (ValueToSettingName != null)
            {
                return ValueToSettingName.TryGetKey(value, out var key) ? key : DefaultEditorConfigString;
            }

            return string.IsNullOrEmpty(value) ? DefaultEditorConfigString : value.ToString().Replace("\r", "\\r").Replace("\n", "\\n");
        }

        public override Optional<string> GetValueFromEditorConfigString(string key)
        {
            if (ValueToSettingName != null)
            {
                return ValueToSettingName.TryGetValue(key, out var value) ? value : DefaultValue;
            }

            if (key == DefaultEditorConfigString)
            {
                return default;
            }

            key ??= "";
            return key.Replace("\\r", "\r").Replace("\\n", "\n");
        }
    }
}
