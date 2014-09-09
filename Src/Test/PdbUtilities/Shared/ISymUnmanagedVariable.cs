// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Roslyn.Utilities.Pdb
{
    [ComImport]
    [Guid("9F60EEBE-2D9A-3F7C-BF58-80BC991C60BB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    internal interface ISymUnmanagedVariable
    {
        void GetName(int cchName, out int pcchName, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] name);
        void GetAttributes(out uint pRetVal);
        void GetSignature(int cSig, out int pcSig, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] sig);

        // the following methods are useless (not implemented, or returning a constant):
        void GetAddressKind(out int pRetVal);
        void GetAddressField1(out int pRetVal);
        void GetAddressField2(out int pRetVal);
        void GetAddressField3(out int pRetVal);
        void GetStartOffset(out int pRetVal);
        void GetEndOffset(out int pRetVal);
    } 
}
