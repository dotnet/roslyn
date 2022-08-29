// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.EditorConfigSettings;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract partial class CodeStyleSetting
    {
        private abstract class BooleanCodeStyleSettingBase : CodeStyleSetting
        {
            public BooleanCodeStyleSettingBase(string description,
                                               string category,
                                               OptionUpdater updater,
                                               IEditorConfigData editorConfigData)
                : base(description, updater, editorConfigData)
            {
                Category = category;
            }

            public override string Category { get; }
            public override Type Type => typeof(bool);
            public override DiagnosticSeverity Severity => GetOption().Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden;
            public override string GetCurrentValue()
            {
                var editorConfigString = ((EditorConfigData<bool>)EditorConfigData).GetEditorConfigStringFromValue(GetOption().Value);
                return EditorConfigData.GetSettingValueDocumentation(editorConfigString) ?? "";
            }

            public override object? Value => GetOption().Value;
            public override string[] GetValues() => EditorConfigData.GetAllSettingValuesDocumentation();
            protected abstract CodeStyleOption2<bool> GetOption();
        }
    }
}
