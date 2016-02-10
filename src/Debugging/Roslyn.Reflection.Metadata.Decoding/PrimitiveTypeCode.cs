// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;

#if SRM
namespace System.Reflection.Metadata.Decoding
#else

namespace Roslyn.Reflection.Metadata.Decoding
#endif
{
    /// <summary>
    /// Represents a primitive type found in metadata signatures.
    /// </summary>
#if SRM && FUTURE
    public
#endif
    enum PrimitiveTypeCode : byte
    {
        Boolean = SignatureTypeCode.Boolean,
        Byte = SignatureTypeCode.Byte,
        SByte = SignatureTypeCode.SByte,
        Char = SignatureTypeCode.Char,
        Single = SignatureTypeCode.Single,
        Double = SignatureTypeCode.Double,
        Int16 = SignatureTypeCode.Int16,
        Int32 = SignatureTypeCode.Int32,
        Int64 = SignatureTypeCode.Int64,
        UInt16 = SignatureTypeCode.UInt16,
        UInt32 = SignatureTypeCode.UInt32,
        UInt64 = SignatureTypeCode.UInt64,
        IntPtr = SignatureTypeCode.IntPtr,
        UIntPtr = SignatureTypeCode.UIntPtr,
        Object = SignatureTypeCode.Object,
        String = SignatureTypeCode.String,
        TypedReference = SignatureTypeCode.TypedReference,
        Void = SignatureTypeCode.Void,
    }
}
