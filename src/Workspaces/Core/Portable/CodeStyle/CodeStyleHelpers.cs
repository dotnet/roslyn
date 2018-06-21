// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class CodeStyleHelpers
    {
        public static bool TryParseStringEditorConfigCodeStyleOption(string arg, out CodeStyleOption<string> option)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(
                    arg, out string value, out NotificationOption notificationOpt))
            {
                option = new CodeStyleOption<string>(value, notificationOpt ?? NotificationOption.Silent);
                return true;
            }

            option = null;
            return false;
        }

        public static bool TryParseBoolEditorConfigCodeStyleOption(string arg, out CodeStyleOption<bool> option)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(
                    arg, out string value, out NotificationOption notificationOpt))
            {
                // First value has to be true or false.  Anything else is unsupported.
                if (bool.TryParse(value.Trim(), out var isEnabled))
                {
                    // We allow 'false' to be provided without a notification option.  However,
                    // 'true' must always be provided with a notification option.
                    if (isEnabled == false)
                    {
                        notificationOpt = notificationOpt ?? NotificationOption.Silent;
                        option = new CodeStyleOption<bool>(false, notificationOpt);
                        return true;
                    }
                    else if (notificationOpt != null)
                    {
                        option = new CodeStyleOption<bool>(true, notificationOpt);
                        return true;
                    }
                }
            }

            option = CodeStyleOption<bool>.Default;
            return false;
        }

        /// <summary>
        /// Given an editor-config code-style-option, gives back the constituent parts of the 
        /// option.  For example, if the option is "true:error" then "true" will be returned
        /// in <paramref name="value"/> and <see cref="NotificationOption.Error"/> will be returned
        /// in <paramref name="notificationOpt"/>.  Note that users are allowed to not provide
        /// a NotificationOption, so <paramref name="notificationOpt"/> may be null.  The caller
        /// of this method must decide what to do in that case.
        /// </summary>
        public static bool TryGetCodeStyleValueAndOptionalNotification(
            string arg, out string value, out NotificationOption notificationOpt)
        {
            var args = arg.Split(':');
            Debug.Assert(args.Length > 0);

            // We allow a single value to be provided in some cases.  For example users 
            // can provide 'false' without supplying a notification as well.  Allow the 
            // caller of this to determine what to do in this case.
            if (args.Length == 1)
            {
                value = args[0];
                notificationOpt = null;
                return true;
            }

            if (args.Length == 2)
            {
                // If we have two args, then the second must be a notification option.  If 
                // it isn't, then this isn't a valid code style option at all.
                if (TryParseNotification(args[1], out var localNotification))
                {
                    value = args[0];
                    notificationOpt = localNotification;
                    return true;
                }
            }

            // We only support 0 or 1 args.  Anything else can't be parsed properly.
            value = null;
            notificationOpt = null;
            return false;
        }

        public static bool TryParseNotification(string value, out NotificationOption notification)
        {
            switch (value.Trim())
            {
                case EditorConfigSeverityStrings.None:
                    notification = NotificationOption.None;
                    return true;

                case EditorConfigSeverityStrings.Silent:
                    notification = NotificationOption.Silent;
                    return true;

                case EditorConfigSeverityStrings.Suggestion: notification = NotificationOption.Suggestion; return true;
                case EditorConfigSeverityStrings.Warning: notification = NotificationOption.Warning; return true;
                case EditorConfigSeverityStrings.Error: notification = NotificationOption.Error; return true;
            }

            notification = NotificationOption.Silent;
            return false;
        }
    }
}
