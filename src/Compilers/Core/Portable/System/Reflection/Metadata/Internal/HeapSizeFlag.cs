// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if SRM
namespace System.Reflection.Metadata.Ecma335
#else
namespace Roslyn.Reflection.Metadata.Ecma335
#endif
{
    internal enum HeapSizeFlag : byte
    {
        StringHeapLarge = 0x01, // 4 byte uint indexes used for string heap offsets
        GuidHeapLarge = 0x02,   // 4 byte uint indexes used for GUID heap offsets
        BlobHeapLarge = 0x04,   // 4 byte uint indexes used for Blob heap offsets
        EnCDeltas = 0x20,       // Indicates only EnC Deltas are present
        DeletedMarks = 0x80,    // Indicates metadata might contain items marked deleted
    }
}
