// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.ViewModel;

internal sealed class NamingStylesStyleViewModel : NotifyPropertyChangedBase
{
    private readonly NamingStyleSetting _setting;
    private string _selectedStyleValue;
    private string[] _styleValues;

    public NamingStylesStyleViewModel(NamingStyleSetting setting)
    {
        _setting = setting;
        _setting.SettingChanged += OnSettingChanged;
        var selectedStyleIndex = Array.IndexOf(_setting.AllStyles, _setting.StyleName);
        _styleValues = _setting.AllStyles;
        _selectedStyleValue = _styleValues[selectedStyleIndex];
    }

    public static string StyleToolTip => ServicesVSResources.Naming_Style;

    public static string StyleAutomationName => ServicesVSResources.Naming_Style;

    public string[] StyleValues
    {
        get => _styleValues;
        set
        {
            if (value is not null && !_styleValues.SequenceEqual(value))
            {
                _styleValues = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(StyleValues)));
            }
        }
    }

    public string SelectedStyleValue
    {
        get => _selectedStyleValue;
        set
        {
            if (value is not null && _selectedStyleValue != value)
            {
                _selectedStyleValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(SelectedStyleValue)));
            }
        }
    }

    internal void SelectionChanged(int selectedIndex)
        => _setting.ChangeStyle(selectedIndex);

    private void OnSettingChanged(object sender, EventArgs e)
    {
        var selectedStyleIndex = Array.IndexOf(_setting.AllStyles, _setting.StyleName);
        StyleValues = _setting.AllStyles;
        SelectedStyleValue = StyleValues[selectedStyleIndex];
    }
}
