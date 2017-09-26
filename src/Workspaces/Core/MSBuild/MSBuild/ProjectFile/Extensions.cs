using System;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal static class Extensions
    {
        public static string ReadPropertyString(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => executedProject.GetProperty(propertyName)?.EvaluatedValue;

        public static bool ReadPropertyBool(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => ConvertToBool(executedProject.ReadPropertyString(propertyName));

        private static bool ConvertToBool(string value)
            => value != null
                && (string.Equals("true", value, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals("On", value, StringComparison.OrdinalIgnoreCase));

        public static int ReadPropertyInt(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => ConvertToInt(executedProject.ReadPropertyString(propertyName));

        private static int ConvertToInt(string value)
        {
            if (value == null)
            {
                return 0;
            }
            else
            {
                int.TryParse(value, out var result);
                return result;
            }
        }

        public static ulong ReadPropertyULong(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => ConvertToULong(executedProject.ReadPropertyString(propertyName));

        private static ulong ConvertToULong(string value)
        {
            if (value == null)
            {
                return 0;
            }
            else
            {
                ulong.TryParse(value, out var result);
                return result;
            }
        }

        public static TEnum? ReadPropertyEnum<TEnum>(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            where TEnum : struct
            => ConvertToEnum<TEnum>(executedProject.ReadPropertyString(propertyName));

        private static TEnum? ConvertToEnum<TEnum>(string value)
            where TEnum : struct
        {
            if (value == null)
            {
                return null;
            }
            else
            {
                if (Enum.TryParse<TEnum>(value, out var result))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

    }
}
