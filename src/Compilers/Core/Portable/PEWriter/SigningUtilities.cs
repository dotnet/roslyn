using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace Microsoft.Cci
{
    internal static class SigningUtilities
    {
        public static byte[] CalculateRsaSignature(IEnumerable<Blob> content, RSAParameters privateKey)
        {
            var hash = CalculateSha1(content);
 
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(privateKey);
                var signature = rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                Array.Reverse(signature);
                return signature;
            }
        }
 
        private static byte[] CalculateSha1(IEnumerable<Blob> content)
        {
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
            {
                var stream = new MemoryStream();
 
                foreach (var blob in content)
                {
                    var segment = blob.GetBytes();
 
                    stream.Write(segment.Array, segment.Offset, segment.Count);
 
                    hash.AppendData(segment.Array, segment.Offset, segment.Count);
                }
 
                return hash.GetHashAndReset();
            }
        }
    }
}
