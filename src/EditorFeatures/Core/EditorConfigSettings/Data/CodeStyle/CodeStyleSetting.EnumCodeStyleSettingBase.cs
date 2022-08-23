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
using Microsoft.CodeAnalysis.EditorConfigSettings;
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

            public EnumCodeStyleSettingBase(string description,
                                            T[] enumValues,
                                            string category,
                                            OptionUpdater updater,
                                            IEditorConfigData editorConfigData)
                : base(description, updater, editorConfigData)
            {
                if (enumValues.Length != editorConfigData?.GetAllSettingValuesDocumentation().Length)
                {
                    throw new InvalidOperationException("Values and descriptions must have matching number of elements");
                }

                _enumValues = enumValues;
                Category = category;
            }

            public override string Category { get; }
            public override Type Type => typeof(T);
            public override string GetCurrentValue()
            {
                var editorConfigString = ((EditorConfigData<T>)EditorConfigData).GetEditorConfigStringFromValue(GetOption().Value);
                return EditorConfigData.GetSettingValueDocumentation(editorConfigString) ?? "";
            }

            public override object? Value => GetOption().Value;
            public override DiagnosticSeverity Severity => GetOption().Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden;
            public override string[] GetValues() => EditorConfigData.GetAllSettingValuesDocumentation();
            protected abstract CodeStyleOption2<T> GetOption();
        }
    }
}
