// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if SRM
namespace System.Reflection.PortableExecutable
#else
namespace Roslyn.Reflection.PortableExecutable
#endif
{
#if SRM
    public
#endif
    struct PESectionLocation
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
