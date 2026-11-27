// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal readonly partial record struct Checksum
{
    internal readonly ref partial struct Builder
    {
        private const int XxHash128SizeBytes = 128 / 8;

        private static readonly XxHash128Pool s_hashPool = XxHash128Pool.Default;

        private enum TypeKind : byte
        {
            Null,
            Bool,
            Int32,
            Int64,
            String,
            Checksum,
            Byte,
            Char,
        }

        // Small (8 byte), per-thread byte array to use as a buffer for appending primitive values to the hash.
        [ThreadStatic]
        private static byte[]? s_buffer;

        private readonly XxHash128 _hash;

        public Builder()
        {
            _hash = s_hashPool.Get();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Span<byte> GetBuffer(int length = 8)
        {
            Debug.Assert(length <= 8, $"length should never be greater than 8.");

            var buffer = s_buffer ??= new byte[8];
            return buffer.AsSpan(0, length);
        }

        public Checksum FreeAndGetChecksum()
        {
            Span<byte> hash = stackalloc byte[XxHash128SizeBytes];
            _hash.GetHashAndReset(hash);
            var result = From(hash);

            s_hashPool.Return(_hash);
            return result;
        }

        private void AppendBuffer(int count)
        {
            Debug.Assert(s_buffer is not null);

            _hash.Append(s_buffer.AsSpan(0, count));
        }

        private void AppendTypeKind(TypeKind kind)
        {
            AppendByteValue((byte)kind);
        }

        private void AppendBoolValue(bool value)
        {
            AppendByteValue((byte)(value ? 1 : 0));
        }

        private void AppendByteValue(byte value)
        {
            var buffer = GetBuffer(length: sizeof(byte));
            buffer[0] = value;
            _hash.Append(buffer);
        }

        private void AppendCharValue(char value)
        {
            var buffer = GetBuffer(length: sizeof(char));
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            _hash.Append(buffer);
        }

        private void AppendInt32Value(int value)
        {
            var buffer = GetBuffer(length: sizeof(int));
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            _hash.Append(buffer);
        }

        private void AppendInt64Value(long value)
        {
            var buffer = GetBuffer(length: sizeof(long));
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            _hash.Append(buffer);
        }

        private void AppendStringValue(string value)
        {
            _hash.Append(MemoryMarshal.AsBytes(value.AsSpan()));
            _hash.Append(MemoryMarshal.AsBytes("\0".AsSpan()));
        }
        private void AppendChecksumValue(Checksum value)
        {
            AppendInt64Value(value.Data1);
            AppendInt64Value(value.Data2);
        }

        public void AppendNull()
        {
            AppendTypeKind(TypeKind.Null);
        }

        public void Append(bool value)
        {
            AppendTypeKind(TypeKind.Bool);
            AppendBoolValue(value);
        }

        public void Append(byte value)
        {
            AppendTypeKind(TypeKind.Byte);
            AppendByteValue(value);
        }

        public void Append(char value)
        {
            AppendTypeKind(TypeKind.Char);
            AppendCharValue(value);
        }

        public void Append(int value)
        {
            AppendTypeKind(TypeKind.Int32);
            AppendInt32Value(value);
        }

        public void Append(long value)
        {
            AppendTypeKind(TypeKind.Int64);
            AppendInt64Value(value);
        }

        public void Append(string? value)
        {
            if (value is null)
            {
                AppendNull();
                return;
            }

            AppendTypeKind(TypeKind.String);
            AppendStringValue(value);
        }

        public void Append(Checksum value)
        {
            AppendTypeKind(TypeKind.Checksum);
            AppendChecksumValue(value);
        }

        public void Append(object? value)
        {
            switch (value)
            {
                case null:
                    AppendNull();
                    break;

                case string s:
                    Append(s);
                    break;

                case Checksum c:
                    Append(c);
                    break;

                case bool b:
                    Append(b);
                    break;

                case int i:
                    Append(i);
                    break;

                case long l:
                    Append(l);
                    break;

                case byte b:
                    Append(b);
                    break;

                case char c:
                    Append(c);
                    break;

                default:
                    throw new ArgumentException(
                        SR.FormatUnsupported_type_0(value.GetType().FullName), nameof(value));
            }
        }
    }
}
