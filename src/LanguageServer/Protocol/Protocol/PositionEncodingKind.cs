// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// The encoding used for representing character offsets in documents
/// as negotiated between the client and server during initialization
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#positionEncodingKind">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
[JsonConverter(typeof(StringEnumConverter<PositionEncodingKind>))]
[TypeConverter(typeof(StringEnumConverter<PositionEncodingKind>.TypeConverter))]
internal readonly record struct PositionEncodingKind(string Value) : IStringEnum
{
    /// <summary>
    /// Character offsets count UTF-8 code units (e.g bytes).
    /// </summary>
    public static readonly PositionEncodingKind UTF8 = new("utf-8");

    /// <summary>
    /// Character offsets count UTF-16 code units.
    /// <para>
    /// This is the default and must always be supported by servers
    /// </para>
    /// </summary>
    public static readonly PositionEncodingKind UTF16 = new("utf-16");

    /// <summary>
    /// Character offsets count UTF-32 code units.
    /// </summary>
    /// <remarks> Implementation note: these are the same as Unicode code points, so
    /// this <see cref="PositionEncodingKind"/> may also be used for an encoding-agnostic
    /// representation of character offsets.
    /// </remarks>
    public static readonly PositionEncodingKind UTF32 = new("utf-32");
}
