// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents a text edit with a change annotation
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#annotatedTextEdit">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal class AnnotatedTextEdit : TextEdit
{
    /// <summary>
    /// The annotation identifier.
    /// </summary>
    [JsonPropertyName("annotationId")]
    [JsonRequired]
    public ChangeAnnotationIdentifier AnnotationId { get; init; }
}
