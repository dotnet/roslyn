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
    internal class StringEditorConfigData : EditorConfigData<string>
    {
        private readonly BidirectionalMap<string, string> ValueToSettingName;
        //private readonly ImmutableDictionary<string, string>? ValueToValueDocumentation;
        private readonly string DefaultEditorConfigString;
        private readonly string DefaultValue;

        public StringEditorConfigData(string settingName, string settingNameDocumentation, BidirectionalMap<string, string> valueToSettingName, string defaultEditorConfigString, string defaultValue)
            : base(settingName, settingNameDocumentation)
        {
            ValueToSettingName = valueToSettingName;
            DefaultEditorConfigString = defaultEditorConfigString;
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

        public override string GetEditorConfigStringFromValue(string value)
        {
            return ValueToSettingName.TryGetKey(value, out var key) ? key : DefaultEditorConfigString;
        }

        public override Optional<string> GetValueFromEditorConfigString(string key)
        {
            return ValueToSettingName.TryGetValue(key, out var value) ? value : DefaultValue;
        }
    }
}
