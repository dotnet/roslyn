// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Microsoft.NET.ProjectData.Tests;

[CollectionDefinition(DotnetRootEnvCollection.Name, DisableParallelization = true)]
public sealed class DotnetRootEnvCollection
{
	public const string Name = "DotnetRootEnv";
}

[Collection(DotnetRootEnvCollection.Name)]
public class CacheFileReaderTests : IDisposable
{
	private static readonly CachePathResolver Resolver = new();
	private static readonly string TestProjectDirectory = Path.Combine(Path.GetTempPath(), "projectdata-cache-tests", "TestProject");
	private static readonly string TestProjectFilePath = Path.Combine(TestProjectDirectory, "TestProject.csproj");
	private readonly CapturingTraceListener listener = new();

	public CacheFileReaderTests()
		=> Trace.Listeners.Add(this.listener);

	public void Dispose()
		=> Trace.Listeners.Remove(this.listener);

	#region Header and Structure

	[Fact]
	public async Task Empty_File_Returns_Empty()
	{
		ImmutableArray<CachedSliceData> slices = await ParseAsync("");
		Assert.True(slices.IsEmpty);
	}

	[Fact]
	public async Task Invalid_Version_Returns_Empty()
	{
		ImmutableArray<CachedSliceData> slices = await ParseAsync("version=99\n");
		Assert.True(slices.IsEmpty);
	}

	[Fact]
	public async Task Minimal_Valid_File_Parses_One_Slice()
	{
		string content = """
			version=2

			[project]
			language=C#
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Single(slices);
		Assert.Equal("C#", slices[0].LanguageName);
	}

	[Fact]
	public async Task ReadProjectCacheAsync_CanceledBeforeOpen_ThrowsAndLogsNoWarnings()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string projectFilePath = Path.Combine(tempRoot, "App.csproj");
			WriteCache(projectFilePath + ".lscache", "App");

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				CacheFileReader.ReadProjectCacheAsync(
					projectFilePath,
					cacheInProject: true,
					Resolver,
					new CancellationToken(canceled: true)));

			Assert.Empty(this.listener.Warnings);
			Assert.DoesNotContain("Opening cache file", this.listener.Information);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadFromAsync_CanceledDuringLineRead_ThrowsWithoutReturningPartialSlicesOrLoggingWarnings()
	{
		using CancellationTokenSource cancellationSource = new();
		using BlockingTextReader reader = new(
			[
				"version=2",
				"[project]",
				"language=C#",
				"---",
			],
			blockAtRead: 4);
		Task<ImmutableArray<CachedSliceData>> readTask = CacheFileReader.ReadFromAsync(
			reader,
			Resolver,
			TestProjectDirectory,
			TestProjectFilePath,
			expectedProjectFilePath: null,
			stringPool: null,
			cancellationToken: cancellationSource.Token);

		await reader
			.WaitUntilBlockedAsync()
			.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		cancellationSource.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readTask);
		Assert.True(readTask.IsCanceled);
		Assert.Empty(this.listener.Warnings);
	}

	[Fact]
	public async Task ReadFromAsync_CanceledWhileParsingLine_ThrowsBeforeReadingAnotherLineOrLoggingWarnings()
	{
		using CancellationTokenSource cancellationSource = new();
		using CancelAfterReturningLineTextReader reader = new(
			[
				"version=2",
				"[project]",
				"language=C#",
				"[sourceFiles]",
				"Program.cs",
			],
			cancelAfterReturningLine: 4,
			cancellationSource);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			CacheFileReader.ReadFromAsync(
				reader,
				Resolver,
				TestProjectDirectory,
				TestProjectFilePath,
				expectedProjectFilePath: null,
				stringPool: null,
				cancellationToken: cancellationSource.Token));

		Assert.False(reader.ReadRequestedAfterCancellation);
		Assert.Empty(this.listener.Warnings);
	}

	[Fact]
	public async Task ReadFromAsync_CanceledAfterHeaderRead_ThrowsWithoutLoggingWarnings()
	{
		using CancellationTokenSource cancellationSource = new();
		using CancelAfterReturningLineTextReader reader = new(
			["version=99"],
			cancelAfterReturningLine: 0,
			cancellationSource);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			CacheFileReader.ReadFromAsync(
				reader,
				Resolver,
				TestProjectDirectory,
				TestProjectFilePath,
				expectedProjectFilePath: null,
				stringPool: null,
				cancellationToken: cancellationSource.Token));

		Assert.Empty(this.listener.Warnings);
	}

	[Fact]
	public async Task ReadFromAsync_CanceledAtEndOfFile_ThrowsBeforeMaterializationOrDiagnostics()
	{
		using CancellationTokenSource cancellationSource = new();
		using CancelAfterReturningLineTextReader reader = new(
			[
				"version=2",
				"[project]",
				"language=C#",
			],
			cancelAfterReturningLine: 3,
			cancellationSource);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			CacheFileReader.ReadFromAsync(
				reader,
				Resolver,
				TestProjectDirectory,
				TestProjectFilePath,
				expectedProjectFilePath: null,
				stringPool: null,
				cancellationToken: cancellationSource.Token));

		Assert.Empty(this.listener.Warnings);
		Assert.DoesNotContain("Read 1 slice(s)", this.listener.Information);
	}

	[Fact]
	public async Task ReadFromAsync_CanceledDuringFinalMaterialization_ThrowsBeforeMismatchWarningOrEmptyResult()
	{
		const string content = """
			version=2
			project=App.csproj

			[project]
			language=C#

			[sourceFiles]
			Program.cs
			""";
		using CancellationTokenSource cancellationSource = new();
		StringPool stringPool = new(cancellationSource.Cancel);
		using StringReader reader = new(StripLeadingTabs(content));
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			CacheFileReader.ReadFromAsync(
				reader,
				Resolver,
				TestProjectDirectory,
				TestProjectFilePath,
				expectedProjectFilePath: Path.Combine(TestProjectDirectory, "Other.csproj"),
				stringPool,
				cancellationToken: cancellationSource.Token));

		Assert.Empty(this.listener.Warnings);
	}

	[Fact]
	public void FrameworkListExpander_CanceledRead_DoesNotWarnOrCacheCancellation()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string packDirectory = Path.Combine(tempRoot, "pack");
			string dataDirectory = Path.Combine(packDirectory, "data");
			Directory.CreateDirectory(dataDirectory);
			File.WriteAllText(Path.Combine(dataDirectory, "FrameworkList.xml"), "<invalid");

			Assert.Throws<OperationCanceledException>(() =>
				FrameworkListExpander.Expand(packDirectory, new CancellationToken(canceled: true)));
			Assert.Empty(this.listener.Warnings);

			FrameworkListExpander.ExpansionResult result = FrameworkListExpander.Expand(
				packDirectory,
				TestContext.Current.CancellationToken);

			Assert.Same(FrameworkListExpander.ExpansionResult.Empty, result);
			Assert.Contains("Failed to parse framework-list manifest", this.listener.Warnings);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task Hash_Header_Is_Ignored_When_Parsing()
	{
		string content = $"hash={new string('a', 64)}\nversion=2\n\n[project]\nlanguage=C#\n";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Single(slices);
		Assert.Equal("C#", slices[0].LanguageName);
	}

	[Fact]
	public async Task Comments_And_Blank_Lines_Are_Skipped()
	{
		string content = """
			version=2

			# This is a comment

			# Another comment

			[project]
			language=C#
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Single(slices);
	}

	[Fact]
	public async Task Multiple_Slices_Separated_By_Dashes()
	{
		string content = """
			version=2

			[project]
			language=C#
			primary
			lastDtbSucceeded

			[sliceDimensions]
			TargetFramework=net10.0

			---

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net8.0
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Equal(2, slices.Length);

		Assert.True(slices[0].IsPrimary);
		Assert.True(slices[0].LastDesignTimeBuildSucceeded);
		Assert.Equal("net10.0", slices[0].SliceDimensions["TargetFramework"]);

		Assert.False(slices[1].IsPrimary);
		Assert.False(slices[1].LastDesignTimeBuildSucceeded);
		Assert.Equal("net8.0", slices[1].SliceDimensions["TargetFramework"]);
	}

	#endregion

	#region Properties

	[Fact]
	public async Task Properties_Are_Parsed()
	{
		string content = """
			version=2

			[project]
			language=C#

			[properties]
			AssemblyName=MyProject
			RootNamespace=MyProject.Root
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Single(slices);
		Assert.Equal("MyProject", slices[0].Properties["AssemblyName"]);
		Assert.Equal("MyProject.Root", slices[0].Properties["RootNamespace"]);
	}

	[Fact]
	public async Task Property_Value_May_Contain_Equals_Sign()
	{
		string content = """
			version=2

			[project]
			language=C#

			[properties]
			Weird=val=ue=with=equals
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Equal("val=ue=with=equals", slices[0].Properties["Weird"]);
	}

	#endregion

	#region Command Line Arguments

	[Fact]
	public async Task CommandLineArguments_Are_Parsed()
	{
		string content = """
			version=2

			[project]
			language=C#

			[commandLineArguments]
			/langversion:preview
			/nullable:enable
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Equal(2, slices[0].CommandLineArguments.Length);
		Assert.Equal("/langversion:preview", slices[0].CommandLineArguments[0]);
		Assert.Equal("/nullable:enable", slices[0].CommandLineArguments[1]);
	}

	[Fact]
	public async Task ReadFromAsync_With_Shared_StringPool_Deduplicates_Shared_Strings_Across_Reads_And_Ignores_Legacy_DynamicFiles_Section()
	{
		const string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net10.0

			[properties]
			AssemblyName=SharedAssembly

			[commandLineArguments]
			/langversion:preview

			[sourceFiles]
			C:\shared\Program.cs

			[metadataReferences]
			C:\shared\System.Runtime.dll
			@aliases=global,shared

			[analyzerReferences]
			C:\shared\MyAnalyzer.dll

			[additionalFiles]
			C:\shared\.editorconfig

			[dynamicFiles]
			C:\shared\Generated.g.cs
			@folderNames=Generated,Shared

			[capabilities]
			CSharp
			""";

		StringPool stringPool = new();

		async Task<CachedSliceData> ParseSingleSliceAsync(string projectFilePath)
		{
			using StringReader reader = new(StripLeadingTabs(content));
			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadFromAsync(
				reader,
				Resolver,
				Path.GetDirectoryName(projectFilePath)!,
				projectFilePath,
				expectedProjectFilePath: null,
				stringPool,
				cancellationToken: TestContext.Current.CancellationToken);
			return Assert.Single(slices);
		}

		CachedSliceData first = await ParseSingleSliceAsync(@"C:\dev\Shared\ProjectA.csproj");
		CachedSliceData second = await ParseSingleSliceAsync(@"C:\dev\Shared\ProjectB.csproj");

		Assert.Same(first.SliceDimensions.Keys.Single(), second.SliceDimensions.Keys.Single());
		Assert.Same(first.SliceDimensions["TargetFramework"], second.SliceDimensions["TargetFramework"]);

		string firstPropertyKey = first.Properties.Keys.Single();
		string secondPropertyKey = second.Properties.Keys.Single();
		Assert.Same(firstPropertyKey, secondPropertyKey);
		Assert.Same(first.Properties[firstPropertyKey], second.Properties[secondPropertyKey]);

		Assert.Same(first.CommandLineArguments[0], second.CommandLineArguments[0]);
		Assert.Same(first.SourceFiles[0].FilePath, second.SourceFiles[0].FilePath);
		Assert.Same(first.MetadataReferences[0].FilePath, second.MetadataReferences[0].FilePath);
		Assert.Same(first.MetadataReferences[0].Aliases[0], second.MetadataReferences[0].Aliases[0]);
		Assert.Same(first.AnalyzerReferences[0], second.AnalyzerReferences[0]);
		Assert.Same(first.AdditionalFiles[0], second.AdditionalFiles[0]);
		Assert.Same(first.Capabilities[0], second.Capabilities[0]);
	}

	#endregion

	#region Indentation Compression

	[Fact]
	public void ExpandCompressedPaths_Flat_Paths()
	{
		List<string> lines = ["Program.cs", "Helper.cs"];
		List<(string Path, Dictionary<string, string>? Metadata)> result = CacheFileReader.ExpandCompressedPaths(lines, TestContext.Current.CancellationToken);

		Assert.Equal(2, result.Count);
		Assert.Equal("Program.cs", result[0].Path);
		Assert.Equal("Helper.cs", result[1].Path);
	}

	[Fact]
	public void ExpandCompressedPaths_Single_Level_Prefix()
	{
		List<string> lines =
		[
			"src/Models/",
			" Product.cs",
			" User.cs",
		];

		List<(string Path, Dictionary<string, string>? Metadata)> result = CacheFileReader.ExpandCompressedPaths(lines, TestContext.Current.CancellationToken);

		Assert.Equal(2, result.Count);
		Assert.Equal("src/Models/Product.cs", result[0].Path);
		Assert.Equal("src/Models/User.cs", result[1].Path);
	}

	[Fact]
	public void ExpandCompressedPaths_Nested_Prefixes()
	{
		List<string> lines =
		[
			"<DOTNET>/packs/Microsoft.NETCore.App.Ref/10.0.3/ref/net10.0/",
			" System.Collections.dll",
			" System.Runtime.dll",
		];

		List<(string Path, Dictionary<string, string>? Metadata)> result = CacheFileReader.ExpandCompressedPaths(lines, TestContext.Current.CancellationToken);

		Assert.Equal(2, result.Count);
		Assert.Equal("<DOTNET>/packs/Microsoft.NETCore.App.Ref/10.0.3/ref/net10.0/System.Collections.dll", result[0].Path);
		Assert.Equal("<DOTNET>/packs/Microsoft.NETCore.App.Ref/10.0.3/ref/net10.0/System.Runtime.dll", result[1].Path);
	}

	[Fact]
	public void ExpandCompressedPaths_Mixed_Compressed_And_Uncompressed()
	{
		List<string> lines =
		[
			"Program.cs",
			"src/A/B/",
			" File1.cs",
			" File2.cs",
			"src/A/C/File3.cs",
		];

		List<(string Path, Dictionary<string, string>? Metadata)> result = CacheFileReader.ExpandCompressedPaths(lines, TestContext.Current.CancellationToken);

		Assert.Equal(4, result.Count);
		Assert.Equal("Program.cs", result[0].Path);
		Assert.Equal("src/A/B/File1.cs", result[1].Path);
		Assert.Equal("src/A/B/File2.cs", result[2].Path);
		Assert.Equal("src/A/C/File3.cs", result[3].Path);
	}

	[Fact]
	public void ExpandCompressedPaths_With_Metadata()
	{
		List<string> lines =
		[
			"lib/Interop.dll",
			" @aliases=global,interop",
			" @embedInteropTypes",
		];

		List<(string Path, Dictionary<string, string>? Metadata)> result = CacheFileReader.ExpandCompressedPaths(lines, TestContext.Current.CancellationToken);

		Assert.Single(result);
		Assert.Equal("lib/Interop.dll", result[0].Path);
		Assert.NotNull(result[0].Metadata);
		Assert.Equal("global,interop", result[0].Metadata!["aliases"]);
		Assert.Equal("", result[0].Metadata!["embedInteropTypes"]);
	}

	[Fact]
	public void ExpandCompressedPaths_Metadata_Under_Compressed_Path()
	{
		List<string> lines =
		[
			"<NUGET>/package/1.0/lib/",
			" File.cs",
			"  @folderNames=External",
		];

		List<(string Path, Dictionary<string, string>? Metadata)> result = CacheFileReader.ExpandCompressedPaths(lines, TestContext.Current.CancellationToken);

		Assert.Single(result);
		Assert.Equal("<NUGET>/package/1.0/lib/File.cs", result[0].Path);
		Assert.NotNull(result[0].Metadata);
		Assert.Equal("External", result[0].Metadata!["folderNames"]);
	}

	#endregion

	#region Source Files with Link

	[Fact]
	public async Task SourceFiles_Link_Parsed_From_Metadata()
	{
		string content = """
			version=2

			[project]
			language=C#

			[sourceFiles]
			../Shared/User.cs
			 @link=Models/User.cs
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Single(slices[0].SourceFiles);
		Assert.Equal("Models/User.cs", slices[0].SourceFiles[0].Link);
	}

	[Fact]
	public async Task SourceFiles_Link_Null_When_Metadata_Missing()
	{
		string content = """
			version=2

			[project]
			language=C#

			[sourceFiles]
			Program.cs
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Single(slices[0].SourceFiles);
		Assert.Null(slices[0].SourceFiles[0].Link);
	}

	#endregion

	#region Metadata References

	[Fact]
	public async Task MetadataReferences_Parsed_With_Aliases_And_EmbedInteropTypes()
	{
		string content = """
			version=2

			[project]
			language=C#

			[metadataReferences]
			lib/Interop.dll
			 @aliases=global,interop
			 @embedInteropTypes
			lib/Other.dll
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Equal(2, slices[0].MetadataReferences.Length);

		Assert.Equal(["global", "interop"], slices[0].MetadataReferences[0].Aliases.ToArray());
		Assert.True(slices[0].MetadataReferences[0].EmbedInteropTypes);

		Assert.True(slices[0].MetadataReferences[1].Aliases.IsEmpty);
		Assert.False(slices[0].MetadataReferences[1].EmbedInteropTypes);
	}

	#endregion

	#region DeriveFolderNames

	[Fact]
	public void DeriveFolderNames_From_Relative_Path()
	{
		ImmutableArray<string> folders = CacheFileReader.DeriveFolderNamesFromPortablePath("src/Models/User.cs");
		Assert.Equal(["src", "Models"], folders.ToArray());
	}

	[Fact]
	public void DeriveFolderNames_Empty_For_Root_File()
	{
		ImmutableArray<string> folders = CacheFileReader.DeriveFolderNamesFromPortablePath("Program.cs");
		Assert.True(folders.IsEmpty);
	}

	[Fact]
	public void DeriveFolderNames_Empty_For_Sentinel_Path()
	{
		ImmutableArray<string> folders = CacheFileReader.DeriveFolderNamesFromPortablePath("<NUGET>/package/1.0/lib/File.cs");
		Assert.True(folders.IsEmpty);
	}

	[Fact]
	public void DeriveFolderNames_Empty_For_Parent_Relative_Path()
	{
		ImmutableArray<string> folders = CacheFileReader.DeriveFolderNamesFromPortablePath("../Other/File.cs");
		Assert.True(folders.IsEmpty);
	}

	#endregion

	#region Complete File Parsing

	[Fact]
	public async Task Complete_Cache_File_Example()
	{
		// This matches the complete example from the cache file specification.
		string content = """
			version=2

			# This file caches language service data.

			[project]
			language=C#
			primary
			lastDtbSucceeded

			[sliceDimensions]
			TargetFramework=net10.0

			[properties]
			AssemblyName=MyProject
			RootNamespace=MyProject

			[commandLineArguments]
			/langversion:preview
			/nullable:enable

			[sourceFiles]
			Program.cs
			src/Models/
			 Product.cs
			 User.cs

			[analyzerConfigFiles]
			.editorconfig

			[additionalFiles]
			data/config.json

			---

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net8.0

			[sourceFiles]
			Program.cs
			src/Models/
			 Product.cs
			 User.cs
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);

		Assert.Equal(2, slices.Length);

		// First slice
		CachedSliceData first = slices[0];
		Assert.True(first.IsPrimary);
		Assert.True(first.LastDesignTimeBuildSucceeded);
		Assert.Equal("C#", first.LanguageName);
		Assert.Equal("net10.0", first.SliceDimensions["TargetFramework"]);
		Assert.Equal("MyProject", first.Properties["AssemblyName"]);
		Assert.Equal(2, first.CommandLineArguments.Length);
		Assert.Equal(3, first.SourceFiles.Length);
		Assert.Single(first.AnalyzerConfigFiles);
		Assert.Single(first.AdditionalFiles);

		// Source files had indentation compression expanded
		Assert.Contains(first.SourceFiles, f => f.FilePath.EndsWith("Program.cs"));
		Assert.Contains(first.SourceFiles, f => f.FilePath.EndsWith("Product.cs"));
		Assert.Contains(first.SourceFiles, f => f.FilePath.EndsWith("User.cs"));

		// Second slice
		CachedSliceData second = slices[1];
		Assert.False(second.IsPrimary);
		Assert.Equal("net8.0", second.SliceDimensions["TargetFramework"]);
		Assert.Equal(3, second.SourceFiles.Length);
	}

	#endregion

	#region Forward Compatibility (version tolerance + unknown data)

	// A newer MINOR version (same major) must be accepted: forward-compatible additions
	// are ignored, not rejected. Only the MAJOR (the token before the first '.') is parsed and
	// gated; everything after the first '.' is deliberately opaque/informational, so an exotic
	// future minor format (extra components, suffixes, non-numeric text) must NOT cause a miss.
	// Note this differs from "version=2x" (no dot), where the whole suffix is the major and must
	// be numeric — that case is a miss (see Rejects_Different_Major_Or_Malformed_Version).
	[Theory]
	[InlineData("version=2")]
	[InlineData("version=2.0")]
	[InlineData("version=2.1")]
	[InlineData("version=2.99")]
	[InlineData("version=2.0.5")]
	[InlineData("version=2.not-a-number")]
	[InlineData("version=2.3-preview")]
	public async Task Accepts_Same_Major_Version(string versionLine)
	{
		string content = $"{versionLine}\n\n[project]\nlanguage=C#\n";
		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Single(slices);
		Assert.Equal("C#", slices[0].LanguageName);
	}

	// A different MAJOR version, or a malformed/missing version header, is a clean cache miss.
	[Theory]
	[InlineData("version=3")]
	[InlineData("version=3.0")]
	[InlineData("version=1")]
	[InlineData("version=1.9")]
	[InlineData("version=")]
	[InlineData("version=abc")]
	[InlineData("version=2x")]
	[InlineData("version=.2")]
	[InlineData("ver=2")]
	[InlineData("")]
	public async Task Rejects_Different_Major_Or_Malformed_Version(string versionLine)
	{
		string content = $"{versionLine}\n\n[project]\nlanguage=C#\n";
		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.True(slices.IsEmpty);
	}

	// Reading a same-major / different-minor cache (a mixed-version team) is a normal, recoverable
	// situation: it is read successfully and surfaced at INFORMATION level, not as a warning. An
	// exact version match must stay silent so steady-state reads never spam the log.
	[Fact]
	public async Task Reading_A_Different_Minor_Version_Logs_Information_Not_Warning()
	{
		ImmutableArray<CachedSliceData> slices = await ParseAsync("version=2.7\n\n[project]\nlanguage=C#\n");
		Assert.Single(slices);
		Assert.Contains("different minor version", this.listener.Information);
		Assert.Contains("version=2.7", this.listener.Information);
		Assert.Empty(this.listener.Warnings);

		this.listener.Clear();
		await ParseAsync($"{CacheFormat.VersionHeader}\n\n[project]\nlanguage=C#\n");
		Assert.DoesNotContain("different minor version", this.listener.Information);
	}

	// A normal, successful read (matching version, slices present) must not emit any warnings: opening
	// the file and reporting the slice count are routine events surfaced at INFORMATION level. Warnings
	// stay reserved for genuine anomalies (version rejection, project-path mismatch, empty-despite-valid).
	[Fact]
	public async Task Reading_A_Valid_Cache_Logs_No_Warnings()
	{
		ImmutableArray<CachedSliceData> slices = await ParseAsync("version=2\n\n[project]\nlanguage=C#\n");
		Assert.Single(slices);
		Assert.Empty(this.listener.Warnings);
		Assert.Contains("Read 1 slice(s)", this.listener.Information);
	}

	// Unknown sections, unknown [project] markers/keys, and unknown @metadata produce no
	// state in the model — the reader behaves exactly as if they weren't there. This is the
	// "reader is lossy for unknown data" contract that lets a newer writer add data an older
	// reader simply ignores.
	[Fact]
	public async Task Unknown_Sections_Markers_And_Metadata_Are_Invisible()
	{
		string known = """
			version=2

			[project]
			language=C#
			primary
			lastDtbSucceeded

			[sliceDimensions]
			TargetFramework=net10.0

			[properties]
			AssemblyName=MyProject
			RootNamespace=MyProject

			[commandLineArguments]
			/nullable:enable

			[sourceFiles]
			Program.cs
			src/Models/
			 Product.cs
			 User.cs

			[analyzerConfigFiles]
			.editorconfig

			[additionalFiles]
			data/config.json
			""";

		// The same project as authored by a hypothetical newer minor version that sprinkles
		// forward-compatible additions the current reader does not understand.
		string withUnknown = """
			version=2.7

			[project]
			language=C#
			primary
			lastDtbSucceeded
			futureProjectMarker
			futureProjectKey=abc

			[sliceDimensions]
			TargetFramework=net10.0

			[properties]
			AssemblyName=MyProject
			RootNamespace=MyProject

			[futureSection]
			some unknown payload
			 indented unknown child

			[commandLineArguments]
			/nullable:enable

			[sourceFiles]
			Program.cs
			src/Models/
			 Product.cs
			 @futureMeta=ignored
			 User.cs

			[analyzerConfigFiles]
			.editorconfig

			[anotherFutureSection]
			more unknown payload

			[additionalFiles]
			data/config.json
			""";

		ImmutableArray<CachedSliceData> knownSlices = await ParseAsync(known);
		ImmutableArray<CachedSliceData> unknownSlices = await ParseAsync(withUnknown);

		Assert.Equal(DumpSlices(knownSlices), DumpSlices(unknownSlices));
	}

	// An unknown key in [properties] is a plausible MINOR addition. The reader must not choke
	// on it, and it must not corrupt any other field. (It is harmlessly retained in the open
	// Properties dictionary; no consumer looks it up.)
	[Fact]
	public async Task Unknown_Property_Key_Does_Not_Corrupt_Other_Fields()
	{
		string content = """
			version=2.4

			[project]
			language=C#

			[properties]
			AssemblyName=MyProject
			FutureProperty=should-not-break-anything
			RootNamespace=MyProject

			[sourceFiles]
			Program.cs
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);

		Assert.Single(slices);
		CachedSliceData slice = slices[0];
		Assert.Equal("MyProject", slice.Properties["AssemblyName"]);
		Assert.Equal("MyProject", slice.Properties["RootNamespace"]);
		Assert.Single(slice.SourceFiles);
		Assert.EndsWith("Program.cs", slice.SourceFiles[0].FilePath);
	}

	// Guard: an unknown section appearing anywhere must never throw and never drop known data
	// that follows it. This locks in the reader's "no fatal default" behavior so a future edit
	// can't accidentally make unknown sections a hard failure.
	[Fact]
	public async Task Unknown_Section_Between_Known_Sections_Does_Not_Drop_Following_Data()
	{
		string content = """
			version=2.1

			[project]
			language=C#

			[futureSectionBeforeKnown]
			junk
			 more junk

			[properties]
			AssemblyName=MyProject

			[sourceFiles]
			Program.cs
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);

		Assert.Single(slices);
		Assert.Equal("MyProject", slices[0].Properties["AssemblyName"]);
		Assert.Single(slices[0].SourceFiles);
	}

	private static string DumpSlices(ImmutableArray<CachedSliceData> slices)
		=> string.Join("\n====\n", slices.Select(DumpSlice));

	private static string DumpSlice(CachedSliceData s)
	{
		System.Text.StringBuilder sb = new();
		sb.AppendLine($"language={s.LanguageName}");
		sb.AppendLine($"projectFilePath={s.ProjectFilePath}");
		sb.AppendLine($"isPrimary={s.IsPrimary}");
		sb.AppendLine($"lastDtb={s.LastDesignTimeBuildSucceeded}");
		sb.AppendLine("sliceDimensions:");
		foreach (KeyValuePair<string, string> kvp in s.SliceDimensions.OrderBy(k => k.Key, StringComparer.Ordinal))
			sb.AppendLine($"  {kvp.Key}={kvp.Value}");
		sb.AppendLine("properties:");
		foreach (KeyValuePair<string, string> kvp in s.Properties.OrderBy(k => k.Key, StringComparer.Ordinal))
			sb.AppendLine($"  {kvp.Key}={kvp.Value}");
		sb.AppendLine("commandLineArguments:");
		foreach (string a in s.CommandLineArguments) sb.AppendLine($"  {a}");
		sb.AppendLine("sourceFiles:");
		foreach (CachedSourceFile f in s.SourceFiles) sb.AppendLine($"  {f.FilePath} link={f.Link}");
		sb.AppendLine("metadataReferences:");
		foreach (CachedMetadataReference m in s.MetadataReferences) sb.AppendLine($"  {m.FilePath} aliases=[{string.Join(",", m.Aliases)}] embed={m.EmbedInteropTypes}");
		sb.AppendLine("analyzerReferences:");
		foreach (string a in s.AnalyzerReferences) sb.AppendLine($"  {a}");
		sb.AppendLine("analyzerConfigFiles:");
		foreach (string a in s.AnalyzerConfigFiles) sb.AppendLine($"  {a}");
		sb.AppendLine("additionalFiles:");
		foreach (string a in s.AdditionalFiles) sb.AppendLine($"  {a}");
		sb.AppendLine("embeddedResources:");
		foreach (CachedEmbeddedResource e in s.EmbeddedResources) sb.AppendLine($"  {e.FilePath} gen={e.Generator} last={e.LastGenOutput} ns={e.CustomToolNamespace}");
		sb.AppendLine("projectReferences:");
		foreach (CachedProjectReference p in s.ProjectReferences) sb.AppendLine($"  {p.FilePath} ({p.ReferenceOutputAssembly})");
		sb.AppendLine("capabilities:");
		foreach (string c in s.Capabilities) sb.AppendLine($"  {c}");
		return sb.ToString();
	}

	#endregion

	#region Project References

	[Fact]
	public async Task ProjectReferences_ParseReferenceOutputAssemblyMetadata()
	{
		string content = """
			version=2.1

			[project]
			language=C#

			[projectReferences]
			BuildOnly/
			 BuildOnly.csproj
			  @ReferenceOutputAssembly=false
			Library/Library.csproj
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		CachedProjectReference[] references = slices[0].ProjectReferences.ToArray();

		Assert.Equal(2, references.Length);
		Assert.EndsWith(Path.Combine("BuildOnly", "BuildOnly.csproj"), references[0].FilePath);
		Assert.False(references[0].ReferenceOutputAssembly!.Value);
		Assert.EndsWith(Path.Combine("Library", "Library.csproj"), references[1].FilePath);
		Assert.True(references[1].ReferenceOutputAssembly!.Value);
	}

	[Fact]
	public async Task ProjectReferences_NewerKnownMinor_DefaultsMissingReferenceOutputAssemblyToTrue()
	{
		string content = """
			version=2.10

			[project]
			language=C#

			[projectReferences]
			Library.csproj
			""";

		CachedProjectReference reference = Assert.Single((await ParseAsync(content))[0].ProjectReferences);
		Assert.True(reference.ReferenceOutputAssembly!.Value);
	}

	[Fact]
	public async Task ProjectReferences_MalformedMinor_LeavesMissingReferenceOutputAssemblyUnclassified()
	{
		string content = """
			version=2.not-a-number

			[project]
			language=C#

			[projectReferences]
			Library.csproj
			""";

		CachedProjectReference reference = Assert.Single((await ParseAsync(content))[0].ProjectReferences);
		Assert.Null(reference.ReferenceOutputAssembly);
	}

	[Fact]
	public async Task ProjectReferences_Pre21Cache_LeavesReferenceOutputAssemblyUnclassified()
	{
		string content = """
			version=2

			[project]
			language=C#

			[projectReferences]
			Legacy.csproj
			""";

		CachedProjectReference reference = Assert.Single((await ParseAsync(content))[0].ProjectReferences);
		Assert.Null(reference.ReferenceOutputAssembly);
	}

	#endregion

	#region User-Folder Mode

	[Fact]
	public async Task UserFolder_Valid_Project_Header_Accepted()
	{
		string projectDirectory = Path.Combine(Path.GetTempPath(), "projectdata-cache-tests", "dev");
		string projectFilePath = Path.Combine(projectDirectory, "MyProject.csproj");
		string content = $"version=2\nproject={projectFilePath.Replace('\\', '/')}\n\n[project]\nlanguage=C#\n";

		using StringReader reader = new(content);
		ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadFromAsync(
			reader, Resolver, projectDirectory, projectFilePath, projectFilePath, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);

		Assert.Single(slices);
	}

	[Fact]
	public async Task Project_Header_Path_Sentinel_Resolves_From_Project_Directory()
	{
		string projectDirectory = Path.Combine(Path.GetTempPath(), "projectdata-cache-tests", "dev");
		string fallbackProjectFilePath = Path.Combine(projectDirectory, "Fallback.csproj");
		string expectedProjectFilePath = Path.Combine(projectDirectory, "Subdir", "MyProject.csproj");
		string content = """
			version=2
			project=<PATH>Subdir/MyProject.csproj

			[project]
			language=C#

			""";

		using StringReader reader = new(content);
		ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadFromAsync(
			reader, Resolver, projectDirectory, fallbackProjectFilePath, expectedProjectFilePath, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);

		CachedSliceData slice = Assert.Single(slices);
		Assert.Equal(expectedProjectFilePath, slice.ProjectFilePath);
	}

	[Fact]
	public async Task UserFolder_Mismatched_Project_Header_Returns_Empty()
	{
		string content = "version=2\nproject=C:\\dev\\OtherProject.csproj\n\n[project]\nlanguage=C#\n";

		using StringReader reader = new(content);
		ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadFromAsync(
			reader, Resolver, @"C:\dev", @"C:\dev\MyProject.csproj", @"C:\dev\MyProject.csproj", stringPool: null, cancellationToken: TestContext.Current.CancellationToken);

		Assert.True(slices.IsEmpty);
	}

	[Fact]
	public async Task UserFolder_Missing_Project_Header_Returns_Empty()
	{
		string content = "version=2\n\n[project]\nlanguage=C#\n";

		using StringReader reader = new(content);
		ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadFromAsync(
			reader, Resolver, @"C:\dev", @"C:\dev\MyProject.csproj", @"C:\dev\MyProject.csproj", stringPool: null, cancellationToken: TestContext.Current.CancellationToken);

		Assert.True(slices.IsEmpty);
	}

	[Fact]
	public async Task Project_Field_Inside_Project_Block_Overrides_Project_Path()
	{
		string projectDirectory = Path.Combine(Path.GetTempPath(), "projectdata-cache-tests", "dev");
		string fallbackProjectFilePath = Path.Combine(projectDirectory, "Fallback.csproj");
		string expectedProjectFilePath = Path.Combine(projectDirectory, "Subdir", "MyProject.csproj");
		string content = "version=2\n\n[project]\nproject=Subdir/MyProject.csproj\nlanguage=C#\n";

		using StringReader reader = new(content);
		ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadFromAsync(
			reader, Resolver, projectDirectory, fallbackProjectFilePath, expectedProjectFilePath, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);

		Assert.Single(slices);
		Assert.Equal(expectedProjectFilePath, slices[0].ProjectFilePath);
	}

	#endregion

	#region Donor Index

	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("1", true)]
	[InlineData("true", true)]
	[InlineData("0", false)]
	[InlineData("false", false)]
	[InlineData("FALSE", false)]
	public void DonorEnvironmentSwitch_UsesOptOutSemantics(string? value, bool expected)
		=> Assert.Equal(expected, ProjectDataDonorOptions.IsEnabledByEnvironmentValue(value));

	[Fact]
	public void CacheReadStream_KeepsOpenedBytesAcrossAtomicReplacement()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string cachePath = Path.Combine(tempRoot, "App.csproj.lscache");
			string replacementPath = Path.Combine(tempRoot, "replacement.tmp");
			File.WriteAllText(cachePath, "original");
			File.WriteAllText(replacementPath, "replacement");

			using FileStream stream = CacheFileReader.OpenCacheFileForRead(cachePath);
			File.Replace(replacementPath, cachePath, destinationBackupFileName: null);
			using StreamReader reader = new(
				stream,
				Encoding.UTF8,
				detectEncodingFromByteOrderMarks: true,
				bufferSize: 1024,
				leaveOpen: true);

			Assert.Equal("replacement", File.ReadAllText(cachePath));
			Assert.Equal("original", reader.ReadToEnd());
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void DonorIndex_DottedRepositoryRoot_ResolvesFromProjectDirectory()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string repositoryRoot = Path.Combine(tempRoot, "repo.v2");
			string projectFile = Path.Combine(repositoryRoot, "App.csproj");
			Directory.CreateDirectory(Path.Combine(repositoryRoot, ".git"));
			WriteProjectFile(projectFile);

			string indexPath = Assert.IsType<string>(ProjectDataDonorIndex.TryResolveDefaultIndexPath(projectFile));

			Assert.Equal(
				Path.Combine(repositoryRoot, ".git", "dotnet-projectdata", "lscache-donor-index.json"),
				indexPath);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void DonorIndex_LinkedWorktree_ResolvesRelativeCommonGitDirectory()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string repositoryRoot = Path.Combine(tempRoot, "repository");
			string commonGitDirectory = Path.Combine(repositoryRoot, ".git");
			string worktreeRoot = Path.Combine(tempRoot, "linked-worktree");
			string worktreeGitDirectory = Path.Combine(commonGitDirectory, "worktrees", "linked-worktree");
			string projectFile = Path.Combine(worktreeRoot, "src", "App", "App.csproj");
			Directory.CreateDirectory(worktreeGitDirectory);
			Directory.CreateDirectory(worktreeRoot);
			File.WriteAllText(Path.Combine(worktreeRoot, ".git"), "gitdir: " + worktreeGitDirectory);
			File.WriteAllText(Path.Combine(worktreeGitDirectory, "commondir"), Path.Combine("..", ".."));
			WriteProjectFile(projectFile);

			string indexPath = Assert.IsType<string>(ProjectDataDonorIndex.TryResolveDefaultIndexPath(projectFile));

			Assert.Equal(
				Path.Combine(commonGitDirectory, "dotnet-projectdata", "lscache-donor-index.json"),
				indexPath);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_OutsideGitRepository_FallsBackToEmpty()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientProject = Path.Combine(tempRoot, "recipient", "src", "App", "App.csproj");
			WriteProjectFile(recipientProject);

			Assert.Null(ProjectDataDonorIndex.TryResolveDefaultIndexPath(recipientProject));

			ProjectDataCacheReadResult result = await CacheFileReader.ReadProjectCacheWithSourceAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				stringPool: null,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: null);

			Assert.Empty(result.Slices);
			Assert.Equal(ProjectDataCacheSource.None, result.Source);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_UnavailableGitMetadata_UsesDiscoveredDonor()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string donorRoot = Path.Combine(tempRoot, "donor");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string donorProject = Path.Combine(donorRoot, "src", "App", "App.csproj");
			Directory.CreateDirectory(Path.Combine(recipientRoot, ".git"));
			WriteProjectFile(recipientProject);
			WriteCache(donorProject + ".lscache", "DonorAssembly");
			string indexPath = Assert.IsType<string>(ProjectDataDonorIndex.TryResolveDefaultIndexPath(recipientProject));
			WriteDonorIndex(indexPath, donorRoot);

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken);

			Assert.Equal("DonorAssembly", Assert.Single(slices).Properties["AssemblyName"]);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_LocalProjectFolderCache_Wins_Over_Donor()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string donorRoot = Path.Combine(tempRoot, "donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string donorProject = Path.Combine(donorRoot, "src", "App", "App.csproj");
			WriteCache(recipientProject + ".lscache", "LocalAssembly");
			WriteCache(donorProject + ".lscache", "DonorAssembly");
			WriteDonorIndex(indexPath, donorRoot);

			ProjectDataCacheReadResult result = await CacheFileReader.ReadProjectCacheWithSourceAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				stringPool: null,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			CachedSliceData slice = Assert.Single(result.Slices);
			Assert.Equal("LocalAssembly", slice.Properties["AssemblyName"]);
			Assert.Equal(ProjectDataCacheSource.ProjectFolder, result.Source);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_LocalUserFolderCache_Wins_Over_Donor_When_ProjectFolderMode_Is_Current()
	{
		string tempRoot = CreateTempRoot();
		string? originalCacheDir = Environment.GetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR");
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string donorRoot = Path.Combine(tempRoot, "donor");
			string cacheRoot = Path.Combine(tempRoot, "user-cache");
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", cacheRoot);
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string donorProject = Path.Combine(donorRoot, "src", "App", "App.csproj");
			WriteCache(CacheFileReader.GetUserFolderCacheFilePath(recipientProject), "LocalUserFolderAssembly");
			WriteCache(donorProject + ".lscache", "DonorAssembly");
			WriteDonorIndex(indexPath, donorRoot);

			ProjectDataCacheReadResult result = await CacheFileReader.ReadProjectCacheWithSourceAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				stringPool: null,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			CachedSliceData slice = Assert.Single(result.Slices);
			Assert.Equal("LocalUserFolderAssembly", slice.Properties["AssemblyName"]);
			Assert.Equal(ProjectDataCacheSource.UserFolder, result.Source);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", originalCacheDir);
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_UnsupportedMarker_Wins_Over_Donor()
	{
		string tempRoot = CreateTempRoot();
		string? originalCacheDir = Environment.GetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR");
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string donorRoot = Path.Combine(tempRoot, "donor");
			string cacheRoot = Path.Combine(tempRoot, "user-cache");
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", cacheRoot);
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string donorProject = Path.Combine(donorRoot, "src", "App", "App.csproj");
			WriteProjectFile(recipientProject);
			UnsupportedProjectDataMarker.Write(recipientProject, "UnsupportedTargetFramework");
			WriteCache(donorProject + ".lscache", "DonorAssembly");
			WriteDonorIndex(indexPath, donorRoot);

			ProjectDataCacheReadResult result = await CacheFileReader.ReadProjectCacheWithSourceAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				stringPool: null,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			Assert.True(result.Slices.IsEmpty);
			Assert.Equal(ProjectDataCacheSource.UnsupportedMarker, result.Source);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", originalCacheDir);
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_MissingLocalCache_Reads_Donor_InPlace_With_Recipient_Context()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string donorRoot = Path.Combine(tempRoot, "donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string donorProject = Path.Combine(donorRoot, "src", "App", "App.csproj");
			WriteCache(donorProject + ".lscache", "DonorAssembly");
			WriteDonorIndex(indexPath, donorRoot);
			CapturingTraceListener listener = new();
			TraceSource traceSource = new("DonorTest", SourceLevels.Information);
			traceSource.Listeners.Clear();
			traceSource.Listeners.Add(listener);
			ProjectDataDonorOptions donorOptions = new()
			{
				IndexPath = indexPath,
				WorkspaceRoot = recipientRoot,
				GitDistanceTopK = 0,
				DiagnosticTraceSource = traceSource,
			};

			ProjectDataCacheReadResult result = await CacheFileReader.ReadProjectCacheWithSourceAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				stringPool: null,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: donorOptions);

			CachedSliceData slice = Assert.Single(result.Slices);
			Assert.Equal(recipientProject, slice.ProjectFilePath);
			Assert.Equal("DonorAssembly", slice.Properties["AssemblyName"]);
			Assert.Equal(Path.Combine(Path.GetDirectoryName(recipientProject)!, "Program.cs"), slice.SourceFiles.Single().FilePath);
			Assert.Equal(ProjectDataCacheSource.Donor, result.Source);
			Assert.Equal($"[donor] Using ProjectData from {donorRoot}", listener.Information.TrimEnd());

			string secondRecipientProject = Path.Combine(recipientRoot, "src", "Library", "App.csproj");
			string secondDonorProject = Path.Combine(donorRoot, "src", "Library", "App.csproj");
			WriteCache(secondDonorProject + ".lscache", "DonorLibrary");

			ProjectDataCacheReadResult secondResult = await CacheFileReader.ReadProjectCacheWithSourceAsync(
				secondRecipientProject,
				cacheInProject: true,
				Resolver,
				stringPool: null,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: donorOptions);

			Assert.Single(secondResult.Slices);
			Assert.Equal(ProjectDataCacheSource.Donor, secondResult.Source);
			Assert.Equal($"[donor] Using ProjectData from {donorRoot}", listener.Information.TrimEnd());
			Assert.DoesNotContain("[lscache-donor]", listener.Information);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_DisabledDonor_FallsBackToEmpty()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string donorRoot = Path.Combine(tempRoot, "donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string donorProject = Path.Combine(donorRoot, "src", "App", "App.csproj");
			WriteCache(donorProject + ".lscache", "DonorAssembly");
			WriteDonorIndex(indexPath, donorRoot);

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					Enabled = false,
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
				});

			Assert.Empty(slices);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_RelativeIndexPath_NormalizesDonorRoot()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string donorRoot = Path.Combine(tempRoot, "donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string donorProject = Path.Combine(donorRoot, "src", "App", "App.csproj");
			WriteCache(donorProject + ".lscache", "DonorAssembly");
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(
				indexPath,
				$$"""
				{
				  "version": 2,
				  "entries": [
				    { "path": {{JsonString(Path.GetRelativePath(Environment.CurrentDirectory, donorRoot))}} }
				  ]
				}
				""");

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			Assert.Equal("DonorAssembly", Assert.Single(slices).Properties["AssemblyName"]);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ReadProjectCacheAsync_MissingLocalCache_Reads_UserFolderDonor_RegardlessOfStorageMode(bool cacheInProject)
	{
		string tempRoot = CreateTempRoot();
		string? originalCacheDir = Environment.GetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR");
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string donorRoot = Path.Combine(tempRoot, "donor");
			string cacheRoot = Path.Combine(tempRoot, "user-cache");
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", cacheRoot);
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string donorProject = Path.Combine(donorRoot, "src", "App", "App.csproj");
			WriteProjectFile(donorProject);
			WriteCache(CacheFileReader.GetUserFolderCacheFilePath(donorProject), "UserFolderDonorAssembly");
			WriteDonorIndex(indexPath, donorRoot);
			ProjectDataDonorOptions donorOptions = new()
			{
				IndexPath = indexPath,
				WorkspaceRoot = recipientRoot,
				GitDistanceTopK = 0,
			};

			ProjectDataDonorCandidate donorCandidate = Assert.Single(
				ProjectDataDonorIndex.EnumerateDonorCandidates(
					recipientProject,
					donorOptions,
					TestContext.Current.CancellationToken));
			Assert.Equal(CacheFileReader.GetUserFolderCacheFilePath(donorProject), donorCandidate.FilePath);

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: donorOptions);

			CachedSliceData slice = Assert.Single(slices);
			Assert.Equal(recipientProject, slice.ProjectFilePath);
			Assert.Equal("UserFolderDonorAssembly", slice.Properties["AssemblyName"]);
			Assert.Equal(Path.Combine(Path.GetDirectoryName(recipientProject)!, "Program.cs"), slice.SourceFiles.Single().FilePath);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", originalCacheDir);
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_Missing_Donor_File_Falls_Back_To_Empty()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string donorRoot = Path.Combine(tempRoot, "donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			Directory.CreateDirectory(donorRoot);
			WriteDonorIndex(indexPath, donorRoot);

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			Assert.True(slices.IsEmpty);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Theory]
	[InlineData("[]")]
	[InlineData("""{"version":2,"entries":{}}""")]
	[InlineData("""{"version":2,"entries":[{"path":"donor","newestMtimeMs":"invalid"}]}""")]
	public async Task ReadProjectCacheAsync_MalformedDonorIndex_Falls_Back_To_Empty(string indexContent)
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			WriteProjectFile(recipientProject);
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(indexPath, indexContent);

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			Assert.Empty(slices);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void CanceledDonorIndexRead_LogsNoWarning()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(indexPath, "{");

			CapturingTraceListener listener = new();
			TraceSource traceSource = new("CanceledDonorIndexRead", SourceLevels.Warning);
			traceSource.Listeners.Clear();
			traceSource.Listeners.Add(listener);
			ProjectDataDonorOptions donorOptions = new()
			{
				IndexPath = indexPath,
				WorkspaceRoot = recipientRoot,
				DiagnosticTraceSource = traceSource,
			};
			ProjectDataDonorIndex.GitQueryContext canceledContext = new(new CancellationToken(canceled: true));

			ProjectDataDonorCandidate[] candidates = ProjectDataDonorIndex
				.EnumerateDonorCandidatesCore(recipientProject, donorOptions, canceledContext)
				.ToArray();

			Assert.Empty(candidates);
			Assert.True(canceledContext.WasCancelled);
			Assert.Empty(listener.Warnings);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_Invalid_First_Donor_Tries_Next_Donor()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string badDonorRoot = Path.Combine(tempRoot, "bad-donor");
			string goodDonorRoot = Path.Combine(tempRoot, "good-donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string badDonorProject = Path.Combine(badDonorRoot, "src", "App", "App.csproj");
			string goodDonorProject = Path.Combine(goodDonorRoot, "src", "App", "App.csproj");
			Directory.CreateDirectory(Path.GetDirectoryName(badDonorProject)!);
			File.WriteAllText(badDonorProject + ".lscache", "version=999\n");
			WriteCache(goodDonorProject + ".lscache", "GoodDonorAssembly");
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(
				indexPath,
				$$"""
				{
				  "version": 2,
				  "entries": [
				    { "path": {{JsonString(badDonorRoot)}}, "updatedUtc": "2026-06-04T00:00:00Z" },
				    { "path": {{JsonString(goodDonorRoot)}}, "updatedUtc": "2026-06-03T00:00:00Z" }
				  ]
				}
				""");

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			CachedSliceData slice = Assert.Single(slices);
			Assert.Equal("GoodDonorAssembly", slice.Properties["AssemblyName"]);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_MalformedPathInFirstDonor_TriesNextDonor()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string badDonorRoot = Path.Combine(tempRoot, "bad-donor");
			string goodDonorRoot = Path.Combine(tempRoot, "good-donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string badDonorCache = Path.Combine(badDonorRoot, "src", "App", "App.csproj.lscache");
			string goodDonorCache = Path.Combine(goodDonorRoot, "src", "App", "App.csproj.lscache");
			WriteCache(badDonorCache, "BadDonorAssembly");
			File.WriteAllText(badDonorCache, File.ReadAllText(badDonorCache).Replace("project=App.csproj", "project=\0"));
			WriteCache(goodDonorCache, "GoodDonorAssembly");
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(
				indexPath,
				$$"""
				{
				  "version": 2,
				  "entries": [
				    { "path": {{JsonString(badDonorRoot)}}, "updatedUtc": "2026-06-04T00:00:00Z" },
				    { "path": {{JsonString(goodDonorRoot)}}, "updatedUtc": "2026-06-03T00:00:00Z" }
				  ]
				}
				""");

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			Assert.Equal("GoodDonorAssembly", Assert.Single(slices).Properties["AssemblyName"]);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_CanceledBeforeDonorSelection_Throws()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string donorRoot = Path.Combine(tempRoot, "donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			WriteCache(Path.Combine(donorRoot, "src", "App", "App.csproj.lscache"), "DonorAssembly");
			WriteDonorIndex(indexPath, donorRoot);
			int selectionCacheCount = ProjectDataDonorIndex.SelectionCacheCount;
			int metadataCacheCount = ProjectDataDonorIndex.WorkspaceMetadataCacheCount;

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				CacheFileReader.ReadProjectCacheAsync(
					recipientProject,
					cacheInProject: true,
					Resolver,
					cancellationToken: new CancellationToken(canceled: true),
					donorOptions: new ProjectDataDonorOptions
					{
						IndexPath = indexPath,
						WorkspaceRoot = recipientRoot,
					}));

			Assert.Equal(selectionCacheCount, ProjectDataDonorIndex.SelectionCacheCount);
			Assert.Equal(metadataCacheCount, ProjectDataDonorIndex.WorkspaceMetadataCacheCount);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task CanceledDonorSelection_IsNotCached_AndHealthyReadRetriesExactHead()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string staleDonorRoot = Path.Combine(tempRoot, "stale-donor");
			string exactDonorRoot = Path.Combine(tempRoot, "exact-donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string staleCache = Path.Combine(staleDonorRoot, "src", "App", "App.csproj.lscache");
			string exactCache = Path.Combine(exactDonorRoot, "src", "App", "App.csproj.lscache");
			InitializeGitRepository(recipientRoot);
			RunGit(tempRoot, "clone", recipientRoot, exactDonorRoot);
			RunGit(tempRoot, "clone", recipientRoot, staleDonorRoot);
			File.WriteAllText(Path.Combine(staleDonorRoot, "README.md"), "stale");
			RunGit(staleDonorRoot, "add", "README.md");
			RunGit(staleDonorRoot, "commit", "-m", "stale");
			WriteCache(staleCache, "StaleAssembly");
			WriteCache(exactCache, "ExactAssembly");
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(
				indexPath,
				$$"""
				{
				  "version": 2,
				  "entries": [
				    { "path": {{JsonString(staleDonorRoot)}}, "updatedUtc": "2026-06-04T00:00:00Z" },
				    { "path": {{JsonString(exactDonorRoot)}}, "updatedUtc": "2026-06-03T00:00:00Z" }
				  ]
				}
				""");
			ProjectDataDonorOptions donorOptions = new()
			{
				IndexPath = indexPath,
				WorkspaceRoot = recipientRoot,
				GitDistanceTopK = 0,
			};
			int selectionCacheCount = ProjectDataDonorIndex.SelectionCacheCount;
			int metadataCacheCount = ProjectDataDonorIndex.WorkspaceMetadataCacheCount;
			ProjectDataDonorIndex.GitQueryContext canceledContext = new(new CancellationToken(canceled: true));

			ProjectDataDonorCandidate canceledCandidate = ProjectDataDonorIndex
				.EnumerateDonorCandidatesCore(recipientProject, donorOptions, canceledContext)
				.First();

			Assert.Equal(staleCache, canceledCandidate.FilePath);
			Assert.True(canceledContext.WasCancelled);
			Assert.False(canceledContext.TimedOut);
			Assert.Equal(selectionCacheCount, ProjectDataDonorIndex.SelectionCacheCount);
			Assert.Equal(metadataCacheCount, ProjectDataDonorIndex.WorkspaceMetadataCacheCount);

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: donorOptions);

			Assert.Equal("ExactAssembly", Assert.Single(slices).Properties["AssemblyName"]);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task CanceledDonorGitDistanceSelection_IsNotCached_AndHealthyReadRetriesNearestHead()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string nearDonorRoot = Path.Combine(tempRoot, "near-donor");
			string farDonorRoot = Path.Combine(tempRoot, "far-donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string nearCache = Path.Combine(nearDonorRoot, "src", "App", "App.csproj.lscache");
			string farCache = Path.Combine(farDonorRoot, "src", "App", "App.csproj.lscache");
			string recipientHead = InitializeGitRepository(recipientRoot);
			File.WriteAllText(Path.Combine(recipientRoot, "README.md"), "second");
			RunGit(recipientRoot, "add", "README.md");
			RunGit(recipientRoot, "commit", "-m", "second");
			string nearHead = RunGit(recipientRoot, "rev-parse", "HEAD").Trim();
			File.WriteAllText(Path.Combine(recipientRoot, "README.md"), "third");
			RunGit(recipientRoot, "add", "README.md");
			RunGit(recipientRoot, "commit", "-m", "third");
			RunGit(tempRoot, "clone", recipientRoot, nearDonorRoot);
			RunGit(nearDonorRoot, "checkout", nearHead);
			RunGit(tempRoot, "clone", recipientRoot, farDonorRoot);
			RunGit(recipientRoot, "reset", "--hard", recipientHead);
			WriteCache(nearCache, "NearAssembly");
			WriteCache(farCache, "FarAssembly");
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(
				indexPath,
				$$"""
				{
				  "version": 2,
				  "entries": [
				    { "path": {{JsonString(farDonorRoot)}}, "updatedUtc": "2026-06-04T00:00:00Z" },
				    { "path": {{JsonString(nearDonorRoot)}}, "updatedUtc": "2026-06-03T00:00:00Z" }
				  ]
				}
				""");
			ProjectDataDonorOptions metadataPrimingOptions = new()
			{
				IndexPath = indexPath,
				WorkspaceRoot = recipientRoot,
				GitDistanceTopK = 0,
			};
			ProjectDataDonorCandidate primingCandidate = ProjectDataDonorIndex
				.EnumerateDonorCandidates(recipientProject, metadataPrimingOptions, TestContext.Current.CancellationToken)
				.First();
			Assert.Equal(farCache, primingCandidate.FilePath);

			int selectionCacheCount = ProjectDataDonorIndex.SelectionCacheCount;
			ProjectDataDonorOptions distanceOptions = new()
			{
				IndexPath = indexPath,
				WorkspaceRoot = recipientRoot,
				GitDistanceTopK = 2,
			};
			ProjectDataDonorIndex.GitQueryContext canceledContext = new(new CancellationToken(canceled: true));

			ProjectDataDonorCandidate canceledCandidate = ProjectDataDonorIndex
				.EnumerateDonorCandidatesCore(recipientProject, distanceOptions, canceledContext)
				.First();

			Assert.Equal(farCache, canceledCandidate.FilePath);
			Assert.True(canceledContext.WasCancelled);
			Assert.False(canceledContext.TimedOut);
			Assert.Equal(selectionCacheCount, ProjectDataDonorIndex.SelectionCacheCount);

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: distanceOptions);

			Assert.Equal("NearAssembly", Assert.Single(slices).Properties["AssemblyName"]);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void TimedOutDonorGitDistanceSelection_CachesFreshnessFallback_UntilIndexChanges()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string nearDonorRoot = Path.Combine(tempRoot, "near-donor");
			string farDonorRoot = Path.Combine(tempRoot, "far-donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string nearCache = Path.Combine(nearDonorRoot, "src", "App", "App.csproj.lscache");
			string farCache = Path.Combine(farDonorRoot, "src", "App", "App.csproj.lscache");
			string recipientHead = InitializeGitRepository(recipientRoot);
			File.WriteAllText(Path.Combine(recipientRoot, "README.md"), "second");
			RunGit(recipientRoot, "add", "README.md");
			RunGit(recipientRoot, "commit", "-m", "second");
			string nearHead = RunGit(recipientRoot, "rev-parse", "HEAD").Trim();
			File.WriteAllText(Path.Combine(recipientRoot, "README.md"), "third");
			RunGit(recipientRoot, "add", "README.md");
			RunGit(recipientRoot, "commit", "-m", "third");
			RunGit(tempRoot, "clone", recipientRoot, nearDonorRoot);
			RunGit(nearDonorRoot, "checkout", nearHead);
			RunGit(tempRoot, "clone", recipientRoot, farDonorRoot);
			RunGit(recipientRoot, "reset", "--hard", recipientHead);
			WriteCache(nearCache, "NearAssembly");
			WriteCache(farCache, "FarAssembly");
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(
				indexPath,
				$$"""
				{
				  "version": 2,
				  "entries": [
				    { "path": {{JsonString(farDonorRoot)}}, "updatedUtc": "2026-06-04T00:00:00Z" },
				    { "path": {{JsonString(nearDonorRoot)}}, "updatedUtc": "2026-06-03T00:00:00Z" }
				  ]
				}
				""");
			ProjectDataDonorOptions metadataPrimingOptions = new()
			{
				IndexPath = indexPath,
				WorkspaceRoot = recipientRoot,
				GitDistanceTopK = 0,
			};
			Assert.Equal(
				farCache,
				ProjectDataDonorIndex
					.EnumerateDonorCandidates(recipientProject, metadataPrimingOptions, TestContext.Current.CancellationToken)
					.First()
					.FilePath);

			CapturingTraceListener listener = new();
			TraceSource traceSource = new("DonorTimeoutTest", SourceLevels.Warning);
			traceSource.Listeners.Clear();
			traceSource.Listeners.Add(listener);
			ProjectDataDonorOptions distanceOptions = new()
			{
				IndexPath = indexPath,
				WorkspaceRoot = recipientRoot,
				GitDistanceTopK = 2,
				DiagnosticTraceSource = traceSource,
			};
			int selectionCacheCount = ProjectDataDonorIndex.SelectionCacheCount;
			ProjectDataDonorIndex.GitQueryContext timedOutContext = new(CancellationToken.None, timeoutMilliseconds: 0);

			ProjectDataDonorCandidate timedOutCandidate = ProjectDataDonorIndex
				.EnumerateDonorCandidatesCore(recipientProject, distanceOptions, timedOutContext)
				.First();
			string timeoutWarning = listener.Warnings;

			Assert.Equal(farCache, timedOutCandidate.FilePath);
			Assert.True(timedOutContext.TimedOut);
			Assert.False(timedOutContext.WasCancelled);
			Assert.Equal(selectionCacheCount + 1, ProjectDataDonorIndex.SelectionCacheCount);
			Assert.Contains("exceeded its 0 ms budget", timeoutWarning);
			Assert.Contains(recipientRoot, timeoutWarning);
			Assert.Contains("caching freshness ordering for 2 candidate worktrees", timeoutWarning);

			ProjectDataDonorCandidate cachedCandidate = ProjectDataDonorIndex
				.EnumerateDonorCandidates(recipientProject, distanceOptions, TestContext.Current.CancellationToken)
				.First();

			Assert.Equal(farCache, cachedCandidate.FilePath);
			Assert.Equal(timeoutWarning, listener.Warnings);

			File.AppendAllText(indexPath, Environment.NewLine);
			ProjectDataDonorCandidate refreshedCandidate = ProjectDataDonorIndex
				.EnumerateDonorCandidates(recipientProject, distanceOptions, TestContext.Current.CancellationToken)
				.First();

			Assert.Equal(nearCache, refreshedCandidate.FilePath);
			Assert.Equal(timeoutWarning, listener.Warnings);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_Donor_Read_Does_Not_Update_Index()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string donorRoot = Path.Combine(tempRoot, "donor");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string donorProject = Path.Combine(donorRoot, "src", "App", "App.csproj");
			WriteCache(donorProject + ".lscache", "DonorAssembly");
			WriteDonorIndex(indexPath, donorRoot);
			string before = await File.ReadAllTextAsync(indexPath, TestContext.Current.CancellationToken);

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			Assert.Single(slices);
			string after = await File.ReadAllTextAsync(indexPath, TestContext.Current.CancellationToken);
			Assert.Equal(before, after);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void RecipientMetadataFingerprint_Ignores_Unrelated_Loose_Refs()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string head = InitializeGitRepository(recipientRoot);
			string initialFingerprint = ProjectDataDonorIndex.GetRecipientMetadataFingerprint(
				recipientRoot,
				new(TestContext.Current.CancellationToken));

			string unrelatedRefPath = Path.Combine(recipientRoot, ".git", "refs", "heads", "unrelated");
			Directory.CreateDirectory(Path.GetDirectoryName(unrelatedRefPath)!);
			File.WriteAllText(unrelatedRefPath, head + "\n");
			string unrelatedRefFingerprint = ProjectDataDonorIndex.GetRecipientMetadataFingerprint(
				recipientRoot,
				new(TestContext.Current.CancellationToken));

			Assert.Equal(initialFingerprint, unrelatedRefFingerprint);

			string currentRef = RunGit(recipientRoot, "symbolic-ref", "HEAD").Trim();
			File.AppendAllText(Path.Combine(recipientRoot, ".git", currentRef.Replace('/', Path.DirectorySeparatorChar)), "\n");
			string changedHeadFingerprint = ProjectDataDonorIndex.GetRecipientMetadataFingerprint(
				recipientRoot,
				new(TestContext.Current.CancellationToken));
			Assert.NotEqual(initialFingerprint, changedHeadFingerprint);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void RecipientMetadataFingerprint_Canceled_Context_Returns_Interrupted_Without_Timeout()
	{
		ProjectDataDonorIndex.GitQueryContext context = new(new CancellationToken(canceled: true));

		string fingerprint = ProjectDataDonorIndex.GetRecipientMetadataFingerprint(TestProjectDirectory, context);

		Assert.Equal("git-interrupted", fingerprint);
		Assert.True(context.WasCancelled);
		Assert.False(context.TimedOut);
	}

	[Fact]
	public void RecipientMetadataFingerprint_Tracks_Reftable_State()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			_ = InitializeGitRepository(recipientRoot);
			string reftableListPath = Path.Combine(recipientRoot, ".git", "reftable", "tables.list");
			Directory.CreateDirectory(Path.GetDirectoryName(reftableListPath)!);
			File.WriteAllText(reftableListPath, "0x000000000001-0x000000000001-test.ref\n");
			string initialFingerprint = ProjectDataDonorIndex.GetRecipientMetadataFingerprint(
				recipientRoot,
				new(TestContext.Current.CancellationToken));

			File.AppendAllText(reftableListPath, "0x000000000002-0x000000000002-test.ref\n");
			string updatedFingerprint = ProjectDataDonorIndex.GetRecipientMetadataFingerprint(
				recipientRoot,
				new(TestContext.Current.CancellationToken));

			Assert.NotEqual(initialFingerprint, updatedFingerprint);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void RecipientMetadataFingerprint_Tracks_LinkedWorktree_Reftable_State()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string repositoryRoot = Path.Combine(tempRoot, "repository");
			string commonGitDirectory = Path.Combine(repositoryRoot, ".git");
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string worktreeGitDirectory = Path.Combine(commonGitDirectory, "worktrees", "recipient");
			string reftableListPath = Path.Combine(worktreeGitDirectory, "reftable", "tables.list");
			Directory.CreateDirectory(Path.GetDirectoryName(reftableListPath)!);
			Directory.CreateDirectory(recipientRoot);
			File.WriteAllText(Path.Combine(recipientRoot, ".git"), "gitdir: " + worktreeGitDirectory);
			File.WriteAllText(Path.Combine(worktreeGitDirectory, "commondir"), Path.Combine("..", ".."));
			File.WriteAllText(Path.Combine(worktreeGitDirectory, "HEAD"), "ref: refs/heads/.invalid\n");
			File.WriteAllText(reftableListPath, "0x000000000001-0x000000000001-test.ref\n");
			string initialFingerprint = ProjectDataDonorIndex.GetRecipientMetadataFingerprint(
				recipientRoot,
				new(TestContext.Current.CancellationToken));

			File.AppendAllText(reftableListPath, "0x000000000002-0x000000000002-test.ref\n");
			string updatedFingerprint = ProjectDataDonorIndex.GetRecipientMetadataFingerprint(
				recipientRoot,
				new(TestContext.Current.CancellationToken));

			Assert.NotEqual(initialFingerprint, updatedFingerprint);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_Selection_Cache_Tracks_Recipient_Head()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string firstHeadDonorRoot = Path.Combine(tempRoot, "first-head");
			string secondHeadDonorRoot = Path.Combine(tempRoot, "second-head");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string firstHead = InitializeGitRepository(recipientRoot);
			File.WriteAllText(Path.Combine(recipientRoot, "README.md"), "test2");
			RunGit(recipientRoot, "add", "README.md");
			RunGit(recipientRoot, "commit", "-m", "second");
			string secondHead = RunGit(recipientRoot, "rev-parse", "HEAD").Trim();
			RunGit(tempRoot, "clone", recipientRoot, firstHeadDonorRoot);
			RunGit(firstHeadDonorRoot, "checkout", firstHead);
			RunGit(tempRoot, "clone", recipientRoot, secondHeadDonorRoot);
			RunGit(recipientRoot, "reset", "--hard", firstHead);
			WriteCache(Path.Combine(firstHeadDonorRoot, "src", "App", "App.csproj.lscache"), "FirstHeadAssembly");
			WriteCache(Path.Combine(secondHeadDonorRoot, "src", "App", "App.csproj.lscache"), "SecondHeadAssembly");
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(
				indexPath,
				$$"""
				{
				  "version": 2,
				  "entries": [
				    { "path": {{JsonString(firstHeadDonorRoot)}}, "updatedUtc": "2026-06-03T00:00:00Z" },
				    { "path": {{JsonString(secondHeadDonorRoot)}}, "updatedUtc": "2026-06-03T00:00:00Z" }
				  ]
				}
				""");

			ProjectDataDonorOptions donorOptions = new()
			{
				IndexPath = indexPath,
				WorkspaceRoot = recipientRoot,
				GitDistanceTopK = 0,
			};

			ImmutableArray<CachedSliceData> firstSlices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: donorOptions);
			int selectionCacheCountAfterFirstRead = ProjectDataDonorIndex.SelectionCacheCount;
			int metadataCacheCountAfterFirstRead = ProjectDataDonorIndex.WorkspaceMetadataCacheCount;

			RunGit(recipientRoot, "reset", "--hard", secondHead);
			ImmutableArray<CachedSliceData> secondSlices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: donorOptions);

			Assert.Equal("FirstHeadAssembly", Assert.Single(firstSlices).Properties["AssemblyName"]);
			Assert.Equal("SecondHeadAssembly", Assert.Single(secondSlices).Properties["AssemblyName"]);
			Assert.Equal(selectionCacheCountAfterFirstRead, ProjectDataDonorIndex.SelectionCacheCount);
			Assert.Equal(metadataCacheCountAfterFirstRead, ProjectDataDonorIndex.WorkspaceMetadataCacheCount);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_Selection_Cache_Rechecks_Candidate_Heads_After_Index_Update()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string firstHeadDonorRoot = Path.Combine(tempRoot, "first-head");
			string secondHeadDonorRoot = Path.Combine(tempRoot, "second-head");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			string firstHead = InitializeGitRepository(recipientRoot);
			File.WriteAllText(Path.Combine(recipientRoot, "README.md"), "test2");
			RunGit(recipientRoot, "add", "README.md");
			RunGit(recipientRoot, "commit", "-m", "second");
			string secondHead = RunGit(recipientRoot, "rev-parse", "HEAD").Trim();
			RunGit(tempRoot, "clone", recipientRoot, firstHeadDonorRoot);
			RunGit(tempRoot, "clone", recipientRoot, secondHeadDonorRoot);
			RunGit(firstHeadDonorRoot, "checkout", firstHead);
			RunGit(recipientRoot, "reset", "--hard", firstHead);
			WriteCache(Path.Combine(firstHeadDonorRoot, "src", "App", "App.csproj.lscache"), "FirstHeadAssembly");
			WriteCache(Path.Combine(secondHeadDonorRoot, "src", "App", "App.csproj.lscache"), "SecondHeadAssembly");
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(
				indexPath,
				$$"""
				{
				  "version": 2,
				  "entries": [
				    { "path": {{JsonString(firstHeadDonorRoot)}}, "updatedUtc": "2026-06-03T00:00:00Z" },
				    { "path": {{JsonString(secondHeadDonorRoot)}}, "updatedUtc": "2026-06-03T00:00:00Z" }
				  ]
				}
				""");

			ProjectDataDonorOptions donorOptions = new()
			{
				IndexPath = indexPath,
				WorkspaceRoot = recipientRoot,
				GitDistanceTopK = 0,
			};

			ImmutableArray<CachedSliceData> firstSlices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: donorOptions);

			int selectionCacheCountAfterFirstRead = ProjectDataDonorIndex.SelectionCacheCount;
			int metadataCacheCountAfterFirstRead = ProjectDataDonorIndex.WorkspaceMetadataCacheCount;
			RunGit(firstHeadDonorRoot, "checkout", secondHead);
			RunGit(secondHeadDonorRoot, "checkout", firstHead);
			ImmutableArray<CachedSliceData> beforeIndexUpdateSlices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: donorOptions);
			File.AppendAllText(indexPath, Environment.NewLine);
			ImmutableArray<CachedSliceData> afterIndexUpdateSlices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: donorOptions);

			Assert.Equal("FirstHeadAssembly", Assert.Single(firstSlices).Properties["AssemblyName"]);
			Assert.Equal("FirstHeadAssembly", Assert.Single(beforeIndexUpdateSlices).Properties["AssemblyName"]);
			Assert.Equal("SecondHeadAssembly", Assert.Single(afterIndexUpdateSlices).Properties["AssemblyName"]);
			Assert.Equal(selectionCacheCountAfterFirstRead, ProjectDataDonorIndex.SelectionCacheCount);
			Assert.Equal(metadataCacheCountAfterFirstRead, ProjectDataDonorIndex.WorkspaceMetadataCacheCount);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_LiveExactHead_Donor_Beats_Newer_Stale_Donor()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string exactDonorRoot = Path.Combine(tempRoot, "exact");
			string staleDonorRoot = Path.Combine(tempRoot, "stale");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			InitializeGitRepository(recipientRoot);
			RunGit(tempRoot, "clone", recipientRoot, staleDonorRoot);
			File.WriteAllText(Path.Combine(recipientRoot, "README.md"), "second");
			RunGit(recipientRoot, "add", "README.md");
			RunGit(recipientRoot, "commit", "-m", "second");
			RunGit(tempRoot, "clone", recipientRoot, exactDonorRoot);
			WriteCache(Path.Combine(exactDonorRoot, "src", "App", "App.csproj.lscache"), "ExactAssembly");
			WriteCache(Path.Combine(staleDonorRoot, "src", "App", "App.csproj.lscache"), "StaleAssembly");

			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(
				indexPath,
				$$"""
				{
				  "version": 2,
				  "entries": [
				    { "path": {{JsonString(staleDonorRoot)}}, "updatedUtc": "2026-06-04T00:00:00Z" },
				    { "path": {{JsonString(exactDonorRoot)}}, "updatedUtc": "2026-06-03T00:00:00Z" }
				  ]
				}
				""");

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			CachedSliceData slice = Assert.Single(slices);
			Assert.Equal("ExactAssembly", slice.Properties["AssemblyName"]);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_EnrichesOnlyFiveFreshestDonorWorktrees()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string[] donorRoots = Enumerable
				.Range(1, 6)
				.Select(index => Path.Combine(tempRoot, $"donor-{index}"))
				.ToArray();
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			InitializeGitRepository(recipientRoot);
			RunGit(tempRoot, "clone", recipientRoot, donorRoots[5]);
			for (int i = 0; i < donorRoots.Length; i++)
			{
				WriteCache(
					Path.Combine(donorRoots[i], "src", "App", "App.csproj.lscache"),
					$"Donor{i + 1}Assembly");
			}

			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(
				indexPath,
				$$"""
				{
				  "version": 2,
				  "entries": [
				    { "path": {{JsonString(donorRoots[0])}}, "updatedUtc": "2026-06-06T00:00:00Z" },
				    { "path": {{JsonString(donorRoots[1])}}, "updatedUtc": "2026-06-05T00:00:00Z" },
				    { "path": {{JsonString(donorRoots[2])}}, "updatedUtc": "2026-06-04T00:00:00Z" },
				    { "path": {{JsonString(donorRoots[3])}}, "updatedUtc": "2026-06-03T00:00:00Z" },
				    { "path": {{JsonString(donorRoots[4])}}, "updatedUtc": "2026-06-02T00:00:00Z" },
				    { "path": {{JsonString(donorRoots[5])}}, "updatedUtc": "2026-06-01T00:00:00Z" }
				  ]
				}
				""");

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			Assert.Equal("Donor1Assembly", Assert.Single(slices).Properties["AssemblyName"]);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task ReadProjectCacheAsync_TriesFreshnessFallbackBeyondEnrichmentBound()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string recipientRoot = Path.Combine(tempRoot, "recipient");
			string[] donorRoots = Enumerable
				.Range(1, 6)
				.Select(index => Path.Combine(tempRoot, $"donor-{index}"))
				.ToArray();
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			string recipientProject = Path.Combine(recipientRoot, "src", "App", "App.csproj");
			WriteProjectFile(recipientProject);
			foreach (string donorRoot in donorRoots.Take(5))
			{
				Directory.CreateDirectory(donorRoot);
			}

			WriteCache(
				Path.Combine(donorRoots[5], "src", "App", "App.csproj.lscache"),
				"FallbackAssembly");
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(
				indexPath,
				$$"""
				{
				  "version": 2,
				  "entries": [
				    { "path": {{JsonString(donorRoots[0])}}, "updatedUtc": "2026-06-06T00:00:00Z" },
				    { "path": {{JsonString(donorRoots[1])}}, "updatedUtc": "2026-06-05T00:00:00Z" },
				    { "path": {{JsonString(donorRoots[2])}}, "updatedUtc": "2026-06-04T00:00:00Z" },
				    { "path": {{JsonString(donorRoots[3])}}, "updatedUtc": "2026-06-03T00:00:00Z" },
				    { "path": {{JsonString(donorRoots[4])}}, "updatedUtc": "2026-06-02T00:00:00Z" },
				    { "path": {{JsonString(donorRoots[5])}}, "updatedUtc": "2026-06-01T00:00:00Z" }
				  ]
				}
				""");

			ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadProjectCacheAsync(
				recipientProject,
				cacheInProject: true,
				Resolver,
				cancellationToken: TestContext.Current.CancellationToken,
				donorOptions: new ProjectDataDonorOptions
				{
					IndexPath = indexPath,
					WorkspaceRoot = recipientRoot,
					GitDistanceTopK = 0,
				});

			Assert.Equal("FallbackAssembly", Assert.Single(slices).Properties["AssemblyName"]);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	#endregion

	#region Cache File Path Computation

	[Fact]
	public void ProjectFolder_CacheFilePath()
	{
		string path = CacheFileReader.GetProjectFolderCacheFilePath(@"C:\dev\Foo\Foo.csproj");
		Assert.Equal(@"C:\dev\Foo\Foo.csproj.lscache", path);
	}

	[Fact]
	public void UserFolder_CacheFilePath_Is_Deterministic()
	{
		string path1 = CacheFileReader.GetUserFolderCacheFilePath(@"C:\dev\Foo\Foo.csproj");
		string path2 = CacheFileReader.GetUserFolderCacheFilePath(@"C:\dev\Foo\Foo.csproj");
		Assert.Equal(path1, path2);
	}

	[Fact]
	public void UserFolder_CacheFilePath_Differs_For_Different_Projects()
	{
		string path1 = CacheFileReader.GetUserFolderCacheFilePath(@"C:\dev\Foo\Foo.csproj");
		string path2 = CacheFileReader.GetUserFolderCacheFilePath(@"C:\dev\Bar\Bar.csproj");
		Assert.NotEqual(path1, path2);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CacheFilePathForWatching_MissingProjectFolderCache_ReturnsUserFolderPath(bool cacheInProject)
	{
		string tempRoot = CreateTempRoot();
		string? originalCacheDir = Environment.GetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR");
		try
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", Path.Combine(tempRoot, "user-cache"));
			string projectFilePath = Path.Combine(tempRoot, "src", "App", "App.csproj");

			Assert.Equal(
				CacheFileReader.GetUserFolderCacheFilePath(projectFilePath),
				CacheFileReader.GetCacheFilePathForWatching(projectFilePath, cacheInProject));
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", originalCacheDir);
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void CacheFilePathForWatching_ExistingProjectFolderCache_ReturnsProjectFolderPath()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string projectFilePath = Path.Combine(tempRoot, "src", "App", "App.csproj");
			string projectFolderCachePath = CacheFileReader.GetProjectFolderCacheFilePath(projectFilePath);
			WriteCache(projectFolderCachePath, "App");

			Assert.Equal(
				projectFolderCachePath,
				CacheFileReader.GetCacheFilePathForWatching(projectFilePath, cacheInProject: false));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void CacheReader_PreservesFiveParameterResolverOverload()
	{
		Assert.NotNull(
			typeof(CacheFileReader).GetMethod(
				nameof(CacheFileReader.ReadProjectCacheAsync),
				[
					typeof(string),
					typeof(bool),
					typeof(CachePathResolver),
					typeof(StringPool),
					typeof(CancellationToken),
				]));
	}

	#endregion

	#region Version 2 / Framework Packs

	[Fact]
	public async Task Version2_Header_Accepted()
	{
		string content = """
			version=2

			[project]
			language=C#
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Single(slices);
		Assert.Equal("C#", slices[0].LanguageName);
	}

	[Fact]
	public async Task FrameworkPacks_Section_Is_Tolerated_Even_With_No_Pack_OnDisk()
	{
		// When the named pack isn't installed on the test machine, expansion is a no-op.
		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net10.0

			[frameworkPacks]
			NoSuch.Pack.That.Does.Not.Exist
			""";

		ImmutableArray<CachedSliceData> slices = await ParseAsync(content);
		Assert.Single(slices);
		Assert.Empty(slices[0].MetadataReferences);
		Assert.Empty(slices[0].AnalyzerReferences);
	}

	[Fact]
	public async Task FrameworkPacks_Expand_Into_MetadataAndAnalyzer_References()
	{
		using TempPack pack = TempPack.Create("Test.Fake.Ref", "10.0.7", managed: ["System.Sample.dll"], analyzers: ["Sample.Analyzer.dll"]);

		string content = $$"""
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net10.0

			[frameworkPacks]
			Test.Fake.Ref
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithDotNetRoot(content, pack.DotNetRoot);

		Assert.Single(slices);
		Assert.Single(slices[0].MetadataReferences);
		Assert.EndsWith("System.Sample.dll", slices[0].MetadataReferences[0].FilePath);
		Assert.Single(slices[0].AnalyzerReferences);
		Assert.EndsWith("Sample.Analyzer.dll", slices[0].AnalyzerReferences[0]);
	}

	[Fact]
	public async Task FrameworkPacks_Expand_FromBoundSdkPathDotNetRoot()
	{
		using TempPack pack = TempPack.Create("Test.Fake.Ref", "10.0.7", managed: ["System.Sample.dll"], analyzers: [], targetFramework: "net10.0");
		string sdkPath = Path.Combine(pack.DotNetRoot, "sdk", "10.0.100");
		Directory.CreateDirectory(sdkPath);

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net10.0

			[frameworkPacks]
			Test.Fake.Ref
			""";

		content = StripLeadingTabs(content);
		CachePathResolver resolver = new("10.0.100", sdkPath);
		using StringReader reader = new(content);
		ImmutableArray<CachedSliceData> slices = await CacheFileReader.ReadFromAsync(reader, resolver, @"C:\dev\TestProject", @"C:\dev\TestProject\TestProject.csproj", expectedProjectFilePath: null, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);

		Assert.Single(slices);
		Assert.Single(slices[0].MetadataReferences);
		Assert.EndsWith("System.Sample.dll", slices[0].MetadataReferences[0].FilePath);
	}

	[Fact]
	public async Task FrameworkPacks_NuGetWinsConflict_PackEntriesWithSameBasenameAreSkipped()
	{
		using TempPack pack = TempPack.Create("Test.Fake.Ref", "10.0.7", managed: ["System.Text.Json.dll", "System.Sample.dll"], analyzers: []);

		string content = $$"""
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net10.0

			[frameworkPacks]
			Test.Fake.Ref

			[metadataReferences]
			<NUGET>/system.text.json/9.0.0/lib/net10.0/System.Text.Json.dll
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithDotNetRoot(content, pack.DotNetRoot);

		Assert.Single(slices);
		// Expect: explicit NuGet entry + System.Sample.dll from pack (NOT the pack's System.Text.Json.dll).
		Assert.Equal(2, slices[0].MetadataReferences.Length);
		Assert.Single(slices[0].MetadataReferences, r => r.FilePath.Contains("system.text.json", StringComparison.OrdinalIgnoreCase));
		Assert.Single(slices[0].MetadataReferences, r => r.FilePath.EndsWith("System.Sample.dll"));
		Assert.DoesNotContain(slices[0].MetadataReferences, r => r.FilePath.Contains(Path.Combine("Test.Fake.Ref", "10.0.7"), StringComparison.OrdinalIgnoreCase) && r.FilePath.EndsWith("System.Text.Json.dll"));
	}

	[Fact]
	public async Task FrameworkPacks_PicksHighestInstalledMatchingTfmMajor()
	{
		using TempPack pack = TempPack.Create("Test.Fake.Ref", "10.0.3", managed: ["Old.dll"], analyzers: []);
		// Add a higher version under the same root.
		string higherVerDir = Path.Combine(pack.DotNetRoot, "packs", "Test.Fake.Ref", "10.0.7");
		Directory.CreateDirectory(Path.Combine(higherVerDir, "ref", "net10.0"));
		Directory.CreateDirectory(Path.Combine(higherVerDir, "data"));
		File.WriteAllText(
			Path.Combine(higherVerDir, "data", "FrameworkList.xml"),
			"<FileList><File Type=\"Managed\" Path=\"ref/net10.0/New.dll\"/></FileList>");
		File.WriteAllText(Path.Combine(higherVerDir, "ref", "net10.0", "New.dll"), string.Empty);

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net10.0

			[frameworkPacks]
			Test.Fake.Ref
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithDotNetRoot(content, pack.DotNetRoot);

		Assert.Single(slices);
		Assert.Single(slices[0].MetadataReferences);
		Assert.EndsWith("New.dll", slices[0].MetadataReferences[0].FilePath);
	}

	[Fact]
	public async Task FrameworkPacks_Expand_PrereleasePackVersion_ForPreviewTfm()
	{
		using TempPack pack = TempPack.Create(
			"Test.Fake.Ref",
			"11.0.0-preview.3.26207.106",
			managed: ["Preview.dll"],
			analyzers: [],
			targetFramework: "net11.0");

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net11.0

			[frameworkPacks]
			Test.Fake.Ref
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithDotNetRoot(content, pack.DotNetRoot);

		Assert.Single(slices);
		Assert.Single(slices[0].MetadataReferences);
		Assert.EndsWith("Preview.dll", slices[0].MetadataReferences[0].FilePath);
	}

	[Fact]
	public async Task FrameworkPacks_FallsBackToNuGet_ExactSdkKnownPackageVersion()
	{
		using TempNuGetPack pack = TempNuGetPack.Create(
			packName: "Microsoft.NETCore.App.Ref",
			sdkKnownVersion: "8.0.26",
			installedVersions: [("8.0.26", "Exact.dll"), ("8.0.25", "Fallback.dll")],
			targetFramework: "net8.0");

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net8.0

			[frameworkPacks]
			Microsoft.NETCore.App.Ref
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithNuGetRoot(content, pack.NuGetRoot, pack.SdkPath);

		Assert.Single(slices);
		Assert.Single(slices[0].MetadataReferences);
		Assert.EndsWith("Exact.dll", slices[0].MetadataReferences[0].FilePath);
	}

	[Fact]
	public async Task FrameworkPacks_FallsBackToNuGet_HighestInstalledMatchingTfmMajor()
	{
		using TempNuGetPack pack = TempNuGetPack.Create(
			packName: "Microsoft.NETCore.App.Ref",
			sdkKnownVersion: "8.0.26",
			installedVersions: [("8.0.24", "Old.dll"), ("8.0.25", "Fallback.dll"), ("9.0.1", "WrongMajor.dll")],
			targetFramework: "net8.0");

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net8.0

			[frameworkPacks]
			Microsoft.NETCore.App.Ref
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithNuGetRoot(content, pack.NuGetRoot, pack.SdkPath);

		Assert.Single(slices);
		Assert.Single(slices[0].MetadataReferences);
		Assert.EndsWith("Fallback.dll", slices[0].MetadataReferences[0].FilePath);
	}

	[Fact]
	public async Task FrameworkPacks_MissingPackage_IsTolerated()
	{
		using TempNuGetPack pack = TempNuGetPack.Create(
			packName: "Microsoft.NETCore.App.Ref",
			sdkKnownVersion: "8.0.26",
			installedVersions: [],
			targetFramework: "net8.0");

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net8.0

			[frameworkPacks]
			Microsoft.NETCore.App.Ref
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithNuGetRoot(content, pack.NuGetRoot, pack.SdkPath);

		Assert.Single(slices);
		Assert.Empty(slices[0].MetadataReferences);
		Assert.Empty(slices[0].AnalyzerReferences);
	}

	/// <summary>
	/// Regression for a test-isolation leak: <see cref="ParseWithDotNetRoot"/> used to
	/// set <c>DOTNET_ROOT</c> and then construct <see cref="CachePathResolver"/> via its
	/// ambient-env constructor, which also unconditionally probes
	/// <c>C:\Program Files\dotnet</c>, <c>~/.dotnet</c>, etc. On developer machines that
	/// have <c>Microsoft.NETCore.App.Ref 8.0.x</c> installed under
	/// <c>Program Files\dotnet\packs\</c>, a <c>[frameworkPacks] Microsoft.NETCore.App.Ref</c>
	/// entry on <c>net8.0</c> resolved to the real system pack (163 DLLs) instead of finding
	/// nothing under the synthetic test root. CI didn't catch it because runners don't have
	/// that pack installed. This test asserts the helper produces a resolver whose dotnet
	/// roots are exactly what the test supplied.
	/// </summary>
	[Fact]
	public async Task FrameworkPacks_DoesNotResolveFromAmbientDotNetInstall()
	{
		// Synthetic root contains an unrelated pack name, so any `Microsoft.NETCore.App.Ref`
		// match could only come from the host's real `dotnet` install — which the test
		// helper must isolate against.
		using TempPack pack = TempPack.Create("Test.Unrelated.Ref", "10.0.7", managed: ["X.dll"], analyzers: []);

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net8.0

			[frameworkPacks]
			Microsoft.NETCore.App.Ref
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithDotNetRoot(content, pack.DotNetRoot);

		Assert.Single(slices);
		Assert.Empty(slices[0].MetadataReferences);
		Assert.Empty(slices[0].AnalyzerReferences);
	}

	[Fact]
	public async Task NetFxRefMetadataReferences_ExpandFromNuGetPackage()
	{
		using TempNetFrameworkReferenceAssemblies pack = TempNetFrameworkReferenceAssemblies.Create(
			version: "v4.7.2",
			assemblies: ["mscorlib.dll", "System.dll", Path.Combine("Facades", "System.Runtime.dll")]);

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net472

			[metadataReferences]
			<NETFXREF>/v4.7.2/mscorlib.dll
			<NETFXREF>/v4.7.2/Facades/System.Runtime.dll
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithNuGetRoot(content, pack.NuGetRoot, pack.SdkPath);

		Assert.Single(slices);
		Assert.Equal(2, slices[0].MetadataReferences.Length);
		Assert.Contains(slices[0].MetadataReferences, reference => reference.FilePath.EndsWith(Path.Combine("v4.7.2", "mscorlib.dll"), StringComparison.OrdinalIgnoreCase));
		Assert.Contains(slices[0].MetadataReferences, reference => reference.FilePath.EndsWith(Path.Combine("Facades", "System.Runtime.dll"), StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task NetFxRefMetadataReferences_MissingPackage_IsTolerated()
	{
		const string missingVersion = "v9.9.9";
		using TempNetFrameworkReferenceAssemblies pack = TempNetFrameworkReferenceAssemblies.Create(version: missingVersion, assemblies: []);

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net472

			[metadataReferences]
			<NETFXREF>/v9.9.9/mscorlib.dll
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithNuGetRoot(content, pack.NuGetRoot, pack.SdkPath);

		Assert.Single(slices);
		Assert.Empty(slices[0].MetadataReferences);
	}

	[Fact]
	public async Task SdkAnalyzerPacks_Expand_ExactSdkKnownPackageVersion()
	{
		using TempSdkAnalyzerPack pack = TempSdkAnalyzerPack.Create(
			packageId: "Microsoft.NET.ILLink.Tasks",
			sdkKnownVersion: "10.0.7",
			installedVersions: [("10.0.7", "Exact.dll"), ("10.0.8", "Wrong.dll")],
			targetFramework: "net10.0");

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net10.0

			[sdkAnalyzerPacks]
			Microsoft.NET.ILLink.Tasks
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithNuGetRoot(content, pack.NuGetRoot, pack.SdkPath);

		Assert.Single(slices);
		Assert.Single(slices[0].AnalyzerReferences);
		Assert.EndsWith("Exact.dll", slices[0].AnalyzerReferences[0]);
	}

	[Fact]
	public async Task SdkAnalyzerPacks_FallsBackToHighestInstalledMatchingTfmMajor()
	{
		using TempSdkAnalyzerPack pack = TempSdkAnalyzerPack.Create(
			packageId: "Microsoft.NET.ILLink.Tasks",
			sdkKnownVersion: "10.0.9",
			installedVersions: [("10.0.5", "Old.dll"), ("10.0.8", "Fallback.dll"), ("9.0.9", "WrongMajor.dll")],
			targetFramework: "net10.0");

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net10.0

			[sdkAnalyzerPacks]
			Microsoft.NET.ILLink.Tasks
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithNuGetRoot(content, pack.NuGetRoot, pack.SdkPath);

		Assert.Single(slices);
		Assert.Single(slices[0].AnalyzerReferences);
		Assert.EndsWith("Fallback.dll", slices[0].AnalyzerReferences[0]);
	}

	[Fact]
	public async Task SdkAnalyzerPacks_MissingPackage_IsTolerated()
	{
		using TempSdkAnalyzerPack pack = TempSdkAnalyzerPack.Create(
			packageId: "Microsoft.NET.ILLink.Tasks",
			sdkKnownVersion: "10.0.7",
			installedVersions: [],
			targetFramework: "net10.0");

		string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net10.0

			[sdkAnalyzerPacks]
			Microsoft.NET.ILLink.Tasks
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithNuGetRoot(content, pack.NuGetRoot, pack.SdkPath);

		Assert.Single(slices);
		Assert.Empty(slices[0].AnalyzerReferences);
	}

	[Fact]
	public async Task SdkAnalyzerConfigPolicy_DefaultAnalysisLevel_ResolvesFromTargetFramework()
	{
		const string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net10.0

			[sdkAnalyzerConfigPolicy]
			Microsoft.NET.Sdk/analyzers
			Microsoft.NET.Sdk/codestyle/cs
			""";

		using TempSdkAnalyzerConfigFiles sdk10 = TempSdkAnalyzerConfigFiles.Create(latestAnalysisLevel: "10.0");
		ImmutableArray<CachedSliceData> sdk10Slices = await ParseWithNuGetRoot(content, sdk10.NuGetRoot, sdk10.SdkPath);

		Assert.Single(sdk10Slices);
		Assert.Single(sdk10Slices[0].AnalyzerConfigFiles);
		Assert.Contains(sdk10Slices[0].AnalyzerConfigFiles, path => path.EndsWith(Path.Combine("config", "analysislevel_10_default.globalconfig"), StringComparison.OrdinalIgnoreCase));
		Assert.DoesNotContain(sdk10Slices[0].AnalyzerConfigFiles, path => path.EndsWith(Path.Combine("config", "analysislevelstyle_default.globalconfig"), StringComparison.OrdinalIgnoreCase));

		using TempSdkAnalyzerConfigFiles sdk11 = TempSdkAnalyzerConfigFiles.Create(latestAnalysisLevel: "11.0");
		ImmutableArray<CachedSliceData> sdk11Slices = await ParseWithNuGetRoot(content, sdk11.NuGetRoot, sdk11.SdkPath);

		Assert.Single(sdk11Slices);
		Assert.Single(sdk11Slices[0].AnalyzerConfigFiles);
		Assert.Contains(sdk11Slices[0].AnalyzerConfigFiles, path => path.EndsWith(Path.Combine("config", "analysislevel_10_default.globalconfig"), StringComparison.OrdinalIgnoreCase));
		Assert.DoesNotContain(sdk11Slices[0].AnalyzerConfigFiles, path => path.EndsWith(Path.Combine("config", "analysislevelstyle_default.globalconfig"), StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task SdkAnalyzerConfigPolicy_ExplicitLatest_ResolvesUsingSelectedSdkLatest()
	{
		const string content = """
			version=2

			[project]
			language=C#

			[sliceDimensions]
			TargetFramework=net10.0

			[sdkAnalyzerConfigPolicy]
			Microsoft.NET.Sdk/analyzers|AnalysisLevel=latest
			Microsoft.NET.Sdk/codestyle/cs|AnalysisLevel=latest
			""";

		using TempSdkAnalyzerConfigFiles sdk11 = TempSdkAnalyzerConfigFiles.Create(latestAnalysisLevel: "11.0");
		ImmutableArray<CachedSliceData> sdk11Slices = await ParseWithNuGetRoot(content, sdk11.NuGetRoot, sdk11.SdkPath);

		Assert.Single(sdk11Slices);
		Assert.Equal(2, sdk11Slices[0].AnalyzerConfigFiles.Length);
		Assert.Contains(sdk11Slices[0].AnalyzerConfigFiles, path => path.EndsWith(Path.Combine("config", "analysislevel_11_default.globalconfig"), StringComparison.OrdinalIgnoreCase));
		Assert.Contains(sdk11Slices[0].AnalyzerConfigFiles, path => path.EndsWith(Path.Combine("config", "analysislevelstyle_default.globalconfig"), StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task SdkAnalyzerConfigPolicy_NoSdkBinding_IsTolerated()
	{
		string content = """
			version=2

			[project]
			language=C#

			[sdkAnalyzerConfigPolicy]
			Microsoft.NET.Sdk/analyzers|AnalysisLevel=latest
			Microsoft.NET.Sdk/codestyle/cs|AnalysisLevel=latest
			""";

		ImmutableArray<CachedSliceData> slices = await ParseWithoutSdkBinding(content);

		Assert.Single(slices);
		Assert.Empty(slices[0].AnalyzerConfigFiles);
	}

	private static async Task<ImmutableArray<CachedSliceData>> ParseWithDotNetRoot(string content, string dotnetRoot)
	{
		content = StripLeadingTabs(content);
		CachePathResolver resolver = new(
			sdkVersion: null,
			sdkPath: null,
			dotnetRoots: [dotnetRoot],
			nugetFolders: [],
			netFxRefRoot: null);
		using StringReader reader = new(content);
		return await CacheFileReader.ReadFromAsync(reader, resolver, @"C:\dev\TestProject", @"C:\dev\TestProject\TestProject.csproj", expectedProjectFilePath: null, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);
	}

	private static async Task<ImmutableArray<CachedSliceData>> ParseWithoutSdkBinding(string content)
	{
		content = StripLeadingTabs(content);
		CachePathResolver resolver = new(
			sdkVersion: null,
			sdkPath: null,
			dotnetRoots: [],
			nugetFolders: [],
			netFxRefRoot: null);
		using StringReader reader = new(content);
		return await CacheFileReader.ReadFromAsync(reader, resolver, @"C:\dev\TestProject", @"C:\dev\TestProject\TestProject.csproj", expectedProjectFilePath: null, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);
	}

	private static string CreateTempRoot()
	{
		string path = Path.Combine(Path.GetTempPath(), "projectdata-cache-donor-tests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}

	private static void DeleteTempRoot(string tempRoot)
	{
		try
		{
			Directory.Delete(tempRoot, recursive: true);
		}
		catch
		{
		}
	}

	private static void WriteCache(string cachePath, string assemblyName)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
		File.WriteAllText(
			cachePath,
			$$"""
			version=2

			[project]
			project=App.csproj
			language=C#

			[properties]
			AssemblyName={{assemblyName}}

			[sourceFiles]
			Program.cs
			""");
	}

	private static void WriteProjectFile(string projectFilePath)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(projectFilePath)!);
		File.WriteAllText(projectFilePath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
	}

	private static void WriteDonorIndex(string indexPath, string donorRoot)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
		File.WriteAllText(
			indexPath,
			$$"""
			{
			  "version": 2,
			  "entries": [
			    { "path": {{JsonString(donorRoot)}}, "updatedUtc": "2026-06-03T00:00:00Z" }
			  ]
			}
			""");
	}

	private static string JsonString(string value)
		=> "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

	private static string InitializeGitRepository(string repositoryRoot)
	{
		Directory.CreateDirectory(repositoryRoot);
		RunGit(repositoryRoot, "init");
		File.WriteAllText(Path.Combine(repositoryRoot, "README.md"), "test");
		RunGit(repositoryRoot, "add", "README.md");
		RunGit(repositoryRoot, "commit", "-m", "initial");
		return RunGit(repositoryRoot, "rev-parse", "HEAD").Trim();
	}

	private static string RunGit(string workingDirectory, params string[] args)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = "git",
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};
		foreach (string arg in args)
		{
			startInfo.ArgumentList.Add(arg);
		}
		startInfo.Environment["GIT_AUTHOR_NAME"] = "ProjectData Test";
		startInfo.Environment["GIT_AUTHOR_EMAIL"] = "projectdata-test@example.com";
		startInfo.Environment["GIT_COMMITTER_NAME"] = "ProjectData Test";
		startInfo.Environment["GIT_COMMITTER_EMAIL"] = "projectdata-test@example.com";

		using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start git.");
		string output = process.StandardOutput.ReadToEnd();
		string error = process.StandardError.ReadToEnd();
		process.WaitForExit();
		Assert.True(process.ExitCode == 0, $"git {string.Join(" ", args)} failed in {workingDirectory}:{Environment.NewLine}{output}{Environment.NewLine}{error}");
		return output;
	}

	private static async Task<ImmutableArray<CachedSliceData>> ParseWithNuGetRoot(string content, string nugetRoot, string sdkPath)
	{
		content = StripLeadingTabs(content);
		CachePathResolver resolver = new(
			sdkVersion: "10.0.100",
			sdkPath: sdkPath,
			dotnetRoots: [],
			nugetFolders: [nugetRoot],
			netFxRefRoot: null);
		using StringReader reader = new(content);
		return await CacheFileReader.ReadFromAsync(reader, resolver, @"C:\dev\TestProject", @"C:\dev\TestProject\TestProject.csproj", expectedProjectFilePath: null, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);
	}

	/// <summary>
	/// Creates a synthetic on-disk SDK ref pack layout with a FrameworkList.xml and empty
	/// reference / analyzer files, rooted under a temp dotnet folder. Returns the path of
	/// that synthetic dotnet root for use as DOTNET_ROOT.
	/// </summary>
	private sealed class TempPack : IDisposable
	{
		public string DotNetRoot { get; }
		private readonly string root;

		private TempPack(string dotnetRoot, string root)
		{
			this.DotNetRoot = dotnetRoot;
			this.root = root;
		}

		public static TempPack Create(string packName, string packVersion, IEnumerable<string> managed, IEnumerable<string> analyzers, string targetFramework = "net10.0")
		{
			string root = Path.Combine(Path.GetTempPath(), "lscache-pack-" + Guid.NewGuid().ToString("N"));
			string dotnetRoot = Path.Combine(root, "dotnet");
			string packDir = Path.Combine(dotnetRoot, "packs", packName, packVersion);
			string dataDir = Path.Combine(packDir, "data");
			string refDir = Path.Combine(packDir, "ref", targetFramework);
			string analyzerDir = Path.Combine(packDir, "analyzers", "dotnet", "cs");
			Directory.CreateDirectory(dataDir);
			Directory.CreateDirectory(refDir);
			Directory.CreateDirectory(analyzerDir);

			System.Text.StringBuilder sb = new();
			sb.Append("<FileList>");
			foreach (string m in managed)
			{
				sb.Append($"<File Type=\"Managed\" Path=\"ref/{targetFramework}/{m}\" AssemblyName=\"{Path.GetFileNameWithoutExtension(m)}\" />");
				File.WriteAllText(Path.Combine(refDir, m), string.Empty);
			}
			foreach (string a in analyzers)
			{
				sb.Append($"<File Type=\"Analyzer\" Language=\"cs\" Path=\"analyzers/dotnet/cs/{a}\" />");
				File.WriteAllText(Path.Combine(analyzerDir, a), string.Empty);
			}
			sb.Append("</FileList>");
			File.WriteAllText(Path.Combine(dataDir, "FrameworkList.xml"), sb.ToString());

			return new TempPack(dotnetRoot, root);
		}

		public void Dispose()
		{
			try { Directory.Delete(this.root, recursive: true); } catch { }
		}
	}

	private sealed class TempNuGetPack : IDisposable
	{
		public string NuGetRoot { get; }
		public string SdkPath { get; }
		private readonly string root;

		private TempNuGetPack(string root, string nugetRoot, string sdkPath)
		{
			this.root = root;
			this.NuGetRoot = nugetRoot;
			this.SdkPath = sdkPath;
		}

		public static TempNuGetPack Create(
			string packName,
			string sdkKnownVersion,
			IEnumerable<(string Version, string ManagedAssembly)> installedVersions,
			string targetFramework)
		{
			string root = Path.Combine(Path.GetTempPath(), "lscache-nuget-pack-" + Guid.NewGuid().ToString("N"));
			string nugetRoot = Path.Combine(root, "nuget");
			string sdkPath = Path.Combine(root, "dotnet", "sdk", "10.0.100");
			Directory.CreateDirectory(sdkPath);
			File.WriteAllText(
				Path.Combine(sdkPath, "Microsoft.NETCoreSdk.BundledVersions.props"),
				$"""
				<Project>
				  <ItemGroup>
				    <KnownFrameworkReference Include="Microsoft.NETCore.App" TargetFramework="{targetFramework}" TargetingPackName="{packName}" TargetingPackVersion="{sdkKnownVersion}" />
				  </ItemGroup>
				</Project>
				""");

			foreach ((string version, string managedAssembly) in installedVersions)
			{
				string packDir = Path.Combine(nugetRoot, packName.ToLowerInvariant(), version);
				string dataDir = Path.Combine(packDir, "data");
				string refDir = Path.Combine(packDir, "ref", targetFramework);
				Directory.CreateDirectory(dataDir);
				Directory.CreateDirectory(refDir);
				File.WriteAllText(
					Path.Combine(dataDir, "FrameworkList.xml"),
					$"<FileList><File Type=\"Managed\" Path=\"ref/{targetFramework}/{managedAssembly}\" /></FileList>");
				File.WriteAllText(Path.Combine(refDir, managedAssembly), string.Empty);
			}

			return new TempNuGetPack(root, nugetRoot, sdkPath);
		}

		public void Dispose()
		{
			try { Directory.Delete(this.root, recursive: true); } catch { }
		}
	}

	private sealed class TempNetFrameworkReferenceAssemblies : IDisposable
	{
		public string NuGetRoot { get; }
		public string SdkPath { get; }
		private readonly string root;

		private TempNetFrameworkReferenceAssemblies(string root, string nugetRoot, string sdkPath)
		{
			this.root = root;
			this.NuGetRoot = nugetRoot;
			this.SdkPath = sdkPath;
		}

		public static TempNetFrameworkReferenceAssemblies Create(string version, IEnumerable<string> assemblies)
		{
			string root = Path.Combine(Path.GetTempPath(), "lscache-netfx-refs-" + Guid.NewGuid().ToString("N"));
			string nugetRoot = Path.Combine(root, "nuget");
			string sdkPath = Path.Combine(root, "dotnet", "sdk", "10.0.100");
			Directory.CreateDirectory(sdkPath);

			string packageId = "microsoft.netframework.referenceassemblies.net" + version.Replace("v", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(".", string.Empty, StringComparison.Ordinal);
			string refDir = Path.Combine(nugetRoot, packageId, "1.0.3", "build", ".NETFramework", version);
			foreach (string assembly in assemblies)
			{
				string path = Path.Combine(refDir, assembly);
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, string.Empty);
			}

			return new TempNetFrameworkReferenceAssemblies(root, nugetRoot, sdkPath);
		}

		public void Dispose()
		{
			try { Directory.Delete(this.root, recursive: true); } catch { }
		}
	}

	private sealed class TempSdkAnalyzerPack : IDisposable
	{
		public string NuGetRoot { get; }
		public string SdkPath { get; }
		private readonly string root;

		private TempSdkAnalyzerPack(string root, string nugetRoot, string sdkPath)
		{
			this.root = root;
			this.NuGetRoot = nugetRoot;
			this.SdkPath = sdkPath;
		}

		public static TempSdkAnalyzerPack Create(
			string packageId,
			string sdkKnownVersion,
			IEnumerable<(string Version, string AnalyzerAssembly)> installedVersions,
			string targetFramework)
		{
			string root = Path.Combine(Path.GetTempPath(), "lscache-sdk-analyzer-pack-" + Guid.NewGuid().ToString("N"));
			string nugetRoot = Path.Combine(root, "nuget");
			string sdkPath = Path.Combine(root, "dotnet", "sdk", "10.0.100");
			Directory.CreateDirectory(sdkPath);
			File.WriteAllText(
				Path.Combine(sdkPath, "Microsoft.NETCoreSdk.BundledVersions.props"),
				$"""
				<Project>
				  <ItemGroup>
				    <KnownILLinkPack Include="{packageId}" TargetFramework="{targetFramework}" ILLinkPackVersion="{sdkKnownVersion}" />
				  </ItemGroup>
				</Project>
				""");

			foreach ((string version, string analyzerAssembly) in installedVersions)
			{
				string analyzerDir = Path.Combine(nugetRoot, packageId.ToLowerInvariant(), version, "analyzers", "dotnet", "cs");
				Directory.CreateDirectory(analyzerDir);
				File.WriteAllText(Path.Combine(analyzerDir, analyzerAssembly), string.Empty);
			}

			return new TempSdkAnalyzerPack(root, nugetRoot, sdkPath);
		}

		public void Dispose()
		{
			try { Directory.Delete(this.root, recursive: true); } catch { }
		}
	}

	private sealed class TempSdkAnalyzerConfigFiles : IDisposable
	{
		public string NuGetRoot { get; }
		public string SdkPath { get; }
		private readonly string root;

		private TempSdkAnalyzerConfigFiles(string root, string nugetRoot, string sdkPath)
		{
			this.root = root;
			this.NuGetRoot = nugetRoot;
			this.SdkPath = sdkPath;
		}

		public static TempSdkAnalyzerConfigFiles Create(string latestAnalysisLevel = "10.0")
		{
			string root = Path.Combine(Path.GetTempPath(), "lscache-sdk-analyzer-config-" + Guid.NewGuid().ToString("N"));
			string nugetRoot = Path.Combine(root, "nuget");
			string sdkPath = Path.Combine(root, "dotnet", "sdk", "10.0.100");
			string targetsDir = Path.Combine(sdkPath, "Sdks", "Microsoft.NET.Sdk", "targets");
			string analyzerConfigDir = Path.Combine(sdkPath, "Sdks", "Microsoft.NET.Sdk", "analyzers", "build", "config");
			string codeStyleConfigDir = Path.Combine(sdkPath, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "build", "config");
			Directory.CreateDirectory(targetsDir);
			Directory.CreateDirectory(analyzerConfigDir);
			Directory.CreateDirectory(codeStyleConfigDir);
			File.WriteAllText(Path.Combine(targetsDir, "Microsoft.NET.Sdk.Analyzers.targets"), $"""
				<Project>
				  <PropertyGroup>
				    <_NoneAnalysisLevel>4.0</_NoneAnalysisLevel>
				    <_LatestAnalysisLevel>{latestAnalysisLevel}</_LatestAnalysisLevel>
				    <_PreviewAnalysisLevel>12.0</_PreviewAnalysisLevel>
				  </PropertyGroup>
				</Project>
				""");
			File.WriteAllText(Path.Combine(analyzerConfigDir, "analysislevel_10_default.globalconfig"), string.Empty);
			File.WriteAllText(Path.Combine(analyzerConfigDir, "analysislevel_11_default.globalconfig"), string.Empty);
			File.WriteAllText(Path.Combine(codeStyleConfigDir, "analysislevelstyle_default.globalconfig"), string.Empty);

			return new TempSdkAnalyzerConfigFiles(root, nugetRoot, sdkPath);
		}

		public void Dispose()
		{
			try { Directory.Delete(this.root, recursive: true); } catch { }
		}
	}

	#endregion

	/// <summary>
	/// Helper to parse a cache file from a string using the test project directory.
	/// </summary>
	private static async Task<ImmutableArray<CachedSliceData>> ParseAsync(string content)
	{
		// Normalize indentation — test strings use tab indentation which we strip.
		content = StripLeadingTabs(content);

		using StringReader reader = new(content);
		return await CacheFileReader.ReadFromAsync(reader, Resolver, TestProjectDirectory, TestProjectFilePath, expectedProjectFilePath: null, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);
	}

	private static string StripLeadingTabs(string content)
	{
		string[] lines = content.Split('\n');
		// Find the minimum tab indentation (ignoring empty lines).
		int minTabs = int.MaxValue;
		foreach (string line in lines)
		{
			if (line.Trim().Length == 0)
				continue;
			int tabs = 0;
			foreach (char c in line)
			{
				if (c == '\t')
					tabs++;
				else
					break;
			}
			minTabs = Math.Min(minTabs, tabs);
		}

		if (minTabs == int.MaxValue || minTabs == 0)
			return content;

		return string.Join('\n', lines.Select(l => l.Length > minTabs ? l[minTabs..] : l));
	}

	private sealed class BlockingTextReader(string[] lines, int blockAtRead) : TextReader
	{
		private readonly TaskCompletionSource<bool> readBlocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int readCount;

		public Task WaitUntilBlockedAsync() => this.readBlocked.Task;

		public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
		{
			if (this.readCount++ == blockAtRead)
			{
				this.readBlocked.TrySetResult(true);
				return new(WaitForCancellationAsync(cancellationToken));
			}

			int lineIndex = this.readCount - 1;
			return ValueTask.FromResult<string?>(lineIndex < lines.Length ? lines[lineIndex] : null);
		}

		private static async Task<string?> WaitForCancellationAsync(CancellationToken cancellationToken)
		{
			await Task.Delay(Timeout.Infinite, cancellationToken);
			return null;
		}
	}

	private sealed class CancelAfterReturningLineTextReader(
		string[] lines,
		int cancelAfterReturningLine,
		CancellationTokenSource cancellationSource) : TextReader
	{
		private int readCount;

		public bool ReadRequestedAfterCancellation { get; private set; }

		public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				this.ReadRequestedAfterCancellation = true;
				throw new InvalidOperationException("The parser requested another line after cancellation.");
			}

			int lineIndex = this.readCount++;
			string? line = lineIndex < lines.Length ? lines[lineIndex] : null;
			if (lineIndex == cancelAfterReturningLine)
			{
				cancellationSource.Cancel();
			}

			return ValueTask.FromResult(line);
		}
	}

	// Captures Trace output by severity so a test can assert that a given message was emitted at
	// Information level (and NOT as a Warning). Only used while explicitly added to Trace.Listeners
	// inside a test; this collection disables parallelization, so no other test logs concurrently.
	private sealed class CapturingTraceListener : TraceListener
	{
		private readonly StringBuilder information = new();
		private readonly StringBuilder warnings = new();

		public string Information => this.information.ToString();

		public string Warnings => this.warnings.ToString();

		public override bool IsThreadSafe => false;

		public void Clear()
		{
			this.information.Clear();
			this.warnings.Clear();
		}

		public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
		{
			StringBuilder? sink = eventType switch
			{
				TraceEventType.Information => this.information,
				TraceEventType.Warning => this.warnings,
				_ => null,
			};
			sink?.Append(message).Append('\n');
		}

		public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
			=> this.TraceEvent(eventCache, source, eventType, id, args is null ? format : string.Format(format ?? string.Empty, args));

		// Unused for event-based tracing, but TraceListener requires them.
		public override void Write(string? message)
		{
		}

		public override void WriteLine(string? message)
		{
		}
	}
}
