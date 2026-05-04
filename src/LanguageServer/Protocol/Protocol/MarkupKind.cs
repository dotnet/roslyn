// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// Value representing the various formats of markup text.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#markupContent">Language Server Protocol specification</see> for additional information.
/// </summary>
[JsonConverter(typeof(StringEnumConverter<MarkupKind>))]
[TypeConverter(typeof(StringEnumConverter<MarkupKind>.TypeConverter))]
internal readonly record struct MarkupKind(string Value) : IStringEnum
{
    /// <summary>
    /// Markup type is plain text.
    /// </summary>
    public static readonly MarkupKind PlainText = new("plaintext");

    /// <summary>
    /// Markup type is Markdown.
    /// </summary>
    public static readonly MarkupKind Markdown = new("markdown");
}
