// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    internal class IntegerEditorConfigData : EditorConfigData<int>
    {
        public IntegerEditorConfigData(string settingName, string settingNameDocumentation)
            : base(settingName, settingNameDocumentation)
        {
        }

        public override string GetSettingName() => SettingName;

        public override string GetSettingNameDocumentation() => SettingNameDocumentation;

        public override bool GetAllowsMultipleValues() => AllowsMultipleValues;

        public override ImmutableArray<string> GetAllSettingValues()
        {
            return ImmutableArray.Create(new[] { "2", "4", "8" });
        }

        public override string[] GetAllSettingValuesDocumentation()
        {
            return Array.Empty<string>();
        }

        public override string? GetSettingValueDocumentation(string key)
        {
            return null;
        }

        public override string GetEditorConfigStringFromValue(int value)
        {
            return value.ToString().ToLowerInvariant();
        }

        public override Optional<int> GetValueFromEditorConfigString(string key)
        {
            return int.TryParse(key, out var result) ? result : new Optional<int>();
        }
    }
}
