// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Cci
{
    // TODO: merge with System.Reflection.PortableExecutable.DirectoryEntry
    internal struct DirectoryEntry
    {
        public readonly int RelativeVirtualAddress;
        public readonly int Size;

        public DirectoryEntry(
            int relativeVirtualAddress,
            int size)
        {
            RelativeVirtualAddress = relativeVirtualAddress;
            Size = size;
        }
    }
}
