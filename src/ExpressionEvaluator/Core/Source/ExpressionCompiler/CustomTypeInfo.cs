// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using System;
using System.Collections.ObjectModel;
using System.Text;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class CustomTypeInfo
    {
        internal static readonly Guid PayloadTypeId = new Guid("108766CE-DF68-46EE-B761-0DCB7AC805F1");

        internal static DkmClrCustomTypeInfo Create(
            ReadOnlyCollection<byte> dynamicFlags,
            ReadOnlyCollection<string> tupleElementNames)
        {
            var payload = Encode(dynamicFlags, tupleElementNames);
            return (payload == null) ? null : DkmClrCustomTypeInfo.Create(PayloadTypeId, payload);
        }

        /// <summary>
        /// Return a copy of the custom type info without tuple element names.
        /// </summary>
        internal static DkmClrCustomTypeInfo WithNoTupleElementNames(this DkmClrCustomTypeInfo typeInfo)
        {
            if ((typeInfo == null) || (typeInfo.Payload == null) || typeInfo.PayloadTypeId != PayloadTypeId)
            {
                return typeInfo;
            }

            var payload = typeInfo.Payload;
            int length = payload[0] + 1;
            if (length == payload.Count)
            {
                return typeInfo;
            }

            return DkmClrCustomTypeInfo.Create(PayloadTypeId, new ReadOnlyCollection<byte>(CopyBytes(payload, 0, length)));
        }

        /// <summary>
        /// Return a copy of the custom type info with the leading dynamic flag removed.
        /// There are no changes to tuple element names since this is used for walking
        /// into an array element type only which does not affect tuple element names.
        /// </summary>
        internal static DkmClrCustomTypeInfo SkipOne(DkmClrCustomTypeInfo customInfo)
        {
            if (customInfo == null)
            {
                return customInfo;
            }

            ReadOnlyCollection<byte> dynamicFlags;
            ReadOnlyCollection<string> tupleElementNames;
            CustomTypeInfo.Decode(
                customInfo.PayloadTypeId,
                customInfo.Payload,
                out dynamicFlags,
                out tupleElementNames);

            if (dynamicFlags == null)
            {
                return customInfo;
            }

            return Create(DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlags), tupleElementNames);
        }

        internal static string GetTupleElementNameIfAny(ReadOnlyCollection<string> tupleElementNames, int index)
        {
            return tupleElementNames?[index];
        }

        // Encode in payload as a sequence of bytes {count}{dynamicFlags}{tupleNames}
        // where {count} is a byte of the number of bytes in {dynamicFlags} (max: 8*256 bits)
        // and {tupleNames} is a UTF8 encoded string of the names separated by '|'.
        internal static ReadOnlyCollection<byte> Encode(
            ReadOnlyCollection<byte> dynamicFlags,
            ReadOnlyCollection<string> tupleElementNames)
        {
            if ((dynamicFlags == null) && (tupleElementNames == null))
            {
                return null;
            }

            var builder = ArrayBuilder<byte>.GetInstance();
            if (dynamicFlags == null)
            {
                builder.Add(0);
            }
            else
            {
                int length = dynamicFlags.Count;
                if (length > byte.MaxValue)
                {
                    // Length exceeds capacity of byte.
                    builder.Free();
                    return null;
                }
                builder.Add((byte)length);
                builder.AddRange(dynamicFlags);
            }

            if (tupleElementNames != null)
            {
                var bytes = EncodeNames(tupleElementNames);
                builder.AddRange(bytes);
            }

            return new ReadOnlyCollection<byte>(builder.ToArrayAndFree());
        }

        internal static void Decode(
            Guid payloadTypeId,
            ReadOnlyCollection<byte> payload,
            out ReadOnlyCollection<byte> dynamicFlags,
            out ReadOnlyCollection<string> tupleElementNames)
        {
            dynamicFlags = null;
            tupleElementNames = null;

            if ((payload == null) || (payloadTypeId != PayloadTypeId))
            {
                return;
            }

            int length = payload[0];
            if (length > 0)
            {
                dynamicFlags = new ReadOnlyCollection<byte>(CopyBytes(payload, 1, length));
            }

            int start = length + 1;
            if (start < payload.Count)
            {
                tupleElementNames = DecodeNames(payload, start);
            }
        }

        private const char NameSeparator = '|';

        private static ReadOnlyCollection<byte> EncodeNames(ReadOnlyCollection<string> names)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            bool any = false;
            foreach (var name in names)
            {
                if (any)
                {
                    builder.Append(NameSeparator);
                }
                if (name != null)
                {
                    builder.Append(name);
                }
                any = true;
            }
            var str = pooledBuilder.ToStringAndFree();
            return new ReadOnlyCollection<byte>(Encoding.UTF8.GetBytes(str));
        }

        private static ReadOnlyCollection<string> DecodeNames(ReadOnlyCollection<byte> bytes, int start)
        {
            int length = bytes.Count - start;
            var array = CopyBytes(bytes, start, length);
            var str = Encoding.UTF8.GetString(array, 0, length);
            return new ReadOnlyCollection<string>(NullNotEmpty(str.Split(NameSeparator)));
        }

        private static byte[] CopyBytes(ReadOnlyCollection<byte> bytes, int start, int length)
        {
            var array = new byte[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = bytes[start + i];
            }
            return array;
        }

        private static string[] NullNotEmpty(string[] names)
        {
            var builder = ArrayBuilder<string>.GetInstance(names.Length);
            bool hasNull = false;
            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name))
                {
                    hasNull = true;
                    builder.Add(null);
                }
                else
                {
                    builder.Add(name);
                }
            }
            if (hasNull)
            {
                names = builder.ToArray();
            }
            builder.Free();
            return names;
        }
    }
}
