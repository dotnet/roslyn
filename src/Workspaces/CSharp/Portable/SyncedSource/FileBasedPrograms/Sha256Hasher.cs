// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.DotNet.Utilities;

internal static class Sha256Hasher
{
    /// <summary>
    /// The hashed mac address needs to be the same hashed value as produced by the other distinct sources given the same input. (e.g. VsCode)
    /// </summary>
    public static string Hash(string text)
    {
#if NET10_0_OR_GREATER
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
#else
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
#endif
    }

    public static string HashWithNormalizedCasing(string text)
        => Hash(text.ToUpperInvariant());
}
