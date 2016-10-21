// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Utilities
{
    internal sealed partial class StreamObjectWriter
    {
        internal enum DataKind : byte
        {
            Null,
            Type,
            TypeRef,      // type ref id as 4 bytes 
            TypeRef_B,    // type ref id as 1 byte
            TypeRef_S,    // type ref id as 2 bytes
            Object_W,     // IObjectWritable
            ObjectRef,    // object ref id as 4 bytes
            ObjectRef_B,  // object ref id as 1 byte
            ObjectRef_S,  // object ref id as 2 bytes
            StringUtf8,   // string in UTF8 encoding
            StringUtf16,  // string in UTF16 encoding
            StringRef,    // string ref id as 4-bytes
            StringRef_B,  // string ref id as 1-byte
            StringRef_S,  // string ref id as 2-bytes
            Boolean_T,    // boolean true
            Boolean_F,    // boolean false
            Char,
            Int8,
            Int16,
            Int32,        // int encoded as 4 bytes
            Int32_B,      // int encoded as 1 byte
            Int32_S,      // int encoded as 2 bytes
            Int32_0,      // int 0
            Int32_1,      // int 1
            Int32_2,      // int 2
            Int32_3,      // int 3,
            Int32_4,      // int 4,
            Int32_5,      // int 5,
            Int32_6,      // int 6,
            Int32_7,      // int 7,
            Int32_8,      // int 8,
            Int32_9,      // int 9,
            Int32_10,     // int 10,
            Int64,
            UInt8,
            UInt16,
            UInt32,       // uint encoded as 4 bytes
            UInt32_B,     // uint encoded as 1 byte
            UInt32_S,     // uint encoded as 2 bytes
            UInt32_0,     // uint 0
            UInt32_1,     // uint 1
            UInt32_2,     // uint 2
            UInt32_3,     // uint 3,
            UInt32_4,     // uint 4,
            UInt32_5,     // uint 5,
            UInt32_6,     // uint 6,
            UInt32_7,     // uint 7,
            UInt32_8,     // uint 8,
            UInt32_9,     // uint 9,
            UInt32_10,    // uint 10,
            UInt64,
            Float4,
            Float8,
            Decimal,
            DateTime,
            Enum,

            Array,      // array with # elements encoded as compressed int
            Array_0,    // array with zero elements
            Array_1,    // array with one element
            Array_2,    // array with two elements
            Array_3,    // array with three elements

            ValueArray,     // value array with # elements encoded as a compressed int
            ValueArray_0,
            ValueArray_1,
            ValueArray_2,
            ValueArray_3,

            BooleanType,    // boolean type marker
            StringType      // string type marker
        }
    }
}