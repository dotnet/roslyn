// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Represents the parsed data of a single project configuration slice from a <c>.lscache</c> file.
/// </summary>
public sealed record CachedSliceData
{
	public required string LanguageName { get; init; }
	public required string ProjectFilePath { get; init; }
	public required ImmutableDictionary<string, string> SliceDimensions { get; init; }
	public required ImmutableArray<string> CommandLineArguments { get; init; }
	public required ImmutableArray<CachedSourceFile> SourceFiles { get; init; }
	public required ImmutableArray<CachedMetadataReference> MetadataReferences { get; init; }
	public required ImmutableArray<string> AnalyzerReferences { get; init; }
	public required ImmutableArray<string> AnalyzerConfigFiles { get; init; }
	public required ImmutableArray<string> AdditionalFiles { get; init; }
	public ImmutableArray<CachedEmbeddedResource> EmbeddedResources { get; init; } = [];
	public required ImmutableArray<CachedProjectReference> ProjectReferences { get; init; }
	public required ImmutableArray<string> Capabilities { get; init; }
	public required ImmutableDictionary<string, string> Properties { get; init; }
	public bool IsPrimary { get; init; }
	public bool LastDesignTimeBuildSucceeded { get; init; }
}
