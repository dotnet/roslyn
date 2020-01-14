// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MetadataImportFieldOffset
    {
        public int FieldDef;
        public uint Offset;
    }
}
