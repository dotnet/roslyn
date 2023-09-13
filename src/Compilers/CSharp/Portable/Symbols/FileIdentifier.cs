// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class FileIdentifier
{
    private static readonly Encoding s_encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private string? _filePath;

    private string? _encoderFallbackErrorMessage;
    private string? _displayFilePath;
    private ImmutableArray<byte> _filePathChecksumOpt;

    private FileIdentifier(string filePath)
    {
        _filePath = filePath;
    }

    private FileIdentifier(ImmutableArray<byte> filePathChecksumOpt, string displayFilePath)
    {
        _filePathChecksumOpt = filePathChecksumOpt;
        _displayFilePath = displayFilePath;
    }

    private void EnsureInitialized()
    {
        var filePath = _filePath;
        if (filePath is not null)
        {
            string? encoderFallbackErrorMessage = null;
            ImmutableArray<byte> hash = default;
            try
            {
                var encodedFilePath = s_encoding.GetBytes(filePath);
                using var hashAlgorithm = SourceHashAlgorithms.CreateDefaultInstance();
                hash = hashAlgorithm.ComputeHash(encodedFilePath).ToImmutableArray();
            }
            catch (EncoderFallbackException ex)
            {
                encoderFallbackErrorMessage = ex.Message;
            }

            var displayFilePath = GeneratedNames.GetDisplayFilePath(filePath);

            _encoderFallbackErrorMessage = encoderFallbackErrorMessage;
            _displayFilePath = displayFilePath;

            ImmutableInterlocked.InterlockedInitialize(ref _filePathChecksumOpt, hash);

            Volatile.Write(ref _filePath, null);
        }
    }

    public string DisplayFilePath
    {
        get
        {
            EnsureInitialized();

            return _displayFilePath!;
        }
    }

    public string? EncoderFallbackErrorMessage
    {
        get
        {
            EnsureInitialized();

            return _encoderFallbackErrorMessage;
        }
    }

    public ImmutableArray<byte> FilePathChecksumOpt
    {
        get
        {
            EnsureInitialized();

            return _filePathChecksumOpt;
        }
    }

    public static FileIdentifier Create(SyntaxTree tree)
        => Create(tree.FilePath);

    public static FileIdentifier Create(string filePath)
        => new FileIdentifier(filePath);

    public static FileIdentifier Create(ImmutableArray<byte> filePathChecksumOpt, string displayFilePath)
        => new FileIdentifier(filePathChecksumOpt, displayFilePath);
}
