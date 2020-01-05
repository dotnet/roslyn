// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Checksum of data can be used later to see whether two data are same or not
    /// without actually comparing data itself
    /// </summary>
    internal sealed partial class Checksum : IObjectWritable, IEquatable<Checksum>
    {
        /// <summary>
        /// The intended size of the <see cref="HashData"/> structure. 
        /// </summary>
        private const int HashSize = 20;

        public static readonly Checksum Null = new Checksum(default);

        private readonly HashData _checksum;

        /// <summary>
        /// Create Checksum from given byte array. if byte array is bigger than
        /// <see cref="HashSize"/>, it will be truncated to the size
        /// </summary>
        public static Checksum From(byte[] checksum)
        {
            if (checksum.Length == 0)
            {
                return Null;
            }

            if (checksum.Length < HashSize)
            {
                throw new ArgumentException($"checksum must be equal or bigger than the hash size: {HashSize}", nameof(checksum));
            }

            return FromWorker(checksum);
        }

        /// <summary>
        /// Create Checksum from given byte array. if byte array is bigger than
        /// <see cref="HashSize"/>, it will be truncated to the size
        /// </summary>
        public static Checksum From(ImmutableArray<byte> checksum)
        {
            if (checksum.Length == 0)
            {
                return Null;
            }

            if (checksum.Length < HashSize)
            {
                throw new ArgumentException($"{nameof(checksum)} must be equal or bigger than the hash size: {HashSize}", nameof(checksum));
            }

            using var pooled = SharedPools.ByteArray.GetPooledObject();
            var bytes = pooled.Object;
            checksum.CopyTo(sourceIndex: 0, bytes, destinationIndex: 0, length: HashSize);

            return FromWorker(bytes);
        }

        public static Checksum FromSerialized(byte[] checksum)
        {
            if (checksum.Length == 0)
            {
                return Null;
            }

            if (checksum.Length != HashSize)
            {
                throw new ArgumentException($"{nameof(checksum)} must be equal to the hash size: {HashSize}", nameof(checksum));
            }

            return FromWorker(checksum);
        }

        private static unsafe Checksum FromWorker(byte[] checksum)
        {
            fixed (byte* data = checksum)
            {
                // Avoid a direct dereferencing assignment since sizeof(HashData) may be greater than HashSize.
                //
                // ex) "https://bugzilla.xamarin.com/show_bug.cgi?id=60298" - LayoutKind.Explicit, Size = 12 ignored with 64bit alignment
                // or  "https://github.com/dotnet/roslyn/issues/23722" - Checksum throws on Mono 64-bit
                return new Checksum(HashData.FromPointer((HashData*)data));
            }
        }

        private Checksum(HashData hash)
        {
            _checksum = hash;
        }

        public bool Equals(Checksum other)
        {
            if (other == null)
            {
                return false;
            }

            return _checksum == other._checksum;
        }

        public override bool Equals(object obj)
            => Equals(obj as Checksum);

        public override int GetHashCode()
            => _checksum.GetHashCode();

        public override unsafe string ToString()
        {
            var data = new byte[sizeof(HashData)];
            fixed (byte* dataPtr = data)
            {
                *(HashData*)dataPtr = _checksum;
            }

            return Convert.ToBase64String(data, 0, HashSize);
        }

        public static bool operator ==(Checksum left, Checksum right)
        {
            return EqualityComparer<Checksum>.Default.Equals(left, right);
        }

        public static bool operator !=(Checksum left, Checksum right)
        {
            return !(left == right);
        }

        bool IObjectWritable.ShouldReuseInSerialization => true;

        public void WriteTo(ObjectWriter writer)
            => _checksum.WriteTo(writer);

        public static Checksum ReadFrom(ObjectReader reader)
            => new Checksum(HashData.ReadFrom(reader));

        public static string GetChecksumLogInfo(Checksum checksum)
        {
            return checksum.ToString();
        }

        public static string GetChecksumsLogInfo(IEnumerable<Checksum> checksums)
        {
            return string.Join("|", checksums.Select(c => c.ToString()));
        }

        /// <summary>
        /// This structure stores the 20-byte hash as an inline value rather than requiring the use of
        /// <c>byte[]</c>.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = HashSize)]
        private struct HashData : IEquatable<HashData>
        {
            [FieldOffset(0)]
            private long Data1;

            [FieldOffset(8)]
            private long Data2;

            [FieldOffset(16)]
            private int Data3;

            public static bool operator ==(HashData x, HashData y)
                => x.Equals(y);

            public static bool operator !=(HashData x, HashData y)
                => !x.Equals(y);

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt64(Data1);
                writer.WriteInt64(Data2);
                writer.WriteInt32(Data3);
            }

            public static unsafe HashData FromPointer(HashData* hash)
            {
                HashData result = default;
                result.Data1 = hash->Data1;
                result.Data2 = hash->Data2;
                result.Data3 = hash->Data3;
                return result;
            }

            public static HashData ReadFrom(ObjectReader reader)
            {
                HashData result = default;
                result.Data1 = reader.ReadInt64();
                result.Data2 = reader.ReadInt64();
                result.Data3 = reader.ReadInt32();
                return result;
            }

            public override int GetHashCode()
            {
                // The checksum is already a hash. Just read a 4-byte value to get a well-distributed hash code.
                return (int)Data1;
            }

            public override bool Equals(object obj)
                => obj is HashData other && Equals(other);

            public bool Equals(HashData other)
            {
                return Data1 == other.Data1
                    && Data2 == other.Data2
                    && Data3 == other.Data3;
            }
        }
    }
}
