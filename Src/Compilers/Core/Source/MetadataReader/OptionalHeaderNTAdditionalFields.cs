using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct OptionalHeaderNTAdditionalFields
    {
        internal ulong ImageBase;
        internal int SectionAlignment;
        internal uint FileAlignment;
        internal ushort MajorOperatingSystemVersion;
        internal ushort MinorOperatingSystemVersion;
        internal ushort MajorImageVersion;
        internal ushort MinorImageVersion;
        internal ushort MajorSubsystemVersion;
        internal ushort MinorSubsystemVersion;
        internal uint Win32VersionValue;
        internal int SizeOfImage;
        internal int SizeOfHeaders;
        internal uint CheckSum;
        internal Subsystem Subsystem;
        internal DllCharacteristics DllCharacteristics;
        internal ulong SizeOfStackReserve;
        internal ulong SizeOfStackCommit;
        internal ulong SizeOfHeapReserve;
        internal ulong SizeOfHeapCommit;
        internal uint LoaderFlags;
        internal int NumberOfRvaAndSizes;
    }
}