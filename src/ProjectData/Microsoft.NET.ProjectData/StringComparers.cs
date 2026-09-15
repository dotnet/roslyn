// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Well-known string comparers and comparisons for Data Model types.
/// </summary>
/// <remarks>
/// This pattern is borrowed from dotnet-project-system, where centralizing comparison
/// semantics avoids scattered <see cref="StringComparer"/> and <see cref="StringComparison"/>
/// choices throughout the codebase.
/// </remarks>
public static class StringComparers
{
	/// <summary>Comparer for item type names (e.g., "Compile", "MetadataReference").</summary>
	public static StringComparer ItemType { get; } = StringComparer.OrdinalIgnoreCase;

	/// <summary>Comparer for item metadata key names (e.g., "aliases", "folderNames").</summary>
	public static StringComparer ItemMetadataName { get; } = StringComparer.OrdinalIgnoreCase;

	/// <summary>Comparer for property names (e.g., "AssemblyName", "TargetPath").</summary>
	public static StringComparer PropertyName { get; } = StringComparer.OrdinalIgnoreCase;

	/// <summary>Comparer for capability names (e.g., "SupportsHotReload", "SupportsIncrementalBuild").</summary>
	public static StringComparer Capabilities { get; } = StringComparer.OrdinalIgnoreCase;

	/// <summary>Comparer for item spec values (file paths or identifiers).</summary>
	public static StringComparer ItemSpec { get; } =
		RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

	/// <summary>Comparer for file paths, respecting platform case sensitivity.</summary>
	public static StringComparer Paths { get; } =
		RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
}

/// <summary>
/// Well-known string comparisons for Data Model types.
/// </summary>
public static class StringComparisons
{
	/// <summary>Comparison for item type names.</summary>
	public static StringComparison ItemType => StringComparison.OrdinalIgnoreCase;

	/// <summary>Comparison for item metadata key names.</summary>
	public static StringComparison ItemMetadataName => StringComparison.OrdinalIgnoreCase;

	/// <summary>Comparison for property names.</summary>
	public static StringComparison PropertyName => StringComparison.OrdinalIgnoreCase;

	/// <summary>Comparison for file paths, respecting platform case sensitivity.</summary>
	public static StringComparison Paths { get; } =
		RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
}
