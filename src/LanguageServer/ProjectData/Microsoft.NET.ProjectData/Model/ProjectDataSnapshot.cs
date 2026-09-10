// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Immutable snapshot of all data for a single project configuration slice.
/// </summary>
public sealed class ProjectDataSnapshot
{
	public ProjectDataSnapshot(
		string projectPath,
		ImmutableDictionary<string, string> dimensions,
		KeyValueCollection properties,
		ImmutableDictionary<string, ImmutableArray<ProjectDataItem>> itemsByType,
		bool isPrimary,
		bool lastDesignTimeBuildSucceeded,
		ImmutableArray<string> capabilities = default)
	{
		this.ProjectPath = projectPath;
		this.Dimensions = dimensions;
		this.Properties = properties;
		this.ItemsByType = itemsByType;
		this.IsPrimary = isPrimary;
		this.LastDesignTimeBuildSucceeded = lastDesignTimeBuildSucceeded;
		this.Capabilities = capabilities.IsDefault ? [] : capabilities;
	}

	/// <summary>The absolute path of the project file.</summary>
	public string ProjectPath { get; }

	/// <summary>The project configuration slice dimensions (e.g., TargetFramework=net10.0).</summary>
	public ImmutableDictionary<string, string> Dimensions { get; }

	/// <summary>Project properties (e.g., AssemblyName, RootNamespace, TargetPath).</summary>
	public KeyValueCollection Properties { get; }

	/// <summary>Items grouped by type (e.g., Compile, MetadataReference, AnalyzerReference).</summary>
	public ImmutableDictionary<string, ImmutableArray<ProjectDataItem>> ItemsByType { get; }

	/// <summary>Whether this is the primary slice for project-level queries.</summary>
	public bool IsPrimary { get; }

	/// <summary>Whether the last design-time build succeeded.</summary>
	public bool LastDesignTimeBuildSucceeded { get; }

	/// <summary>Capabilities exported by this project slice.</summary>
	public ImmutableArray<string> Capabilities { get; }
}
