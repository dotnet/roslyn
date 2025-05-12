// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Describes how the client handles change annotations on workspace edits.
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class ChangeAnnotationSupport
{
    /// <summary>
    /// Whether the client groups edits with equal labels into tree nodes,
	/// for instance all edits labelled with "Changes in Strings" would
    /// be a tree node..
    /// </summary>
    [JsonPropertyName("groupsOnLabel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? GroupsOnLabel { get; init; }
}
