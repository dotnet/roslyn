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
    internal class BooleanEditorConfigData : EditorConfigData<bool>
    {
        private readonly BidirectionalMap<string, bool>? ValueToSettingName;
        //private readonly ImmutableDictionary<string, string>? ValueToValueDocumentation;

        public BooleanEditorConfigData(string settingName, string settingNameDocumentation, BidirectionalMap<string, bool>? valueToSettingName = null)
            : base(settingName, settingNameDocumentation)
        {
            ValueToSettingName = valueToSettingName;
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

        public override string? GetSettingValueDocumentation(string key)
        {
            throw new NotImplementedException();
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
                ValueToSettingName.TryGetValue(key, out var value);
                return value;
            }

            return bool.TryParse(key, out var result) ? result : new Optional<bool>();
        }
    }
}
