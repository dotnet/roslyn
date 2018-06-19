
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Roslyn.Test.Utilities
{
    public static class CultureHelpers
    {
        public static readonly CultureInfo EnglishCulture = new CultureInfo("en");

        [DllImport("kernel32.dll", CharSet= CharSet.Unicode, SetLastError=true, BestFitMapping=true)]            
        public static unsafe extern int FormatMessage(int dwFlags, IntPtr lpSource_mustBeNull, uint dwMessageId,
            int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr[] arguments);
    }
}
