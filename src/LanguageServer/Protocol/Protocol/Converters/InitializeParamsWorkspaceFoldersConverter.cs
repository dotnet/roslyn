// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// This is used to surface the distinction between "not supported" and "none open" for <see cref="InitializeParams.WorkspaceFolders"/>
/// <para>
/// Per <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#initialize">the LSP Spec</see>:
/// <list type="bullet">
/// <item>
///  If the property is NOT present, the client does not support workspace folders.
/// </item>
/// <item>
/// If the property is present but null, or an empty array, the client supports workspace folders but none are open.
/// </item>
/// </list>
/// </para>
/// To represent this on <see cref="InitializeParams.WorkspaceFolders"/>, we use <see langword="null"/>
/// to represent that the client does not support workspace folders, and use an empty array to represent that none are open.
/// </summary>
internal sealed class InitializeParamsWorkspaceFoldersConverter : JsonConverter<WorkspaceFolder[]?>
{
    public override WorkspaceFolder[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<WorkspaceFolder[]>(ref reader, options) ?? [];
    }

    public override void Write(Utf8JsonWriter writer, WorkspaceFolder[]? value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
