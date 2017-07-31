﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        public static readonly Checksum Null = new Checksum(Array.Empty<byte>());

        private Sha1Hash _checkSum;

        public unsafe Checksum(byte[] checksum)
        {
            if (checksum.Length == 0)
            {
                _checkSum = default;
                return;
            }
            else if (checksum.Length != sizeof(Sha1Hash))
            {
                throw new ArgumentException($"{nameof(checksum)} must be a SHA-1 hash", nameof(checksum));
            }

            fixed (byte* data = checksum)
            {
                _checkSum = *(Sha1Hash*)data;
            }
        }

        private Checksum(Sha1Hash hash)
        {
            _checkSum = hash;
        }

        public bool Equals(Checksum other)
        {
            if (other == null)
            {
                return false;
            }

            return _checkSum == other._checkSum;
        }

        public override bool Equals(object obj)
            => Equals(obj as Checksum);

        public override int GetHashCode()
            => _checkSum.GetHashCode();

        public override unsafe string ToString()
        {
            var data = new byte[sizeof(Sha1Hash)];
            fixed (byte* dataPtr = data)
            {
                *(Sha1Hash*)dataPtr = _checkSum;
            }

            return Convert.ToBase64String(data);
        }

        public static bool operator ==(Checksum left, Checksum right)
        {
            return EqualityComparer<Checksum>.Default.Equals(left, right);
        }

        public static bool operator !=(Checksum left, Checksum right)
        {
            return !(left == right);
        }

        public void WriteTo(ObjectWriter writer)
            => _checkSum.WriteTo(writer);

        public static Checksum ReadFrom(ObjectReader reader)
            => new Checksum(Sha1Hash.ReadFrom(reader));

        public static string GetChecksumLogInfo(Checksum checksum)
        {
            return checksum.ToString();
        }

        public static string GetChecksumsLogInfo(IEnumerable<Checksum> checksums)
        {
            return string.Join("|", checksums.Select(c => c.ToString()));
        }

        /// <summary>
        /// This structure stores the 20-byte SHA 1 hash as an inline value rather than requiring the use of
        /// <c>byte[]</c>.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 20)]
        private struct Sha1Hash : IEquatable<Sha1Hash>
        {
            [FieldOffset(0)]
            private long Data1;

            [FieldOffset(8)]
            private long Data2;

            [FieldOffset(16)]
            private int Data3;

            public static bool operator ==(Sha1Hash x, Sha1Hash y)
                => x.Equals(y);

            public static bool operator !=(Sha1Hash x, Sha1Hash y)
                => !x.Equals(y);

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt64(Data1);
                writer.WriteInt64(Data2);
                writer.WriteInt32(Data3);
            }

            public static Sha1Hash ReadFrom(ObjectReader reader)
            {
                Sha1Hash result = default;
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
                => obj is Sha1Hash other && Equals(other);

            public bool Equals(Sha1Hash other)
            {
                return Data1 == other.Data1
                    && Data2 == other.Data2
                    && Data3 == other.Data3;
            }
        }
    }
}
