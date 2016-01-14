// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        // Non-portable CompilationRelaxations value:
        public const int CompilationRelaxations_NoStringInterning = 0x0008;
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
}
