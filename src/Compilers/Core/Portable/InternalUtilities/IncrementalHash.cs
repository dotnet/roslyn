// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Implementation of System.Security.Cryptography.IncrementalHash that works accross Desktop and Core CLR.
    /// Remove once we can use the real one from netstandard1.3.
    /// </summary>
    internal abstract class IncrementalHash : IDisposable
    {
        /// <summary>
        /// Append the entire contents of <paramref name="data"/> to the data already processed in the hash or HMAC.
        /// </summary>
        /// <param name="data">The data to process.</param>
        public void AppendData(byte[] data) => AppendData(data, 0, data.Length);

        /// <summary>
        /// Append <paramref name="count"/> bytes of <paramref name="data"/>, starting at <paramref name="offset"/>,
        /// to the data already processed in the hash.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <param name="offset">The offset into the byte array from which to begin using data.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        public abstract void AppendData(byte[] data, int offset, int count);

        /// <summary>
        /// Retrieve the hash for the data accumulated from prior calls to
        /// <see cref="AppendData(byte[])"/>, and return to the state the object
        /// was in at construction.
        /// </summary>
        public abstract byte[] GetHashAndReset();

        public abstract void Dispose();

        public static IncrementalHash Create(AssemblyHashAlgorithm hashAlgorithm)
        {
            if (PortableShim.IncrementalHash.TypeOpt != null)
            {
                return new Core(hashAlgorithm);
            }
            else
            {
                return new Desktop(hashAlgorithm);
            }
        }

        /// <summary>
        /// CoreCLR implementation.
        /// </summary>
        private sealed class Core : IncrementalHash
        {
            // IncrementalHash
            private readonly IDisposable _incrementalHashImpl;

            internal Core(AssemblyHashAlgorithm hashAlgorithm)
            {
                var name = GetHashAlgorithmNameObj(hashAlgorithm);
                _incrementalHashImpl = PortableShim.IncrementalHash.CreateHash(name);
            }

            /// <summary>
            /// Returns the actual FX implementation of HashAlgorithmName for given hash algorithm id.
            /// </summary>
            private static object GetHashAlgorithmNameObj(AssemblyHashAlgorithm algorithmId)
            {
                switch (algorithmId)
                {
                    case AssemblyHashAlgorithm.Sha1:
                        return PortableShim.HashAlgorithmName.SHA1;

                    default:
                        // More algorithms can be added as needed.
                        throw ExceptionUtilities.UnexpectedValue(algorithmId);
                }
            }

            public override void AppendData(byte[] data, int offset, int count) => PortableShim.IncrementalHash.AppendData(_incrementalHashImpl, data, offset, count);
            public override byte[] GetHashAndReset() => PortableShim.IncrementalHash.GetHashAndReset(_incrementalHashImpl);
            public override void Dispose() => _incrementalHashImpl.Dispose();
        }

        /// <summary>
        /// Desktop implementation.
        /// </summary>
        private sealed class Desktop : IncrementalHash
        {
            // HashAlgorithm
            private readonly IDisposable _hashAlgorithmImpl;

            internal Desktop(AssemblyHashAlgorithm hashAlgorithm)
            {
                _hashAlgorithmImpl = GetAlgorithmImpl(hashAlgorithm);
            }

            /// <summary>
            /// Returns the actual FX implementation of HashAlgorithm.
            /// </summary>
            private static IDisposable GetAlgorithmImpl(AssemblyHashAlgorithm algorithmId)
            {
                switch (algorithmId)
                {
                    case AssemblyHashAlgorithm.None:
                    case AssemblyHashAlgorithm.Sha1:
                        return PortableShim.SHA1.Create();

                    case AssemblyHashAlgorithm.Sha256:
                        return PortableShim.SHA256.Create();

                    case AssemblyHashAlgorithm.Sha384:
                        return PortableShim.SHA384.Create();

                    case AssemblyHashAlgorithm.Sha512:
                        return PortableShim.SHA512.Create();

                    case AssemblyHashAlgorithm.MD5:
                        return PortableShim.MD5.Create();

                    default:
                        throw ExceptionUtilities.UnexpectedValue(algorithmId);
                }
            }

            public override void AppendData(byte[] data, int offset, int count)
            {
                while (count > 0)
                {
                    int written = PortableShim.HashAlgorithm.TransformBlock(_hashAlgorithmImpl, data, offset, count, data, offset);
                    Debug.Assert(count == written); // does the TransformBlock method always consume the complete data given to it?
                    count -= written;
                    offset += written;
                }
            }

            public override byte[] GetHashAndReset()
            {
                PortableShim.HashAlgorithm.TransformFinalBlock(_hashAlgorithmImpl, SpecializedCollections.EmptyBytes, 0, 0);
                return PortableShim.HashAlgorithm.Hash(_hashAlgorithmImpl);
            }

            public override void Dispose()
            {
                _hashAlgorithmImpl.Dispose();
            }
        }
    }
}
