using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

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

        // From strongname.h
        // Note: different from the PUBLICKEYBLOB specified in wincrypt.h. The 
        // PublicKey in the SnPublicKeyBlob contains a PUBLICKEYBLOB
        //
        // The public key blob has the following format as a little-endian packed C struct:
        //
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
#pragma warning disable 0649
        private unsafe struct SnPublicKeyBlob
        {
            public UInt32 SigAlgId;          // Signature algorithm ID
            public UInt32 HashAlgId;         // Hash algorithm ID
            public UInt32 PublicKeySize;     // Size of public key data in bytes, not including the header
            // Note: PublicKey is variable sized
            public fixed byte PublicKey[1];  // PublicKeySize bytes of public key data
        }

        // From wincrypt.h
        private const byte PublicKeyBlobId = 0x06;
        private const byte PrivateKeyBlobId = 0x07;

        // internal for testing
        internal unsafe static readonly int s_publicKeyHeaderSize = sizeof(SnPublicKeyBlob) - 1;

        private static uint ToUInt32(ImmutableArray<byte> bytes, int offset)
        {
            Debug.Assert((bytes.Length - offset) > sizeof(int));
            return (uint)(bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24));
        }

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
            var blobArray = blob.DangerousGetUnderlyingArray();
            fixed (byte* blobPtr = blobArray)
            {
                var pkb = (SnPublicKeyBlob*)blobPtr;

                // The number of public key bytes must be the same as the size of the header plus the size of the public key data.
                if (blob.Length != s_publicKeyHeaderSize + pkb->PublicKeySize)
                {
                    return false;
                }

                // Check for the ECMA key, which does not obey the invariants checked below.
                if (ByteSequenceComparer.Equals(blob, s_ecmaKey))
                {
                    return true;
                }

                // The public key must be in the wincrypto PUBLICKEYBLOB format
                if (pkb->PublicKey[0] != PublicKeyBlobId)
                {
                    return false;
                }

                var signatureAlgorithmId = new AlgorithmId(pkb->SigAlgId);
                if (signatureAlgorithmId.IsSet && signatureAlgorithmId.Class != AlgorithmClass.Signature)
                {
                    return false;
                }

                var hashAlgorithmId = new AlgorithmId(pkb->HashAlgId);
                if (hashAlgorithmId.IsSet && (hashAlgorithmId.Class != AlgorithmClass.Hash || hashAlgorithmId.SubId < AlgorithmSubId.Sha1Hash))
                {
                    return false;
                }
            }

            return true;
        }

        // PUBLICKEYSTRUC struct from wincrypt.h
        [StructLayout(LayoutKind.Sequential)]
        private struct BlobHeader
        {
            public byte Type;       // Blob type
            public byte Version;    // Blob format version
            public UInt16 Reserved; // Must be 0
            public UInt32 AlgId;    // Algorithm ID. Must be one of ALG_ID specified in wincrypto.h
        }

        // RSAPUBKEY struct from wincrypt.h
        [StructLayout(LayoutKind.Sequential)]
        private struct RsaPubKey
        {
            public UInt32 Magic; // Indicates RSA1 or RSA2
            public UInt32 BitLen; // Number of bits in the modulus. Must be multiple of 8.
            public UInt32 PubExp; // The public exponent
        }
        private const UInt32 RSA1 = 0x31415352;
        private const UInt32 RSA2 = 0x32415352;

        // In wincrypt.h both public and private key blobs start with a
        // PUBLICKEYSTRUC and RSAPUBKEY and then start the key data
        private unsafe static readonly int s_offsetToKeyData = sizeof(BlobHeader) + sizeof(RsaPubKey);

        private static ImmutableArray<byte> CreateSnPublicKeyBlob(BlobHeader header, RsaPubKey rsa, byte[] pubKeyData)
        {
            var snPubKey = new SnPublicKeyBlob()
            {
                SigAlgId = AlgorithmId.RsaSign,
                HashAlgId = AlgorithmId.Sha,
                PublicKeySize = (UInt32)(s_offsetToKeyData + pubKeyData.Length)
            };

            using (var ms = new MemoryStream(160))
            using (var binaryWriter = new BinaryWriter(ms))
            {
                binaryWriter.Write(snPubKey.SigAlgId);
                binaryWriter.Write(snPubKey.HashAlgId);
                binaryWriter.Write(snPubKey.PublicKeySize);

                binaryWriter.Write(header.Type);
                binaryWriter.Write(header.Version);
                binaryWriter.Write(header.Reserved);
                binaryWriter.Write(header.AlgId);

                binaryWriter.Write(rsa.Magic);
                binaryWriter.Write(rsa.BitLen);
                binaryWriter.Write(rsa.PubExp);

                binaryWriter.Write(pubKeyData);

                return ms.ToImmutable();
            }
        }

        private unsafe static bool TryGetPublicKeyFromPublicKeyBlob(byte* blob, int blobLen, out ImmutableArray<byte> publicKey)
        {
            var header = (BlobHeader)Marshal.PtrToStructure((IntPtr)blob, typeof(BlobHeader));
            var rsaPubKey = (RsaPubKey)Marshal.PtrToStructure((IntPtr)(blob + sizeof(BlobHeader)), typeof(RsaPubKey));
            var modulus = new byte[rsaPubKey.BitLen >> 3];

            // The key blob data just contains the modulus
            if (blobLen - s_offsetToKeyData != modulus.Length)
            {
                publicKey = ImmutableArray<byte>.Empty;
                return false;
            }

            Marshal.Copy((IntPtr)(blob + s_offsetToKeyData), modulus, 0, modulus.Length);

            publicKey = CreateSnPublicKeyBlob(header, rsaPubKey, modulus);
            return true;
        }

        private unsafe static bool TryGetPublicKeyFromPrivateKeyBlob(byte* blob, int blobLen, out ImmutableArray<byte> publicKey)
        {
            var header = (BlobHeader*)blob;
            var rsa = (RsaPubKey*)(blob + sizeof(BlobHeader));

            var version = header->Version;
            var modulusBitLength = rsa->BitLen;
            var exponent = rsa->PubExp;
            var modulus = new byte[modulusBitLength >> 3];

            if (blobLen - s_offsetToKeyData < modulus.Length)
            {
                publicKey = ImmutableArray<byte>.Empty;
                return false;
            }

            Marshal.Copy((IntPtr)(blob + s_offsetToKeyData), modulus, 0, modulus.Length);

            var newHeader = new BlobHeader()
            {
                Type = PublicKeyBlobId,
                Version = version,
                Reserved = 0,
                AlgId = AlgorithmId.RsaSign
            };

            var newRsaKey = new RsaPubKey()
            {
                Magic = RSA1, // Public key
                BitLen = modulusBitLength,
                PubExp = exponent
            };

            publicKey = CreateSnPublicKeyBlob(newHeader, newRsaKey, modulus);
            return true;
        }

        /// <summary>
        /// Try to retrieve the public key from a crypto blob.
        /// </summary>
        /// <remarks>
        /// Can be either a PUBLICKEYBLOB or PRIVATEKEYBLOB. The BLOB must /// be unencrypted.
        /// </remarks>
        public unsafe static bool TryGetPublicKey(ImmutableArray<byte> blob, out ImmutableArray<byte> publicKey)
        {
            publicKey = ImmutableArray<byte>.Empty;

            // Is this already a strong name PublicKeyBlob?
            if (IsValidPublicKey(blob))
            {
                publicKey = blob;
                return true;
            }

            // Must be at least as large as header + RSA info
            if (blob.Length < sizeof(BlobHeader) + sizeof(RsaPubKey))
            {
                return false;
            }

            fixed (byte* backing = blob.DangerousGetUnderlyingArray())
            {
                var header = (BlobHeader*)backing;
                var rsa = (RsaPubKey*)(backing + sizeof(BlobHeader));

                // The RSA magic key must match the blob id
                if (header->Type == PrivateKeyBlobId &&
                    rsa->Magic == RSA2)
                {
                    return TryGetPublicKeyFromPrivateKeyBlob(backing, blob.Length, out publicKey);
                }
                else if (header->Type == PublicKeyBlobId &&
                    rsa->Magic == RSA1)
                {
                    return TryGetPublicKeyFromPublicKeyBlob(backing, blob.Length, out publicKey);
                }
            }

            return false;
        }
    }
}
