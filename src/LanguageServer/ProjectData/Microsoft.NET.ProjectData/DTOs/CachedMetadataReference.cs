// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Immutable;

namespace Microsoft.NET.ProjectData;

public sealed record CachedMetadataReference
{
	public required string FilePath { get; init; }
	public required ImmutableArray<string> Aliases { get; init; }
	public bool EmbedInteropTypes { get; init; }
}
