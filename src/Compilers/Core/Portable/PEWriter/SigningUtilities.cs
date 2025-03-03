// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class SigningUtilities
    {
        internal static byte[] CalculateRsaSignature(IEnumerable<Blob> content, RSAParameters privateKey)
        {
            var hash = calculateSha1(content);

            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(privateKey);
                // CodeQL [SM02196] ECMA-335 requires us to use SHA-1 and there is no alternative.
                var signature = rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                Array.Reverse(signature);
                return signature;
            }

            static byte[] calculateSha1(IEnumerable<Blob> content)
            {
                // CodeQL [SM02196] ECMA-335 requires us to use SHA-1 and there is no alternative.
                using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
                {
                    hash.AppendData(content);
                    return hash.GetHashAndReset();
                }
            }
        }

        internal static int CalculateStrongNameSignatureSize(CommonPEModuleBuilder module, RSAParameters? privateKey)
        {
            ISourceAssemblySymbolInternal? assembly = module.SourceAssemblyOpt;
            if (assembly == null && !privateKey.HasValue)
            {
                return 0;
            }

            int keySize = 0;

            // EDMAURER the count of characters divided by two because the each pair of characters will turn in to one byte.
            if (keySize == 0 && assembly != null)
            {
                keySize = (assembly.SignatureKey == null) ? 0 : assembly.SignatureKey.Length / 2;
            }

            if (keySize == 0 && assembly != null)
            {
                keySize = assembly.Identity.PublicKey.Length;
            }

            if (keySize == 0 && privateKey.HasValue)
            {
                keySize = privateKey.Value.Modulus!.Length;
            }

            if (keySize == 0)
            {
                return 0;
            }

            return (keySize < 128 + 32) ? 128 : keySize - 32;
        }
    }
}
