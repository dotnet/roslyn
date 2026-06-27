// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.NET.ProjectData;

public sealed record CachedSourceFile
{
	public required string FilePath { get; init; }
	public string? Link { get; init; }
}
