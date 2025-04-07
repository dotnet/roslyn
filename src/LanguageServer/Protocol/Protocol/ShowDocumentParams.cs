// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Params for the 'windows/showDocument' request
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#showDocumentParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal class ShowDocumentParams
{
    /// <summary>
    /// The uri to show.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonRequired]
    public Uri Uri { get; init; }

    /// <summary>
    /// Indicates whether to show the resource in an external program.
    /// <para>
    /// To show, for example, <c>https://code.visualstudio.com/</c> in the default
    /// web browser, set <see cref="External"/> to <see langword="true"/>.
    /// </para>
    /// </summary>
    [JsonPropertyName("external")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool External { get; init; }

    /// <summary>
    /// Optionally indicates whether the editor showing the document should take focus or not.
    /// <para>
    /// Clients might ignore this property if an external program is started.
    /// </para>
    /// </summary>
    [JsonPropertyName("takeFocus")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool TakeFocus { get; init; }

    /// <summary>
    /// An optional selection range if the document is a text document.
    /// <para>
    /// Clients might ignore the property if an external program is started or the file is not a text file.
    /// </para>
    /// </summary>
    [JsonPropertyName("selection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Range? Selection { get; init; }
}
