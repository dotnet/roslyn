// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.ViewModel
{
    internal class WhitespaceSettingBoolViewModel
    {
        private readonly Setting _setting;
        private bool _isChecked;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                if (_setting.GetValue() is bool currentValue && value != currentValue)
                {
                    _setting.SetValue(value);
                }
            }
        }

        public string ToolTip => ServicesVSResources.Value;

        public string AutomationName => ServicesVSResources.Value;

        public WhitespaceSettingBoolViewModel(Setting setting)
        {
            if (setting.GetValue() is bool value)
            {
                _isChecked = value;
            }

            _setting = setting;
        }
    }
}
