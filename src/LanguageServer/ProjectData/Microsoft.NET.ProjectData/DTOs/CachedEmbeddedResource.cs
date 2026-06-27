// Copyright (c) Microsoft Corporation. All rights reserved.

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
