// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Cci
{
    internal class SectionHeader
    {
        internal string Name;
        internal uint VirtualSize;
        internal uint RelativeVirtualAddress;
        internal uint SizeOfRawData;
        internal uint PointerToRawData;
        internal uint PointerToRelocations;
        internal uint PointerToLinenumbers;
        internal ushort NumberOfRelocations;
        internal ushort NumberOfLinenumbers;
        internal uint Characteristics;
    }
}