// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis
{
    internal static class CryptoBlobParser
    {
        private enum AlgorithmClass
        {
            Signature = 1,
            Hash = 4,
        }

        private enum AlgorithmSubId
        {
            Sha1Hash = 4,
            MacHash = 5,
            RipeMdHash = 6,
            RipeMd160Hash = 7,
            Ssl3ShaMD5Hash = 8,
            HmacHash = 9,
            Tls1PrfHash = 10,
            HashReplacOwfHash = 11,
            Sha256Hash = 12,
            Sha384Hash = 13,
            Sha512Hash = 14,
        }

        private struct AlgorithmId
        {
            // From wincrypt.h
            private const int AlgorithmClassOffset = 13;
            private const int AlgorithmClassMask = 0x7;
            private const int AlgorithmSubIdOffset = 0;
            private const int AlgorithmSubIdMask = 0x1ff;

            private readonly uint _flags;

            public const int RsaSign = 0x00002400;
            public const int Sha = 0x00008004;

            public bool IsSet
            {
                get { return _flags != 0; }
            }

            public AlgorithmClass Class
            {
                get { return (AlgorithmClass)((_flags >> AlgorithmClassOffset) & AlgorithmClassMask); }
            }

            public AlgorithmSubId SubId
            {
                get { return (AlgorithmSubId)((_flags >> AlgorithmSubIdOffset) & AlgorithmSubIdMask); }
            }

            public AlgorithmId(uint flags)
            {
                _flags = flags;
            }
        }

        // From ECMAKey.h
        private static readonly ImmutableArray<byte> s_ecmaKey = ImmutableArray.Create(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0 });

        private const int SnPublicKeyBlobSize = 13;

        // From wincrypt.h
        private const byte PublicKeyBlobId = 0x06;
        private const byte PrivateKeyBlobId = 0x07;

        // internal for testing
        internal const int s_publicKeyHeaderSize = SnPublicKeyBlobSize - 1;

        // From StrongNameInternal.cpp
        // Checks to see if a public key is a valid instance of a PublicKeyBlob as
        // defined in StongName.h
        internal static bool IsValidPublicKey(ImmutableArray<byte> blob)
        {
            // The number of public key bytes must be at least large enough for the header and one byte of data.
            if (blob.IsDefault || blob.Length < s_publicKeyHeaderSize + 1)
            {
                return false;
            }

            return IsValidPublicKeyUnsafe(blob);
        }

        private static unsafe bool IsValidPublicKeyUnsafe(ImmutableArray<byte> blob)
        {
            fixed (byte* ptr = blob.DangerousGetUnderlyingArray())
            {
                var blobReader = new BlobReader(ptr, blob.Length);

                // Signature algorithm ID
                UInt32 SigAlgId = blobReader.ReadUInt32();
                // Hash algorithm ID
                UInt32 HashAlgId = blobReader.ReadUInt32();
                // Size of public key data in bytes, not including the header
                UInt32 PublicKeySize = blobReader.ReadUInt32();

                // The number of public key bytes must be the same as the size of the header plus the size of the public key data.
                if (blob.Length != s_publicKeyHeaderSize + PublicKeySize)
                {
                    return false;
                }

                // Check for the ECMA key, which does not obey the invariants checked below.
                if (ByteSequenceComparer.Equals(blob, s_ecmaKey))
                {
                    return true;
                }

                var signatureAlgorithmId = new AlgorithmId(SigAlgId);
                if (signatureAlgorithmId.IsSet && signatureAlgorithmId.Class != AlgorithmClass.Signature)
                {
                    return false;
                }

                var hashAlgorithmId = new AlgorithmId(HashAlgId);
                if (hashAlgorithmId.IsSet && (hashAlgorithmId.Class != AlgorithmClass.Hash || hashAlgorithmId.SubId < AlgorithmSubId.Sha1Hash))
                {
                    return false;
                }
            }

            return true;
        }

        // PUBLICKEYSTRUC struct from wincrypt.h
        private struct BlobHeader
        {
            public byte Type;       // Blob type
            public byte Version;    // Blob format version
            public UInt16 Reserved; // Must be 0
            public UInt32 AlgId;    // Algorithm ID. Must be one of ALG_ID specified in wincrypto.h
        }

        private const int BlobHeaderSize = 8;

        // RSAPUBKEY struct from wincrypt.h
        private struct RsaPubKey
        {
            public UInt32 Magic; // Indicates RSA1 or RSA2
            public UInt32 BitLen; // Number of bits in the modulus. Must be multiple of 8.
            public UInt32 PubExp; // The public exponent
        }

        private const int RsaPubKeySize = 12;

        private const UInt32 RSA1 = 0x31415352;
        private const UInt32 RSA2 = 0x32415352;

        // In wincrypt.h both public and private key blobs start with a
        // PUBLICKEYSTRUC and RSAPUBKEY and then start the key data
        private const int s_offsetToKeyData = BlobHeaderSize + RsaPubKeySize;

        private static ImmutableArray<byte> CreateSnPublicKeyBlob(ref BlobHeader header, ref RsaPubKey rsa, byte[] pubKeyData)
        {
            var w = new BlobWriter(4 + 4 + 4 + s_offsetToKeyData + pubKeyData.Length);
            w.WriteUInt32(AlgorithmId.RsaSign);
            w.WriteUInt32(AlgorithmId.Sha);
            w.WriteUInt32((uint)(s_offsetToKeyData + pubKeyData.Length));

            w.WriteByte(header.Type);
            w.WriteByte(header.Version);
            w.WriteUInt16(header.Reserved);
            w.WriteUInt32(header.AlgId);

            w.WriteUInt32(rsa.Magic);
            w.WriteUInt32(rsa.BitLen);
            w.WriteUInt32(rsa.PubExp);

            w.WriteBytes(pubKeyData);

            return w.ToImmutableArray();
        }

        private static ImmutableArray<byte> TryGetPublicKeyFromPrivateKeyBlob(BlobHeader header, RsaPubKey rsa, byte[] pubKeyData)
        {
            header.Type = PublicKeyBlobId;
            header.Reserved = 0;
            header.AlgId = AlgorithmId.RsaSign;

            rsa.Magic = RSA1;

            return CreateSnPublicKeyBlob(ref header, ref rsa, pubKeyData);
        }

        /// <summary>
        /// Try to retrieve the public key from a crypto blob.
        /// </summary>
        /// <remarks>
        /// Can be either a PUBLICKEYBLOB or PRIVATEKEYBLOB. The BLOB must /// be unencrypted.
        /// </remarks>
        public unsafe static bool TryGetPublicKey(ImmutableArray<byte> blob, out ImmutableArray<byte> publicKey)
        {
            // Is this already a strong name PublicKeyBlob?
            if (IsValidPublicKey(blob))
            {
                publicKey = blob;
                return true;
            }

            publicKey = ImmutableArray<byte>.Empty;

            // Must be at least as large as header + RSA info
            if (blob.Length < BlobHeaderSize + RsaPubKeySize)
            {
                return false;
            }

            fixed (byte* ptr = blob.DangerousGetUnderlyingArray())
            {
                var blobReader = new BlobReader(ptr, blob.Length);

                var header = new BlobHeader {
                    Type = blobReader.ReadByte(),
                    Version = blobReader.ReadByte(),
                    Reserved = blobReader.ReadUInt16(),
                    AlgId = blobReader.ReadUInt32()
                };

                var rsa = new RsaPubKey {
                    Magic = blobReader.ReadUInt32(),
                    BitLen = blobReader.ReadUInt32(),
                    PubExp = blobReader.ReadUInt32()
                };

                var modulusLength = (int)(rsa.BitLen >> 3);
                // The key blob data just contains the modulus
                if (blob.Length - s_offsetToKeyData < modulusLength)
                {
                    return false;
                }

                byte[] pubKeyData = blobReader.ReadBytes(modulusLength);

                // The RSA magic key must match the blob id
                if (header.Type == PrivateKeyBlobId &&
                    rsa.Magic == RSA2)
                {
                    publicKey = TryGetPublicKeyFromPrivateKeyBlob(header, rsa, pubKeyData);
                    return true;
                }
                else if (header.Type == PublicKeyBlobId &&
                    rsa.Magic == RSA1)
                {
                    publicKey = CreateSnPublicKeyBlob(ref header, ref rsa, pubKeyData);
                    return true;
                }
            }

            return false;
        }
    }
}
