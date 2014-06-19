//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

// ^ using Microsoft.Contracts;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFileFlags
{
    internal enum Machine : ushort
    {
        Unknown = 0x0000,
        I386 = 0x014C,        // Intel 386.
        R3000 = 0x0162,       // MIPS little-endian, 0x160 big-endian
        R4000 = 0x0166,       // MIPS little-endian
        R10000 = 0x0168,      // MIPS little-endian
        WCEMIPSV2 = 0x0169,   // MIPS little-endian WCE v2
        Alpha = 0x0184,       // Alpha_AXP
        SH3 = 0x01a2,         // SH3 little-endian
        SH3DSP = 0x01a3,
        SH3E = 0x01a4,        // SH3E little-endian
        SH4 = 0x01a6,         // SH4 little-endian
        SH5 = 0x01a8,         // SH5
        ARM = 0x01c0,         // ARM Little-Endian
        Thumb = 0x01c2,
        AM33 = 0x01d3,
        PowerPC = 0x01F0,     // IBM PowerPC Little-Endian
        PowerPCFP = 0x01f1,
        IA64 = 0x0200,        // Intel 64
        MIPS16 = 0x0266,      // MIPS
        Alpha64 = 0x0284,     // ALPHA64
        MIPSFPU = 0x0366,     // MIPS
        MIPSFPU16 = 0x0466,   // MIPS
        AXP64 = Alpha64,
        Tricore = 0x0520,     // Infineon
        CEF = 0x0CEF,
        EBC = 0x0EBC,         // EFI Byte Code
        AMD64 = 0x8664,       // AMD64 (K8)
        M32R = 0x9041,        // M32R little-endian
        CEE = 0xC0EE,
    }

    internal enum Characteristics : ushort
    {
        RelocsStripped = 0x0001,         // Relocation info stripped from file.
        ExecutableImage = 0x0002,        // File is executable  (i.e. no unresolved external references).
        LineNumsStripped = 0x0004,       // Line numbers stripped from file.
        LocalSymsStripped = 0x0008,      // Local symbols stripped from file.
        AggressiveWsTrim = 0x0010,       // Agressively trim working set
        LargeAddressAware = 0x0020,      // App can handle >2gb addresses
        BytesReversedLo = 0x0080,        // Bytes of machine word are reversed.
        Bit32Machine = 0x0100,           // 32 bit word machine.
        DebugStripped = 0x0200,          // Debugging info stripped from file in .DBG file
        RemovableRunFromSwap = 0x0400,   // If Image is on removable media, copy and run from the swap file.
        NetRunFromSwap = 0x0800,         // If Image is on Net, copy and run from the swap file.
        System = 0x1000,                 // System File.
        Dll = 0x2000,                    // File is a DLL.
        UpSystemOnly = 0x4000,           // File should only be run on a UP machine
        BytesReversedHi = 0x8000,        // Bytes of machine word are reversed.
    }

    internal enum PEMagic : ushort
    {
        PEMagic32 = 0x010B,
        PEMagic64 = 0x020B,
    }

    internal enum Directories : ushort
    {
        Export,
        Import,
        Resource,
        Exception,
        Certificate,
        BaseRelocation,
        Debug,
        Copyright,
        GlobalPointer,
        ThreadLocalStorage,
        LoadConfig,
        BoundImport,
        ImportAddress,
        DelayImport,
        COR20Header,
        Reserved,
        Cor20HeaderMetaData,
        Cor20HeaderResources,
        Cor20HeaderStrongNameSignature,
        Cor20HeaderCodeManagerTable,
        Cor20HeaderVtableFixups,
        Cor20HeaderExportAddressTableJumps,
        Cor20HeaderManagedNativeHeader,
    }

    internal enum Subsystem : ushort
    {
        Unknown = 0,                // Unknown subsystem.
        Native = 1,                 // Image doesn't require a subsystem.
        WindowsGUI = 2,             // Image runs in the Windows GUI subsystem.
        WindowsCUI = 3,             // Image runs in the Windows character subsystem.
        OS2CUI = 5,                 // image runs in the OS/2 character subsystem.
        POSIXCUI = 7,               // image runs in the Posix character subsystem.
        NativeWindows = 8,          // image is a native Win9x driver.
        WindowsCEGUI = 9,           // Image runs in the Windows CE subsystem.
        EFIApplication = 10,
        EFIBootServiceDriver = 11,
        EFIRuntimeDriver = 12,
        EFIROM = 13,
        XBOX = 14,
    }

    internal enum DllCharacteristics : ushort
    {
        ProcessInit = 0x0001,   // Reserved.
        ProcessTerm = 0x0002,   // Reserved.
        ThreadInit = 0x0004,    // Reserved.
        ThreadTerm = 0x0008,    // Reserved.
        DynamicBase = 0x0040,
        NxCompatible = 0x0100,
        NoIsolation = 0x0200,   // Image understands isolation and doesn't want it
        NoSEH = 0x0400,         // Image does not use SEH.  No SE handler may reside in this image
        NoBind = 0x0800,        // Do not bind this image.
        //                     0x1000     // Reserved.
        WDM_Driver = 0x2000,    // Driver uses WDM model
        //                     0x4000     // Reserved.
        TerminalServerAware = 0x8000,
    }

    internal enum SectionCharacteristics : uint
    {
        TypeReg = 0x00000000,               // Reserved.
        TypeDSect = 0x00000001,             // Reserved.
        TypeNoLoad = 0x00000002,            // Reserved.
        TypeGroup = 0x00000004,             // Reserved.
        TypeNoPad = 0x00000008,             // Reserved.
        TypeCopy = 0x00000010,              // Reserved.

        CNTCode = 0x00000020,               // Section contains code.
        CNTInitializedData = 0x00000040,    // Section contains initialized data.
        CNTUninitializedData = 0x00000080,  // Section contains uninitialized data.

        LNKOther = 0x00000100,            // Reserved.
        LNKInfo = 0x00000200,             // Section contains comments or some other type of information.
        TypeOver = 0x00000400,            // Reserved.
        LNKRemove = 0x00000800,           // Section contents will not become part of image.
        LNKCOMDAT = 0x00001000,           // Section contents comdat.
        //                               0x00002000  // Reserved.
        MemProtected = 0x00004000,
        No_Defer_Spec_Exc = 0x00004000,   // Reset speculative exceptions handling bits in the TLB entries for this section.
        GPRel = 0x00008000,               // Section content can be accessed relative to GP
        MemFardata = 0x00008000,
        MemSysheap = 0x00010000,
        MemPurgeable = 0x00020000,
        Mem16Bit = 0x00020000,
        MemLocked = 0x00040000,
        MemPreload = 0x00080000,

        Align1Bytes = 0x00100000,
        Align2Bytes = 0x00200000,
        Align4Bytes = 0x00300000,
        Align8Bytes = 0x00400000,
        Align16Bytes = 0x00500000,    // Default alignment if no others are specified.
        Align32Bytes = 0x00600000,
        Align64Bytes = 0x00700000,
        Align128Bytes = 0x00800000,
        Align256Bytes = 0x00900000,
        Align512Bytes = 0x00A00000,
        Align1024Bytes = 0x00B00000,
        Align2048Bytes = 0x00C00000,
        Align4096Bytes = 0x00D00000,
        Align8192Bytes = 0x00E00000,

        // Unused                     0x00F00000
        AlignMask = 0x00F00000,

        LNKNRelocOvfl = 0x01000000,   // Section contains extended relocations.
        MemDiscardable = 0x02000000,  // Section can be discarded.
        MemNotCached = 0x04000000,    // Section is not cachable.
        MemNotPaged = 0x08000000,     // Section is not pageable.
        MemShared = 0x10000000,       // Section is shareable.
        MemExecute = 0x20000000,      // Section is executable.
        MemRead = 0x40000000,         // Section is readable.
        MemWrite = 0x80000000,        // Section is writeable.
    }

    internal enum COR20Flags : uint
    {
        ILOnly = 0x00000001,
        Bit32Required = 0x00000002,
        ILLibrary = 0x00000004,
        StrongNameSigned = 0x00000008,
        NativeEntryPoint = 0x00000010,
        TrackDebugData = 0x00010000,
    }

    internal enum MetadataStreamKind
    {
        Illegal,
        Compressed,
        UnCompressed,
    }

    internal enum TableIndices : byte
    {
        Module = 0x00,
        TypeRef = 0x01,
        TypeDef = 0x02,
        FieldPtr = 0x03,
        Field = 0x04,
        MethodPtr = 0x05,
        Method = 0x06,
        ParamPtr = 0x07,
        Param = 0x08,
        InterfaceImpl = 0x09,
        MemberRef = 0x0A,
        Constant = 0x0B,
        CustomAttribute = 0x0C,
        FieldMarshal = 0x0D,
        DeclSecurity = 0x0E,
        ClassLayout = 0x0F,
        FieldLayout = 0x10,
        StandAloneSig = 0x11,
        EventMap = 0x12,
        EventPtr = 0x13,
        Event = 0x14,
        PropertyMap = 0x15,
        PropertyPtr = 0x16,
        Property = 0x17,
        MethodSemantics = 0x18,
        MethodImpl = 0x19,
        ModuleRef = 0x1A,
        TypeSpec = 0x1B,
        ImplMap = 0x1C,
        FieldRva = 0x1D,
        EnCLog = 0x1E,
        EnCMap = 0x1F,
        Assembly = 0x20,
        AssemblyProcessor = 0x21,
        AssemblyOS = 0x22,
        AssemblyRef = 0x23,
        AssemblyRefProcessor = 0x24,
        AssemblyRefOS = 0x25,
        File = 0x26,
        ExportedType = 0x27,
        ManifestResource = 0x28,
        NestedClass = 0x29,
        GenericParam = 0x2A,
        MethodSpec = 0x2B,
        GenericParamConstraint = 0x2C,
        Count,
    }

    internal enum TableMask : ulong
    {
        Module = 0x0000000000000001UL << 0x00,
        TypeRef = 0x0000000000000001UL << 0x01,
        TypeDef = 0x0000000000000001UL << 0x02,
        FieldPtr = 0x0000000000000001UL << 0x03,
        Field = 0x0000000000000001UL << 0x04,
        MethodPtr = 0x0000000000000001UL << 0x05,
        Method = 0x0000000000000001UL << 0x06,
        ParamPtr = 0x0000000000000001UL << 0x07,
        Param = 0x0000000000000001UL << 0x08,
        InterfaceImpl = 0x0000000000000001UL << 0x09,
        MemberRef = 0x0000000000000001UL << 0x0A,
        Constant = 0x0000000000000001UL << 0x0B,
        CustomAttribute = 0x0000000000000001UL << 0x0C,
        FieldMarshal = 0x0000000000000001UL << 0x0D,
        DeclSecurity = 0x0000000000000001UL << 0x0E,
        ClassLayout = 0x0000000000000001UL << 0x0F,
        FieldLayout = 0x0000000000000001UL << 0x10,
        StandAloneSig = 0x0000000000000001UL << 0x11,
        EventMap = 0x0000000000000001UL << 0x12,
        EventPtr = 0x0000000000000001UL << 0x13,
        Event = 0x0000000000000001UL << 0x14,
        PropertyMap = 0x0000000000000001UL << 0x15,
        PropertyPtr = 0x0000000000000001UL << 0x16,
        Property = 0x0000000000000001UL << 0x17,
        MethodSemantics = 0x0000000000000001UL << 0x18,
        MethodImpl = 0x0000000000000001UL << 0x19,
        ModuleRef = 0x0000000000000001UL << 0x1A,
        TypeSpec = 0x0000000000000001UL << 0x1B,
        ImplMap = 0x0000000000000001UL << 0x1C,
        FieldRva = 0x0000000000000001UL << 0x1D,
        EnCLog = 0x0000000000000001UL << 0x1E,
        EnCMap = 0x0000000000000001UL << 0x1F,
        Assembly = 0x0000000000000001UL << 0x20,
        AssemblyProcessor = 0x0000000000000001UL << 0x21,
        AssemblyOS = 0x0000000000000001UL << 0x22,
        AssemblyRef = 0x0000000000000001UL << 0x23,
        AssemblyRefProcessor = 0x0000000000000001UL << 0x24,
        AssemblyRefOS = 0x0000000000000001UL << 0x25,
        File = 0x0000000000000001UL << 0x26,
        ExportedType = 0x0000000000000001UL << 0x27,
        ManifestResource = 0x0000000000000001UL << 0x28,
        NestedClass = 0x0000000000000001UL << 0x29,
        GenericParam = 0x0000000000000001UL << 0x2A,
        MethodSpec = 0x0000000000000001UL << 0x2B,
        GenericParamConstraint = 0x0000000000000001UL << 0x2C,

        SortedTablesMask =
          TableMask.ClassLayout
          | TableMask.Constant
          | TableMask.CustomAttribute
          | TableMask.DeclSecurity
          | TableMask.FieldLayout
          | TableMask.FieldMarshal
          | TableMask.FieldRva
          | TableMask.GenericParam
          | TableMask.GenericParamConstraint
          | TableMask.ImplMap
          | TableMask.InterfaceImpl
          | TableMask.MethodImpl
          | TableMask.MethodSemantics
          | TableMask.NestedClass,
        CompressedStreamNotAllowedMask =
          TableMask.FieldPtr
          | TableMask.MethodPtr
          | TableMask.ParamPtr
          | TableMask.EventPtr
          | TableMask.PropertyPtr
          | TableMask.EnCLog
          | TableMask.EnCMap,
        V1_0_TablesMask =
          TableMask.Module
          | TableMask.TypeRef
          | TableMask.TypeDef
          | TableMask.FieldPtr
          | TableMask.Field
          | TableMask.MethodPtr
          | TableMask.Method
          | TableMask.ParamPtr
          | TableMask.Param
          | TableMask.InterfaceImpl
          | TableMask.MemberRef
          | TableMask.Constant
          | TableMask.CustomAttribute
          | TableMask.FieldMarshal
          | TableMask.DeclSecurity
          | TableMask.ClassLayout
          | TableMask.FieldLayout
          | TableMask.StandAloneSig
          | TableMask.EventMap
          | TableMask.EventPtr
          | TableMask.Event
          | TableMask.PropertyMap
          | TableMask.PropertyPtr
          | TableMask.Property
          | TableMask.MethodSemantics
          | TableMask.MethodImpl
          | TableMask.ModuleRef
          | TableMask.TypeSpec
          | TableMask.ImplMap
          | TableMask.FieldRva
          | TableMask.EnCLog
          | TableMask.EnCMap
          | TableMask.Assembly
          | TableMask.AssemblyRef
          | TableMask.File
          | TableMask.ExportedType
          | TableMask.ManifestResource
          | TableMask.NestedClass,
        V1_1_TablesMask =
          TableMask.Module
          | TableMask.TypeRef
          | TableMask.TypeDef
          | TableMask.FieldPtr
          | TableMask.Field
          | TableMask.MethodPtr
          | TableMask.Method
          | TableMask.ParamPtr
          | TableMask.Param
          | TableMask.InterfaceImpl
          | TableMask.MemberRef
          | TableMask.Constant
          | TableMask.CustomAttribute
          | TableMask.FieldMarshal
          | TableMask.DeclSecurity
          | TableMask.ClassLayout
          | TableMask.FieldLayout
          | TableMask.StandAloneSig
          | TableMask.EventMap
          | TableMask.EventPtr
          | TableMask.Event
          | TableMask.PropertyMap
          | TableMask.PropertyPtr
          | TableMask.Property
          | TableMask.MethodSemantics
          | TableMask.MethodImpl
          | TableMask.ModuleRef
          | TableMask.TypeSpec
          | TableMask.ImplMap
          | TableMask.FieldRva
          | TableMask.EnCLog
          | TableMask.EnCMap
          | TableMask.Assembly
          | TableMask.AssemblyRef
          | TableMask.File
          | TableMask.ExportedType
          | TableMask.ManifestResource
          | TableMask.NestedClass,
        V2_0_TablesMask =
          TableMask.Module
          | TableMask.TypeRef
          | TableMask.TypeDef
          | TableMask.FieldPtr
          | TableMask.Field
          | TableMask.MethodPtr
          | TableMask.Method
          | TableMask.ParamPtr
          | TableMask.Param
          | TableMask.InterfaceImpl
          | TableMask.MemberRef
          | TableMask.Constant
          | TableMask.CustomAttribute
          | TableMask.FieldMarshal
          | TableMask.DeclSecurity
          | TableMask.ClassLayout
          | TableMask.FieldLayout
          | TableMask.StandAloneSig
          | TableMask.EventMap
          | TableMask.EventPtr
          | TableMask.Event
          | TableMask.PropertyMap
          | TableMask.PropertyPtr
          | TableMask.Property
          | TableMask.MethodSemantics
          | TableMask.MethodImpl
          | TableMask.ModuleRef
          | TableMask.TypeSpec
          | TableMask.ImplMap
          | TableMask.FieldRva
          | TableMask.EnCLog
          | TableMask.EnCMap
          | TableMask.Assembly
          | TableMask.AssemblyRef
          | TableMask.File
          | TableMask.ExportedType
          | TableMask.ManifestResource
          | TableMask.NestedClass
          | TableMask.GenericParam
          | TableMask.MethodSpec
          | TableMask.GenericParamConstraint,
    }

    internal enum HeapSizeFlag : byte
    {
        StringHeapLarge = 0x01, // 4 byte uint indexes used for string heap offsets
        GUIDHeapLarge = 0x02,   // 4 byte uint indexes used for GUID heap offsets
        BlobHeapLarge = 0x04,   // 4 byte uint indexes used for Blob heap offsets
        EnCDeltas = 0x20,       // Indicates only EnC Deltas are present
        DeletedMarks = 0x80,    // Indicates metadata might contain items marked deleted
    }

    internal static class TokenTypeIds
    {
        internal const uint Module = 0x00000000;
        internal const uint TypeRef = 0x01000000;
        internal const uint TypeDef = 0x02000000;
        internal const uint FieldDef = 0x04000000;
        internal const uint MethodDef = 0x06000000;
        internal const uint ParamDef = 0x08000000;
        internal const uint InterfaceImpl = 0x09000000;
        internal const uint MemberRef = 0x0a000000;
        internal const uint CustomAttribute = 0x0c000000;
        internal const uint Permission = 0x0e000000;
        internal const uint Signature = 0x11000000;
        internal const uint Event = 0x14000000;
        internal const uint Property = 0x17000000;
        internal const uint ModuleRef = 0x1a000000;
        internal const uint TypeSpec = 0x1b000000;
        internal const uint Assembly = 0x20000000;
        internal const uint AssemblyRef = 0x23000000;
        internal const uint File = 0x26000000;
        internal const uint ExportedType = 0x27000000;
        internal const uint ManifestResource = 0x28000000;
        internal const uint GenericParam = 0x2a000000;
        internal const uint MethodSpec = 0x2b000000;
        internal const uint GenericParamConstraint = 0x2c000000;
        internal const uint String = 0x70000000;
        internal const uint Name = 0x71000000;
        internal const uint BaseType = 0x72000000;       // Leave this on the high end value. This does not correspond to metadata table???

        internal const uint RIDMask = 0x00FFFFFF;
        internal const uint TokenTypeMask = 0xFF000000;
    }

    internal enum AssemblyHashAlgorithmFlags : uint
    {
        None = 0x00000000,
        MD5 = 0x00008003,
        SHA1 = 0x00008004
    }

    internal enum TypeDefFlags : uint
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
        SeqentialLayout = 0x00000008,
        ExplicitLayout = 0x00000010,
        LayoutMask = 0x00000018,

        ClassSemantics = 0x00000000,
        InterfaceSemantics = 0x00000020,
        AbstractSemantics = 0x00000080,
        SealedSemantics = 0x00000100,
        SpecialNameSemantics = 0x00000400,

        ImportImplementation = 0x00001000,
        SerializableImplementation = 0x00002000,
        BeforeFieldInitImplementation = 0x00100000,
        ForwarderImplementation = 0x00200000,

        AnsiString = 0x00000000,
        UnicodeString = 0x00010000,
        AutoCharString = 0x00020000,
        CustomFormatString = 0x00020000,
        StringMask = 0x00030000,

        RTSpecialNameReserved = 0x00000800,
        HasSecurityReserved = 0x00040000,
    }

    internal enum FieldFlags : ushort
    {
        CompilerControlledAccess = 0x0000,
        PrivateAccess = 0x0001,
        FamilyAndAssemblyAccess = 0x0002,
        AssemblyAccess = 0x0003,
        FamilyAccess = 0x0004,
        FamilyOrAssemblyAccess = 0x0005,
        PublicAccess = 0x0006,
        AccessMask = 0x0007,

        StaticContract = 0x0010,
        InitOnlyContract = 0x0020,
        LiteralContract = 0x0040,
        NotSerializedContract = 0x0080,

        SpecialNameImpl = 0x0200,
        PInvokeImpl = 0x2000,

        RTSpecialNameReserved = 0x0400,
        HasFieldMarshalReserved = 0x1000,
        HasDefaultReserved = 0x8000,
        HasFieldRVAReserved = 0x0100,

        // Load flags
        FieldLoaded = 0x4000,
    }

    internal enum MethodFlags : ushort
    {
        CompilerControlledAccess = 0x0000,
        PrivateAccess = 0x0001,
        FamilyAndAssemblyAccess = 0x0002,
        AssemblyAccess = 0x0003,
        FamilyAccess = 0x0004,
        FamilyOrAssemblyAccess = 0x0005,
        PublicAccess = 0x0006,
        AccessMask = 0x0007,

        StaticContract = 0x0010,
        FinalContract = 0x0020,
        VirtualContract = 0x0040,
        HideBySignatureContract = 0x0080,

        ReuseSlotVTable = 0x0000,
        NewSlotVTable = 0x0100,

        CheckAccessOnOverrideImpl = 0x0200,
        AbstractImpl = 0x0400,
        SpecialNameImpl = 0x0800,

        PInvokeInterop = 0x2000,
        UnmanagedExportInterop = 0x0008,

        RTSpecialNameReserved = 0x1000,
        HasSecurityReserved = 0x4000,
        RequiresSecurityObjectReserved = 0x8000,
    }

    internal enum ParamFlags : ushort
    {
        InSemantics = 0x0001,
        OutSemantics = 0x0002,
        OptionalSemantics = 0x0010,

        HasDefaultReserved = 0x1000,
        HasFieldMarshalReserved = 0x2000,
    }

    internal enum PropertyFlags : ushort
    {
        SpecialNameImpl = 0x0200,

        RTSpecialNameReserved = 0x0400,
        HasDefaultReserved = 0x1000,

        // Comes from signature...
        HasThis = 0x0001,
        ReturnValueIsByReference = 0x0002,

        // Load flags
        GetterLoaded = 0x0004,
        SetterLoaded = 0x0008,
    }

    internal enum EventFlags : ushort
    {
        SpecialNameImpl = 0x0200,

        RTSpecialNameReserved = 0x0400,

        // Load flags
        AdderLoaded = 0x0001,
        RemoverLoaded = 0x0002,
        FireLoaded = 0x0004,
    }

    internal enum MethodSemanticsFlags : ushort
    {
        Setter = 0x0001,
        Getter = 0x0002,
        Other = 0x0004,
        AddOn = 0x0008,
        RemoveOn = 0x0010,
        Fire = 0x0020,
    }

    internal enum DeclSecurityActionFlags : ushort
    {
        ActionNil = 0x0000,
        Request = 0x0001,
        Demand = 0x0002,
        Assert = 0x0003,
        Deny = 0x0004,
        PermitOnly = 0x0005,
        LinktimeCheck = 0x0006,
        InheritanceCheck = 0x0007,
        RequestMinimum = 0x0008,
        RequestOptional = 0x0009,
        RequestRefuse = 0x000A,
        PrejitGrant = 0x000B,
        PrejitDenied = 0x000C,
        NonCasDemand = 0x000D,
        NonCasLinkDemand = 0x000E,
        NonCasInheritance = 0x000F,
        MaximumValue = 0x000F,
        ActionMask = 0x001F,
    }

    internal enum MethodImplFlags : ushort
    {
        ILCodeType = 0x0000,
        NativeCodeType = 0x0001,
        OPTILCodeType = 0x0002,
        RuntimeCodeType = 0x0003,
        CodeTypeMask = 0x0003,

        Unmanaged = 0x0004,
        NoInlining = 0x0008,
        ForwardRefInterop = 0x0010,
        Synchronized = 0x0020,
        NoOptimization = 0x0040,
        PreserveSigInterop = 0x0080,
        InternalCall = 0x1000,
    }

    internal enum PInvokeMapFlags : ushort
    {
        NoMangle = 0x0001,

        DisabledBestFit = 0x0020,
        EnabledBestFit = 0x0010,
        UseAssemblyBestFit = 0x0000,
        BestFitMask = 0x0030,

        CharSetNotSpec = 0x0000,
        CharSetAnsi = 0x0002,
        CharSetUnicode = 0x0004,
        CharSetAuto = 0x0006,
        CharSetMask = 0x0006,

        EnabledThrowOnUnmappableChar = 0x1000,
        DisabledThrowOnUnmappableChar = 0x2000,
        UseAssemblyThrowOnUnmappableChar = 0x0000,
        ThrowOnUnmappableCharMask = 0x3000,

        SupportsLastError = 0x0040,

        WinAPICallingConvention = 0x0100,
        CDeclCallingConvention = 0x0200,
        StdCallCallingConvention = 0x0300,
        ThisCallCallingConvention = 0x0400,
        FastCallCallingConvention = 0x0500,
        CallingConventionMask = 0x0700,
    }

    internal enum AssemblyFlags : uint
    {
        PublicKey = 0x00000001,
        Retargetable = 0x00000100
    }

    internal enum ManifestResourceFlags : uint
    {
        PublicVisibility = 0x00000001,
        PrivateVisibility = 0x00000002,
        VisibilityMask = 0x00000007,

        InExternalFile = 0x00000010,
    }

    internal enum FileFlags : uint
    {
        ContainsMetadata = 0x00000000,
        ContainsNoMetadata = 0x00000001,
    }

    internal enum GenericParamFlags : ushort
    {
        NonVariant = 0x0000,
        Covariant = 0x0001,
        Contravariant = 0x0002,
        VarianceMask = 0x0003,

        ReferenceTypeConstraint = 0x0004,
        ValueTypeConstraint = 0x0008,
        DefaultConstructorConstraint = 0x0010,
    }
}

#if INCLUDE_IL_SPECIFIC_DATA
namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
    using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;


    #region IL Specific data

  internal static class CILMethodFlags {
    internal const byte ILTinyFormat = 0x02;
    internal const byte ILFatFormat = 0x03;
    internal const byte ILFormatMask = 0x03;
    internal const int ILTinyFormatSizeShift = 2;
    internal const byte ILMoreSects = 0x08;
    internal const byte ILInitLocals = 0x10;
    internal const byte ILFatFormatHeaderSize = 0x03;
    internal const int ILFatFormatHeaderSizeShift = 4;

    internal const byte SectEHTable = 0x01;
    internal const byte SectOptILTable = 0x02;
    internal const byte SectFatFormat = 0x40;
    internal const byte SectMoreSects = 0x40;
  }

  internal enum SEHFlags : uint {
    Catch = 0x0000,
    Filter = 0x0001,
    Finally = 0x0002,
    Fault = 0x0004,
  }

  internal struct SEHTableEntry {
    internal readonly SEHFlags SEHFlags;
    internal readonly uint TryOffset;
    internal readonly uint TryLength;
    internal readonly uint HandlerOffset;
    internal readonly uint HandlerLength;
    internal readonly uint ClassTokenOrFilterOffset;
    internal SEHTableEntry(
      SEHFlags sehFlags,
      uint tryOffset,
      uint tryLength,
      uint handlerOffset,
      uint handlerLength,
      uint classTokenOrFilterOffset) {
      this.SEHFlags = sehFlags;
      this.TryOffset = tryOffset;
      this.TryLength = tryLength;
      this.HandlerOffset = handlerOffset;
      this.HandlerLength = handlerLength;
      this.ClassTokenOrFilterOffset = classTokenOrFilterOffset;
    }
  }

  internal sealed class MethodIL {
    internal readonly bool LocalVariablesInited;
    internal readonly ushort MaxStack;
    internal readonly uint LocalSignatureToken;
    internal readonly MemoryBlock EncodedILMemoryBlock;
    internal readonly SEHTableEntry[]/*?*/ SEHTable;
    internal MethodIL(
      bool localVariablesInited,
      ushort maxStack,
      uint localSignatureToken,
      MemoryBlock encodedILMemoryBlock,
      SEHTableEntry[]/*?*/ sehTable) {
      this.LocalVariablesInited = localVariablesInited;
      this.MaxStack = maxStack;
      this.LocalSignatureToken = localSignatureToken;
      this.EncodedILMemoryBlock = encodedILMemoryBlock;
      this.SEHTable = sehTable;
    }
  }

    #endregion IL Specific Data

}
#endif // INCLUDE_IL_SPECIFIC_DATA