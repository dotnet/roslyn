// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// Moniker uniqueness level to define scope of a <see cref="Moniker"/>.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#uniquenessLevel">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
/// 
[JsonConverter(typeof(StringEnumConverter<UniquenessLevel>))]
[TypeConverter(typeof(StringEnumConverter<UniquenessLevel>.TypeConverter))]
internal readonly record struct UniquenessLevel(string Value) : IStringEnum
{
    /// <summary>
    /// The moniker is only unique inside a document
    /// </summary>
    public static readonly UniquenessLevel Document = new("document");

    /// <summary>
    /// The moniker is unique inside a project for which a dump got created
    /// </summary>
    public static readonly UniquenessLevel Project = new("project");

    /// <summary>
    /// The moniker is unique inside the group to which a project belongs
    /// </summary>
    public static readonly UniquenessLevel Group = new("group");

    /// <summary>
    /// The moniker is unique inside the moniker scheme.
    /// </summary>
    public static readonly UniquenessLevel Scheme = new("scheme");

    /// <summary>
    /// The moniker is globally unique
    /// </summary>
    public static readonly UniquenessLevel Global = new("global");
}
