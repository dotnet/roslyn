// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfigSettings.DataProvider
{
    internal class EditorConfigData<T> where T : notnull
    {
        public string SettingName { get; }
        public string SettingNameDocumentation { get; }
        private readonly BidirectionalMap<string, T> ValueToSettingName;
        private readonly ImmutableDictionary<T, string> ValueToValueDocumentation;

        public EditorConfigData(string settingName, string settingNameDocumentation, BidirectionalMap<string, T> valueToSettingName, ImmutableDictionary<T, string> valueToValueDocumentation)
        {
            SettingName = settingName;
            SettingNameDocumentation = settingNameDocumentation;
            ValueToSettingName = valueToSettingName;
            ValueToValueDocumentation = valueToValueDocumentation;
        }

        public string? GetEditorConfigStringFromValue(T value)
        {
            ValueToSettingName.TryGetKey(value, out var key);
            return key;
        }

        public T? GetValueFromEditorConfigString(string key)
        {
            ValueToSettingName.TryGetValue(key, out var value);
            return value;
        }

        public string? GetValueDocumentation(T key)
        {
            ValueToValueDocumentation.TryGetValue(key, out var value);
            return value;
        }
    }
}
