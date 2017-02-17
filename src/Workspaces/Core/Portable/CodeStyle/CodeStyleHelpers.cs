// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class CodeStyleHelpers
    {
        public static CodeStyleOption<bool> ParseEditorConfigCodeStyleOption(string arg)
        {
            var args = arg.Split(':');
            bool isEnabled = false;
            if (args.Length != 2)
            {
                if (args.Length == 1)
                {
                    if (bool.TryParse(args[0].Trim(), out isEnabled) && !isEnabled)
                    {
                        return new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.None);
                    }
                    else
                    {
                        return CodeStyleOption<bool>.Default;
                    }
                }
                return CodeStyleOption<bool>.Default;
            }

            if (!bool.TryParse(args[0].Trim(), out isEnabled))
            {
                return CodeStyleOption<bool>.Default;
            }

            switch (args[1].Trim())
            {
                case EditorConfigSeverityStrings.Silent: return new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.None);
                case EditorConfigSeverityStrings.Suggestion: return new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.Suggestion);
                case EditorConfigSeverityStrings.Warning: return new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.Warning);
                case EditorConfigSeverityStrings.Error: return new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.Error);
                default: return new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.None);
            }
        }
    }
}
