// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
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
        }
    }
}
