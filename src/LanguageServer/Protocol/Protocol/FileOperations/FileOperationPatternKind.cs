// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A pattern kind describing if a glob pattern matches a file a folder or both.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#fileOperationPatternKind">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
[JsonConverter(typeof(StringEnumConverter<FileOperationPatternKind>))]
[TypeConverter(typeof(StringEnumConverter<FileOperationPatternKind>.TypeConverter))]
internal readonly record struct FileOperationPatternKind(string Value) : IStringEnum
{
    /// <summary>
    /// The pattern matches a file only.
    /// </summary>
    public static readonly FileOperationPatternKind File = new("file");

    /// <summary>
    /// The pattern matches a folder only.
    /// </summary>
    public static readonly FileOperationPatternKind Folder = new("folder");
}
