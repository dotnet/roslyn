// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Represents a cached embedded resource item from the <c>[embeddedResources]</c> section
/// of a <c>.lscache</c> file, with its associated metadata.
/// </summary>
public sealed record CachedEmbeddedResource
{
	public required string FilePath { get; init; }
	public string? Generator { get; init; }
	public string? LastGenOutput { get; init; }
	public string? CustomToolNamespace { get; init; }
}
