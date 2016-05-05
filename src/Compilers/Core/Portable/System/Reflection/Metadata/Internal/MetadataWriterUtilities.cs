// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

#if SRM
using System.Reflection.Internal;
#else
using System;
using System.Reflection;
using System.Reflection.Metadata;
#endif

#if SRM
namespace System.Reflection.Metadata.Ecma335
#else
namespace Roslyn.Reflection.Metadata.Ecma335
#endif
{
    internal static class MetadataWriterUtilities
    {
        public static SignatureTypeCode GetConstantTypeCode(object value)
        {
            if (value == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a zero.
                return (SignatureTypeCode)0x12; // TODO
            }

            Debug.Assert(!value.GetType().GetTypeInfo().IsEnum);

            // Perf: Note that JIT optimizes each expression val.GetType() == typeof(T) to a single register comparison.
            // Also the checks are sorted by commonality of the checked types.

            if (value.GetType() == typeof(int))
            {
                return SignatureTypeCode.Int32;
            }

            if (value.GetType() == typeof(string))
            {
                return SignatureTypeCode.String;
            }

            if (value.GetType() == typeof(bool))
            {
                return SignatureTypeCode.Boolean;
            }

            if (value.GetType() == typeof(char))
            {
                return SignatureTypeCode.Char;
            }

            if (value.GetType() == typeof(byte))
            {
                return SignatureTypeCode.Byte;
            }

            if (value.GetType() == typeof(long))
            {
                return SignatureTypeCode.Int64;
            }

            if (value.GetType() == typeof(double))
            {
                return SignatureTypeCode.Double;
            }

            if (value.GetType() == typeof(short))
            {
                return SignatureTypeCode.Int16;
            }

            if (value.GetType() == typeof(ushort))
            {
                return SignatureTypeCode.UInt16;
            }

            if (value.GetType() == typeof(uint))
            {
                return SignatureTypeCode.UInt32;
            }

            if (value.GetType() == typeof(sbyte))
            {
                return SignatureTypeCode.SByte;
            }

            if (value.GetType() == typeof(ulong))
            {
                return SignatureTypeCode.UInt64;
            }

            if (value.GetType() == typeof(float))
            {
                return SignatureTypeCode.Single;
            }

            // TODO: localize
            throw new ArgumentException("Invalid constant type", nameof(value));
        }

        internal static void SerializeRowCounts(BlobBuilder writer, ImmutableArray<int> rowCounts)
        {
            for (int i = 0; i < rowCounts.Length; i++)
            {
                int rowCount = rowCounts[i];
                if (rowCount > 0)
                {
                    writer.WriteInt32(rowCount);
                }
            }
        }
    }
}
