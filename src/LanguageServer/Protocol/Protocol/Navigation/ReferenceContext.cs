// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing reference context information for find reference request parameter.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#referenceContext">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class ReferenceContext
{
    /// <summary>
    /// Include the declaration of the current symbol.
    /// </summary>
    [JsonPropertyName("includeDeclaration")]
    public bool IncludeDeclaration { get; set; }
}
