﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Runtime.Serialization;

/// <summary>
/// Class representing the cancellation data returned from a diagnostic request.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#diagnosticServerCancellationData">Language Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
internal class DiagnosticServerCancellationData
{
    /// <summary>
    /// Gets or sets a value indicating whether the client should re-trigger the request.
    /// </summary>
    [DataMember(Name = "retriggerRequest")]
    public bool RetriggerRequest
    {
        get;
        set;
    }
}