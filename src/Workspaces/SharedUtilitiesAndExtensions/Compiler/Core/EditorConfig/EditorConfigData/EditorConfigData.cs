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
    internal interface IEditorConfigData
    {
        public string GetSettingName();
        public string GetSettingNameDocumentation();
        public bool GetAllowsMultipleValues();
        public ImmutableArray<string> GetAllSettingValues();
        public string[] GetAllSettingValuesDocumentation();
        public string? GetSettingValueDocumentation(string key);
    }

    internal abstract class EditorConfigData<T> : IEditorConfigData
    {
        public string SettingName { get; }
        public string SettingNameDocumentation { get; }
        public bool AllowsMultipleValues { get; }

        public EditorConfigData(string settingName, string settingNameDocumentation, bool allowsMultipleValues = false)
        {
            SettingName = settingName;
            SettingNameDocumentation = settingNameDocumentation;
            AllowsMultipleValues = allowsMultipleValues;
        }

        public abstract string GetSettingName();
        public abstract string GetSettingNameDocumentation();
        public abstract bool GetAllowsMultipleValues();
        public abstract ImmutableArray<string> GetAllSettingValues();
        public abstract string[] GetAllSettingValuesDocumentation();
        public abstract string? GetSettingValueDocumentation(string key);

        public abstract string GetEditorConfigStringFromValue(T value);
        public abstract Optional<T> GetValueFromEditorConfigString(string key);
    }
}
