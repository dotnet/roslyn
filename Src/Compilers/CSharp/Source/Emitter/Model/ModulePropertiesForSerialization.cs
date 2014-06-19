using System;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal class ModulePropertiesForSerialization
    {
        public readonly uint FileAlignment = 512;
        public readonly string TargetRuntimeVersion = "v4.0.30319";
        public readonly bool Requires64Bits;
        public readonly byte MetadataFormatMajorVersion = 2;
        public readonly byte MetadataFormatMinorVersion;
        public Guid PersistentIdentifier = Guid.NewGuid();
        public readonly bool ILOnly = true;
        public readonly bool Requires32bits;
        public readonly bool TrackDebugData;
        public readonly ulong BaseAddress = 0x400000;
        public readonly ulong SizeOfHeapReserve = 0x100000;
        public readonly ulong SizeOfHeapCommit = 0x1000;
        public readonly ulong SizeOfStackReserve = 0x100000;
        public readonly ulong SizeOfStackCommit = 0x1000;
    }
}
