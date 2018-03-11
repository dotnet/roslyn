// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    /// <summary>
    /// This allows us to get pNode as an IntPtr instead of a via a RCW. Otherwise, a second 
    /// invocation of the same snippet may cause an AccessViolationException.
    /// </summary>
    [Guid("3DFA7603-3B51-4484-81CD-FF1470123C7C")]
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsExpansionSessionInternal
    {
        void Reserved1();
        void Reserved2();
        void Reserved3();
        void Reserved4();
        void Reserved5();
        void Reserved6();
        void Reserved7();
        void Reserved8();

        /// <summary>
        /// WARNING: Marshal pNode with GetUniqueObjectForIUnknown and call ReleaseComObject on it
        /// before leaving the calling method.
        /// </summary>
        [PreserveSig]
        int GetSnippetNode([MarshalAs(UnmanagedType.BStr)] string bstrNode, out IntPtr pNode);

        void Reserved9();
        void Reserved10();
        void Reserved11();
    }
}
