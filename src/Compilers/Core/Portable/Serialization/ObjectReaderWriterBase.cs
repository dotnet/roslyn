// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal class ObjectReaderWriterBase
    {
        // we have s_typeMap and s_reversedTypeMap since there is no bidirectional map in compiler
        protected static readonly ImmutableDictionary<Type, DataKind> s_typeMap = ImmutableDictionary.CreateRange<Type, DataKind>(
            new KeyValuePair<Type, DataKind>[]
            {
                KeyValuePair.Create(typeof(bool), DataKind.BooleanType),
                KeyValuePair.Create(typeof(char), DataKind.Char),
                KeyValuePair.Create(typeof(string), DataKind.StringType),
                KeyValuePair.Create(typeof(sbyte), DataKind.Int8),
                KeyValuePair.Create(typeof(short), DataKind.Int16),
                KeyValuePair.Create(typeof(int), DataKind.Int32),
                KeyValuePair.Create(typeof(long), DataKind.Int64),
                KeyValuePair.Create(typeof(byte), DataKind.UInt8),
                KeyValuePair.Create(typeof(ushort), DataKind.UInt16),
                KeyValuePair.Create(typeof(uint), DataKind.UInt32),
                KeyValuePair.Create(typeof(ulong), DataKind.UInt64),
                KeyValuePair.Create(typeof(float), DataKind.Float4),
                KeyValuePair.Create(typeof(double), DataKind.Float8),
                KeyValuePair.Create(typeof(decimal), DataKind.Decimal),
            });

        protected static readonly ImmutableDictionary<DataKind, Type> s_reverseTypeMap = s_typeMap.ToImmutableDictionary(kv => kv.Value, kv => kv.Key);

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
            Int32,        // int32 encoded as 4 bytes
            Int32_B,      // int32 encoded as 1 byte
            Int32_S,      // int32 encoded as 2 bytes
            Int32_Z,      // int32 zero
            Int64,
            UInt8,
            UInt16,
            UInt32,
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
            BooleanType,    // boolean type marker
            StringType      // string type marker
        }

        internal static readonly byte ByteMarkerMask = 3 << 6;
        internal static readonly byte Byte1Marker = 0;
        internal static readonly byte Byte2Marker = 1 << 6;
        internal static readonly byte Byte4Marker = 2 << 6;
    }
}
