// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Notebook specific client capabilities
/// </summary>
/// <remarks>Since LSP 3.15</remarks>
internal class WindowClientCapabilities : IWorkDoneProgressOptions
{
    /// <summary>
    /// Indicates whether the client supports server initiated
    /// progress using the `window/workDoneProgress/create` request.
    /// <para>
    /// The capability also controls Whether client supports handling
    /// of progress notifications. If set, servers are allowed to report a
    /// `workDoneProgress` property in the request specific server
    /// capabilities.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    [JsonPropertyName("workDoneProgress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WorkDoneProgress { get; init; }

    /// <summary>
    /// The client supports sending execution summary data per cell.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("showMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ShowMessageRequestClientCapabilities? ShowMessage { get; init; }

    /// <summary>
    /// Client capabilities for the show document request.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("showDocument")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ShowDocumentClientCapabilities? ShowDocument { get; init; }
}
