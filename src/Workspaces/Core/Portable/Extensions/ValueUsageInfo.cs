// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    [Flags]
    internal enum ValueUsageInfo
    {
        None = 0x00000,
        Read = 0x00001,
        Write = 0x00010,
        ReadableRef = 0x00100,
        WritableRef = 0x01000,
        NonReadWriteRef = 0x10000,

        ReadWrite = Read | Write,
        ReadableWritableRef = ReadableRef | WritableRef
    }

    internal static class ValueUsageInfoExtensions
    {
        public static bool ContainsReadOrReadableRef(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & (ValueUsageInfo.Read | ValueUsageInfo.ReadableRef)) != 0;

        public static bool ContainsWriteOrWritableRef(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & (ValueUsageInfo.Write | ValueUsageInfo.WritableRef)) != 0;

        public static bool ContainsNonReadWriteRef(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & ValueUsageInfo.NonReadWriteRef) != 0;
    }
}
