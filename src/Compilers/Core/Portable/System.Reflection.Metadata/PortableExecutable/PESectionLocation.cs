// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace System.Reflection.PortableExecutable
{
    internal struct PESectionLocation
    {
        public int RelativeVirtualAddress { get; }
        public int PointerToRawData { get; }

        public PESectionLocation(int relativeVirtualAddress, int pointerToRawData)
        {
            RelativeVirtualAddress = relativeVirtualAddress;
            PointerToRawData = pointerToRawData;
        }
    }
}
