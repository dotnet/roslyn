// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract partial class CodeStyleSetting
    {
        private class EnumCodeStyleSetting<T> : EnumCodeStyleSettingBase<T>
            where T : Enum
        {
            private readonly Option2<CodeStyleOption2<T>> _option;
            private readonly AnalyzerConfigOptions _editorConfigOptions;
            private readonly OptionSet _visualStudioOptions;

            public EnumCodeStyleSetting(Option2<CodeStyleOption2<T>> option,
                                        string description,
                                        T[] enumValues,
                                        string[] valueDescriptions,
                                        AnalyzerConfigOptions editorConfigOptions,
                                        OptionSet visualStudioOptions,
                                        OptionUpdater updater,
                                        string fileName)
                : base(description, enumValues, valueDescriptions, option.Group.Description, updater)
            {
                _option = option;
                _editorConfigOptions = editorConfigOptions;
                _visualStudioOptions = visualStudioOptions;
                Location = new SettingLocation(IsDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            }

            public override bool IsDefinedInEditorConfig => _editorConfigOptions.TryGetEditorConfigOption<CodeStyleOption2<T>>(_option, out _);

            public override SettingLocation Location { get; protected set; }

            protected override void ChangeSeverity(NotificationOption2 severity)
            {
                ICodeStyleOption option = GetOption();
                Location = Location with { LocationKind = LocationKind.EditorConfig };
                Updater.QueueUpdate(_option, option.WithNotification(severity));
            }

            public override void ChangeValue(int valueIndex)
            {
                ICodeStyleOption option = GetOption();
                Location = Location with { LocationKind = LocationKind.EditorConfig };
                Updater.QueueUpdate(_option, option.WithValue(_enumValues[valueIndex]));
            }

            protected override CodeStyleOption2<T> GetOption()
                => _editorConfigOptions.TryGetEditorConfigOption(_option, out CodeStyleOption2<T>? value) && value is not null
                    ? value
                    : _visualStudioOptions.GetOption(_option);
        }
    }
}
