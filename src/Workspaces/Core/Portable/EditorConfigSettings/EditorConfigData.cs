// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        public string? GetSettingValueDocumentation(string key);
    }

    internal abstract class EditorConfigData<T> : IEditorConfigData where T : notnull
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
        public abstract string? GetSettingValueDocumentation(string key);
        public abstract string? GetEditorConfigStringFromValue(T value);
        public abstract Optional<T> GetValueFromEditorConfigString(string key);
    }

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

        public override string? GetEditorConfigStringFromValue(bool value)
        {
            if (ValueToSettingName == null)
            {
                return value.ToString()!.ToLowerInvariant();

            }

            ValueToSettingName.TryGetKey(value, out var key);
            return key;
        }

        public override Optional<bool> GetValueFromEditorConfigString(string key)
        {
            if (ValueToSettingName == null)
            {
                return bool.TryParse(key, out var result) ? result : new Optional<bool>();
            }

            ValueToSettingName.TryGetValue(key, out var value);
            return value;
        }
    }

    internal class EnumEditorConfigData<T> : EditorConfigData<T> where T : Enum
    {
        private readonly BidirectionalMap<string, T> ValueToSettingName;
        //private readonly ImmutableDictionary<string, string>? ValueToValueDocumentation;

        public EnumEditorConfigData(string settingName, string settingNameDocumentation, BidirectionalMap<string, T> valueToSettingName, bool allowsMultipleValues = false)
            : base(settingName, settingNameDocumentation, allowsMultipleValues)
        {
            ValueToSettingName = valueToSettingName;
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

        public override string? GetEditorConfigStringFromValue(T value)
        {
            ValueToSettingName.TryGetKey(value, out var key);
            return key;
        }

        public override Optional<T> GetValueFromEditorConfigString(string key)
        {
            ValueToSettingName.TryGetValue(key, out var value);
            return value ?? new Optional<T>();
        }
    }

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

        public override string? GetSettingValueDocumentation(string key)
        {
            throw new NotImplementedException();
        }

        public override string? GetEditorConfigStringFromValue(int value)
        {
            return value.ToString().ToLowerInvariant();
        }

        public override Optional<int> GetValueFromEditorConfigString(string key)
        {
            return int.TryParse(key, out var result) ? result : new Optional<int>();
        }
    }

    internal class StringEditorConfigData : EditorConfigData<string>
    {
        private readonly BidirectionalMap<string, string> ValueToSettingName;
        //private readonly ImmutableDictionary<string, string>? ValueToValueDocumentation;

        public StringEditorConfigData(string settingName, string settingNameDocumentation, BidirectionalMap<string, string> valueToSettingName)
            : base(settingName, settingNameDocumentation)
        {
            ValueToSettingName = valueToSettingName;
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

        public override string? GetEditorConfigStringFromValue(string value)
        {
            ValueToSettingName.TryGetKey(value, out var key);
            return key;
        }

        public override Optional<string> GetValueFromEditorConfigString(string key)
        {
            ValueToSettingName.TryGetValue(key, out var value);
            return value ?? new Optional<string>();
        }
    }

    internal class AnalyzerEditorConfigData : EditorConfigData<DiagnosticSeverity>
    {
        private readonly BidirectionalMap<string, DiagnosticSeverity> ValueToSettingName;
        //private readonly ImmutableDictionary<string, string>? ValueToValueDocumentation;

        public AnalyzerEditorConfigData(string settingName, BidirectionalMap<string, DiagnosticSeverity> valueToSettingName, bool allowsMultipleValues = false)
            : base(settingName, "", allowsMultipleValues)
        {
            ValueToSettingName = valueToSettingName;
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

        public override string? GetEditorConfigStringFromValue(DiagnosticSeverity value)
        {
            ValueToSettingName.TryGetKey(value, out var key);
            return key;
        }

        public override Optional<DiagnosticSeverity> GetValueFromEditorConfigString(string key)
        {
            ValueToSettingName.TryGetValue(key, out var value);
            return value;
        }
    }
}
