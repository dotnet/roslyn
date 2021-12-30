// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Test.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CryptoBlobParserTests : TestBase
    {
        private const int HEADER_LEN = 20;
        private const int MOD_LEN = 128;
        private const int HALF_LEN = 64;

        [Fact]
        public void GetPrivateKeyFromKeyPair()
        {
            var key = ImmutableArray.Create(TestResources.General.snKey);

            RSAParameters? privateKeyOpt;
            Assert.True(CryptoBlobParser.TryParseKey(key, out _, out privateKeyOpt));
            Debug.Assert(privateKeyOpt.HasValue);
            var privKey = privateKeyOpt.Value;

            AssertEx.Equal(privKey.Exponent, new byte[] { 0x01, 0x00, 0x01 });

            var expectedModulus = key.Skip(HEADER_LEN).Take(MOD_LEN).ToArray();
            Array.Reverse(expectedModulus);
            AssertEx.Equal(expectedModulus, privKey.Modulus);

            var expectedP = key.Skip(HEADER_LEN + MOD_LEN).Take(HALF_LEN).ToArray();
            Array.Reverse(expectedP);
            AssertEx.Equal(expectedP, privKey.P);

            var expectedQ = key.Skip(HEADER_LEN + MOD_LEN + HALF_LEN).Take(HALF_LEN).ToArray();
            Array.Reverse(expectedQ);
            AssertEx.Equal(expectedQ, privKey.Q);

            var expectedDP = key.Skip(HEADER_LEN + MOD_LEN + HALF_LEN * 2).Take(HALF_LEN).ToArray();
            Array.Reverse(expectedDP);
            AssertEx.Equal(expectedDP, privKey.DP);

            var expectedDQ = key.Skip(HEADER_LEN + MOD_LEN + HALF_LEN * 3).Take(HALF_LEN).ToArray();
            Array.Reverse(expectedDQ);
            AssertEx.Equal(expectedDQ, privKey.DQ);

            var expectedInverseQ = key.Skip(HEADER_LEN + MOD_LEN + HALF_LEN * 4).Take(HALF_LEN).ToArray();
            Array.Reverse(expectedInverseQ);
            AssertEx.Equal(expectedInverseQ, privKey.InverseQ);

            var expectedD = key.Skip(HEADER_LEN + MOD_LEN + HALF_LEN * 5).Take(MOD_LEN).ToArray();
            Array.Reverse(expectedD);
            AssertEx.Equal(expectedD, privKey.D);

            Assert.True(key.Skip(HEADER_LEN + MOD_LEN * 2 + HALF_LEN * 5).ToArray().Length == 0);
        }

        [Fact]
        public void GetPrivateKeyFromKeyPair2()
        {
            var key = ImmutableArray.Create(TestResources.General.snKey2);

            RSAParameters? privateKeyOpt;
            Assert.True(CryptoBlobParser.TryParseKey(key, out _, out privateKeyOpt));
            Assert.True(privateKeyOpt.HasValue);
            var privKey = privateKeyOpt.Value;

            AssertEx.Equal(privKey.Exponent, new byte[] { 0x01, 0x00, 0x01 });

            var expectedModulus = key.Skip(HEADER_LEN).Take(MOD_LEN).ToArray();
            Array.Reverse(expectedModulus);
            AssertEx.Equal(expectedModulus, privKey.Modulus);

            var expectedP = key.Skip(HEADER_LEN + MOD_LEN).Take(HALF_LEN).ToArray();
            Array.Reverse(expectedP);
            AssertEx.Equal(expectedP, privKey.P);

            var expectedQ = key.Skip(HEADER_LEN + MOD_LEN + HALF_LEN).Take(HALF_LEN).ToArray();
            Array.Reverse(expectedQ);
            AssertEx.Equal(expectedQ, privKey.Q);

            var expectedDP = key.Skip(HEADER_LEN + MOD_LEN + HALF_LEN * 2).Take(HALF_LEN).ToArray();
            Array.Reverse(expectedDP);
            AssertEx.Equal(expectedDP, privKey.DP);

            var expectedDQ = key.Skip(HEADER_LEN + MOD_LEN + HALF_LEN * 3).Take(HALF_LEN).ToArray();
            Array.Reverse(expectedDQ);
            AssertEx.Equal(expectedDQ, privKey.DQ);

            var expectedInverseQ = key.Skip(HEADER_LEN + MOD_LEN + HALF_LEN * 4).Take(HALF_LEN).ToArray();
            Array.Reverse(expectedInverseQ);
            AssertEx.Equal(expectedInverseQ, privKey.InverseQ);

            var expectedD = key.Skip(HEADER_LEN + MOD_LEN + HALF_LEN * 5).Take(MOD_LEN).ToArray();
            Array.Reverse(expectedD);
            AssertEx.Equal(expectedD, privKey.D);

            Assert.True(key.Skip(HEADER_LEN + MOD_LEN * 2 + HALF_LEN * 5).ToArray().Length == 0);
        }

        [Fact]
        public void GetPublicKeyFromKeyPair()
        {
            var key = ImmutableArray.Create(TestResources.General.snKey);

            ImmutableArray<byte> pubKey;
            Assert.True(CryptoBlobParser.TryParseKey(key, out pubKey, out _));
            Assert.True(CryptoBlobParser.IsValidPublicKey(pubKey));
            AssertEx.Equal(TestResources.General.snPublicKey, pubKey);
        }

        [Fact]
        public void GetPublicKeyFromKeyPair2()
        {
            var key = ImmutableArray.Create(TestResources.General.snKey2);

            ImmutableArray<byte> pubKey;
            Assert.True(CryptoBlobParser.TryParseKey(key, out pubKey, out _));
            Assert.True(CryptoBlobParser.IsValidPublicKey(pubKey));
            AssertEx.Equal(TestResources.General.snPublicKey2, pubKey);
        }

        [Fact]
        public void SnPublicKeyIsReturnedAsIs()
        {
            var key = ImmutableArray.Create(TestResources.General.snPublicKey);

            ImmutableArray<byte> pubKey;
            Assert.True(CryptoBlobParser.TryParseKey(key, out pubKey, out _));
            Assert.True(CryptoBlobParser.IsValidPublicKey(pubKey));
            AssertEx.Equal(key, pubKey);
        }

        [Fact]
        public void GetSnPublicKeyFromPublicKeyBlob()
        {
            // A Strongname public key blob includes an additional header on top
            // of the wincrypt.h public key blob
            var snBlob = TestResources.General.snPublicKey;

            var buf = new byte[snBlob.Length - CryptoBlobParser.s_publicKeyHeaderSize];
            Array.Copy(snBlob, CryptoBlobParser.s_publicKeyHeaderSize, buf, 0, buf.Length);

            var publicKeyBlob = ImmutableArray.Create(buf);

            ImmutableArray<byte> pubKey;
            Assert.True(CryptoBlobParser.TryParseKey(publicKeyBlob, out pubKey, out _));
            Assert.True(CryptoBlobParser.IsValidPublicKey(pubKey));
            AssertEx.Equal(snBlob, pubKey);
        }

        [Fact]
        public void TryGetPublicKeyFailsForInvalidKeyBlobs()
        {
            var invalidKeyBlobs = new[]
            {
                string.Empty,
                new string('0', 160 * 2), // 160 * 2 - the length of a public key, 2 - 2 chars per byte
                new string('0', 596 * 2), // 596 * 2 - the length of a key pair, 2 - 2 chars per byte
                "0702000000240000DEADBEEF" + new string('0', 584 * 2), // private key blob without magic private key
                "0602000000240000DEADBEEF" + new string('0', 136 * 2), // public key blob without magic public key
            };

            Assert.False(CryptoBlobParser.TryParseKey(HexToBin(invalidKeyBlobs[0]), out _, out _));
            Assert.False(CryptoBlobParser.TryParseKey(HexToBin(invalidKeyBlobs[1]), out _, out _));
            Assert.False(CryptoBlobParser.TryParseKey(HexToBin(invalidKeyBlobs[2]), out _, out _));
            Assert.False(CryptoBlobParser.TryParseKey(HexToBin(invalidKeyBlobs[3]), out _, out _));
        }

        private static ImmutableArray<byte> HexToBin(string input)
        {
            Assert.True(input != null && (input.Length & 1) == 0, "invalid input string.");

            var result = new byte[input.Length >> 1];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = byte.Parse(input.Substring(i << 1, 2), NumberStyles.HexNumber);
            }

            return ImmutableArray.Create(result);
        }
    }
}
