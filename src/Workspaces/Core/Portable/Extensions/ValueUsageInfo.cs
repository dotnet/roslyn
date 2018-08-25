// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    [Flags]
    internal enum ValueUsageInfo
    {
        None = 0x0000,
        Read = 0x0001,
        Write = 0x0010,
        ReadableRef = 0x0100,
        WritableRef = 0x1000,

        ReadWrite = Read | Write,
        ReadableWritableRef = ReadableRef | WritableRef
    }

    internal static class ValueUsageInfoExtensions
    {
        public static bool ContainsReadOrReadableRef(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & (ValueUsageInfo.Read | ValueUsageInfo.ReadableRef)) != 0;

        public static bool ContainsWriteOrWritableRef(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & (ValueUsageInfo.Write | ValueUsageInfo.WritableRef)) != 0;
    }
}
