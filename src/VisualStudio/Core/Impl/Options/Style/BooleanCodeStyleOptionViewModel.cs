// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

/// <summary>
/// This class represents the view model for a <see cref="CodeStyleOption2{T}"/>
/// that binds to the codestyle options UI.
/// </summary>
internal sealed class BooleanCodeStyleOptionViewModel : AbstractCodeStyleOptionViewModel
{
    private readonly string _truePreview;
    private readonly string _falsePreview;

    private CodeStylePreference _selectedPreference;
    private NotificationOptionViewModel _selectedNotificationPreference;

    public BooleanCodeStyleOptionViewModel(
        IOption2 option,
        string description,
        string truePreview,
        string falsePreview,
        AbstractOptionPreviewViewModel info,
        OptionStore optionStore,
        string groupName,
        List<CodeStylePreference> preferences = null,
        List<NotificationOptionViewModel> notificationPreferences = null)
        : base(option, description, info, groupName, preferences, notificationPreferences)
    {
        _truePreview = truePreview;
        _falsePreview = falsePreview;

        var codeStyleOption = optionStore.GetOption<CodeStyleOption2<bool>>(option, option.IsPerLanguage ? info.Language : null);
        _selectedPreference = Preferences.Single(c => c.IsChecked == codeStyleOption.Value);

        var notificationViewModel = NotificationPreferences.Single(i => i.Notification.Severity == codeStyleOption.Notification.Severity);
        _selectedNotificationPreference = NotificationPreferences.Single(p => p.Notification.Severity == notificationViewModel.Notification.Severity);

        NotifyPropertyChanged(nameof(SelectedPreference));
        NotifyPropertyChanged(nameof(SelectedNotificationPreference));
    }

    public override CodeStylePreference SelectedPreference
    {
        get => _selectedPreference;

        set
        {
            if (SetProperty(ref _selectedPreference, value))
            {
                Info.SetOptionAndUpdatePreview(new CodeStyleOption2<bool>(_selectedPreference.IsChecked, _selectedNotificationPreference.Notification), Option, GetPreview());
            }
        }
    }

    public override NotificationOptionViewModel SelectedNotificationPreference
    {
        get => _selectedNotificationPreference;

        set
        {
            if (SetProperty(ref _selectedNotificationPreference, value))
            {
                Info.SetOptionAndUpdatePreview(new CodeStyleOption2<bool>(_selectedPreference.IsChecked, _selectedNotificationPreference.Notification), Option, GetPreview());
            }
        }
    }

    public override string GetPreview()
        => SelectedPreference.IsChecked ? _truePreview : _falsePreview;
}
