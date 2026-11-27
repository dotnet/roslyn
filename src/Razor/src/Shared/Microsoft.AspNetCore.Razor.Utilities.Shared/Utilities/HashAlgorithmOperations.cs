// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal static class HashAlgorithmOperations
{
    public static HashAlgorithm Create()
        => SHA256.Create();

    public static string? GetAlgorithmName()
        => HashAlgorithmName.SHA256.Name;
}
