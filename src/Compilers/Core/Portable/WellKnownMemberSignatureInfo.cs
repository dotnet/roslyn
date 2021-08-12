// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.RuntimeMembers;

namespace Microsoft.CodeAnalysis
{
    internal class WellKnownMemberSignatureInfo
    {
        public MemberFlags MemberFlags { get; init; }
        public WellKnownType DeclaringType { get; init; }
        public int Arity { get; init; }
        public int MethodSignature { get; init; }
        public string Name { get; init; }
        public WellKnownMemberArgumentInfo ReturnType { get; init; }
        public ImmutableArray<WellKnownMemberArgumentInfo> Arguments { get; init; }

        public WellKnownMemberArgumentInfo[] ArgumentArray
        {
            init => Arguments = value.ToImmutableArray();
        }

        /// <summary>
        /// Upon set to <see langword="true"/>, sets the arguments array to an empty one.
        /// Can be used as a shortcut instead of manually initializing with <see cref="Array.Empty{T}"/>.
        /// </summary>
        public bool NoArguments
        {
            init
            {
                if (!value)
                    return;

                Arguments = ImmutableArray<WellKnownMemberArgumentInfo>.Empty;
            }
        }

        /// <summary>
        /// Determines the byte count for the well-known member as it will appear in
        /// the memory buffer when initializing the descriptors, excluding its name.
        /// </summary>
        /// <returns>The total serialized byte count that will be present in the memory buffer.</returns>
        public int GetTotalSerializedByteCount()
        {
            int declaringTypeBytes = 1 + (DeclaringType > WellKnownType.ExtSentinel ? 1 : 0);
            return 1 // MemberFlags
                 + declaringTypeBytes // DeclaringType
                 + 1 // Arity
                 + Arguments.Length
                 + ReturnType.Length
                 + Arguments.Sum(arg => arg.Length);
        }

        public void SerializeToBuffer(MemoryStream stream)
        {
            // MemberFlags
            stream.WriteByte((byte)MemberFlags);

            // DeclaringType
            if (DeclaringType > WellKnownType.ExtSentinel)
            {
                stream.WriteByte((byte)WellKnownType.ExtSentinel);
                stream.WriteByte((byte)(DeclaringType - WellKnownType.ExtSentinel));
            }
            else
            {
                stream.WriteByte((byte)DeclaringType);
            }

            // Arity
            stream.WriteByte((byte)Arity);

            // Method Signature
            stream.WriteByte((byte)Arguments.Length);

            // Return Type
            Write(stream, ReturnType.Bytes);

            // Arguments
            for (int i = 0; i < Arguments.Length; i++)
                Write(stream, Arguments[i].Bytes);
        }

        // This method exists because netstandard2.0 does not support Write(ReadOnlySpan<byte>)
        private static void Write(MemoryStream stream, Span<byte> span)
        {
            for (int i = 0; i < span.Length; i++)
                stream.WriteByte(span[i]);
        }
    }

    internal unsafe struct WellKnownMemberArgumentInfo
    {
        // Struct designed to be 8 bytes in total
        private fixed byte _bytes[7];
        private readonly byte _length;

        public int Length => _length;

        public Span<byte> Bytes
        {
            get
            {
                fixed (byte* b = _bytes)
                    return new(b, _length);
            }
        }

        private WellKnownMemberArgumentInfo(byte* bytes, int length, bool isSZArray, bool isByReference)
        {
            int start = 0;
            evaluateFlag(isByReference, SignatureTypeCode.ByReference, bytes, ref start);
            evaluateFlag(isSZArray, SignatureTypeCode.SZArray, bytes, ref start);

            _length = (byte)(length + start);

            for (int i = 0; i < length; i++)
                _bytes[start + i] = bytes[i];

            static void evaluateFlag(bool condition, SignatureTypeCode typeCode, byte* bytes, ref int start)
            {
                if (!condition)
                    return;

                bytes[start] = (byte)typeCode;
                start++;
            }
        }

        // Factory methods
        public static WellKnownMemberArgumentInfo FromSimpleSpecialType(SpecialType specialType, bool isSZArray = false, bool isByReference = false)
        {
            var bytes = new[]
            {
                (byte)SignatureTypeCode.TypeHandle,
                (byte)specialType
            };

            fixed (byte* b = bytes)
                return new(b, bytes.Length, isSZArray, isByReference);
        }
        public static WellKnownMemberArgumentInfo FromSimpleWellKnownType(WellKnownType wellKnownType, bool isSZArray = false, bool isByReference = false)
        {
            byte[] bytes;

            if (wellKnownType > WellKnownType.ExtSentinel)
            {
                bytes = new[]
                {
                    (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel,
                    (byte)(wellKnownType - WellKnownType.ExtSentinel)
                };

                fixed (byte* b = bytes)
                    return new(b, bytes.Length, isSZArray, isByReference);
            }

            bytes = new[]
            {
                (byte)SignatureTypeCode.TypeHandle,
                (byte)wellKnownType
            };

            fixed (byte* b = bytes)
                return new(b, bytes.Length, isSZArray, isByReference);
        }
        public static WellKnownMemberArgumentInfo FromGenericMethodParameter(int index, bool isSZArray = false, bool isByReference = false)
        {
            return FromIndexedTypeCode(index, SignatureTypeCode.GenericMethodParameter, isSZArray, isByReference);
        }
        public static WellKnownMemberArgumentInfo FromGenericTypeParameter(int index, bool isSZArray = false, bool isByReference = false)
        {
            return FromIndexedTypeCode(index, SignatureTypeCode.GenericTypeParameter, isSZArray, isByReference);
        }
        private static WellKnownMemberArgumentInfo FromIndexedTypeCode(int index, SignatureTypeCode typeCode, bool isSZArray, bool isByReference)
        {
            var bytes = new[]
            {
                (byte)typeCode,
                (byte)index
            };

            fixed (byte* b = bytes)
                return new(b, bytes.Length, isSZArray, isByReference);
        }
        public static WellKnownMemberArgumentInfo FromGenericTypeInstance(WellKnownMemberArgumentInfo genericType, WellKnownMemberArgumentInfo[] typeArguments, bool isSZArray = false, bool isByReference = false)
        {
            int byteCount = genericType.Length
                          + 1 // Arity
                          + typeArguments.Sum(arg => arg.Length);

            // Copy the generic type's bytes
            var bytes = new byte[byteCount];
            for (int i = 0; i < genericType.Length; i++)
                bytes[i] = genericType._bytes[i];

            // Copy arity
            bytes[genericType.Length] = (byte)typeArguments.Length;

            // Copy type arguments iteratively
            int resultIndex = genericType.Length + 1;
            for (int i = 0; i < typeArguments.Length; i++)
            {
                for (int argumentByte = 0; argumentByte < typeArguments[i].Length; argumentByte++, resultIndex++)
                    bytes[resultIndex] = typeArguments[i]._bytes[argumentByte];
            }

            fixed (byte* b = bytes)
                return new(b, byteCount, isSZArray, isByReference);
        }
    }
}
