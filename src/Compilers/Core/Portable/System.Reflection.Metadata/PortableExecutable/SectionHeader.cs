// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.PortableExecutable;

namespace Microsoft.Cci
{
    // TODO: merge with System.Reflection.PortableExecutable.SectionHeader
    internal sealed class SectionHeader
    {
        internal readonly string Name;
        internal readonly int VirtualSize;
        internal readonly int RelativeVirtualAddress;
        internal readonly int SizeOfRawData;
        internal readonly int PointerToRawData;
        internal readonly int PointerToRelocations;
        internal readonly int PointerToLinenumbers;
        internal readonly ushort NumberOfRelocations;
        internal readonly ushort NumberOfLinenumbers;
        internal readonly SectionCharacteristics Characteristics;

        public SectionHeader(
            string name,
            int virtualSize,
            int relativeVirtualAddress,
            int sizeOfRawData,
            int pointerToRawData,
            int pointerToRelocations,
            int pointerToLinenumbers,
            ushort numberOfRelocations,
            ushort numberOfLinenumbers,
            SectionCharacteristics characteristics)
        {
            Name = name;
            VirtualSize = virtualSize;
            RelativeVirtualAddress = relativeVirtualAddress;
            SizeOfRawData = sizeOfRawData;
            PointerToRawData = pointerToRawData;
            PointerToRelocations = pointerToRelocations;
            PointerToLinenumbers = pointerToLinenumbers;
            NumberOfRelocations = numberOfRelocations;
            NumberOfLinenumbers = numberOfLinenumbers;
            Characteristics = characteristics;
        }
    }
}
