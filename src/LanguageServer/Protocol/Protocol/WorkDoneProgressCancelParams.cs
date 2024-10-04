// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The parameters sent with the 'window/workDoneProgress/cancel' notification.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#window_workDoneProgress_cancel">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.15</remarks>
internal class WorkDoneProgressCancelParams
{
    /// <summary>
    /// The token used to report progress.
    /// </summary>
    [JsonPropertyName("token")]
    [JsonRequired]
    public SumType<int, string> Token { get; init; }
}
