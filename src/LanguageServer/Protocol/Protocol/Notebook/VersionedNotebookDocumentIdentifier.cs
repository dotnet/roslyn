// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A versioned notebook document identifier.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#versionedNotebookDocumentIdentifier">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class VersionedNotebookDocumentIdentifier
{

    /// <summary>
    /// The version number of this notebook document.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonRequired]
    public int Version { get; init; }

    /// <summary>
    /// The notebook document's URI.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonRequired]
    public Uri Uri { get; init; }
}
