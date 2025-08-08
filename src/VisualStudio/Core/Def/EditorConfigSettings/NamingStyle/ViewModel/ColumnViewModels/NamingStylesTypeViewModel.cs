// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.ViewModel;

internal sealed class NamingStylesTypeViewModel : NotifyPropertyChangedBase
{
    private readonly NamingStyleSetting _setting;
    private string _typeValue;

    public NamingStylesTypeViewModel(NamingStyleSetting setting)
    {
        _setting = setting;
        _setting.SettingChanged += OnSettingChanged;
        _typeValue = _setting.TypeName;
    }

    public string TypeValue
    {
        get => _typeValue;
        set
        {
            if (value is not null && _typeValue != value)
            {
                _typeValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(TypeValue)));
            }
        }
    }

    public static string TypeToolTip { get; } = ServicesVSResources.Type;
    public static string TypeAutomationName { get; } = ServicesVSResources.Type;

    private void OnSettingChanged(object sender, System.EventArgs e)
        => TypeValue = _setting.TypeName;
}
