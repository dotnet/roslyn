// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.EditorConfigSettings;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class CodeStyleHelpers
    {
        public static bool TryParseStringEditorConfigCodeStyleOption(string arg, CodeStyleOption2<string> defaultValue, [NotNullWhen(true)] out CodeStyleOption2<string>? option)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(
                    arg, defaultValue.Notification, out var value, out var notification))
            {
                option = new CodeStyleOption2<string>(value, notification);
                return true;
            }

            option = null;
            return false;
        }

        public static bool TryParseBoolEditorConfigCodeStyleOption(string arg, CodeStyleOption2<bool> defaultValue, out CodeStyleOption2<bool> option, EditorConfigData<bool> editorConfigData)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(
                    arg, defaultValue.Notification, out var value, out var notification))
            {
                // First value has to be true or false.  Anything else is unsupported.
                var result = editorConfigData.GetValueFromEditorConfigString(value);
                if (result.HasValue)
                {
                    option = new CodeStyleOption2<bool>(result.Value, notification);
                    return true;
                }
            }

            option = defaultValue;
            return false;
        }

        /// <summary>
        /// Given an editor-config code-style-option, gives back the core value part of the 
        /// option.  For example, if the option is "true:error" or "true" then "true" will be returned
        /// in <paramref name="value"/>.
        /// </summary>
        public static bool TryGetCodeStyleValue(
            string arg, [NotNullWhen(true)] out string? value)
            => TryGetCodeStyleValueAndOptionalNotification(arg, defaultNotification: NotificationOption2.None, out value, out _);

        /// <summary>
        /// Given an editor-config code-style-option, gives back the constituent parts of the 
        /// option.  For example, if the option is "true:error" then "true" will be returned
        /// in <paramref name="value"/> and <see cref="NotificationOption2.Error"/> will be returned
        /// in <paramref name="notification"/>.  Note that users are allowed to not provide
        /// a NotificationOption, so <paramref name="notification"/> will default to <paramref name="defaultNotification"/>.
        /// </summary>
        public static bool TryGetCodeStyleValueAndOptionalNotification(
            string arg, NotificationOption2 defaultNotification, [NotNullWhen(true)] out string? value, [NotNullWhen(true)] out NotificationOption2 notification)
        {
            var args = arg.Split(':');
            Debug.Assert(args.Length > 0);

            // We allow a single value to be provided without an explicit notification.
            if (args.Length == 1)
            {
                value = args[0].Trim();
                notification = defaultNotification;
                return true;
            }

            if (args.Length == 2)
            {
                // If we have two args, then the second must be a notification option.  If 
                // it isn't, then this isn't a valid code style option at all.
                if (TryParseNotification(args[1], out var localNotification))
                {
                    value = args[0].Trim();
                    notification = localNotification;
                    return true;
                }
            }

            // We only support 0 or 1 args.  Anything else can't be parsed properly.
            value = null;
            notification = default;
            return false;
        }

        private static bool TryParseNotification(string value, out NotificationOption2 notification)
        {
            switch (value.Trim())
            {
                case EditorConfigSeverityStrings.None:
                    notification = NotificationOption2.None;
                    return true;

                case EditorConfigSeverityStrings.Refactoring:
                case EditorConfigSeverityStrings.Silent:
                    notification = NotificationOption2.Silent;
                    return true;

                case EditorConfigSeverityStrings.Suggestion: notification = NotificationOption2.Suggestion; return true;
                case EditorConfigSeverityStrings.Warning: notification = NotificationOption2.Warning; return true;
                case EditorConfigSeverityStrings.Error: notification = NotificationOption2.Error; return true;
            }

            notification = NotificationOption2.Silent;
            return false;
        }

        public static Option2<T> CreateOption<T>(
            OptionGroup group,
            string feature,
            string name,
            T defaultValue,
            ImmutableArray<IOption2>.Builder optionsBuilder,
            OptionStorageLocation2 storageLocation,
            string languageName)
        {
            var option = new Option2<T>(feature, group, name, defaultValue, ImmutableArray.Create(storageLocation), languageName);
            optionsBuilder.Add(option);
            return option;
        }

        public static Option2<T> CreateOption<T>(
            OptionGroup group,
            string feature,
            string name,
            T defaultValue,
            ImmutableArray<IOption2>.Builder optionsBuilder,
            OptionStorageLocation2 storageLocation1,
            OptionStorageLocation2 storageLocation2,
            string languageName)
        {
            var option = new Option2<T>(feature, group, name, defaultValue, ImmutableArray.Create(storageLocation1, storageLocation2), languageName);
            optionsBuilder.Add(option);
            return option;
        }

        private static readonly CodeStyleOption2<UnusedValuePreference> s_preferNoneUnusedValuePreference =
            new(default, NotificationOption2.None);

        public static Option2<CodeStyleOption2<UnusedValuePreference>> CreateUnusedExpressionAssignmentOption(
            OptionGroup group,
            string feature,
            string name,
            EditorConfigData<UnusedValuePreference> editorConfigData,
            CodeStyleOption2<UnusedValuePreference> defaultValue,
            ImmutableArray<IOption2>.Builder optionsBuilder,
            string languageName)
            => CreateOption(
                group,
                feature,
                name,
                defaultValue,
                optionsBuilder,
                new EditorConfigStorageLocation<CodeStyleOption2<UnusedValuePreference>>(
                    editorConfigData.GetSettingName(),
                    s => ParseUnusedExpressionAssignmentPreference(s, defaultValue, editorConfigData),
                    o => GetUnusedExpressionAssignmentPreferenceEditorConfigString(o, defaultValue, editorConfigData)),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{name}Preference"),
                languageName);

        private static Optional<CodeStyleOption2<UnusedValuePreference>> ParseUnusedExpressionAssignmentPreference(
            string optionString,
            CodeStyleOption2<UnusedValuePreference> defaultCodeStyleOption,
            EditorConfigData<UnusedValuePreference> editorConfigData)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                    defaultCodeStyleOption.Notification, out var value, out var notification))
            {
                Debug.Assert(editorConfigData.GetValueFromEditorConfigString(value).HasValue);
                return new CodeStyleOption2<UnusedValuePreference>(editorConfigData.GetValueFromEditorConfigString(value).Value, notification);
            }

            return s_preferNoneUnusedValuePreference;
        }

        private static string GetUnusedExpressionAssignmentPreferenceEditorConfigString(CodeStyleOption2<UnusedValuePreference> option, CodeStyleOption2<UnusedValuePreference> defaultValue, EditorConfigData<UnusedValuePreference> editorConfigData)
        {
            var editorConfigString = editorConfigData.GetEditorConfigStringFromValue(option.Value);
            Debug.Assert(editorConfigString != "");
            var value = editorConfigString == "" ? editorConfigData.GetEditorConfigStringFromValue(defaultValue.Value) : editorConfigString;
            return $"{value}{GetEditorConfigStringNotificationPart(option, defaultValue)}";
        }

        internal static string GetEditorConfigStringNotificationPart<T>(CodeStyleOption2<T> option, CodeStyleOption2<T> defaultValue)
            => option.Notification != defaultValue.Notification
                ? $":{option.Notification.ToEditorConfigString()}"
                : string.Empty;
    }
}
