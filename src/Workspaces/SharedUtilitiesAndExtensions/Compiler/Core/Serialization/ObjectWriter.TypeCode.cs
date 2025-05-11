// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis;
using EncodingExtensions = Microsoft.CodeAnalysis.EncodingExtensions;

namespace Roslyn.Utilities;

internal sealed partial class ObjectWriter
{
    internal enum TypeCode : byte
    {
        /// <summary>
        /// The null value
        /// </summary>
        Null,

        /// <summary>
        /// A string encoded as UTF-8 (using BinaryWriter.Write(string))
        /// </summary>
        StringUtf8,

        /// <summary>
        /// A string encoded as UTF16 (as array of UInt16 values)
        /// </summary>
        StringUtf16,

        /// <summary>
        /// A reference to a string with the id encoded as 1 byte.
        /// </summary>
        StringRef_1Byte,

        /// <summary>
        /// A reference to a string with the id encoded as 2 bytes.
        /// </summary>
        StringRef_2Bytes,

        /// <summary>
        /// A reference to a string with the id encoded as 4 bytes.
        /// </summary>
        StringRef_4Bytes,

        /// <summary>
        /// The boolean value true.
        /// </summary>
        Boolean_True,

        /// <summary>
        /// The boolean value char.
        /// </summary>
        Boolean_False,

        /// <summary>
        /// A character value encoded as 2 bytes.
        /// </summary>
        Char,

        /// <summary>
        /// An Int8 value encoded as 1 byte.
        /// </summary>
        Int8,

        /// <summary>
        /// An Int16 value encoded as 2 bytes.
        /// </summary>
        Int16,

        /// <summary>
        /// An Int32 value encoded as 4 bytes.
        /// </summary>
        Int32,

        /// <summary>
        /// An Int32 value encoded as 1 byte.
        /// </summary>
        Int32_1Byte,

        /// <summary>
        /// An Int32 value encoded as 2 bytes.
        /// </summary>
        Int32_2Bytes,

        /// <summary>
        /// The Int32 value 0
        /// </summary>
        Int32_0,

        /// <summary>
        /// The Int32 value 1
        /// </summary>
        Int32_1,

        /// <summary>
        /// The Int32 value 2
        /// </summary>
        Int32_2,

        /// <summary>
        /// The Int32 value 3
        /// </summary>
        Int32_3,

        /// <summary>
        /// The Int32 value 4
        /// </summary>
        Int32_4,

        /// <summary>
        /// The Int32 value 5
        /// </summary>
        Int32_5,

        /// <summary>
        /// The Int32 value 6
        /// </summary>
        Int32_6,

        /// <summary>
        /// The Int32 value 7
        /// </summary>
        Int32_7,

        /// <summary>
        /// The Int32 value 8
        /// </summary>
        Int32_8,

        /// <summary>
        /// The Int32 value 9
        /// </summary>
        Int32_9,

        /// <summary>
        /// The Int32 value 10
        /// </summary>
        Int32_10,

        /// <summary>
        /// An Int64 value encoded as 8 bytes
        /// </summary>
        Int64,

        /// <summary>
        /// A UInt8 value encoded as 1 byte.
        /// </summary>
        UInt8,

        /// <summary>
        /// A UIn16 value encoded as 2 bytes.
        /// </summary>
        UInt16,

        /// <summary>
        /// A UInt32 value encoded as 4 bytes.
        /// </summary>
        UInt32,

        /// <summary>
        /// A UInt32 value encoded as 1 byte.
        /// </summary>
        UInt32_1Byte,

        /// <summary>
        /// A UInt32 value encoded as 2 bytes.
        /// </summary>
        UInt32_2Bytes,

        /// <summary>
        /// The UInt32 value 0
        /// </summary>
        UInt32_0,

        /// <summary>
        /// The UInt32 value 1
        /// </summary>
        UInt32_1,

        /// <summary>
        /// The UInt32 value 2
        /// </summary>
        UInt32_2,

        /// <summary>
        /// The UInt32 value 3
        /// </summary>
        UInt32_3,

        /// <summary>
        /// The UInt32 value 4
        /// </summary>
        UInt32_4,

        /// <summary>
        /// The UInt32 value 5
        /// </summary>
        UInt32_5,

        /// <summary>
        /// The UInt32 value 6
        /// </summary>
        UInt32_6,

        /// <summary>
        /// The UInt32 value 7
        /// </summary>
        UInt32_7,

        /// <summary>
        /// The UInt32 value 8
        /// </summary>
        UInt32_8,

        /// <summary>
        /// The UInt32 value 9
        /// </summary>
        UInt32_9,

        /// <summary>
        /// The UInt32 value 10
        /// </summary>
        UInt32_10,

        /// <summary>
        /// A UInt64 value encoded as 8 bytes.
        /// </summary>
        UInt64,

        /// <summary>
        /// A float value encoded as 4 bytes.
        /// </summary>
        Float4,

        /// <summary>
        /// A double value encoded as 8 bytes.
        /// </summary>
        Float8,

        /// <summary>
        /// A decimal value encoded as 12 bytes.
        /// </summary>
        Decimal,

        /// <summary>
        /// A DateTime value
        /// </summary>
        DateTime,

        /// <summary>
        /// An array with length encoded as compressed uint
        /// </summary>
        Array,

        /// <summary>
        /// An array with zero elements
        /// </summary>
        Array_0,

        /// <summary>
        /// An array with one element
        /// </summary>
        Array_1,

        /// <summary>
        /// An array with 2 elements
        /// </summary>
        Array_2,

        /// <summary>
        /// An array with 3 elements
        /// </summary>
        Array_3,

        /// <summary>
        /// Encoding serialized as <see cref="Encoding.WebName"/>.
        /// </summary>
        EncodingName,

        /// <summary>
        /// Encoding serialized as <see cref="TextEncodingKind"/>.
        /// </summary>
        FirstWellKnownTextEncoding,
        LastWellKnownTextEncoding = FirstWellKnownTextEncoding + EncodingExtensions.LastTextEncodingKind - EncodingExtensions.FirstTextEncodingKind,

        /// <summary>
        /// Encoding serialized as <see cref="Encoding.CodePage"/>.
        /// </summary>
        EncodingCodePage,
    }
}
