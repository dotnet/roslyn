// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    internal class BooleanEditorConfigData : EditorConfigData<bool>
    {
        private readonly BidirectionalMap<string, bool>? ValueToSettingName;
        private readonly Dictionary<string, string>? ValuesDocumentation;

        public BooleanEditorConfigData(string settingName, string settingNameDocumentation, BidirectionalMap<string, bool>? valueToSettingName = null, Dictionary<string, string>? valuesDocumentation = null)
            : base(settingName, settingNameDocumentation)
        {
            ValueToSettingName = valueToSettingName;
            ValuesDocumentation = valuesDocumentation;
        }

        public override string GetSettingName() => SettingName;

        public override string GetSettingNameDocumentation() => SettingNameDocumentation;

        public override bool GetAllowsMultipleValues() => AllowsMultipleValues;

        public override ImmutableArray<string> GetAllSettingValues()
        {
            if (ValueToSettingName == null)
            {
                return ImmutableArray.Create(new[] { "true", "false" });
            }

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

        public override string GetEditorConfigStringFromValue(bool value)
        {
            if (ValueToSettingName != null)
            {
                ValueToSettingName.TryGetKey(value, out var key);
                return key!;
            }

            return value.ToString().ToLowerInvariant();
        }

        public override Optional<bool> GetValueFromEditorConfigString(string key)
        {
            if (ValueToSettingName != null)
            {
                ValueToSettingName.TryGetValue(key.Trim(), out var value);
                return value;
            }

            return bool.TryParse(key, out var result) ? result : new Optional<bool>();
        }

        public override bool IsValueValid(string value)
        {
            if (ValueToSettingName != null)
            {
                ValueToSettingName.TryGetValue(value.Trim(), out var _);
            }

            return bool.TryParse(value, out var _);
        }
    }
}
