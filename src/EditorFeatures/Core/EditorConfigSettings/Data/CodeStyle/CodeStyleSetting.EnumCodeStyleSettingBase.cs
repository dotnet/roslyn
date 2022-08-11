// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract partial class CodeStyleSetting
    {
        private abstract class EnumCodeStyleSettingBase<T> : CodeStyleSetting
            where T : Enum
        {
            protected readonly T[] _enumValues;
            private readonly string[] _valueDescriptions;

            public EnumCodeStyleSettingBase(string description,
                                            T[] enumValues,
                                            string[] valueDescriptions,
                                            string category,
                                            OptionUpdater updater)
                : base(description, updater)
            {
                if (enumValues.Length != valueDescriptions.Length)
                {
                    throw new InvalidOperationException("Values and descriptions must have matching number of elements");
                }

                _enumValues = enumValues;
                _valueDescriptions = valueDescriptions;
                Category = category;
            }

            public override string Category { get; }
            public override Type Type => typeof(T);
            public override string GetCurrentValue() => _valueDescriptions[_enumValues.IndexOf(GetOption().Value)];
            public override object? Value => GetOption().Value;
            public override DiagnosticSeverity Severity => GetOption().Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden;
            public override string[] GetValues() => _valueDescriptions;
            protected abstract CodeStyleOption2<T> GetOption();
            protected static ImmutableArray<string>? GetSettingValuesHelper(IEditorConfigStorageLocation2? storageLocation, OptionSet optionSet)
            {
                var type = typeof(T);
                var strings = new List<string>();
                var enumValues = type.GetEnumValues();

                foreach (var enumValue in enumValues)
                {
                    var codeStyleSetting = new CodeStyleOption2<T>((T)enumValue!, NotificationOption2.Silent);
                    var option = storageLocation?.GetEditorConfigStringValue(codeStyleSetting, optionSet);
                    if (option != null)
                    {
                        // TODO: Switch to EditorConfig value holder: https://github.com/dotnet/roslyn/issues/63329
                        option = option.Contains(':') ? option.Split(':').First() : option;
                        strings.Add(option);
                    }
                }

                return strings.ToImmutableArray();
            }
        }
    }
}
