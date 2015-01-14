// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal enum PredefinedType
    {
        None = 0,
        Boolean = 1,
        Byte = 1 << 1,
        Char = 1 << 2,
        DateTime = 1 << 3,
        Decimal = 1 << 4,
        Double = 1 << 5,
        Int16 = 1 << 6,
        Int32 = 1 << 7,
        Int64 = 1 << 8,
        Object = 1 << 9,
        SByte = 1 << 10,
        Single = 1 << 11,
        String = 1 << 12,
        UInt16 = 1 << 13,
        UInt32 = 1 << 14,
        UInt64 = 1 << 15,
        Void = 1 << 16,
    }
}
