// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// NOTE: This is a temporary internal copy of code that will be cut from System.Reflection.Metadata v1.1 and
//       ship in System.Reflection.Metadata v1.2 (with breaking changes). Remove and use the public API when
//       a v1.2 prerelease is available and code flow is such that we can start to depend on it.

using System.Reflection.Metadata;

namespace Roslyn.Reflection.Metadata.Decoding
{
    /// <summary>
    /// Represents a primitive type found in metadata signatures.
    /// </summary>
    internal enum PrimitiveTypeCode : byte
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
