// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The params sent in a 'callHierarchy/incomingCalls' request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#callHierarchyIncomingCallsParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class CallHierarchyOutgoingCallsParams : TextDocumentPositionParams, IWorkDoneProgressParams, IPartialResultParams<CallHierarchyOutgoingCall[]>
{
    /// <summary>
    /// The item returned from `textDocument/prepareCallHierarchy`
    /// </summary>
    [JsonPropertyName("item")]
    [JsonRequired]
    public CallHierarchyItem Item { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="LocationLink"/> may only be used if the client opts in via <see cref="DefinitionClientCapabilities.LinkSupport"/>
    /// </remarks>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<CallHierarchyOutgoingCall[]>? PartialResultToken { get; set; }
}
