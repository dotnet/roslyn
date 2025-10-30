// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

internal abstract class CodeStyleSetting(OptionKey2 optionKey, string description, OptionUpdater updater, SettingLocation location) : Setting(optionKey, description, updater, location)
{
    private static readonly bool[] s_boolValues = [true, false];

    public abstract ICodeStyleOption2 GetCodeStyle();

    public ReportDiagnostic GetSeverity()
    {
        var severity = GetCodeStyle().Notification.Severity;
        if (severity is ReportDiagnostic.Default or ReportDiagnostic.Suppress)
            severity = ReportDiagnostic.Hidden;
        return severity;
    }

    public sealed override object? GetValue()
        => GetCodeStyle();

    public abstract string[] GetValueDescriptions();
    public abstract string GetCurrentValueDescription();

    protected abstract object GetPossibleValue(int valueIndex);

    public void ChangeSeverity(ReportDiagnostic severity)
    {
        var notification = severity switch
        {
            ReportDiagnostic.Hidden => NotificationOption2.Silent,
            ReportDiagnostic.Info => NotificationOption2.Suggestion,
            ReportDiagnostic.Warn => NotificationOption2.Warning,
            ReportDiagnostic.Error => NotificationOption2.Error,
            _ => NotificationOption2.None,
        };

        SetValue(GetCodeStyle().WithNotification(notification));
    }

    public void ChangeValue(int valueIndex)
    {
        SetValue(GetCodeStyle().WithValue(GetPossibleValue(valueIndex)));
    }

    internal static CodeStyleSetting Create(
        Option2<CodeStyleOption2<bool>> option,
        string description,
        TieredAnalyzerConfigOptions options,
        OptionUpdater updater,
        string? trueValueDescription = null,
        string? falseValueDescription = null)
    {
        var optionKey = new OptionKey2(option);
        options.GetInitialLocationAndValue<CodeStyleOption2<bool>>(option, out var initialLocation, out var initialValue);

        var valueDescriptions = new[]
        {
            trueValueDescription ?? EditorFeaturesResources.Yes,
            falseValueDescription ?? EditorFeaturesResources.No
        };

        return new CodeStyleSetting<bool>(optionKey, description, updater, initialLocation, initialValue, s_boolValues, valueDescriptions);
    }

    internal static CodeStyleSetting Create(
        PerLanguageOption2<CodeStyleOption2<bool>> option,
        string description,
        TieredAnalyzerConfigOptions options,
        OptionUpdater updater,
        string? trueValueDescription = null,
        string? falseValueDescription = null)
    {
        var optionKey = new OptionKey2(option, options.Language);
        options.GetInitialLocationAndValue<CodeStyleOption2<bool>>(option, out var initialLocation, out var initialValue);

        var valueDescriptions = new[]
        {
            trueValueDescription ?? EditorFeaturesResources.Yes,
            falseValueDescription ?? EditorFeaturesResources.No
        };

        return new CodeStyleSetting<bool>(optionKey, description, updater, initialLocation, initialValue, s_boolValues, valueDescriptions);
    }

    internal static CodeStyleSetting Create<T>(
        Option2<CodeStyleOption2<T>> option,
        string description,
        TieredAnalyzerConfigOptions options,
        OptionUpdater updater,
        T[] enumValues,
        string[] valueDescriptions)
        where T : Enum
    {
        var optionKey = new OptionKey2(option);
        options.GetInitialLocationAndValue<CodeStyleOption2<T>>(option, out var initialLocation, out var initialValue);
        return new CodeStyleSetting<T>(optionKey, description, updater, initialLocation, initialValue, enumValues, valueDescriptions);
    }

    internal static CodeStyleSetting Create<T>(
        PerLanguageOption2<CodeStyleOption2<T>> option,
        string description,
        TieredAnalyzerConfigOptions options,
        OptionUpdater updater,
        T[] enumValues,
        string[] valueDescriptions)
        where T : Enum
    {
        var optionKey = new OptionKey2(option, options.Language);
        options.GetInitialLocationAndValue<CodeStyleOption2<T>>(option, out var initialLocation, out var initialValue);
        return new CodeStyleSetting<T>(optionKey, description, updater, initialLocation, initialValue, enumValues, valueDescriptions);
    }
}
