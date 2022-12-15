// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp;

internal struct FileIdentifier
{
    private static readonly Encoding s_encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public string? EncoderFallbackErrorMessage { get; init; }
    public ImmutableArray<byte> FilePathChecksumOpt { get; init; }
    public string DisplayFilePath { get; init; }

    public static FileIdentifier Create(SyntaxTree tree)
        => Create(tree.FilePath);

    public static FileIdentifier Create(string filePath)
    {
        string? encoderFallbackErrorMessage = null;
        ImmutableArray<byte> hash = default;
        try
        {
            var encodedFilePath = s_encoding.GetBytes(filePath);
            using var sha256 = SHA256.Create();
            hash = sha256.ComputeHash(encodedFilePath).ToImmutableArray();
        }
        catch (EncoderFallbackException ex)
        {
            encoderFallbackErrorMessage = ex.Message;
        }

        var displayFilePath = GeneratedNames.GetDisplayFilePath(filePath);
        return new FileIdentifier { EncoderFallbackErrorMessage = encoderFallbackErrorMessage, FilePathChecksumOpt = hash, DisplayFilePath = displayFilePath };
    }
}
