// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.NET.ProjectData.Tests;

public class ProjectDataSnapshotFactoryTests
{
	[Fact]
	public void CreateSnapshots_Merges_Shared_Slice_Into_TargetFramework_Slices()
	{
		CachedSliceData sharedSlice = CreateSlice(
			properties: new() { ["AssemblyName"] = "App" },
			sourceFiles: [new CachedSourceFile { FilePath = @"C:\repo\App\Program.cs" }]);
		CachedSliceData tfmSlice = CreateSlice(
			sliceDimensions: new() { ["TargetFramework"] = "net10.0" },
			properties: new() { ["TargetFramework"] = "net10.0" },
			metadataReferences: [new CachedMetadataReference { FilePath = @"C:\packs\System.Runtime.dll", Aliases = [] }]);

		ImmutableArray<ProjectDataSnapshot> snapshots = ProjectDataSnapshotFactory.CreateSnapshots([sharedSlice, tfmSlice]);

		ProjectDataSnapshot snapshot = Assert.Single(snapshots);
		Assert.Equal("App", snapshot.Properties["AssemblyName"]);
		Assert.Equal("net10.0", snapshot.Properties["TargetFramework"]);
		Assert.True(snapshot.ItemsByType.TryGetValue("Compile", out ImmutableArray<ProjectDataItem> compileItems));
		Assert.Equal(@"C:\repo\App\Program.cs", Assert.Single(compileItems).ItemSpec);
		Assert.True(snapshot.ItemsByType.TryGetValue("MetadataReference", out ImmutableArray<ProjectDataItem> referenceItems));
		Assert.Equal(@"C:\packs\System.Runtime.dll", Assert.Single(referenceItems).ItemSpec);
	}

	[Fact]
	public void CreateSnapshot_Uses_Runtime_SolutionPath_Instead_Of_Cached_Value()
	{
		CachedSliceData slice = CreateSlice(properties: new()
		{
			["SolutionPath"] = @"C:\stale\Stale.sln",
		});

		ProjectDataSnapshot snapshot = ProjectDataSnapshotFactory.CreateSnapshot(slice, @"C:\current\Current.sln");

		Assert.Equal(@"C:\current\Current.sln", snapshot.Properties["SolutionPath"]);
	}

	[Fact]
	public void CreateSnapshot_Omits_SolutionPath_When_Runtime_SolutionPath_Is_Empty()
	{
		CachedSliceData slice = CreateSlice(properties: new()
		{
			["SolutionPath"] = @"C:\stale\Stale.sln",
		});

		ProjectDataSnapshot snapshot = ProjectDataSnapshotFactory.CreateSnapshot(slice, solutionPath: null);

		Assert.False(snapshot.Properties.TryGetValue("SolutionPath", out _));
	}

	[Fact]
	public void CreateSnapshot_PreservesProjectReferenceOutputAssemblyMetadata()
	{
		CachedSliceData slice = CreateSlice(
			projectReferences:
			[
				new CachedProjectReference
				{
					FilePath = @"C:\repo\BuildOnly\BuildOnly.csproj",
					ReferenceOutputAssembly = false,
				},
				new CachedProjectReference
				{
					FilePath = @"C:\repo\Library\Library.csproj",
					ReferenceOutputAssembly = true,
				},
				new CachedProjectReference
				{
					FilePath = @"C:\repo\Legacy\Legacy.csproj",
				},
			]);

		ProjectDataSnapshot snapshot = ProjectDataSnapshotFactory.CreateSnapshot(slice);
		ImmutableArray<ProjectDataItem> references = snapshot.ItemsByType["ProjectReference"];

		Assert.Equal(3, references.Length);
		Assert.Equal("false", references[0].Metadata["ReferenceOutputAssembly"]);
		Assert.Equal("true", references[1].Metadata["ReferenceOutputAssembly"]);
		Assert.False(references[2].Metadata.TryGetValue("ReferenceOutputAssembly", out _));
	}

	private static CachedSliceData CreateSlice(
		Dictionary<string, string>? sliceDimensions = null,
		Dictionary<string, string>? properties = null,
		ImmutableArray<CachedSourceFile> sourceFiles = default,
		ImmutableArray<CachedMetadataReference> metadataReferences = default,
		ImmutableArray<CachedProjectReference> projectReferences = default)
		=> new()
		{
			LanguageName = "C#",
			ProjectFilePath = @"C:\repo\App\App.csproj",
			SliceDimensions = (sliceDimensions ?? []).ToImmutableDictionary(),
			CommandLineArguments = [],
			SourceFiles = sourceFiles.IsDefault ? [] : sourceFiles,
			MetadataReferences = metadataReferences.IsDefault ? [] : metadataReferences,
			AnalyzerReferences = [],
			AnalyzerConfigFiles = [],
			AdditionalFiles = [],
			EmbeddedResources = [],
			ProjectReferences = projectReferences.IsDefault ? [] : projectReferences,
			Capabilities = [],
			Properties = (properties ?? []).ToImmutableDictionary(),
		};
}
