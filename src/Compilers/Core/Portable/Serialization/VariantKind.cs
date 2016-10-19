// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Utilities
{
    internal enum VariantKind
    {
        None = 0,
        Null,
        Boolean,
        SByte,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Decimal,
        Float4,
        Float8,
        Char,
        String,
        DateTime,
        Object,
        BoxedEnum,
        Array,
        Type
    }
}