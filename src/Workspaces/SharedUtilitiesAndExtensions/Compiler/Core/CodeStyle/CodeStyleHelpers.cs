// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class CodeStyleHelpers
    {
        public static bool TryParseStringEditorConfigCodeStyleOption(string arg, out CodeStyleOption2<string> option)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(
                    arg, out var value, out var notificationOpt))
            {
                option = new CodeStyleOption2<string>(value, notificationOpt ?? NotificationOption2.Silent);
                return true;
            }

            option = null;
            return false;
        }

        public static bool TryParseBoolEditorConfigCodeStyleOption(string arg, out CodeStyleOption2<bool> option)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(
                    arg, out var value, out var notificationOpt))
            {
                // First value has to be true or false.  Anything else is unsupported.
                if (bool.TryParse(value, out var isEnabled))
                {
                    // We allow 'false' to be provided without a notification option.  However,
                    // 'true' must always be provided with a notification option.
                    if (isEnabled == false)
                    {
                        notificationOpt ??= NotificationOption2.Silent;
                        option = new CodeStyleOption2<bool>(false, notificationOpt);
                        return true;
                    }
                    else if (notificationOpt != null)
                    {
                        option = new CodeStyleOption2<bool>(true, notificationOpt);
                        return true;
                    }
                }
            }

            option = CodeStyleOption2<bool>.Default;
            return false;
        }

        /// <summary>
        /// Given an editor-config code-style-option, gives back the constituent parts of the 
        /// option.  For example, if the option is "true:error" then "true" will be returned
        /// in <paramref name="value"/> and <see cref="NotificationOption2.Error"/> will be returned
        /// in <paramref name="notificationOpt"/>.  Note that users are allowed to not provide
        /// a NotificationOption, so <paramref name="notificationOpt"/> may be null.  The caller
        /// of this method must decide what to do in that case.
        /// </summary>
        public static bool TryGetCodeStyleValueAndOptionalNotification(
            string arg, out string value, out NotificationOption2 notificationOpt)
        {
            var args = arg.Split(':');
            Debug.Assert(args.Length > 0);

            // We allow a single value to be provided in some cases.  For example users 
            // can provide 'false' without supplying a notification as well.  Allow the 
            // caller of this to determine what to do in this case.
            if (args.Length == 1)
            {
                value = args[0].Trim();
                notificationOpt = null;
                return true;
            }

            if (args.Length == 2)
            {
                // If we have two args, then the second must be a notification option.  If 
                // it isn't, then this isn't a valid code style option at all.
                if (TryParseNotification(args[1], out var localNotification))
                {
                    value = args[0].Trim();
                    notificationOpt = localNotification;
                    return true;
                }
            }

            // We only support 0 or 1 args.  Anything else can't be parsed properly.
            value = null;
            notificationOpt = null;
            return false;
        }

        public static bool TryParseNotification(string value, out NotificationOption2 notification)
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
            params OptionStorageLocation2[] storageLocations)
        {
            var option = new Option2<T>(feature, group, name, defaultValue, storageLocations);
            optionsBuilder.Add(option);
            return option;
        }

        private static readonly CodeStyleOption2<UnusedValuePreference> s_preferNoneUnusedValuePreference =
            new CodeStyleOption2<UnusedValuePreference>(default, NotificationOption2.None);

        private static readonly BidirectionalMap<string, UnusedValuePreference> s_unusedExpressionAssignmentPreferenceMap =
            new BidirectionalMap<string, UnusedValuePreference>(new[]
            {
                KeyValuePairUtil.Create("discard_variable", UnusedValuePreference.DiscardVariable),
                KeyValuePairUtil.Create("unused_local_variable", UnusedValuePreference.UnusedLocalVariable),
            });

        public static Option2<CodeStyleOption2<UnusedValuePreference>> CreateUnusedExpressionAssignmentOption(
            OptionGroup group,
            string feature,
            string name,
            string editorConfigName,
            CodeStyleOption2<UnusedValuePreference> defaultValue,
            ImmutableArray<IOption2>.Builder optionsBuilder)
            => CreateOption(
                group,
                feature,
                name,
                defaultValue,
                optionsBuilder,
                storageLocations: new OptionStorageLocation2[]{
                    new EditorConfigStorageLocation<CodeStyleOption2<UnusedValuePreference>>(
                        editorConfigName,
                        s => ParseUnusedExpressionAssignmentPreference(s, defaultValue),
                        o => GetUnusedExpressionAssignmentPreferenceEditorConfigString(o, defaultValue.Value)),
                    new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{name}Preference")});

        private static Optional<CodeStyleOption2<UnusedValuePreference>> ParseUnusedExpressionAssignmentPreference(
            string optionString,
            CodeStyleOption2<UnusedValuePreference> defaultCodeStyleOption)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                out var value, out var notificationOpt))
            {
                return new CodeStyleOption2<UnusedValuePreference>(
                    s_unusedExpressionAssignmentPreferenceMap.GetValueOrDefault(value), notificationOpt ?? defaultCodeStyleOption.Notification);
            }

            return s_preferNoneUnusedValuePreference;
        }

        private static string GetUnusedExpressionAssignmentPreferenceEditorConfigString(CodeStyleOption2<UnusedValuePreference> option, UnusedValuePreference defaultPreference)
        {
            Debug.Assert(s_unusedExpressionAssignmentPreferenceMap.ContainsValue(option.Value));
            var value = s_unusedExpressionAssignmentPreferenceMap.GetKeyOrDefault(option.Value) ?? s_unusedExpressionAssignmentPreferenceMap.GetKeyOrDefault(defaultPreference);
            return $"{value}:{option.Notification.ToEditorConfigString()}";
        }
    }
}
