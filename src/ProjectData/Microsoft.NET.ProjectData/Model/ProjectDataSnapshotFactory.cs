// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Converts parsed <see cref="CachedSliceData"/> from project-data cache files
/// into canonical immutable <see cref="ProjectDataSnapshot"/> instances.
/// </summary>
public static class ProjectDataSnapshotFactory
{
	/// <summary>
	/// The schema for project-level properties. All properties from the cache file
	/// are stored here, along with the well-known properties from the schema.
	/// </summary>
	/// <remarks>
	/// This schema is shared across all snapshots and is the basis for O(1) property lookups.
	/// Additional properties found in cache files that aren't in this schema are still accessible
	/// through a per-snapshot schema extension.
	/// </remarks>
	private static readonly KeySchema PropertySchema = new(ProjectProperties.All);

	/// <summary>
	/// Well-known item metadata schemas.
	/// </summary>
	private static readonly KeySchema SourceFileMetadataSchema = new([ProjectItems.Compile.Link], StringComparers.ItemMetadataName);
	private static readonly KeySchema MetadataReferenceMetadataSchema = new([ProjectItems.MetadataReference.Aliases, ProjectItems.MetadataReference.EmbedInteropTypes], StringComparers.ItemMetadataName);
	private static readonly KeySchema ProjectReferenceMetadataSchema = new([ProjectItems.ProjectReference.ReferenceOutputAssembly], StringComparers.ItemMetadataName);
	private static readonly KeySchema EmbeddedResourceMetadataSchema = new([ProjectItems.EmbeddedResource.Generator, ProjectItems.EmbeddedResource.LastGenOutput, ProjectItems.EmbeddedResource.CustomToolNamespace], StringComparers.ItemMetadataName);

	/// <summary>
	/// Converts a set of parsed cache slices into canonical project-data snapshots.
	/// </summary>
	/// <param name="slices">The parsed cache slices.</param>
	/// <param name="solutionPath">
	/// Runtime-injected solution path. Overrides any stale <c>SolutionPath</c> value
	/// that may exist in the cache. When <see langword="null"/> or empty, the key is
	/// omitted from the snapshot properties so consumers see "no solution."
	/// </param>
	public static ImmutableArray<ProjectDataSnapshot> CreateSnapshots(ImmutableArray<CachedSliceData> slices, string? solutionPath = null)
	{
		if (slices.IsDefaultOrEmpty)
			return [];

		// The cache file produces a "shared" slice (no dimensions) with common data
		// (source files, analyzers, common properties) and per-TFM slices with
		// TFM-specific data (metadata references, TFM properties). Merge shared data
		// into each TFM slice so consumers get the complete picture per slice.
		CachedSliceData? sharedSlice = null;
		List<CachedSliceData>? tfmSlices = null;

		foreach (CachedSliceData slice in slices)
		{
			if (slice.SliceDimensions.IsEmpty)
				sharedSlice = slice;
			else
			{
				tfmSlices ??= [];
				tfmSlices.Add(slice);
			}
		}

		if (sharedSlice is not null && tfmSlices is { Count: > 0 })
		{
			// Multi-TFM: merge shared data into each TFM slice, drop the shared slice.
			ImmutableArray<ProjectDataSnapshot>.Builder builder = ImmutableArray.CreateBuilder<ProjectDataSnapshot>(tfmSlices.Count);
			ProjectDataSnapshot sharedSnapshot = CreateSnapshot(sharedSlice, solutionPath);

			foreach (CachedSliceData tfmSlice in tfmSlices)
			{
				ProjectDataSnapshot tfmSnapshot = CreateSnapshot(tfmSlice, solutionPath);
				builder.Add(MergeSharedIntoSlice(sharedSnapshot, tfmSnapshot));
			}

			return builder.MoveToImmutable();
		}

		// Single-TFM or no shared slice: produce snapshots as-is.
		ImmutableArray<ProjectDataSnapshot>.Builder result = ImmutableArray.CreateBuilder<ProjectDataSnapshot>(slices.Length);
		foreach (CachedSliceData slice in slices)
		{
			result.Add(CreateSnapshot(slice, solutionPath));
		}

		return result.MoveToImmutable();
	}

	/// <summary>
	/// Validates that required properties and item types from the schema are present in every snapshot.
	/// Returns a list of missing required names (empty if all present).
	/// </summary>
	public static IReadOnlyList<string> ValidateRequiredData(ImmutableArray<ProjectDataSnapshot> snapshots)
	{
		if (snapshots.IsDefaultOrEmpty)
			return [];

		List<string>? missing = null;
		foreach (ProjectDataSnapshot snapshot in snapshots)
		{
			foreach (string requiredProp in ProjectProperties.Required)
			{
				if (!snapshot.Properties.TryGetValue(requiredProp, out _))
				{
					missing ??= [];
					string sliceLabel = snapshot.Dimensions.TryGetValue(ProjectProperties.TargetFramework, out string? tf) ? tf : "default";
					missing.Add($"property:{requiredProp} (slice:{sliceLabel})");
				}
			}

			foreach (string requiredItem in ProjectItems.RequiredItemTypes)
			{
				if (!snapshot.ItemsByType.ContainsKey(requiredItem))
				{
					missing ??= [];
					string sliceLabel = snapshot.Dimensions.TryGetValue(ProjectProperties.TargetFramework, out string? tf) ? tf : "default";
					missing.Add($"item:{requiredItem} (slice:{sliceLabel})");
				}
			}
		}

		return missing ?? (IReadOnlyList<string>)[];
	}

	/// <summary>
	/// Converts a single parsed cache slice into a canonical project-data snapshot.
	/// </summary>
	public static ProjectDataSnapshot CreateSnapshot(CachedSliceData slice, string? solutionPath = null)
	{
		KeyValueCollection properties = BuildProperties(slice, solutionPath);
		ImmutableDictionary<string, ImmutableArray<ProjectDataItem>> itemsByType = BuildItems(slice);

		return new ProjectDataSnapshot(
			projectPath: slice.ProjectFilePath,
			dimensions: slice.SliceDimensions,
			properties: properties,
			itemsByType: itemsByType,
			isPrimary: slice.IsPrimary,
			lastDesignTimeBuildSucceeded: slice.LastDesignTimeBuildSucceeded,
			capabilities: slice.Capabilities);
	}

	/// <summary>
	/// Merges items and properties from the shared (dimensionless) slice into a per-TFM slice.
	/// TFM-specific items and properties take priority; shared data fills in the gaps.
	/// </summary>
	private static ProjectDataSnapshot MergeSharedIntoSlice(ProjectDataSnapshot shared, ProjectDataSnapshot tfmSlice)
	{
		// Merge items: start with shared, overlay TFM-specific.
		ImmutableDictionary<string, ImmutableArray<ProjectDataItem>>.Builder mergedItems =
			ImmutableDictionary.CreateBuilder<string, ImmutableArray<ProjectDataItem>>(StringComparer.OrdinalIgnoreCase);

		foreach (KeyValuePair<string, ImmutableArray<ProjectDataItem>> kvp in shared.ItemsByType)
			mergedItems[kvp.Key] = kvp.Value;

		foreach (KeyValuePair<string, ImmutableArray<ProjectDataItem>> kvp in tfmSlice.ItemsByType)
		{
			if (mergedItems.TryGetValue(kvp.Key, out ImmutableArray<ProjectDataItem> existing))
				mergedItems[kvp.Key] = existing.AddRange(kvp.Value);
			else
				mergedItems[kvp.Key] = kvp.Value;
		}

		// Properties: merge shared as base, TFM-specific overrides.
		KeyValueCollection mergedProperties;
		if (tfmSlice.Properties.IsEmpty)
		{
			mergedProperties = shared.Properties;
		}
		else if (shared.Properties.IsEmpty)
		{
			mergedProperties = tfmSlice.Properties;
		}
		else
		{
			// Merge via dictionaries since the collections may have different schemas
			// (different sets of extra properties beyond the well-known schema).
			Dictionary<string, string> merged = shared.Properties.ToDictionary();
			foreach (KeyValuePair<string, string> kvp in tfmSlice.Properties)
			{
				merged[kvp.Key] = kvp.Value;
			}

			// Rebuild a KeyValueCollection from the merged dictionary using the shared schema.
			mergedProperties = BuildKeyValueCollection(merged, shared.Properties.Schema);
		}

		return new ProjectDataSnapshot(
			projectPath: tfmSlice.ProjectPath,
			dimensions: tfmSlice.Dimensions,
			properties: mergedProperties,
			itemsByType: mergedItems.ToImmutable(),
			isPrimary: tfmSlice.IsPrimary,
			lastDesignTimeBuildSucceeded: tfmSlice.LastDesignTimeBuildSucceeded || shared.LastDesignTimeBuildSucceeded,
			capabilities: MergeCapabilities(shared.Capabilities, tfmSlice.Capabilities));
	}

	private static ImmutableArray<string> MergeCapabilities(ImmutableArray<string> sharedCapabilities, ImmutableArray<string> sliceCapabilities)
	{
		if (sharedCapabilities.IsDefaultOrEmpty)
			return sliceCapabilities.IsDefault ? [] : sliceCapabilities;
		if (sliceCapabilities.IsDefaultOrEmpty)
			return sharedCapabilities;

		return sharedCapabilities
			.AddRange(sliceCapabilities)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(static c => c, StringComparer.OrdinalIgnoreCase)
			.ToImmutableArray();
	}

	private static KeyValueCollection BuildProperties(CachedSliceData slice, string? solutionPath)
	{
		// Build the properties array using the schema for fast lookups.
		// Properties not in the schema are stored via a dynamic approach.
		ImmutableArray<string?>.Builder values = ImmutableArray.CreateBuilder<string?>(PropertySchema.Count);
		values.Count = PropertySchema.Count;

		// Collect any properties not in the schema.
		List<string>? extraKeys = null;
		List<string?>? extraValues = null;

		foreach (KeyValuePair<string, string> kvp in slice.Properties)
		{
			if (PropertySchema.TryGetIndex(kvp.Key, out int index))
			{
				values[index] = kvp.Value;
			}
			else
			{
				// Property not in the well-known schema; still store it.
				// We'll build a dynamic schema extension.
				extraKeys ??= [];
				extraValues ??= [];
				extraKeys.Add(kvp.Key);
				extraValues.Add(kvp.Value);
			}
		}

		// Inject runtime SolutionPath; overrides any stale value from the cache.
		// When empty/null, clear the slot so consumers see "no solution" rather than "".
		if (PropertySchema.TryGetIndex(ProjectProperties.SolutionPath, out int solutionPathIndex))
		{
			values[solutionPathIndex] = string.IsNullOrEmpty(solutionPath) ? null : solutionPath;
		}

		if (extraKeys is not null)
		{
			return BuildKeyValueCollection(PropertySchema, values.MoveToImmutable(), extraKeys, extraValues!);
		}

		return new KeyValueCollection(PropertySchema, values.MoveToImmutable());
	}

	private static KeyValueCollection BuildKeyValueCollection(Dictionary<string, string> valuesByKey, KeySchema baseSchema)
	{
		ImmutableArray<string?>.Builder values = ImmutableArray.CreateBuilder<string?>(baseSchema.Count);
		values.Count = baseSchema.Count;

		List<string>? extraKeys = null;
		List<string?>? extraValues = null;

		foreach (KeyValuePair<string, string> kvp in valuesByKey)
		{
			if (baseSchema.TryGetIndex(kvp.Key, out int index))
			{
				values[index] = kvp.Value;
			}
			else
			{
				extraKeys ??= [];
				extraValues ??= [];
				extraKeys.Add(kvp.Key);
				extraValues.Add(kvp.Value);
			}
		}

		if (extraKeys is null)
		{
			return new KeyValueCollection(baseSchema, values.MoveToImmutable());
		}

		return BuildKeyValueCollection(baseSchema, values.MoveToImmutable(), extraKeys, extraValues!);
	}

	private static KeyValueCollection BuildKeyValueCollection(KeySchema baseSchema, ImmutableArray<string?> values, List<string> extraKeys, List<string?> extraValues)
	{
		// Build a combined schema that includes both well-known and extra properties.
		List<string> allKeys = new(baseSchema.Count + extraKeys.Count);
		for (int i = 0; i < baseSchema.Count; i++)
		{
			allKeys.Add(baseSchema.GetKey(i));
		}
		allKeys.AddRange(extraKeys);

		KeySchema combinedSchema = new(allKeys);
		ImmutableArray<string?>.Builder combinedValues = ImmutableArray.CreateBuilder<string?>(allKeys.Count);
		combinedValues.Count = allKeys.Count;

		// Copy known values.
		for (int i = 0; i < values.Length; i++)
		{
			combinedValues[i] = values[i];
		}

		// Copy extra values.
		for (int i = 0; i < extraValues.Count; i++)
		{
			combinedValues[baseSchema.Count + i] = extraValues[i];
		}

		return new KeyValueCollection(combinedSchema, combinedValues.MoveToImmutable());
	}

	private static ImmutableDictionary<string, ImmutableArray<ProjectDataItem>> BuildItems(CachedSliceData slice)
	{
		ImmutableDictionary<string, ImmutableArray<ProjectDataItem>>.Builder items =
			ImmutableDictionary.CreateBuilder<string, ImmutableArray<ProjectDataItem>>(StringComparers.ItemType);

		// Source files → Compile items
		AddItems(items, ProjectItems.Compile.ItemType, slice.SourceFiles, BuildSourceFileItems);

		// Metadata references → MetadataReference items
		AddItems(items, ProjectItems.MetadataReference.ItemType, slice.MetadataReferences, BuildMetadataReferenceItems);

		// Analyzer references → AnalyzerReference items
		AddSimpleItems(items, ProjectItems.AnalyzerReference, slice.AnalyzerReferences);

		// Analyzer config files → AnalyzerConfigFile items
		AddSimpleItems(items, ProjectItems.AnalyzerConfigFile, slice.AnalyzerConfigFiles);

		// Additional files → AdditionalFile items
		AddSimpleItems(items, ProjectItems.AdditionalFile, slice.AdditionalFiles);

		// Embedded resources → EmbeddedResource items
		AddItems(items, ProjectItems.EmbeddedResource.ItemType, slice.EmbeddedResources, BuildEmbeddedResourceItems);

		// Project references → ProjectReference items
		AddItems(items, ProjectItems.ProjectReference.ItemType, slice.ProjectReferences, BuildProjectReferenceItems);

		// Command-line arguments → CommandLineArgument items
		AddSimpleItems(items, ProjectItems.CommandLineArgument, slice.CommandLineArguments);

		return items.ToImmutable();
	}

	/// <summary>
	/// Adds items to the builder. Always adds the key (even with an empty array) so that
	/// required item types are present for validation and downstream consumers.
	/// </summary>
	private static void AddItems<T>(
		ImmutableDictionary<string, ImmutableArray<ProjectDataItem>>.Builder items,
		string itemType,
		ImmutableArray<T> source,
		Func<ImmutableArray<T>, ImmutableArray<ProjectDataItem>> builder)
	{
		items[itemType] = source.IsDefaultOrEmpty ? [] : builder(source);
	}

	private static void AddSimpleItems(
		ImmutableDictionary<string, ImmutableArray<ProjectDataItem>>.Builder items,
		string itemType,
		ImmutableArray<string> source)
	{
		items[itemType] = source.IsDefaultOrEmpty ? [] : BuildSimpleItems(source);
	}

	private static ImmutableArray<ProjectDataItem> BuildSourceFileItems(ImmutableArray<CachedSourceFile> sourceFiles)
	{
		ImmutableArray<ProjectDataItem>.Builder builder = ImmutableArray.CreateBuilder<ProjectDataItem>(sourceFiles.Length);

		foreach (CachedSourceFile file in sourceFiles)
		{
			ImmutableArray<string?>.Builder metaValues = ImmutableArray.CreateBuilder<string?>(SourceFileMetadataSchema.Count);
			metaValues.Count = SourceFileMetadataSchema.Count;

			if (!string.IsNullOrEmpty(file.Link))
			{
				metaValues[0] = file.Link;
			}

			builder.Add(new ProjectDataItem(
				file.FilePath,
				new KeyValueCollection(SourceFileMetadataSchema, metaValues.MoveToImmutable())));
		}

		return builder.MoveToImmutable();
	}

	private static ImmutableArray<ProjectDataItem> BuildMetadataReferenceItems(ImmutableArray<CachedMetadataReference> references)
	{
		ImmutableArray<ProjectDataItem>.Builder builder = ImmutableArray.CreateBuilder<ProjectDataItem>(references.Length);

		foreach (CachedMetadataReference reference in references)
		{
			ImmutableArray<string?>.Builder metaValues = ImmutableArray.CreateBuilder<string?>(MetadataReferenceMetadataSchema.Count);
			metaValues.Count = MetadataReferenceMetadataSchema.Count;

			if (!reference.Aliases.IsDefaultOrEmpty)
			{
				metaValues[0] = string.Join(",", reference.Aliases); // aliases
			}

			if (reference.EmbedInteropTypes)
			{
				metaValues[1] = ""; // embedInteropTypes (flag)
			}

			builder.Add(new ProjectDataItem(
				reference.FilePath,
				new KeyValueCollection(MetadataReferenceMetadataSchema, metaValues.MoveToImmutable())));
		}

		return builder.MoveToImmutable();
	}

	private static ImmutableArray<ProjectDataItem> BuildProjectReferenceItems(ImmutableArray<CachedProjectReference> references)
	{
		ImmutableArray<ProjectDataItem>.Builder builder = ImmutableArray.CreateBuilder<ProjectDataItem>(references.Length);

		foreach (CachedProjectReference reference in references)
		{
			builder.Add(new ProjectDataItem(
				reference.FilePath,
				new KeyValueCollection(
					ProjectReferenceMetadataSchema,
					[
						reference.ReferenceOutputAssembly switch
						{
							true => "true",
							false => "false",
							null => null,
						},
					])));
		}

		return builder.MoveToImmutable();
	}

	private static ImmutableArray<ProjectDataItem> BuildEmbeddedResourceItems(ImmutableArray<CachedEmbeddedResource> resources)
	{
		ImmutableArray<ProjectDataItem>.Builder builder = ImmutableArray.CreateBuilder<ProjectDataItem>(resources.Length);

		foreach (CachedEmbeddedResource resource in resources)
		{
			ImmutableArray<string?>.Builder metaValues = ImmutableArray.CreateBuilder<string?>(EmbeddedResourceMetadataSchema.Count);
			metaValues.Count = EmbeddedResourceMetadataSchema.Count;

			metaValues[0] = resource.Generator; // Generator
			metaValues[1] = resource.LastGenOutput; // LastGenOutput
			metaValues[2] = resource.CustomToolNamespace; // CustomToolNamespace

			builder.Add(new ProjectDataItem(
				resource.FilePath,
				new KeyValueCollection(EmbeddedResourceMetadataSchema, metaValues.MoveToImmutable())));
		}

		return builder.MoveToImmutable();
	}

	private static ImmutableArray<ProjectDataItem> BuildSimpleItems(ImmutableArray<string> paths)
	{
		ImmutableArray<ProjectDataItem>.Builder builder = ImmutableArray.CreateBuilder<ProjectDataItem>(paths.Length);

		foreach (string path in paths)
		{
			builder.Add(new ProjectDataItem(path, KeyValueCollection.Empty));
		}

		return builder.MoveToImmutable();
	}
}
