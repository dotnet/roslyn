// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Roslyn.Utilities;

/// <summary>Provides support for computing a hash value incrementally across several segments.</summary>
internal sealed class RoslynIncrementalHash : IDisposable
{
    private const int NTE_BAD_ALGID = -2146893816;

    private readonly HashAlgorithmName _algorithmName;
    private HashAlgorithm _hash;
    private bool _disposed;
    private bool _resetPending;

    /// <summary>Gets the name of the algorithm being performed.</summary>
    /// <value>The name of the algorithm being performed.</value>
    public HashAlgorithmName AlgorithmName => _algorithmName;

    private RoslynIncrementalHash(HashAlgorithmName name, HashAlgorithm hash)
    {
        _algorithmName = name;
        _hash = hash;
    }

    /// <summary>Appends the specified data to the data already processed in the hash or HMAC.</summary>
    /// <param name="data">The data to process.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The <see cref="RoslynIncrementalHash"/> object has already been disposed.</exception>
    public void AppendData(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        AppendData(data, 0, data.Length);
    }

    /// <summary>Appends the specified number of bytes from the specified data, starting at the specified offset, to the
    /// data already processed in the hash.</summary>
    /// <param name="data">The data to process.</param>
    /// <param name="offset">The offset into the byte array from which to begin using data.</param>
    /// <param name="count">The number of bytes to use from <paramref name="data"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> or <paramref name="offset"/> is negative.
    /// -or-
    /// <paramref name="count"/> is larger than the length of <paramref name="data"/>.</exception>
    /// <exception cref="ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the data length.</exception>
    /// <exception cref="ObjectDisposedException">The <see cref="RoslynIncrementalHash"/> object has already been disposed.</exception>
    public void AppendData(byte[] data, int offset, int count)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "ArgumentOutOfRange_NeedNonNegNum");
        }

        if (count < 0 || count > data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (data.Length - count < offset)
        {
            throw new ArgumentException("Argument_InvalidOffLen");
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(typeof(RoslynIncrementalHash).Name);
        }

        if (_resetPending)
        {
            _hash.Initialize();
            _resetPending = false;
        }

        _hash.TransformBlock(data, offset, count, null, 0);
    }

    /// <summary>Retrieves the hash for the data accumulated from prior calls to the <see cref="AppendData(byte[])"/>
    /// method, and resets the object to its initial state.</summary>
    /// <returns>The computed hash or HMAC.</returns>
    /// <exception cref="ObjectDisposedException">The <see cref="RoslynIncrementalHash"/> object has already been disposed.</exception>
    public byte[] GetHashAndReset()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(typeof(RoslynIncrementalHash).Name);
        }

        if (_resetPending)
        {
            _hash.Initialize();
        }

        _hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        byte[] hash = _hash.Hash!;
        _resetPending = true;
        return hash;
    }

    /// <summary>Releases the resources used by the current instance of the <see cref="RoslynIncrementalHash"/> class.</summary>
    public void Dispose()
    {
        _disposed = true;
        if (_hash != null)
        {
            _hash.Dispose();
            _hash = null!;
        }
    }

    /// <summary>Creates a <see cref="RoslynIncrementalHash"/> for the specified algorithm.</summary>
    /// <param name="hashAlgorithm">The name of the hash algorithm to perform.</param>
    /// <returns>An <see cref="RoslynIncrementalHash"/> instance ready to compute the hash algorithm specified by <paramref name="hashAlgorithm"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="hashAlgorithm"/>.<see cref="HashAlgorithmName.Name"/> is <see langword="null"/> or an empty string.</exception>
    /// <exception cref="CryptographicException"><paramref name="hashAlgorithm" /> is not a known hash algorithm.</exception>
    public static RoslynIncrementalHash CreateHash(HashAlgorithmName hashAlgorithm)
    {
        if (string.IsNullOrEmpty(hashAlgorithm.Name))
        {
            throw new ArgumentException("Cryptography_HashAlgorithmNameNullOrEmpty", nameof(hashAlgorithm));
        }
        return new RoslynIncrementalHash(hashAlgorithm, GetHashAlgorithm(hashAlgorithm));
    }

    private static HashAlgorithm GetHashAlgorithm(HashAlgorithmName hashAlgorithm)
    {
        if (hashAlgorithm == HashAlgorithmName.MD5)
        {
            return MD5.Create();
        }

        if (hashAlgorithm == HashAlgorithmName.SHA1)
        {
            return SHA1.Create();
        }

        if (hashAlgorithm == HashAlgorithmName.SHA256)
        {
            return SHA256.Create();
        }

        if (hashAlgorithm == HashAlgorithmName.SHA384)
        {
            return SHA384.Create();
        }

        if (hashAlgorithm == HashAlgorithmName.SHA512)
        {
            return SHA512.Create();
        }

        throw new CryptographicException(NTE_BAD_ALGID);
    }
}
