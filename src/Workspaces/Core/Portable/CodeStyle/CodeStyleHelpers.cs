namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class CodeStyleHelpers
    {
        public static CodeStyleOption<bool> ParseEditorConfigCodeStyleOption(string arg)
        {
            var args = arg.Split(':');
            if (args.Length != 2)
            {
                return CodeStyleOption<bool>.Default;
            }

            bool isEnabled = false;
            if (!bool.TryParse(args[0], out isEnabled))
            {
                return CodeStyleOption<bool>.Default;
            }

            switch (args[1].Trim())
            {
                case "none": return new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.None);
                case "suggestion": return new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.Suggestion);
                case "warning": return new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.Warning);
                case "error": return new CodeStyleOption<bool>(value: isEnabled, notification: NotificationOption.Error);
                default: return CodeStyleOption<bool>.Default;
            }
        }
    }
}
