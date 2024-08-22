// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Describes how the client handles stale requests
/// </summary>
internal class StaleRequestSupport
{
    /// <summary>
    /// The client will actively cancel the request.
    /// </summary>
    [JsonPropertyName("cancel")]
    [JsonRequired]
    public bool Cancel { get; init; }

    /// <summary>
    /// The list of requests for which the client
    /// will retry the request if it receives a
    /// response with error code `ContentModified`
    /// </summary>
    [JsonPropertyName("retryOnContentModified")]
    public string[] RetryOnContentModified { get; init; }
}
