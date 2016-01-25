// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection.PortableExecutable;

namespace Microsoft.Cci
{
    /// <summary>
    /// This class is used to store the module serialization properties for a compilation.
    /// </summary>
    internal sealed class ModulePropertiesForSerialization
    {
        /// <summary>
        /// The alignment factor (in bytes) that is used to align the raw data of sections in the image file.
       ///  The value should be a power of 2 between 512 and 64K, inclusive. The default is 512.
        /// </summary>
        public readonly int FileAlignment;

        /// <summary>
        /// The alignment (in bytes) of sections when they are loaded into memory. 
        /// It must be greater than or equal to <see cref="FileAlignment"/>. 
        /// The default is the page size for the architecture.
        /// </summary>
        public readonly int SectionAlignment;

        /// <summary>
        /// Identifies the version of the CLR that is required to load this module or assembly.
        /// </summary>
        public readonly string TargetRuntimeVersion;

        /// <summary>
        /// Specifies the target CPU. <see cref="Machine.Unknown"/> means AnyCPU.
        /// </summary>
        public readonly Machine Machine;

        /// <summary>
        /// True if the module contains only IL and is processor independent. Should there be a choice between launching as a 64-bit or 32-bit
        /// process, this setting will cause the host to launch it as a 32-bit process. 
        /// </summary>
        public readonly bool Prefers32Bit;

        /// <summary>
        /// The first part of a two part version number indicating the version of the format used to persist this module. For example, the 1 in 1.0.
        /// </summary>
        public readonly byte MetadataFormatMajorVersion = 2;

        /// <summary>
        /// The second part of a two part version number indicating the version of the format used to persist this module. For example, the 0 in 1.0.
        /// </summary>
        public readonly byte MetadataFormatMinorVersion;

        /// <summary>
        /// A globally unique persistent identifier for this module.
        /// </summary>
        public readonly Guid PersistentIdentifier;

        /// <summary>
        /// True if the module contains only IL and is processor independent.
        /// </summary>
        public readonly bool ILOnly = true;

        /// <summary>
        /// True if the instructions in this module must be compiled in such a way that the debugging experience is not compromised.
        /// To set the value of this property, add an instance of System.Diagnostics.DebuggableAttribute to the MetadataAttributes list.
        /// </summary>
        public readonly bool TrackDebugData;

        /// <summary>
        /// The preferred memory address at which the module is to be loaded at runtime.
        /// </summary>
        public readonly ulong BaseAddress;

        /// <summary>
        /// The size of the virtual memory to reserve for the initial process heap.
        /// Must fit into 32 bits if the target platform is 32 bit.
        /// </summary>
        public readonly ulong SizeOfHeapReserve;

        /// <summary>
        /// The size of the virtual memory initially committed for the initial process heap.
        /// Must fit into 32 bits if the target platform is 32 bit.
        /// </summary>
        public readonly ulong SizeOfHeapCommit;

        /// <summary>
        /// The size of the virtual memory to reserve for the initial thread's stack.
        /// Must fit into 32 bits if the target platform is 32 bit.
        /// </summary>
        public readonly ulong SizeOfStackReserve;

        public readonly ulong SizeOfStackCommit;
        public readonly bool StrongNameSigned;
        public readonly ushort MajorSubsystemVersion;
        public readonly ushort MinorSubsystemVersion;

        /// <summary>
        /// The first part of a two part version number indicating the version of the linker that produced this module. For example, the 8 in 8.0.
        /// </summary>
        public readonly byte LinkerMajorVersion;

        /// <summary>
        /// The first part of a two part version number indicating the version of the linker that produced this module. For example, the 0 in 8.0.
        /// </summary>
        public readonly byte LinkerMinorVersion;

        /// <summary>
        /// Flags that control the behavior of the target operating system. CLI implementations are supposed to ignore this, but some operating system pay attention.
        /// </summary>
        public DllCharacteristics DllCharacteristics { get; }

        public Characteristics ImageCharacteristics { get; }

        public Subsystem Subsystem { get; }

        public const ulong DefaultExeBaseAddress32Bit = 0x00400000;
        public const ulong DefaultExeBaseAddress64Bit = 0x0000000140000000;

        public const ulong DefaultDllBaseAddress32Bit = 0x10000000;
        public const ulong DefaultDllBaseAddress64Bit = 0x0000000180000000;

        public const ulong DefaultSizeOfHeapReserve32Bit = 0x00100000;
        public const ulong DefaultSizeOfHeapReserve64Bit = 0x00400000;

        public const ulong DefaultSizeOfHeapCommit32Bit = 0x1000;
        public const ulong DefaultSizeOfHeapCommit64Bit = 0x2000;

        public const ulong DefaultSizeOfStackReserve32Bit = 0x00100000;
        public const ulong DefaultSizeOfStackReserve64Bit = 0x00400000;

        public const ulong DefaultSizeOfStackCommit32Bit = 0x1000;
        public const ulong DefaultSizeOfStackCommit64Bit = 0x4000;

        public const ushort DefaultFileAlignment32Bit = 0x200;
        public const ushort DefaultFileAlignment64Bit = 0x200; //both 32 and 64 bit binaries used this value in the native stack.

        public const ushort DefaultSectionAlignment = 0x2000;

        internal ModulePropertiesForSerialization(
            Guid persistentIdentifier,
            int fileAlignment,
            int sectionAlignment,
            string targetRuntimeVersion,
            Machine machine,
            bool prefer32Bit,
            bool trackDebugData,
            ulong baseAddress,
            ulong sizeOfHeapReserve,
            ulong sizeOfHeapCommit,
            ulong sizeOfStackReserve,
            ulong sizeOfStackCommit,
            bool enableHighEntropyVA,
            bool strongNameSigned,
            bool configureToExecuteInAppContainer,
            Characteristics imageCharacteristics,
            Subsystem subsystem,
            ushort majorSubsystemVersion,
            ushort minorSubsystemVersion,
            byte linkerMajorVersion,
            byte linkerMinorVersion)
        {
            this.PersistentIdentifier = persistentIdentifier;
            this.FileAlignment = fileAlignment;
            this.SectionAlignment = sectionAlignment;
            this.TargetRuntimeVersion = targetRuntimeVersion;
            this.Machine = machine;
            this.Prefers32Bit = prefer32Bit;
            this.TrackDebugData = trackDebugData;
            this.BaseAddress = baseAddress;
            this.SizeOfHeapReserve = sizeOfHeapReserve;
            this.SizeOfHeapCommit = sizeOfHeapCommit;
            this.SizeOfStackReserve = sizeOfStackReserve;
            this.SizeOfStackCommit = sizeOfStackCommit;
            this.StrongNameSigned = strongNameSigned;
            this.LinkerMajorVersion = linkerMajorVersion;
            this.LinkerMinorVersion = linkerMinorVersion;
            this.MajorSubsystemVersion = majorSubsystemVersion;
            this.MinorSubsystemVersion = minorSubsystemVersion;
            this.ImageCharacteristics = imageCharacteristics;
            this.Subsystem = subsystem;
            this.DllCharacteristics = GetDllCharacteristics(enableHighEntropyVA, configureToExecuteInAppContainer);
        }

        private static DllCharacteristics GetDllCharacteristics(bool enableHighEntropyVA, bool configureToExecuteInAppContainer) 
        {
            var result =
                DllCharacteristics.DynamicBase |
                DllCharacteristics.NxCompatible |
                DllCharacteristics.NoSeh |
                DllCharacteristics.TerminalServerAware;

            if (enableHighEntropyVA)
            {
                result |= DllCharacteristics.HighEntropyVirtualAddressSpace;
            }

            if (configureToExecuteInAppContainer)
            {
                result |= DllCharacteristics.AppContainer;
            }

            return result;
        }

        /// <summary>
        /// If set, the module must include a machine code stub that transfers control to the virtual execution system.
        /// </summary>
        internal bool RequiresStartupStub => Machine == Machine.I386 || Machine == 0;

        /// <summary>
        /// If set, the module contains instructions or assumptions that are specific to the AMD 64 bit instruction set. 
        /// </summary>
        internal bool RequiresAmdInstructionSet => Machine == Machine.Amd64;

        /// <summary>
        /// If set, the module contains instructions that assume a 32 bit instruction set. For example it may depend on an address being 32 bits.
        /// This may be true even if the module contains only IL instructions because of PlatformInvoke and COM interop.
        /// </summary>
        internal bool Requires32bits => Machine == Machine.I386;

        /// <summary>
        /// If set, the module contains instructions that assume a 64 bit instruction set. For example it may depend on an address being 64 bits.
        /// This may be true even if the module contains only IL instructions because of PlatformInvoke and COM interop.
        /// </summary>
        internal bool Requires64bits => Machine == Machine.Amd64 || Machine == Machine.IA64;

        internal CorFlags GetCorHeaderFlags()
        {
            CorFlags result = 0;
            if (ILOnly)
            {
                result |= CorFlags.ILOnly;
            }

            if (Requires32bits)
            {
                result |= CorFlags.Requires32Bit;
            }

            if (StrongNameSigned)
            {
                result |= CorFlags.StrongNameSigned;
            }

            if (TrackDebugData)
            {
                result |= CorFlags.TrackDebugData;
            }

            if (Prefers32Bit)
            {
                result |= CorFlags.Requires32Bit | CorFlags.Prefers32Bit;
            }

            return result;
        }
    }
}
