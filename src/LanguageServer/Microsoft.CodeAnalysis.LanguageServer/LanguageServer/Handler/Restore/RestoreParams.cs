// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal sealed record RestoreParams(
    // An empty set of project file paths means restore all projects in the workspace.
    [property: JsonPropertyName("projectFilePaths")] string[] ProjectFilePaths
) : IPartialResultParams<RestorePartialResult>
{
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<RestorePartialResult>? PartialResultToken { get; set; }
}
