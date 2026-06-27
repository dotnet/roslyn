// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.NET.ProjectData;

/// <summary>
/// A single immutable item in a project (e.g., a source file, a reference).
/// </summary>
public readonly struct ProjectDataItem
{
	public ProjectDataItem(string itemSpec, KeyValueCollection metadata)
	{
		this.ItemSpec = itemSpec;
		this.Metadata = metadata;
	}

	/// <summary>The item specification (typically a file path).</summary>
	public string ItemSpec { get; }

	/// <summary>Per-item metadata (e.g., Aliases, Link, EmbedInteropTypes).</summary>
	public KeyValueCollection Metadata { get; }
}
