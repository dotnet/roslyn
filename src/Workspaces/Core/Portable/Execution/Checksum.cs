// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Checksum of data can be used later to see whether two data are same or not
    /// without actually comparing data itself
    /// </summary>
    internal sealed partial class Checksum : IObjectWritable, IEquatable<Checksum>
    {
        public static readonly Checksum Null = new Checksum(ImmutableArray<byte>.Empty);

        private readonly ImmutableArray<byte> _checkSum;
        private int _lazyHash;

        public Checksum(byte[] checksum) :
            this(ImmutableArray.Create(checksum))
        {
        }

        public Checksum(ImmutableArray<byte> checksum)
        {
            // 0 means it is not initialized
            _lazyHash = 0;

            _checkSum = checksum;
        }

        public byte[] ToArray()
        {
            // TODO: think a way to make this better
            return _checkSum.ToArray();
        }

        public bool Equals(Checksum other)
        {
            if (other == null)
            {
                return false;
            }

            if (_checkSum.Length != other._checkSum.Length)
            {
                return false;
            }

            for (var i = 0; i < _checkSum.Length; i++)
            {
                if (_checkSum[i] != other._checkSum[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Checksum);
        }

        public override int GetHashCode()
        {
            if (_lazyHash == 0)
            {
                _lazyHash = CalculateHashCode();
            }

            return _lazyHash;
        }

        public override string ToString()
        {
            return Convert.ToBase64String(ToArray());
        }

        private int CalculateHashCode()
        {
            // lazily calculate hash for checksum
            var hash = _checkSum.Length;

            for (var i = 0; i < _checkSum.Length; i++)
            {
                hash = Hash.Combine((int)_checkSum[i], hash);
            }

            // make sure we never return 0
            return hash == 0 ? 1 : hash;
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
        {
            writer.WriteInt32(_checkSum.Length);

            for (var i = 0; i < _checkSum.Length; i++)
            {
                writer.WriteByte(_checkSum[i]);
            }
        }

        public static Checksum ReadFrom(ObjectReader reader)
        {
            var length = reader.ReadInt32();
            var builder = ImmutableArray.CreateBuilder<byte>(length);

            for (var i = 0; i < length; i++)
            {
                builder.Add(reader.ReadByte());
            }

            return new Checksum(builder.MoveToImmutable());
        }

        public static string GetChecksumLogInfo(Checksum checksum)
        {
            return checksum.ToString();
        }

        public static string GetChecksumsLogInfo(IEnumerable<Checksum> checksums)
        {
            return string.Join("|", checksums.Select(c => c.ToString()));
        }
    }
}
