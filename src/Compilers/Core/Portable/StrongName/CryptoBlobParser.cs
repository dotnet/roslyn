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
                var sigAlgId = blobReader.ReadUInt32();
                // Hash algorithm ID
                var hashAlgId = blobReader.ReadUInt32();
                // Size of public key data in bytes, not including the header
                var publicKeySize = blobReader.ReadUInt32();
                // publicKeySize bytes of public key data
                var publicKey = blobReader.ReadByte();

                // The number of public key bytes must be the same as the size of the header plus the size of the public key data.
                if (blob.Length != s_publicKeyHeaderSize + publicKeySize)
                {
                    return false;
                }

                // Check for the ECMA key, which does not obey the invariants checked below.
                if (ByteSequenceComparer.Equals(blob, s_ecmaKey))
                {
                    return true;
                }

                // The public key must be in the wincrypto PUBLICKEYBLOB format
                if (publicKey != PublicKeyBlobId)
                {
                    return false;
                }

                var signatureAlgorithmId = new AlgorithmId(sigAlgId);
                if (signatureAlgorithmId.IsSet && signatureAlgorithmId.Class != AlgorithmClass.Signature)
                {
                    return false;
                }

                var hashAlgorithmId = new AlgorithmId(hashAlgId);
                if (hashAlgorithmId.IsSet && (hashAlgorithmId.Class != AlgorithmClass.Hash || hashAlgorithmId.SubId < AlgorithmSubId.Sha1Hash))
                {
                    return false;
                }
            }

            return true;
        }

        private const int BlobHeaderSize = sizeof(byte) + sizeof(byte) + sizeof(ushort) + sizeof(uint);

        private const int RsaPubKeySize = sizeof(uint) + sizeof(uint) + sizeof(uint);

        private const UInt32 RSA1 = 0x31415352;
        private const UInt32 RSA2 = 0x32415352;

        // In wincrypt.h both public and private key blobs start with a
        // PUBLICKEYSTRUC and RSAPUBKEY and then start the key data
        private const int s_offsetToKeyData = BlobHeaderSize + RsaPubKeySize;

        private static ImmutableArray<byte> CreateSnPublicKeyBlob(byte type, byte version, ushort reserved, uint algId, uint magic, uint bitLen, uint pubExp, byte[] pubKeyData)
        {
            var w = new BlobWriter(3 * sizeof(uint) + s_offsetToKeyData + pubKeyData.Length);
            w.WriteUInt32(AlgorithmId.RsaSign);
            w.WriteUInt32(AlgorithmId.Sha);
            w.WriteUInt32((uint)(s_offsetToKeyData + pubKeyData.Length));

            w.WriteByte(type);
            w.WriteByte(version);
            w.WriteUInt16(reserved);
            w.WriteUInt32(algId);

            w.WriteUInt32(magic);
            w.WriteUInt32(bitLen);
            w.WriteUInt32(pubExp);

            w.WriteBytes(pubKeyData);

            return w.ToImmutableArray();
        }

        /// <summary>
        /// Try to retrieve the public key from a crypto blob.
        /// </summary>
        /// <remarks>
        /// Can be either a PUBLICKEYBLOB or PRIVATEKEYBLOB. The BLOB must be unencrypted.
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

                // Header (corresponds to PUBLICKEYSTRUC struct in wincrypt.h)
                var type = blobReader.ReadByte();
                var version = blobReader.ReadByte();
                var reserved = blobReader.ReadUInt16();
                var algId = blobReader.ReadUInt32();

                // Info (corresponds to RSAPUBKEY struct in wincrypt.h)
                var magic = blobReader.ReadUInt32();
                var bitLen = blobReader.ReadUInt32();
                var pubExp = blobReader.ReadUInt32();

                var modulusLength = (int)(bitLen >> 3);
                // The key blob data just contains the modulus
                if (blob.Length - s_offsetToKeyData < modulusLength)
                {
                    return false;
                }

                byte[] pubKeyData = blobReader.ReadBytes(modulusLength);

                // The RSA magic key must match the blob id
                if (type == PrivateKeyBlobId && magic == RSA2)
                {
                    publicKey = CreateSnPublicKeyBlob(PublicKeyBlobId, version, 0, AlgorithmId.RsaSign, RSA1, bitLen, pubExp, pubKeyData);
                    return true;
                }

                if (type == PublicKeyBlobId && magic == RSA1)
                {
                    publicKey = CreateSnPublicKeyBlob(type, version, reserved, algId, magic, bitLen, pubExp, pubKeyData);
                    return true;
                }
            }

            return false;
        }
    }
}
