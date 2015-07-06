// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// This class is used to store the module serialization properties for a compilation.
    /// </summary>
    internal sealed class ModulePropertiesForSerialization
    {
        public readonly ushort FileAlignment;
        public readonly string TargetRuntimeVersion;
        public readonly Platform Platform;
        public readonly byte MetadataFormatMajorVersion = 2;
        public readonly byte MetadataFormatMinorVersion;
        public readonly Guid PersistentIdentifier;
        public readonly bool ILOnly = true;
        public readonly bool TrackDebugData;
        public readonly ulong BaseAddress;
        public readonly ulong SizeOfHeapReserve;
        public readonly ulong SizeOfHeapCommit;
        public readonly ulong SizeOfStackReserve;
        public readonly ulong SizeOfStackCommit;
        public readonly bool EnableHighEntropyVA;
        public readonly bool StrongNameSigned;
        public readonly bool ConfigureToExecuteInAppContainer;
        public readonly SubsystemVersion SubsystemVersion;

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

        public const ushort DefaultFileAlignment32Bit = 0x00000200;
        public const ushort DefaultFileAlignment64Bit = 0x00000200; //both 32 and 64 bit binaries used this value in the native stack.

        public const Platform DefaultPlatform = Platform.AnyCpu;

        internal ModulePropertiesForSerialization(
            Guid persistentIdentifier,
            ushort fileAlignment,
            string targetRuntimeVersion,
            Platform platform,
            bool trackDebugData,
            ulong baseAddress,
            ulong sizeOfHeapReserve,
            ulong sizeOfHeapCommit,
            ulong sizeOfStackReserve,
            ulong sizeOfStackCommit,
            bool enableHighEntropyVA,
            bool strongNameSigned,
            bool configureToExecuteInAppContainer,
            SubsystemVersion subsystemVersion)
        {
            this.PersistentIdentifier = persistentIdentifier;
            this.FileAlignment = fileAlignment;
            this.TargetRuntimeVersion = targetRuntimeVersion;
            this.Platform = platform;
            this.TrackDebugData = trackDebugData;
            this.BaseAddress = baseAddress;
            this.SizeOfHeapReserve = sizeOfHeapReserve;
            this.SizeOfHeapCommit = sizeOfHeapCommit;
            this.SizeOfStackReserve = sizeOfStackReserve;
            this.SizeOfStackCommit = sizeOfStackCommit;
            this.EnableHighEntropyVA = enableHighEntropyVA;
            this.StrongNameSigned = strongNameSigned;
            this.ConfigureToExecuteInAppContainer = configureToExecuteInAppContainer;
            this.SubsystemVersion = subsystemVersion.Equals(SubsystemVersion.None)
                ? SubsystemVersion.Default(OutputKind.ConsoleApplication, this.Platform)
                : subsystemVersion;
        }

        internal DllCharacteristics DllCharacteristics
        {
            get
            {
                var result =
                    DllCharacteristics.DynamicBase |
                    DllCharacteristics.NxCompatible |
                    DllCharacteristics.NoSeh |
                    DllCharacteristics.TerminalServerAware;

                if (EnableHighEntropyVA)
                {
                    // IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA
                    result |= (DllCharacteristics)0x0020;
                }

                if (ConfigureToExecuteInAppContainer)
                {
                    result |= DllCharacteristics.AppContainer;
                }

                return result;
            }
        }

        internal Machine Machine
        {
            get
            {
                switch (Platform)
                {
                    case Platform.Arm:
                        return Machine.ArmThumb2;
                    case Platform.X64:
                        return Machine.Amd64;
                    case Platform.Itanium:
                        return Machine.IA64;
                    default:
                        return Machine.I386;
                }
            }
        }

        internal bool RequiresStartupStub
        {
            get
            {
                return Platform != Platform.Arm &&
                       Platform != Platform.Itanium &&
                       Platform != Platform.X64;
            }
        }
    }
}
