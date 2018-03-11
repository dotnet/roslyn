// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("9BDA66AE-CA28-4e22-AA27-8A7218A0E3FA")]
    internal interface IEventHandler
    {
        [PreserveSig]
        int AddHandler([MarshalAs(UnmanagedType.BStr)] string bstrEventName);

        [PreserveSig]
        int RemoveHandler([MarshalAs(UnmanagedType.BStr)] string bstrEventName);

        [PreserveSig]
        int GetHandledEvents([MarshalAs(UnmanagedType.Interface)] out IVsEnumBSTR ppUnk);

        [PreserveSig]
        int HandlesEvent([MarshalAs(UnmanagedType.BStr)] string bstrEventName, [MarshalAs(UnmanagedType.Bool)] out bool result);
    }
}
