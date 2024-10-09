// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// The kind of a <see cref="Moniker"/>.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#monikerKind">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
[JsonConverter(typeof(StringEnumConverter<MonikerKind>))]
[TypeConverter(typeof(StringEnumConverter<MonikerKind>.TypeConverter))]
internal readonly record struct MonikerKind(string Value) : IStringEnum
{
    /// <summary>
    /// The moniker represent a symbol that is imported into a project
    /// </summary>
    public static readonly MonikerKind Import = new("import");

    /// <summary>
    /// The moniker represents a symbol that is exported from a project
    /// </summary>
    public static readonly MonikerKind Export = new("export");

    /// <summary>
    /// The moniker represents a symbol that is local to a project (e.g. a local
    /// variable of a function, a class not visible outside the project, ...)
    /// </summary>
    public static readonly MonikerKind Local = new("local");
}
