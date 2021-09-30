// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Cci
{
    internal static class Constants
    {
        // Non-portable CharSet values:
        public const CharSet CharSet_None = (CharSet)1;
        public const CharSet CharSet_Auto = (CharSet)4;

        // Non-portable CallingConvention values:
        public const System.Runtime.InteropServices.CallingConvention CallingConvention_FastCall = (System.Runtime.InteropServices.CallingConvention)5;

        // Non-portable UnmanagedType values:
        public const UnmanagedType UnmanagedType_CustomMarshaler = (UnmanagedType)44;
        public const UnmanagedType UnmanagedType_IDispatch = (UnmanagedType)26;
        public const UnmanagedType UnmanagedType_SafeArray = (UnmanagedType)29;
        public const UnmanagedType UnmanagedType_VBByRefStr = (UnmanagedType)34;
        public const UnmanagedType UnmanagedType_AnsiBStr = (UnmanagedType)35;
        public const UnmanagedType UnmanagedType_TBStr = (UnmanagedType)36;

        public const ComInterfaceType ComInterfaceType_InterfaceIsDual = 0;
        public const ComInterfaceType ComInterfaceType_InterfaceIsIDispatch = (ComInterfaceType)2;

        public const ClassInterfaceType ClassInterfaceType_AutoDispatch = (ClassInterfaceType)1;
        public const ClassInterfaceType ClassInterfaceType_AutoDual = (ClassInterfaceType)2;

        // Non-portable CompilationRelaxations value:
        public const int CompilationRelaxations_NoStringInterning = 0x0008;

        public const TypeAttributes TypeAttributes_TypeForwarder = (TypeAttributes)0x00200000;
    }

    /// <summary>
    /// System.Runtime.InteropServices.VarEnum is obsolete.
    /// </summary>
    internal enum VarEnum
    {
        VT_EMPTY = 0,
        VT_NULL = 1,
        VT_I2 = 2,
        VT_I4 = 3,
        VT_R4 = 4,
        VT_R8 = 5,
        VT_CY = 6,
        VT_DATE = 7,
        VT_BSTR = 8,
        VT_DISPATCH = 9,
        VT_ERROR = 10,
        VT_BOOL = 11,
        VT_VARIANT = 12,
        VT_UNKNOWN = 13,
        VT_DECIMAL = 14,
        VT_I1 = 16,
        VT_UI1 = 17,
        VT_UI2 = 18,
        VT_UI4 = 19,
        VT_I8 = 20,
        VT_UI8 = 21,
        VT_INT = 22,
        VT_UINT = 23,
        VT_VOID = 24,
        VT_HRESULT = 25,
        VT_PTR = 26,
        VT_SAFEARRAY = 27,
        VT_CARRAY = 28,
        VT_USERDEFINED = 29,
        VT_LPSTR = 30,
        VT_LPWSTR = 31,
        VT_RECORD = 36,
        VT_FILETIME = 64,
        VT_BLOB = 65,
        VT_STREAM = 66,
        VT_STORAGE = 67,
        VT_STREAMED_OBJECT = 68,
        VT_STORED_OBJECT = 69,
        VT_BLOB_OBJECT = 70,
        VT_CF = 71,
        VT_CLSID = 72,
        VT_VECTOR = 0x1000,
        VT_ARRAY = 0x2000,
        VT_BYREF = 0x4000
    }
}
