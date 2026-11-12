// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The params sent in a 'typeHierarchy/supertypes' request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#typeHierarchySupertypesParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class TypeHierarchySupertypesParams : IWorkDoneProgressParams, IPartialResultParams<TypeHierarchyItem[]>
{
    /// <summary>
    /// The <see cref="TypeHierarchyItem"/> for which to return supertypes
    /// </summary>
    [JsonPropertyName("item")]
    [JsonRequired]
    public TypeHierarchyItem Item { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<TypeHierarchyItem[]>? PartialResultToken { get; set; }
}
