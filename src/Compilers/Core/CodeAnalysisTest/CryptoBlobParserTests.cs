using Roslyn.Test.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CryptoBlobParserTests : TestBase
    {
        [Fact]
        public void GetPublicKeyFromKeyPair()
        {
            var key = ImmutableArray.Create(TestResources.General.snKey);

            ImmutableArray<byte> pubKey;
            Assert.True(CryptoBlobParser.TryGetPublicKey(key, out pubKey));
            Assert.True(CryptoBlobParser.IsValidPublicKey(pubKey));
            Assert.Equal(TestResources.General.snPublicKey, pubKey);
        }

        [Fact]
        public void GetPublicKeyFromKeyPair2()
        {
            var key = ImmutableArray.Create(TestResources.General.snKey2);

            ImmutableArray<byte> pubKey;
            Assert.True(CryptoBlobParser.TryGetPublicKey(key, out pubKey));
            Assert.True(CryptoBlobParser.IsValidPublicKey(pubKey));
            Assert.Equal(TestResources.General.snPublicKey2, pubKey);
        }

        [Fact]
        public void SnPublicKeyIsReturnedAsIs()
        {
            var key = ImmutableArray.Create(TestResources.General.snPublicKey);
            
            ImmutableArray<byte> pubKey;
            Assert.True(CryptoBlobParser.TryGetPublicKey(key, out pubKey));
            Assert.True(CryptoBlobParser.IsValidPublicKey(pubKey));
            Assert.Equal(key, pubKey);
        }

        [Fact]
        public void GetSnPublicKeyFromPublicKeyBlob()
        {
            // An Strongname public key blob includes an additional header on top
            // of the wincrypt.h public key blob
            var snBlob = TestResources.General.snPublicKey;

            var buf = new byte[snBlob.Length - CryptoBlobParser.s_publicKeyHeaderSize];
            Array.Copy(snBlob, CryptoBlobParser.s_publicKeyHeaderSize, buf, 0, buf.Length);

            var publicKeyBlob = ImmutableArray.Create(buf);

            ImmutableArray<byte> pubKey;
            Assert.True(CryptoBlobParser.TryGetPublicKey(publicKeyBlob, out pubKey));
            Assert.True(CryptoBlobParser.IsValidPublicKey(pubKey));
            Assert.Equal(snBlob, pubKey);
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

            ImmutableArray<byte> pubKey;
            foreach (var key in invalidKeyBlobs)
            {
                Assert.False(CryptoBlobParser.TryGetPublicKey(HexToBin(key), out pubKey));
            }
        }

        private static ImmutableArray<byte> HexToBin(string input)
        {
            Debug.Assert(input != null && (input.Length & 1) == 0, "invalid input string.");

            var result = new byte[input.Length >> 1];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = byte.Parse(input.Substring(i << 1, 2), NumberStyles.HexNumber);
            }

            return ImmutableArray.Create(result);
        }
    }
}
