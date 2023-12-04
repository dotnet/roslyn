// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class FileIdentifier
{
    private class FileIdentifierData(string? encoderFallbackErrorMessage, string displayFilePath, ImmutableArray<byte> filePathChecksumOpt)
    {
        public readonly string? EncoderFallbackErrorMessage = encoderFallbackErrorMessage;
        public readonly string DisplayFilePath = displayFilePath;
        public readonly ImmutableArray<byte> FilePathChecksumOpt = filePathChecksumOpt;
    }

    private static readonly Encoding s_encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly string _filePath;
    private FileIdentifierData? _data;

    private FileIdentifier(string filePath)
    {
        _filePath = filePath;
    }

    private FileIdentifier(ImmutableArray<byte> filePathChecksumOpt, string displayFilePath)
    {
        _data = new FileIdentifierData(encoderFallbackErrorMessage: null, displayFilePath, filePathChecksumOpt);
        _filePath = string.Empty;
    }

    [MemberNotNull(nameof(_data))]
    private void EnsureInitialized()
    {
        if (_data is null)
        {
            string? encoderFallbackErrorMessage = null;
            ImmutableArray<byte> hash = default;
            try
            {
                var encodedFilePath = s_encoding.GetBytes(_filePath);
                using var hashAlgorithm = SourceHashAlgorithms.CreateDefaultInstance();
                hash = hashAlgorithm.ComputeHash(encodedFilePath).ToImmutableArray();
            }
            catch (EncoderFallbackException ex)
            {
                encoderFallbackErrorMessage = ex.Message;
            }

            var displayFilePath = GeneratedNames.GetDisplayFilePath(_filePath);

            _data = new FileIdentifierData(encoderFallbackErrorMessage, displayFilePath, hash);
        }
    }

    public string DisplayFilePath
    {
        get
        {
            EnsureInitialized();

            return _data.DisplayFilePath;
        }
    }

    public string? EncoderFallbackErrorMessage
    {
        get
        {
            EnsureInitialized();

            return _data.EncoderFallbackErrorMessage;
        }
    }

    public ImmutableArray<byte> FilePathChecksumOpt
    {
        get
        {
            EnsureInitialized();

            return _data.FilePathChecksumOpt;
        }
    }

    public static FileIdentifier Create(SyntaxTree syntaxTree, SourceReferenceResolver? resolver)
        => new FileIdentifier(syntaxTree.GetNormalizedPath(resolver));

    public static FileIdentifier Create(string normalizedFilePath)
        => new FileIdentifier(normalizedFilePath);

    public static FileIdentifier Create(ImmutableArray<byte> filePathChecksumOpt, string displayFilePath)
        => new FileIdentifier(filePathChecksumOpt, displayFilePath);
}
