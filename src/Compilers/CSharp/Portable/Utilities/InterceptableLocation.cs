﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

[Experimental(RoslynExperiments.Interceptors, UrlFormat = RoslynExperiments.Interceptors_Url)]
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
    private readonly ImmutableArray<byte> _checksum;
    private readonly string _path;
    private readonly int _position;
    private readonly int _lineNumberOneIndexed;
    private readonly int _characterNumberOneIndexed;

    internal InterceptableLocation1(ImmutableArray<byte> checksum, string path, int position, int lineNumberOneIndexed, int characterNumberOneIndexed)
    {
        if (checksum.Length != 16)
        {
            throw new ArgumentException(message: "checksum must be exactly 16 bytes in length", paramName: nameof(checksum));
        }

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

    public override int Version => 1;
    public override string Data
    {
        get
        {
            var builder = new BlobBuilder();
            builder.WriteBytes(_checksum, start: 0, 16);
            builder.WriteInt32(_position);

            var displayFileName = Path.GetFileName(_path);
            builder.WriteUTF8(displayFileName);

            return Convert.ToBase64String(builder.ToArray());
        }
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
