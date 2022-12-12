// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract class CodeStyleSetting : Setting
    {
        private static readonly bool[] s_boolValues = new[] { true, false };

        public CodeStyleSetting(IOptionWithGroup option, OptionKey2 optionKey, string description, OptionUpdater updater, SettingLocation location)
            : base(option, optionKey, description, updater, location)
        {
        }

        public abstract ICodeStyleOption GetCodeStyle();

        public DiagnosticSeverity GetSeverity()
            => GetCodeStyle().Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden;

        public sealed override object? GetValue()
            => GetCodeStyle();

        public abstract string[] GetValueDescriptions();
        public abstract string GetCurrentValueDescription();

        protected abstract object GetPossibleValue(int valueIndex);

        public void ChangeSeverity(DiagnosticSeverity severity)
        {
            var notification = severity switch
            {
                DiagnosticSeverity.Hidden => NotificationOption2.Silent,
                DiagnosticSeverity.Info => NotificationOption2.Suggestion,
                DiagnosticSeverity.Warning => NotificationOption2.Warning,
                DiagnosticSeverity.Error => NotificationOption2.Error,
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

            return new CodeStyleSetting<bool>(option, optionKey, description, updater, initialLocation, initialValue, s_boolValues, valueDescriptions);
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

            return new CodeStyleSetting<bool>(option, optionKey, description, updater, initialLocation, initialValue, s_boolValues, valueDescriptions);
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
            return new CodeStyleSetting<T>(option, optionKey, description, updater, initialLocation, initialValue, enumValues, valueDescriptions);
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
            return new CodeStyleSetting<T>(option, optionKey, description, updater, initialLocation, initialValue, enumValues, valueDescriptions);
        }
    }
}
