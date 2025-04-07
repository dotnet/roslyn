// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Additional information about the context in which inline values were requested.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlineValueContext">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class InlineValueContext
{
    /// <summary>
    /// The stack frame (as a DAP Id) where the execution has stopped.
    /// </summary>
    [JsonPropertyName("frameId")]
    [JsonRequired]
    public int FrameId { get; set; }

    /// <summary>
    /// The document range where execution has stopped. Typically the end
    /// position of the range denotes the line where the inline values are shown
    /// </summary>
    [JsonPropertyName("stoppedLocation")]
    [JsonRequired]
    public Range StoppedLocation { get; set; }
}
