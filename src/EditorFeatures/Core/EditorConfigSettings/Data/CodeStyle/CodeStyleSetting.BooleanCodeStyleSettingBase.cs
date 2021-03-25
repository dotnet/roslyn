// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract partial class CodeStyleSetting
    {
        private abstract class BooleanCodeStyleSettingBase : CodeStyleSetting
        {
            private readonly string _trueValueDescription;
            private readonly string _falseValueDescription;

            public BooleanCodeStyleSettingBase(string description,
                                               string category,
                                               string? trueValueDescription,
                                               string? falseValueDescription,
                                               OptionUpdater updater)
                : base(description, updater)
            {
                Category = category;
                _trueValueDescription = trueValueDescription ?? EditorFeaturesResources.Yes;
                _falseValueDescription = falseValueDescription ?? EditorFeaturesResources.No;
            }

            public override string Category { get; }
            public override Type Type => typeof(bool);
            public override DiagnosticSeverity Severity => GetOption().Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden;
            public override string GetCurrentValue() => GetOption().Value ? _trueValueDescription : _falseValueDescription;
            public override object? Value => GetOption().Value;
            public override string[] GetValues() => new[] { _trueValueDescription, _falseValueDescription };
            protected abstract CodeStyleOption2<bool> GetOption();
        }
    }
}
