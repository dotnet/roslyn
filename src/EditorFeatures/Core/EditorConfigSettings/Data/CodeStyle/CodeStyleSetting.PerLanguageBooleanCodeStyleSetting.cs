// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract partial class CodeStyleSetting
    {
        private class PerLanguageBooleanCodeStyleSetting : BooleanCodeStyleSettingBase
        {
            private readonly PerLanguageOption2<CodeStyleOption2<bool>> _option;
            private readonly AnalyzerConfigOptions _editorConfigOptions;
            private readonly OptionSet _visualStudioOptions;

            public PerLanguageBooleanCodeStyleSetting(PerLanguageOption2<CodeStyleOption2<bool>> option,
                                                      string description,
                                                      string? trueValueDescription,
                                                      string? falseValueDescription,
                                                      AnalyzerConfigOptions editorConfigOptions,
                                                      OptionSet visualStudioOptions,
                                                      OptionUpdater updater,
                                                      string fileName)
                : base(description, option.Group.Description, trueValueDescription, falseValueDescription, updater)
            {
                _option = option;
                _editorConfigOptions = editorConfigOptions;
                _visualStudioOptions = visualStudioOptions;
                Location = new SettingLocation(IsDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            }

            public override bool IsDefinedInEditorConfig => _editorConfigOptions.TryGetEditorConfigOption<CodeStyleOption2<bool>>(_option, out _);

            public override SettingLocation Location { get; protected set; }

            protected override void ChangeSeverity(NotificationOption2 severity)
            {
                ICodeStyleOption option = GetOption();
                Location = Location with { LocationKind = LocationKind.EditorConfig };
                Updater.QueueUpdate(_option, option.WithNotification(severity));
            }

            public override void ChangeValue(int valueIndex)
            {
                var value = valueIndex == 0;
                ICodeStyleOption option = GetOption();
                Location = Location with { LocationKind = LocationKind.EditorConfig };
                Updater.QueueUpdate(_option, option.WithValue(value));
            }

            protected override CodeStyleOption2<bool> GetOption()
                => _editorConfigOptions.TryGetEditorConfigOption(_option, out CodeStyleOption2<bool>? value) && value is not null
                    ? value
                    : _visualStudioOptions.GetOption<CodeStyleOption2<bool>>(new OptionKey2(_option, LanguageNames.CSharp));
        }
    }
}
