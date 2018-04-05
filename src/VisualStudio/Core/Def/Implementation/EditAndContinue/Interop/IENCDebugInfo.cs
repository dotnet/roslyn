// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ENC_LOCALINFO
    {
        [MarshalAs(UnmanagedType.BStr)]
        public readonly string LocalName; // SysFreeString

        public readonly uint Attributes;

        public readonly IntPtr Signature;    // CoTaskMemFree this
        public readonly int SignatureSize;

        public readonly int Slot;
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("1DA15C39-7E02-4ee8-8F60-FFF81275EE14")]
    internal interface IENCDebugInfo
    {
        [PreserveSig]
        int GetLocalVariableCount(uint methodToken, out int pcLocals);

        [PreserveSig]
        int GetLocalVariableLayout(
            uint methodToken,
            int cLocals,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ENC_LOCALINFO[] rgLocalInfo,
            out int pceltFetched);

        // not implemented in Concord:
        void __GetCountOfExpressionContextsForMethod(/*...*/);
        void __GetExpressionContextsForMethod(/*...*/);
    }
}
