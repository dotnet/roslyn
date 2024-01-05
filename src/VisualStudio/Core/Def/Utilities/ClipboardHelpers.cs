// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal static class ClipboardHelpers
    {
        private const ushort CF_TEXT = 1;            // winuser.h
        private const ushort CF_UNICODETEXT = 13;    // winuser.h

        private static readonly FORMATETC[] TextFormats =
        [
            CreateFormatEtc(CF_UNICODETEXT),
            CreateFormatEtc(CF_TEXT),
        ];

        #region Native Methods
        [DllImport("ole32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int OleGetClipboard(out IDataObject dataObject);

        [DllImport("ole32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern void ReleaseStgMedium(ref STGMEDIUM medium);

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GlobalLock(HandleRef handle);

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalUnlock(HandleRef handle);
        #endregion

        /// <summary>
        /// For cases where the clipboard data is retrieved in a performance critical path this 
        /// should be used to avoid the overhead that WPF/WinForms adds for the clipboard APIs.
        /// If performance is less of a concern (for example responding directly to a user paste
        /// command instead of VS activation) then it is recommended to use paste command handlers
        /// or the clipboard API directly. 
        /// </summary>
        public static string? GetTextNoRetry()
        {
            if (OleGetClipboard(out var dataObject) != VSConstants.S_OK || dataObject is null)
            {
                return null;
            }

            foreach (var format in TextFormats)
            {
                if (dataObject.QueryGetData([format]) == VSConstants.S_OK)
                {
                    return GetData(dataObject, format);
                }
            }

            return null;
        }

        // 
        // The rest of the methods are derived from the WPF implementation of getting
        // data from an OLE IDataObject. We use our own logic because there are built in 
        // mechanisms within WPF that result in retries on getting clipboard data which
        // is undesirable for paths that check the clipboard data in a hot path. 
        // See https://github.com/dotnet/wpf/blob/212f376fbca58bf2970964610c426ee05e633872/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/dataobject.cs
        // for information on how dataobject is used in WPF. This code only supports
        // a subset of the functionality for now because it is limited to text.
        //
        private static string? GetData(IDataObject dataObject, FORMATETC format)
        {
            var af = new FORMATETC[1];
            af[0] = format;
            var sm = new STGMEDIUM[1];
            dataObject.GetData(af, sm);
            var medium = sm[0];

            try
            {
                if (medium.tymed != 1) // TYMED_HGLOBAL
                {
                    return null;
                }

                return ReadStringFromHandle(medium.unionmember, format.cfFormat == CF_UNICODETEXT);
            }
            finally
            {
                ReleaseStgMedium(ref medium);
            }
        }

        private static FORMATETC CreateFormatEtc(ushort format)
            => new FORMATETC
            {
                cfFormat = format,
                ptd = IntPtr.Zero,
                dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = (uint)TYMED.TYMED_HGLOBAL
            };

        private static unsafe string? ReadStringFromHandle(IntPtr handle, bool unicode)
        {
            string? stringData = null;
            IntPtr ptr;
            object handleRefObj = new();

            ptr = Win32GlobalLock(new HandleRef(handleRefObj, handle));
            try
            {
                if (unicode)
                {
                    stringData = new string((char*)ptr);
                }
                else
                {
                    stringData = new string((sbyte*)ptr);
                }
            }
            finally
            {
                Win32GlobalUnlock(new HandleRef(handleRefObj, handle));
            }

            return stringData;
        }

        private static IntPtr Win32GlobalLock(HandleRef handle)
        {
            var win32Pointer = GlobalLock(handle);
            var win32Error = Marshal.GetLastWin32Error();
            if (win32Pointer == IntPtr.Zero)
            {
                throw new Win32Exception(win32Error);
            }

            return win32Pointer;
        }

        private static void Win32GlobalUnlock(HandleRef handle)
        {
            var win32Return = GlobalUnlock(handle);
            var win32Error = Marshal.GetLastWin32Error();
            if (!win32Return && win32Error != 0)
            {
                throw new Win32Exception(win32Error);
            }
        }
    }
}
