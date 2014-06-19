// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract class CryptographicHashProvider
    {
        private ImmutableArray<byte> lazySHA1Hash;
        private ImmutableArray<byte> lazySHA256Hash;
        private ImmutableArray<byte> lazySHA384Hash;
        private ImmutableArray<byte> lazySHA512Hash;
        private ImmutableArray<byte> lazyMD5Hash;

        public CryptographicHashProvider()
        {
        }

        internal abstract ImmutableArray<byte> ComputeHash(HashAlgorithm algorithm);

        internal ImmutableArray<byte> GetHash(AssemblyHashAlgorithm algorithmId)
        {
            using (HashAlgorithm algorithm = TryGetAlgorithm(algorithmId))
            {
                // ERR_CryptoHashFailed has already been reported:
                if (algorithm == null)
                {
                    return ImmutableArray.Create<byte>();
                }

                switch (algorithmId)
                {
                    case AssemblyHashAlgorithm.None:
                    case AssemblyHashAlgorithm.Sha1:
                        return GetHash(ref lazySHA1Hash, algorithm);

                    case AssemblyHashAlgorithm.Sha256:
                        return GetHash(ref lazySHA256Hash, algorithm);

                    case AssemblyHashAlgorithm.Sha384:
                        return GetHash(ref lazySHA384Hash, algorithm);

                    case AssemblyHashAlgorithm.Sha512:
                        return GetHash(ref lazySHA512Hash, algorithm);

                    case AssemblyHashAlgorithm.MD5:
                        return GetHash(ref lazyMD5Hash, algorithm);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(algorithmId);
                }
            }
        }

        private static HashAlgorithm TryGetAlgorithm(AssemblyHashAlgorithm algorithmId)
        {
            switch (algorithmId)
            {
                case AssemblyHashAlgorithm.None:
                case AssemblyHashAlgorithm.Sha1:
                    return new SHA1CryptoServiceProvider();

                case AssemblyHashAlgorithm.Sha256:
                    return new SHA256CryptoServiceProvider();

                case AssemblyHashAlgorithm.Sha384:
                    return new SHA384CryptoServiceProvider();

                case AssemblyHashAlgorithm.Sha512:
                    return new SHA512CryptoServiceProvider();

                case AssemblyHashAlgorithm.MD5:
                    return new MD5CryptoServiceProvider();

                default:
                    return null;
            }
        }

        internal static bool IsSupportedAlgorithm(AssemblyHashAlgorithm algorithmId)
        {
            switch (algorithmId)
            {
                case AssemblyHashAlgorithm.None:
                case AssemblyHashAlgorithm.Sha1:
                case AssemblyHashAlgorithm.Sha256:
                case AssemblyHashAlgorithm.Sha384:
                case AssemblyHashAlgorithm.Sha512:
                case AssemblyHashAlgorithm.MD5:
                    return true;

                default:
                    return false;
            }
        }

        private ImmutableArray<byte> GetHash(ref ImmutableArray<byte> lazyHash, HashAlgorithm algorithm)
        {
            if (lazyHash.IsDefault)
            {
                ImmutableInterlocked.InterlockedCompareExchange(ref lazyHash, ComputeHash(algorithm), default(ImmutableArray<byte>));
            }

            return lazyHash;
        }

        internal const int Sha1HashSize = 20;

        internal static ImmutableArray<byte> ComputeSha1(Stream stream)
        {
            if (stream != null)
            {
                stream.Seek(0, SeekOrigin.Begin);
                using (var hashProvider = new SHA1CryptoServiceProvider())
                {
                    return ImmutableArray.Create(hashProvider.ComputeHash(stream));
                }
            }

            return ImmutableArray<byte>.Empty;
        }

        internal static ImmutableArray<byte> ComputeSha1(ImmutableArray<byte> bytes)
        {
            return ComputeSha1(bytes.ToArray());
        }

        internal static ImmutableArray<byte> ComputeSha1(byte[] bytes)
        {
            using (var hashProvider = new SHA1CryptoServiceProvider())
            {
                return ImmutableArray.Create(hashProvider.ComputeHash(bytes));
            }
        }
    }
}
