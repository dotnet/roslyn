// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace ProcessWatchdog
{
    internal static class ConsoleUtils
    {
        internal static void ReportError(string messageFormat, params string[] args)
        {
            string fullMessage = string.Format(
                CultureInfo.InvariantCulture,
                Resources.ErrorFormat,
                string.Format(CultureInfo.InvariantCulture, messageFormat, args));

            Console.Error.WriteLine(fullMessage);
        }
    }
}