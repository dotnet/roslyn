// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract class CryptographicHashProvider
    {
        private ImmutableArray<byte> _lazySHA1Hash;
        private ImmutableArray<byte> _lazySHA256Hash;
        private ImmutableArray<byte> _lazySHA384Hash;
        private ImmutableArray<byte> _lazySHA512Hash;
        private ImmutableArray<byte> _lazyMD5Hash;

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
                        return GetHash(ref _lazySHA1Hash, algorithm);

                    case AssemblyHashAlgorithm.Sha256:
                        return GetHash(ref _lazySHA256Hash, algorithm);

                    case AssemblyHashAlgorithm.Sha384:
                        return GetHash(ref _lazySHA384Hash, algorithm);

                    case AssemblyHashAlgorithm.Sha512:
                        return GetHash(ref _lazySHA512Hash, algorithm);

                    case AssemblyHashAlgorithm.MD5:
                        return GetHash(ref _lazyMD5Hash, algorithm);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(algorithmId);
                }
            }
        }

        internal static int GetHashSize(SourceHashAlgorithm algorithmId)
        {
            switch (algorithmId)
            {
                case SourceHashAlgorithm.Sha1:
                    return 160 / 8;

                case SourceHashAlgorithm.Sha256:
                    return 256 / 8;

                default:
                    throw ExceptionUtilities.UnexpectedValue(algorithmId);
            }
        }

        internal static HashAlgorithm TryGetAlgorithm(SourceHashAlgorithm algorithmId)
        {
            switch (algorithmId)
            {
                case SourceHashAlgorithm.Sha1:
                    return SHA1.Create();

                case SourceHashAlgorithm.Sha256:
                    return SHA256.Create();

                default:
                    return null;
            }
        }

        internal static HashAlgorithmName GetAlgorithmName(SourceHashAlgorithm algorithmId)
        {
            switch (algorithmId)
            {
                case SourceHashAlgorithm.Sha1:
                    return HashAlgorithmName.SHA1;

                case SourceHashAlgorithm.Sha256:
                    return HashAlgorithmName.SHA256;

                default:
                    throw ExceptionUtilities.UnexpectedValue(algorithmId);
            }
        }

        internal static HashAlgorithm TryGetAlgorithm(AssemblyHashAlgorithm algorithmId)
        {
            switch (algorithmId)
            {
                case AssemblyHashAlgorithm.None:
                case AssemblyHashAlgorithm.Sha1:
                    return SHA1.Create();

                case AssemblyHashAlgorithm.Sha256:
                    return SHA256.Create();

                case AssemblyHashAlgorithm.Sha384:
                    return SHA384.Create();

                case AssemblyHashAlgorithm.Sha512:
                    return SHA512.Create();

                case AssemblyHashAlgorithm.MD5:
                    return MD5.Create();

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
                using (var hashProvider = SHA1.Create())
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
            using (var hashProvider = SHA1.Create())
            {
                return ImmutableArray.Create(hashProvider.ComputeHash(bytes));
            }
        }

        internal static ImmutableArray<byte> ComputeHash(HashAlgorithmName algorithmName, IEnumerable<Blob> bytes)
        {
            using (var incrementalHash = IncrementalHash.CreateHash(algorithmName))
            {
                incrementalHash.AppendData(bytes);
                return ImmutableArray.Create(incrementalHash.GetHashAndReset());
            }
        }

        internal static ImmutableArray<byte> ComputeHash(HashAlgorithmName algorithmName, IEnumerable<ArraySegment<byte>> bytes)
        {
            using (var incrementalHash = IncrementalHash.CreateHash(algorithmName))
            {
                incrementalHash.AppendData(bytes);
                return ImmutableArray.Create(incrementalHash.GetHashAndReset());
            }
        }

        internal static ImmutableArray<byte> ComputeSourceHash(ImmutableArray<byte> bytes, SourceHashAlgorithm hashAlgorithm = SourceHashAlgorithmUtils.DefaultContentHashAlgorithm)
        {
            var algorithmName = GetAlgorithmName(hashAlgorithm);
            using (var incrementalHash = IncrementalHash.CreateHash(algorithmName))
            {
                incrementalHash.AppendData(bytes.ToArray());
                return ImmutableArray.Create(incrementalHash.GetHashAndReset());
            }
        }

        internal static ImmutableArray<byte> ComputeSourceHash(IEnumerable<Blob> bytes, SourceHashAlgorithm hashAlgorithm = SourceHashAlgorithmUtils.DefaultContentHashAlgorithm)
        {
            return ComputeHash(GetAlgorithmName(hashAlgorithm), bytes);
        }
    }
}
