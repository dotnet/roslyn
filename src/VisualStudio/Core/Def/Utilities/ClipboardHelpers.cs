// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal static class ClipboardHelpers
    {
        [DllImport("ole32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int OleGetClipboard(out IDataObject dataObject);

        public static string? GetText()
        {
            IDataObject? dataObject = null;
            var result = OleGetClipboard(out dataObject);
            if (result != 0) // S_OK
            {
                // Report NFW?
            }

            if (dataObject is null || !dataObject.GetDataPresent(typeof(string)))
            {
                return null;
            }

            return (string)dataObject.GetData(typeof(string));
        }
    }
}
