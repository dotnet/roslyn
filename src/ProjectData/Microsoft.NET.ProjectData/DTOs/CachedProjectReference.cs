// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Represents an evaluated project-to-project reference stored in a project-data cache.
/// </summary>
public sealed record CachedProjectReference
{
	public required string FilePath { get; init; }

	/// <summary>
	/// Whether the referenced project's output participates in compilation, or
	/// <see langword="null"/> when a pre-2.1 cache did not carry this metadata.
	/// </summary>
	public bool? ReferenceOutputAssembly { get; init; }
}
