// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class SigningUtilities
    {
        internal static byte[] CalculateRsaSignature(IEnumerable<Blob> content, RSAParameters privateKey)
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

        internal static byte[] CalculateSha1(IEnumerable<Blob> content)
        {
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
            {
                hash.AppendData(content);
                return hash.GetHashAndReset();
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
                keySize = privateKey.Value.Modulus.Length;
            }

            if (keySize == 0)
            {
                return 0;
            }

            return (keySize < 128 + 32) ? 128 : keySize - 32;
        }
    }
}
