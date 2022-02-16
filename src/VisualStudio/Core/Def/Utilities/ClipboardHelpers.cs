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
        [DllImport("user32.dll")]
        [PreserveSig]
        private static extern int GetPriorityClipboardFormat([In] uint[] formatPriorityList, [In] int formatCount);

        /// <summary>
        /// Determines if clipboard data exists for the text format without blocking
        /// on <see cref="Clipboard.GetText()"/> or <see cref="Clipboard.GetDataObject()"/> for cases where the clipboard is being
        /// delay rendered. See https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1463413/ 
        /// </summary>
        public static bool CanGetText()
        {
            const uint TextFormatId = 1;          // ID for clipboard text format (CF_TEXT)

            var formats = new uint[] { TextFormatId };

            // GetPriorityClipboardFormat returns 0 if the clipboard is empty, -1 if there is data in the clipboard but it does not match
            // any of the input formats and otherwise returns the ID of the format available in the clipboard.
            return GetPriorityClipboardFormat(formats, formats.Length) > 0;
        }

        /// <summary>
        /// Gets the text from the clipboard. Check <see cref="CanGetText"/>
        /// first before calling this to verify text can be safely retrieved
        /// without a deadlock
        /// </summary>
        public static string? GetText()
        {
            Debug.Assert(CanGetText());

            var dataObject = Clipboard.GetDataObject();

            if (dataObject is not null && dataObject.GetDataPresent(DataFormats.Text))
            {
                return (string)dataObject.GetData(DataFormats.Text, true);
            }

            return null;
        }
    }
}
