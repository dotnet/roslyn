// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.Metadata;
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

        // Not exposed by the metadata reader since they are abstracted away and presented as SignatureTypeCode.TypeHandle
        public const SignatureTypeCode SignatureTypeCode_Class = (SignatureTypeCode)0x12;
        public const SignatureTypeCode SignatureTypeCode_ValueType = (SignatureTypeCode)0x11;
    }

    internal enum HeapSizeFlag : byte
    {
        StringHeapLarge = 0x01, // 4 byte uint indexes used for string heap offsets
        GuidHeapLarge = 0x02,   // 4 byte uint indexes used for GUID heap offsets
        BlobHeapLarge = 0x04,   // 4 byte uint indexes used for Blob heap offsets
        EnCDeltas = 0x20,       // Indicates only EnC Deltas are present
        DeletedMarks = 0x80,    // Indicates metadata might contain items marked deleted
    }

    internal static class TokenTypeIds
    {
        internal const int Module = 0x00000000;
        internal const int TypeRef = 0x01000000;
        internal const int TypeDef = 0x02000000;
        internal const int FieldDef = 0x04000000;
        internal const int MethodDef = 0x06000000;
        internal const int ParamDef = 0x08000000;
        internal const int InterfaceImpl = 0x09000000;
        internal const int MemberRef = 0x0a000000;
        internal const int Constant = 0x0b000000;
        internal const int CustomAttribute = 0x0c000000;
        internal const int Permission = 0x0e000000;
        internal const int Signature = 0x11000000;
        internal const int EventMap = 0x12000000;
        internal const int Event = 0x14000000;
        internal const int PropertyMap = 0x15000000;
        internal const int Property = 0x17000000;
        internal const int MethodSemantics = 0x18000000;
        internal const int MethodImpl = 0x19000000;
        internal const int ModuleRef = 0x1a000000;
        internal const int TypeSpec = 0x1b000000;
        internal const int Assembly = 0x20000000;
        internal const int AssemblyRef = 0x23000000;
        internal const int File = 0x26000000;
        internal const int ExportedType = 0x27000000;
        internal const int ManifestResource = 0x28000000;
        internal const int NestedClass = 0x29000000;
        internal const int GenericParam = 0x2a000000;
        internal const int MethodSpec = 0x2b000000;
        internal const int GenericParamConstraint = 0x2c000000;
        internal const int UserString = 0x70000000;
        internal const int String = 0x71000000;
    }

    internal enum TypeFlags : uint
    {
        PrivateAccess = 0x00000000,
        PublicAccess = 0x00000001,
        NestedPublicAccess = 0x00000002,
        NestedPrivateAccess = 0x00000003,
        NestedFamilyAccess = 0x00000004,
        NestedAssemblyAccess = 0x00000005,
        NestedFamilyAndAssemblyAccess = 0x00000006,
        NestedFamilyOrAssemblyAccess = 0x00000007,
        AccessMask = 0x0000007,
        NestedMask = 0x00000006,

        AutoLayout = 0x00000000,
        SequentialLayout = 0x00000008,
        ExplicitLayout = 0x00000010,
        LayoutMask = 0x00000018,

        ClassSemantics = 0x00000000,
        InterfaceSemantics = 0x00000020,
        AbstractSemantics = 0x00000080,
        SealedSemantics = 0x00000100,
        SpecialNameSemantics = 0x00000400,

        ImportImplementation = 0x00001000,
        SerializableImplementation = 0x00002000,
        WindowsRuntimeImplementation = 0x00004000,
        BeforeFieldInitImplementation = 0x00100000,
        ForwarderImplementation = 0x00200000,

        AnsiString = 0x00000000,
        UnicodeString = 0x00010000,
        AutoCharString = 0x00020000,
        StringMask = 0x00030000,

        RTSpecialNameReserved = 0x00000800,
        HasSecurityReserved = 0x00040000,
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
