// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.View;

internal class CodeStyleValueViewModel
{
    private readonly CodeStyleSetting _setting;

    private string? _selectedValue;

    public string[] Values => _setting.GetValueDescriptions();

    public string SelectedValue
    {
        get
        {
            _selectedValue ??= _setting.GetCurrentValueDescription();

            return _selectedValue;
        }
        set => _selectedValue = value;
    }

    public string ToolTip => ServicesVSResources.Value;

    public static string AutomationName => ServicesVSResources.Value;

    public CodeStyleValueViewModel(CodeStyleSetting setting)
        => _setting = setting;

    public void SelectionChanged(int selectedIndex)
        => _setting.ChangeValue(selectedIndex);
}
