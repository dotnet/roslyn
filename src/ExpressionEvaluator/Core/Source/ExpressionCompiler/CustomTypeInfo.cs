// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class CustomTypeInfo
    {
        internal static readonly Guid PayloadTypeId = new Guid("108766CE-DF68-46EE-B761-0DCB7AC805F1");

        internal static DkmClrCustomTypeInfo? Create(
            ReadOnlyCollection<byte>? dynamicFlags,
            ReadOnlyCollection<string?>? tupleElementNames)
        {
            var payload = Encode(dynamicFlags, tupleElementNames);
            return (payload == null) ? null : DkmClrCustomTypeInfo.Create(PayloadTypeId, payload);
        }

        /// <summary>
        /// Return a copy of the custom type info without tuple element names.
        /// </summary>
        internal static DkmClrCustomTypeInfo? WithNoTupleElementNames(this DkmClrCustomTypeInfo typeInfo)
        {
            if (typeInfo == null || typeInfo.Payload == null || typeInfo.PayloadTypeId != PayloadTypeId)
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
        internal static DkmClrCustomTypeInfo? SkipOne(DkmClrCustomTypeInfo customInfo)
        {
            if (customInfo == null)
            {
                return customInfo;
            }

            Decode(
                customInfo.PayloadTypeId,
                customInfo.Payload,
                out var dynamicFlags,
                out var tupleElementNames);

            if (dynamicFlags == null)
            {
                return customInfo;
            }

            return Create(DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlags), tupleElementNames);
        }

        internal static string? GetTupleElementNameIfAny(ReadOnlyCollection<string> tupleElementNames, int index)
        {
            return tupleElementNames != null && index < tupleElementNames.Count
                ? tupleElementNames[index]
                : null;
        }

        // Encode in payload as a sequence of bytes {count}{dynamicFlags}{tupleNames}
        // where {count} is a byte of the number of bytes in {dynamicFlags} (max: 8*256 bits)
        // and {tupleNames} is a UTF-8 encoded string of the names each preceded by '|'.
        internal static ReadOnlyCollection<byte>? Encode(
            ReadOnlyCollection<byte>? dynamicFlags,
            ReadOnlyCollection<string?>? tupleElementNames)
        {
            if (dynamicFlags == null && tupleElementNames == null)
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
            out ReadOnlyCollection<byte>? dynamicFlags,
            out ReadOnlyCollection<string?>? tupleElementNames)
        {
            dynamicFlags = null;
            tupleElementNames = null;

            if (payload == null || payloadTypeId != PayloadTypeId)
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

        private static ReadOnlyCollection<byte> EncodeNames(ReadOnlyCollection<string?> names)
        {
            var str = JoinNames(names);
            return new ReadOnlyCollection<byte>(Encoding.UTF8.GetBytes(str));
        }

        private static ReadOnlyCollection<string?> DecodeNames(ReadOnlyCollection<byte> bytes, int start)
        {
            int length = bytes.Count - start;
            var array = CopyBytes(bytes, start, length);
            var str = Encoding.UTF8.GetString(array, 0, length);
            return SplitNames(str);
        }

        private static string JoinNames(ReadOnlyCollection<string?> names)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            foreach (var name in names)
            {
                builder.Append(NameSeparator);
                if (name != null)
                {
                    builder.Append(name);
                }
            }
            return pooledBuilder.ToStringAndFree();
        }

        private static ReadOnlyCollection<string?> SplitNames(string str)
        {
            Debug.Assert(str.Length > 0);
            Debug.Assert(str[0] == NameSeparator);

            var builder = ArrayBuilder<string?>.GetInstance();
            int offset = 1;
            while (true)
            {
                int next = str.IndexOf(NameSeparator, offset);
                var name = (next < 0) ? str.Substring(offset) : str.Substring(offset, next - offset);
                builder.Add((name.Length == 0) ? null : name);
                if (next < 0)
                {
                    break;
                }

                offset = next + 1;
            }

            return new ReadOnlyCollection<string?>(builder.ToArrayAndFree());
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
    }
}
