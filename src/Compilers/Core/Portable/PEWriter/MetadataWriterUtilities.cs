// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal static class MetadataWriterUtilities
    {
        public static SignatureTypeCode GetConstantTypeCode(object val)
        {
            if (val == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a zero.
                return Constants.SignatureTypeCode_Class;
            }

            Debug.Assert(!val.GetType().GetTypeInfo().IsEnum);

            // Perf: Note that JIT optimizes each expression val.GetType() == typeof(T) to a single register comparison.
            // Also the checks are sorted by commonality of the checked types.

            if (val.GetType() == typeof(int))
            {
                return SignatureTypeCode.Int32;
            }

            if (val.GetType() == typeof(string))
            {
                return SignatureTypeCode.String;
            }

            if (val.GetType() == typeof(bool))
            {
                return SignatureTypeCode.Boolean;
            }

            if (val.GetType() == typeof(char))
            {
                return SignatureTypeCode.Char;
            }

            if (val.GetType() == typeof(byte))
            {
                return SignatureTypeCode.Byte;
            }

            if (val.GetType() == typeof(long))
            {
                return SignatureTypeCode.Int64;
            }

            if (val.GetType() == typeof(double))
            {
                return SignatureTypeCode.Double;
            }

            if (val.GetType() == typeof(short))
            {
                return SignatureTypeCode.Int16;
            }

            if (val.GetType() == typeof(ushort))
            {
                return SignatureTypeCode.UInt16;
            }

            if (val.GetType() == typeof(uint))
            {
                return SignatureTypeCode.UInt32;
            }

            if (val.GetType() == typeof(sbyte))
            {
                return SignatureTypeCode.SByte;
            }

            if (val.GetType() == typeof(ulong))
            {
                return SignatureTypeCode.UInt64;
            }

            if (val.GetType() == typeof(float))
            {
                return SignatureTypeCode.Single;
            }

            throw ExceptionUtilities.UnexpectedValue(val);
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
