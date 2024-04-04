// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

[Experimental(RoslynExperiments.Interceptors, UrlFormat = RoslynExperiments.Interceptors_Url)]
//[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
public abstract class InterceptableLocation
{
    private protected InterceptableLocation() { }

    /// <summary>
    /// The version of the location encoding. Used as an argument to 'InterceptsLocationAttribute'.
    /// </summary>
    public abstract int Version { get; }

    /// <summary>
    /// Opaque data which references a call when used as an argument to 'InterceptsLocationAttribute'.
    /// The value does not require escaping, i.e. it is valid in a string literal when wrapped in " (double-quote) characters.
    /// </summary>
    public abstract string Data { get; }

    /// <summary>
    /// Gets a human-readable representation of the location, suitable for including in comments in generated code.
    /// </summary>
    public abstract string GetDisplayLocation();
}

#pragma warning disable RSEXPERIMENTAL002 // internal usage of experimental API
/// <summary>
/// Version 1 of the InterceptableLocation encoding.
/// </summary>
internal sealed class InterceptableLocation1 : InterceptableLocation
{
    internal const int ContentHashLength = 16;

    private readonly ImmutableArray<byte> _checksum;
    private readonly string _path;
    private readonly int _position;
    private readonly int _lineNumberOneIndexed;
    private readonly int _characterNumberOneIndexed;
    private string? _lazyData;

    internal InterceptableLocation1(ImmutableArray<byte> checksum, string path, int position, int lineNumberOneIndexed, int characterNumberOneIndexed)
    {
        Debug.Assert(checksum.Length == ContentHashLength);
        Debug.Assert(path is not null);
        Debug.Assert(position >= 0);
        Debug.Assert(lineNumberOneIndexed > 0);
        Debug.Assert(characterNumberOneIndexed > 0);

        _checksum = checksum;
        _path = path;
        _position = position;
        _lineNumberOneIndexed = lineNumberOneIndexed;
        _characterNumberOneIndexed = characterNumberOneIndexed;
    }

    public override string GetDisplayLocation()
    {
        // e.g. `C:\project\src\Program.cs(12,34)`
        return $"{_path}({_lineNumberOneIndexed},{_characterNumberOneIndexed})";
    }

    public override string ToString() => GetDisplayLocation();

    public override int Version => 1;
    public override string Data
    {
        get
        {
            if (_lazyData is null)
                _lazyData = makeData();

            return _lazyData;

            string makeData()
            {
                var builder = PooledBlobBuilder.GetInstance();
                builder.WriteBytes(_checksum, start: 0, 16);
                builder.WriteInt32(_position);

                var displayFileName = Path.GetFileName(_path);
                builder.WriteUTF8(displayFileName);

                var bytes = builder.ToArray();
                builder.Free();
                return Convert.ToBase64String(bytes);
            }
        }
    }

    internal static (ReadOnlyMemory<byte> checksum, int position, string displayFileName)? Decode(string? data, Location diagnosticLocation, BindingDiagnosticBag diagnostics)
    {
        if (data is null)
        {
            diagnostics.Add(ErrorCode.ERR_InterceptsLocationDataInvalidFormat, diagnosticLocation);
            return null;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(data);
        }
        catch (FormatException)
        {
            diagnostics.Add(ErrorCode.ERR_InterceptsLocationDataInvalidFormat, diagnosticLocation);
            return null;
        }

        // format:
        // - 16 bytes of target file content hash (xxHash128)
        // - int32 position (little endian)
        // - utf-8 display filename
        const int hashIndex = 0;
        const int hashSize = 16;
        const int positionIndex = hashIndex + hashSize;
        const int positionSize = sizeof(int);
        const int displayNameIndex = positionIndex + positionSize;
        const int minLength = displayNameIndex;

        if (bytes.Length < minLength)
        {
            diagnostics.Add(ErrorCode.ERR_InterceptsLocationDataInvalidFormat, diagnosticLocation);
            return null;
        }

        var hash = bytes.AsMemory(start: hashIndex, length: hashSize);
        var position = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(start: positionIndex));

        // https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding.utf8?view=net-8.0#remarks states:
        // > `Encoding.UTF8` returns a UTF8Encoding object that uses replacement fallback to replace each string that it can't encode and each byte that it can't decode with a question mark ("?") character.
        //
        // If these assertions are violated, it means the encoder may throw for invalid UTF-8 sequences, which we don't expect.
        // In this case we would either need to adjust the code to start handling ArgumentException from GetString, or create a static UTF8 decoder with non-throwing behavior.
        Debug.Assert(Encoding.UTF8.DecoderFallback is DecoderReplacementFallback);
        Debug.Assert(Encoding.UTF8.IsReadOnly);
        string displayFileName = Encoding.UTF8.GetString(bytes, index: displayNameIndex, count: bytes.Length - displayNameIndex);

        return (hash, position, displayFileName);
    }

    // Note: the goal of implementing equality here is so that incremental state tables etc. can detect and use it.
    // This encoding which uses the checksum of the referenced file may not be stable across incremental runs in practice, but it seems correct in principle to implement equality here anyway.
    public override bool Equals(object? obj)
    {
        if ((object)this == obj)
            return true;

        return obj is InterceptableLocation1 other
            && _checksum.SequenceEqual(other._checksum)
            && _path == other._path
            && _position == other._position
            && _lineNumberOneIndexed == other._lineNumberOneIndexed
            && _characterNumberOneIndexed == other._characterNumberOneIndexed;
    }

    public override int GetHashCode()
    {
        return Hash.Combine(
           BinaryPrimitives.ReadInt32LittleEndian(_checksum.AsSpan()),
           Hash.Combine(
               _path.GetHashCode(),
               Hash.Combine(
                   _position,
                   Hash.Combine(
                       _lineNumberOneIndexed,
                       _characterNumberOneIndexed))));
    }
}
