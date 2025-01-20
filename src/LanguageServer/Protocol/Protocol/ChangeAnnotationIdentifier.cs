// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// An identifier referring to a change annotation managed by a workspace edit
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#changeAnnotationIdentifier">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>
/// Since LSP 3.16
/// </remarks>
[JsonConverter(typeof(StringEnumConverter<ChangeAnnotationIdentifier>))]
[TypeConverter(typeof(StringEnumConverter<ChangeAnnotationIdentifier>.TypeConverter))]
internal readonly record struct ChangeAnnotationIdentifier(string Value) : IStringEnum
{
}
