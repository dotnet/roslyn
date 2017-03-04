// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class CodeStyleHelpers
    {
        public static CodeStyleOption<bool> ParseEditorConfigCodeStyleOption(string arg)
        {
            TryParseEditorConfigCodeStyleOption(arg, out var option);
            return option;
        }

        public static bool TryParseEditorConfigCodeStyleOption(string arg, out CodeStyleOption<bool> option)
        {
            var args = arg.Split(':');
            var isEnabled = false;
            if (args.Length != 2)
            {
                if (args.Length == 1)
                {
                    if (bool.TryParse(args[0].Trim(), out isEnabled) && !isEnabled)
                    {
                        option = new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.None);
                        return true;
                    }
                    else
                    {
                        option = CodeStyleOption<bool>.Default;
                        return false;
                    }
                }
                option = CodeStyleOption<bool>.Default;
                return false;
            }

            if (!bool.TryParse(args[0].Trim(), out isEnabled))
            {
                option = CodeStyleOption<bool>.Default;
                return false;
            }

            switch (args[1].Trim())
            {
                case EditorConfigSeverityStrings.Silent: option = new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.None); return true;
                case EditorConfigSeverityStrings.Suggestion: option = new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.Suggestion); return true;
                case EditorConfigSeverityStrings.Warning: option = new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.Warning); return true;
                case EditorConfigSeverityStrings.Error: option = new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.Error); return true;
                default: option = new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.None); return false;
            }
        }
    }
}
