// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.ViewModel;

internal sealed class NamingStylesLocationViewModel : NotifyPropertyChangedBase
{
    private readonly NamingStyleSetting _setting;
    private string _locationValue;

    public NamingStylesLocationViewModel(NamingStyleSetting setting)
    {
        _setting = setting;
        _setting.SettingChanged += OnSettingChanged;
        _locationValue = GetLocationString(_setting.Location);
    }

    public string LocationValue
    {
        get => _locationValue;
        set
        {
            if (value is not null && _locationValue != value)
            {
                _locationValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(LocationValue)));
            }
        }
    }

    public static string LocationToolTip { get; } = ServicesVSResources.Location;
    public static string LocationAutomationName { get; } = ServicesVSResources.Location;

    private void OnSettingChanged(object sender, System.EventArgs e)
        => LocationValue = GetLocationString(_setting.Location);

    private static string GetLocationString(SettingLocation? location)
        => location?.LocationKind switch
        {
            LocationKind.EditorConfig or LocationKind.GlobalConfig => location.Path!,
            _ => ServicesVSResources.Visual_Studio_Settings,
        };
}
