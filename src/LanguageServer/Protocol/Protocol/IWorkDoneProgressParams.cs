// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Interface to describe parameters for requests that support reporting work done via the <c>$/progress</c> notification.
/// <para>
/// </para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#clientInitiatedProgress">Language Server Protocol specification</see> for additional information.
/// </summary>
/// <remarks>Since LSP 3.15</remarks>
internal interface IWorkDoneProgressParams
{
    /// <summary>
    /// An optional token that a server can use to report work done progress.
    /// <para>
    /// The derived classes <see cref="WorkDoneProgressBegin"/>, <see cref="WorkDoneProgressReport"/> and <see cref="WorkDoneProgressEnd"/>
    /// are used to report the beginning, progression, and end of the operation.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    // NOTE: these JSON attributes are not inherited, they are here as a reference for implementations
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }
}
