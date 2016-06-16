// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace ProcessWatchdog
{
    internal static class ConsoleUtils
    {
        internal static void LogMessage(string format, params object[] args)
        {
            Console.WriteLine(
                string.Format(CultureInfo.CurrentCulture, format, args));
        }

        internal static void LogError(ErrorCode errorCode, string messageFormat, params object[] args)
        {
            string fullMessage = string.Format(
                CultureInfo.InvariantCulture,
                Resources.ErrorFormat,
                FormatErrorCode(errorCode),
                string.Format(CultureInfo.CurrentCulture, messageFormat, args));

            Console.Error.WriteLine(fullMessage);
        }

        private static string FormatErrorCode(ErrorCode errorCode)
        {
            return string.Format(CultureInfo.InvariantCulture, "PW{0:D4}", (int)errorCode);
        }
    }
}