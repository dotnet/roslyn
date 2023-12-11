// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Checksum of data can be used later to see whether two data are same or not
    /// without actually comparing data itself
    /// </summary>
    [DataContract, StructLayout(LayoutKind.Explicit, Size = HashSize)]
    internal readonly partial record struct Checksum(
        [field: FieldOffset(0)][property: DataMember(Order = 0)] long Data1,
        [field: FieldOffset(8)][property: DataMember(Order = 1)] long Data2)
    {
        /// <summary>
        /// The intended size of the <see cref="Checksum"/> structure. 
        /// </summary>
        public const int HashSize = 16;

        /// <summary>
        /// Represents a default/null/invalid Checksum, equivalent to <c>default(Checksum)</c>.  This values contains
        /// all zeros which is considered infinitesimally unlikely to ever happen from hashing data (including when
        /// hashing null/empty/zero data inputs).
        /// </summary>
        public static readonly Checksum Null = default;

        /// <summary>
        /// Create Checksum from given byte array. if byte array is bigger than <see cref="HashSize"/>, it will be
        /// truncated to the size.
        /// </summary>
        public static Checksum From(byte[] checksum)
            => From(checksum.AsSpan());

        /// <summary>
        /// Create Checksum from given byte array. if byte array is bigger than <see cref="HashSize"/>, it will be
        /// truncated to the size.
        /// </summary>
        public static Checksum From(ImmutableArray<byte> checksum)
            => From(checksum.AsSpan());

        public static Checksum From(ReadOnlySpan<byte> checksum)
        {
            if (checksum.Length < HashSize)
                throw new ArgumentException($"checksum must be equal or bigger than the hash size: {HashSize}", nameof(checksum));

            Contract.ThrowIfFalse(MemoryMarshal.TryRead(checksum, out Checksum result));
            return result;
        }

        public string ToBase64String()
        {
#if NETCOREAPP
            Span<byte> bytes = stackalloc byte[HashSize];
            this.WriteTo(bytes);
            return Convert.ToBase64String(bytes);
#else
            var bytes = new byte[HashSize];
            this.WriteTo(bytes.AsSpan());
            return Convert.ToBase64String(bytes);
#endif
        }

        public static Checksum FromBase64String(string value)
            => From(Convert.FromBase64String(value));

        public override string ToString()
            => ToBase64String();

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteInt64(Data1);
            writer.WriteInt64(Data2);
        }

        public void WriteTo(Span<byte> span)
        {
            Contract.ThrowIfTrue(span.Length < HashSize);
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), this);
        }

        public static Checksum ReadFrom(ObjectReader reader)
            => new(reader.ReadInt64(), reader.ReadInt64());

        public static Func<Checksum, string> GetChecksumLogInfo { get; }
            = checksum => checksum.ToString();

        public static Func<IEnumerable<Checksum>, string> GetChecksumsLogInfo { get; }
            = checksums => string.Join("|", checksums.Select(c => c.ToString()));

        public static Func<ProjectStateChecksums, string> GetProjectChecksumsLogInfo { get; }
            = checksums => checksums.Checksum.ToString();

        // Explicitly implement this method as default jit for records on netfx doesn't properly devirtualize the
        // standard calls to EqualityComparer<long>.Default.Equals
        public bool Equals(Checksum other)
            => this.Data1 == other.Data1 && this.Data2 == other.Data2;

        // Directly override to any overhead that records add when hashing things like the EqualityContract
        public override int GetHashCode()
        {
            // The checksum is already a hash. Just read a 4-byte value to get a well-distributed hash code.
            return (int)Data1;
        }
    }

    internal static class ChecksumExtensions
    {
        public static void AddIfNotNullChecksum(this HashSet<Checksum> checksums, Checksum checksum)
        {
            if (checksum != Checksum.Null)
                checksums.Add(checksum);
        }
    }
}
