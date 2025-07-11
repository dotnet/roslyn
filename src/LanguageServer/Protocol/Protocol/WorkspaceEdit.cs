// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Class representing a request sent from a language server to modify resources in the workspace.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceEdit">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class WorkspaceEdit
{
    /// <summary>
    /// Holds changes to existing resources.
    /// </summary>
    [JsonPropertyName("changes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, TextEdit[]>? Changes { get; set; }

    /// <summary>
    /// Depending on the client capability <see cref="WorkspaceEditSetting.ResourceOperations"/>,
    /// document changes are either an array of <see cref="TextDocumentEdit"/> to express changes
    /// to n different text documents where each text document edit addresses a specific version of a text document,
    /// or it can contain above `TextDocumentEdit`s mixed with create, rename and delete file / folder operations.
    /// <para>
    /// Whether a client supports versioned document edits is expressed via the
    /// <see cref="WorkspaceEditSetting.DocumentChanges"/> client capability.
    /// </para>
    /// <para>
    /// If a client neither supports <see cref="WorkspaceEditSetting.DocumentChanges"/> nor
    /// <see cref="WorkspaceEditSetting.ResourceOperations"/> then only plain <see cref="TextEdit"/>s
    /// using the <see cref="Changes"/> property are supported.
    /// </para>
    /// </summary>
    [JsonPropertyName("documentChanges")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SumType<TextDocumentEdit[], SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]>? DocumentChanges { get; set; }

    /// <summary>
    /// A map of change annotations that can be referenced in <see cref="AnnotatedTextEdit"/>s or create, rename
    /// and delete file / folder operations.
    /// <para>
    /// </para>
    /// Whether clients honor this property depends on the client capability <see cref="WorkspaceEditSetting.ChangeAnnotationSupport"/>.
    /// </summary>
    /// <remarks>Since 3.16</remarks>
    [JsonPropertyName("changeAnnotations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<ChangeAnnotationIdentifier, ChangeAnnotation>? ChangeAnnotations { get; init; }
}
