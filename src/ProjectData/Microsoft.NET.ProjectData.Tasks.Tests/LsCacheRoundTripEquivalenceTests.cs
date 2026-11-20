// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias ProjectDataAssembly;

using System.Collections.Immutable;
using Microsoft.Build.Framework;
using Moq;
using Xunit;
using ProductDataFromProjectDataAssembly = ProjectDataAssembly::Microsoft.NET.ProjectData;

namespace Microsoft.NET.ProjectData.Tasks.Tests;

[CollectionDefinition(DotnetRootEnvCollection.Name, DisableParallelization = true)]
public sealed class DotnetRootEnvCollection
{
	public const string Name = "DotnetRootEnv";
}

/// <summary>
/// End-to-end equivalence: a cache file produced by the writer must, when read back through
/// <see cref="CacheFileReader"/>, materialize the same set of metadata and analyzer references
/// the writer was handed. This guards against drift between the new <c>[frameworkPacks]</c>
/// section + reader-side <c>FrameworkList.xml</c> expansion and the old "list every ref-pack
/// DLL inline" approach.
/// </summary>
[Collection(DotnetRootEnvCollection.Name)]
public class LsCacheRoundTripEquivalenceTests
{
	[Fact]
	public async Task FullRoundTrip_FrameworkPackEntries_AreReconstructedAfterRead()
	{
		// Arrange a synthetic ref pack with two managed and one analyzer entry.
		using TempPack pack = TempPack.Create(
			packName: "Test.Equivalence.App.Ref",
			packVersion: "10.0.7",
			managed: ["System.Sample.dll", "Other.dll"],
			analyzers: ["Sample.Analyzer.dll"]);

		string projectFile = Path.Combine(pack.Root, "App.csproj");

		// Hand the writer a mix of pack-rooted refs (which it will divert to [frameworkPacks])
		// and one explicit NuGet ref (which it will keep inline).
		string packRefRoot = Path.Combine(pack.DotNetRoot, "packs", "Test.Equivalence.App.Ref", "10.0.7");
		string packDll1 = Path.Combine(packRefRoot, "ref", "net10.0", "System.Sample.dll");
		string packDll2 = Path.Combine(packRefRoot, "ref", "net10.0", "Other.dll");
		string packAnalyzer = Path.Combine(packRefRoot, "analyzers", "dotnet", "cs", "Sample.Analyzer.dll");
		string nugetDll = @"C:\nuget\foo\1.0\lib\net10.0\Foo.dll";

		ITaskItem[] metadataRefs = [MakeItem(packDll1), MakeItem(packDll2), MakeItem(nugetDll)];
		ITaskItem[] analyzerRefs = [MakeItem(packAnalyzer)];

		ITaskItem[] sliceDimensions = [MakeKvp("TargetFramework", "net10.0")];

		string previous = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty;
		try
		{
			// The writer still consults DOTNET_ROOT to discover whether a ref-pack-rooted file
			// should be diverted into [frameworkPacks]; isolate this from the host env so a
			// developer machine with a real install doesn't poison the synthetic pack lookup.
			Environment.SetEnvironmentVariable("DOTNET_ROOT", pack.DotNetRoot);

			string content = ProjectDataWriter.BuildContent(
				projectFile, writeHeader: true, isPrimary: true, lastDtbSucceeded: true,
				sliceDimensions: sliceDimensions,
				properties: null, commandLineArguments: null,
				sourceFiles: null,
				metadataReferences: metadataRefs,
				analyzerReferences: analyzerRefs,
				analyzerConfigFiles: null, additionalFiles: null, projectReferences: null);

			// Sanity: writer produced version=2 + a [frameworkPacks] section, and dropped pack DLLs.
			Assert.Contains("version=2", content);
			Assert.Contains("[frameworkPacks]", content);
			Assert.Contains("Test.Equivalence.App.Ref", content);
			Assert.DoesNotContain("System.Sample.dll", content);
			Assert.DoesNotContain("Other.dll", content);
			Assert.DoesNotContain("Sample.Analyzer.dll", content);

			// Round-trip read against an isolated resolver that only sees the synthetic
			// dotnet root — never the host install. This is critical on dev / CI machines
			// where /usr/share/dotnet/packs or C:\Program Files\dotnet\packs contains real
			// targeting packs that would silently leak into the FrameworkList.xml lookup.
			ProductDataFromProjectDataAssembly.CachePathResolver readerResolver = new(
				sdkVersion: null,
				sdkPath: null,
				dotnetRoots: [pack.DotNetRoot],
				nugetFolders: [],
				netFxRefRoot: null);
			using StringReader reader = new(content);
			ImmutableArray<ProductDataFromProjectDataAssembly.CachedSliceData> slices = await ProductDataFromProjectDataAssembly.CacheFileReader.ReadFromAsync(
				reader, readerResolver, Path.GetDirectoryName(projectFile)!, projectFile, expectedProjectFilePath: null, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);

			Assert.Single(slices);
			ProductDataFromProjectDataAssembly.CachedSliceData slice = slices[0];

			// Materialized metadata refs: the explicit NuGet ref + both pack DLLs (re-expanded).
			HashSet<string> metaBasenames = [.. slice.MetadataReferences.Select(r => Path.GetFileName(r.FilePath))];
			Assert.Equal(3, slice.MetadataReferences.Length);
			Assert.Contains("Foo.dll", metaBasenames);
			Assert.Contains("System.Sample.dll", metaBasenames);
			Assert.Contains("Other.dll", metaBasenames);

			// Materialized analyzer refs: the pack analyzer.
			Assert.Single(slice.AnalyzerReferences);
			Assert.EndsWith("Sample.Analyzer.dll", slice.AnalyzerReferences[0]);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", previous);
		}
	}

	[Fact]
	public async Task FullRoundTrip_NetSdkAnalyzerEntries_AreReconstructedAfterRead()
	{
		// Arrange a synthetic dotnet root with an SDK install that contains an
		// analyzer DLL and a global config under <DOTNET>/sdk/<ver>/Sdks/...
		string root = Path.Combine(Path.GetTempPath(), "lscache-netsdk-rt-" + Guid.NewGuid().ToString("N"));
		string dotnetRoot = Path.Combine(root, "dotnet");
		string sdkVersion = "10.0.202";
		string sdkAnalyzerDir = Path.Combine(dotnetRoot, "sdk", sdkVersion, "Sdks", "Microsoft.NET.Sdk", "analyzers");
		string sdkConfigDir = Path.Combine(sdkAnalyzerDir, "build", "config");
		Directory.CreateDirectory(sdkAnalyzerDir);
		Directory.CreateDirectory(sdkConfigDir);
		string analyzerDll = Path.Combine(sdkAnalyzerDir, "Microsoft.CodeAnalysis.NetAnalyzers.dll");
		string configFile = Path.Combine(sdkConfigDir, "analysislevel_10_default.globalconfig");
		File.WriteAllText(analyzerDll, string.Empty);
		File.WriteAllText(configFile, string.Empty);

		string projectFile = Path.Combine(root, "App.csproj");
		ITaskItem[] sliceDimensions = [MakeKvp("TargetFramework", "net10.0")];

		string previous = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty;
		try
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", dotnetRoot);

			string content = ProjectDataWriter.BuildContent(
				projectFile, writeHeader: true, isPrimary: true, lastDtbSucceeded: true,
				sliceDimensions: sliceDimensions,
				properties: null, commandLineArguments: null,
				sourceFiles: null,
				metadataReferences: null,
				analyzerReferences: [MakeItem(analyzerDll)],
				analyzerConfigFiles: [configFile],
				additionalFiles: null, projectReferences: null);

			// Sanity: the writer dropped the SDK version from both paths and wrote them
			// under the <NETSDK> sentinel. The version literal must not appear anywhere
			// in the SDK-shipped path encoding.
			Assert.Contains("<NETSDK>/Sdks/Microsoft.NET.Sdk/analyzers", content);
			Assert.DoesNotContain($"sdk/{sdkVersion}", content);

			// Reader must be SDK-bound. Constructing without a binding would throw
			// when expanding <NETSDK>; this is the required call shape for any
			// consumer of cache files containing SDK content.
			ProductDataFromProjectDataAssembly.CachePathResolver readerResolver = new(sdkVersion);
			using StringReader reader = new(content);
			ImmutableArray<ProductDataFromProjectDataAssembly.CachedSliceData> slices = await ProductDataFromProjectDataAssembly.CacheFileReader.ReadFromAsync(
				reader, readerResolver, Path.GetDirectoryName(projectFile)!, projectFile, expectedProjectFilePath: null, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);

			Assert.Single(slices);
			ProductDataFromProjectDataAssembly.CachedSliceData slice = slices[0];

			// Materialized analyzer ref reconstructs the original absolute path.
			Assert.Single(slice.AnalyzerReferences);
			Assert.Equal(analyzerDll, slice.AnalyzerReferences[0]);

			// Materialized config file likewise.
			Assert.Single(slice.AnalyzerConfigFiles);
			Assert.Equal(configFile, slice.AnalyzerConfigFiles[0]);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", previous);
			try { Directory.Delete(root, recursive: true); } catch { }
		}
	}

	[Fact]
	public async Task FullRoundTrip_NetSdkContent_ReaderWithoutBinding_SkipsNetSdkEntries()
	{
		// Demonstrates the contract: a cache file containing <NETSDK> entries
		// is still readable without an SDK binding. The reader skips any
		// <NETSDK>-prefixed entries and logs a warning — the rest of the
		// cache loads normally and the analyzer list is simply empty.
		string root = Path.Combine(Path.GetTempPath(), "lscache-netsdk-rt-" + Guid.NewGuid().ToString("N"));
		string dotnetRoot = Path.Combine(root, "dotnet");
		string sdkAnalyzerDir = Path.Combine(dotnetRoot, "sdk", "10.0.202", "Sdks", "Microsoft.NET.Sdk", "analyzers");
		Directory.CreateDirectory(sdkAnalyzerDir);
		string analyzerDll = Path.Combine(sdkAnalyzerDir, "Foo.dll");
		File.WriteAllText(analyzerDll, string.Empty);

		string projectFile = Path.Combine(root, "App.csproj");
		string previous = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty;
		try
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", dotnetRoot);

			string content = ProjectDataWriter.BuildContent(
				projectFile, writeHeader: true, isPrimary: true, lastDtbSucceeded: true,
				sliceDimensions: [MakeKvp("TargetFramework", "net10.0")],
				properties: null, commandLineArguments: null,
				sourceFiles: null, metadataReferences: null,
				analyzerReferences: [MakeItem(analyzerDll)],
				analyzerConfigFiles: null, additionalFiles: null, projectReferences: null);

			ProductDataFromProjectDataAssembly.CachePathResolver readerResolver = new(); // no binding
			Assert.False(readerResolver.IsNetSdkBound);
			using StringReader reader = new(content);

			ImmutableArray<ProductDataFromProjectDataAssembly.CachedSliceData> slices = await ProductDataFromProjectDataAssembly.CacheFileReader.ReadFromAsync(
				reader, readerResolver, Path.GetDirectoryName(projectFile)!, projectFile, expectedProjectFilePath: null, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);

			// Cache reads successfully. <NETSDK> analyzer entries are skipped
			// (no SDK binding), so the analyzer list is empty. The slice itself
			// is present — the rest of the project data is intact.
			Assert.Single(slices);
			Assert.Empty(slices[0].AnalyzerReferences);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", previous);
			try { Directory.Delete(root, recursive: true); } catch { }
		}
	}

	[Fact]
	public async Task FullRoundTrip_NuGetFrameworkPackEntries_AreReconstructedAfterRead()
	{
		using TempNuGetPack pack = TempNuGetPack.Create(
			packName: "Microsoft.NETCore.App.Ref",
			packVersion: "8.0.26",
			targetFramework: "net8.0",
			managed: ["System.Runtime.dll", "System.Collections.dll"],
			analyzers: ["Framework.Analyzer.dll"]);

		string projectFile = Path.Combine(pack.Root, "App.csproj");
		string packageRoot = Path.Combine(pack.NuGetRoot, "microsoft.netcore.app.ref", "8.0.26");
		string packDll1 = Path.Combine(packageRoot, "ref", "net8.0", "System.Runtime.dll");
		string packDll2 = Path.Combine(packageRoot, "ref", "net8.0", "System.Collections.dll");
		string packAnalyzer = Path.Combine(packageRoot, "analyzers", "dotnet", "cs", "Framework.Analyzer.dll");
		ITaskItem[] sliceDimensions = [MakeKvp("TargetFramework", "net8.0")];

		string previousNuGet = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? string.Empty;
		try
		{
			// Writer still consults NUGET_PACKAGES when classifying refs into [frameworkPacks];
			// isolate from the host env so a real ~/.nuget/packages cache doesn't influence the
			// classification of our synthetic pack paths.
			Environment.SetEnvironmentVariable("NUGET_PACKAGES", pack.NuGetRoot);

			string content = ProjectDataWriter.BuildContent(
				projectFile, writeHeader: true, isPrimary: true, lastDtbSucceeded: true,
				sliceDimensions: sliceDimensions,
				properties: null, commandLineArguments: null,
				sourceFiles: null,
				metadataReferences:
				[
					MakeItem(packDll1, nuGetPackageId: "Microsoft.NETCore.App.Ref", nuGetPackageVersion: "8.0.26", frameworkReferenceName: "Microsoft.NETCore.App"),
					MakeItem(packDll2, nuGetPackageId: "Microsoft.NETCore.App.Ref", nuGetPackageVersion: "8.0.26", frameworkReferenceName: "Microsoft.NETCore.App"),
				],
				analyzerReferences: [MakeItem(packAnalyzer, nuGetPackageId: "Microsoft.NETCore.App.Ref", nuGetPackageVersion: "8.0.26", frameworkReferenceName: "Microsoft.NETCore.App")],
				analyzerConfigFiles: null, additionalFiles: null, projectReferences: null,
				capabilities: null);

			Assert.Contains("[frameworkPacks]", content);
			Assert.DoesNotContain("[nugetFrameworkPacks]", content);
			Assert.Contains("Microsoft.NETCore.App.Ref", content);
			Assert.DoesNotContain("System.Runtime.dll", content);
			Assert.DoesNotContain("8.0.26/ref/net8.0", content);

			// Use the test seam so the reader only sees our synthetic NuGet root. The default
			// resolver would also probe `/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/...`
			// (or `C:\Program Files\dotnet\packs\...`) and, on a machine with a real .NET 8 SDK
			// install, that pack's FrameworkList.xml would expand to its full ~163-entry inventory
			// instead of the two entries we wrote.
			ProductDataFromProjectDataAssembly.CachePathResolver readerResolver = new(
				sdkVersion: "10.0.100",
				sdkPath: pack.SdkPath,
				dotnetRoots: [],
				nugetFolders: [pack.NuGetRoot],
				netFxRefRoot: null);
			using StringReader reader = new(content);
			ImmutableArray<ProductDataFromProjectDataAssembly.CachedSliceData> slices = await ProductDataFromProjectDataAssembly.CacheFileReader.ReadFromAsync(
				reader, readerResolver, Path.GetDirectoryName(projectFile)!, projectFile, expectedProjectFilePath: null, stringPool: null, cancellationToken: TestContext.Current.CancellationToken);

			Assert.Single(slices);
			ProductDataFromProjectDataAssembly.CachedSliceData slice = slices[0];
			HashSet<string> metaBasenames = [.. slice.MetadataReferences.Select(r => Path.GetFileName(r.FilePath))];
			Assert.Equal(2, slice.MetadataReferences.Length);
			Assert.Contains("System.Runtime.dll", metaBasenames);
			Assert.Contains("System.Collections.dll", metaBasenames);
			Assert.Single(slice.AnalyzerReferences);
			Assert.EndsWith("Framework.Analyzer.dll", slice.AnalyzerReferences[0]);
		}
		finally
		{
			Environment.SetEnvironmentVariable("NUGET_PACKAGES", previousNuGet);
		}
	}

	private static ITaskItem MakeItem(
		string identity,
		string? nuGetPackageId = null,
		string? nuGetPackageVersion = null,
		string? frameworkReferenceName = null)
	{
		var mock = new Mock<ITaskItem>();
		mock.Setup(i => i.ItemSpec).Returns(identity);
		mock.Setup(i => i.GetMetadata("Aliases")).Returns(string.Empty);
		mock.Setup(i => i.GetMetadata("EmbedInteropTypes")).Returns(string.Empty);
		mock.Setup(i => i.GetMetadata("Value")).Returns(string.Empty);
		mock.Setup(i => i.GetMetadata("NuGetPackageId")).Returns(nuGetPackageId ?? string.Empty);
		mock.Setup(i => i.GetMetadata("NuGetPackageVersion")).Returns(nuGetPackageVersion ?? string.Empty);
		mock.Setup(i => i.GetMetadata("FrameworkReferenceName")).Returns(frameworkReferenceName ?? string.Empty);
		return mock.Object;
	}

	private static ITaskItem MakeKvp(string key, string value)
	{
		var mock = new Mock<ITaskItem>();
		mock.Setup(i => i.ItemSpec).Returns(key);
		mock.Setup(i => i.GetMetadata("Value")).Returns(value);
		return mock.Object;
	}

	private sealed class TempPack : IDisposable
	{
		public string Root { get; }
		public string DotNetRoot { get; }

		private TempPack(string root, string dotnetRoot)
		{
			this.Root = root;
			this.DotNetRoot = dotnetRoot;
		}

		public static TempPack Create(string packName, string packVersion, IEnumerable<string> managed, IEnumerable<string> analyzers)
		{
			string root = Path.Combine(Path.GetTempPath(), "lscache-rt-" + Guid.NewGuid().ToString("N"));
			string dotnetRoot = Path.Combine(root, "dotnet");
			string packDir = Path.Combine(dotnetRoot, "packs", packName, packVersion);
			string dataDir = Path.Combine(packDir, "data");
			string refDir = Path.Combine(packDir, "ref", "net10.0");
			string analyzerDir = Path.Combine(packDir, "analyzers", "dotnet", "cs");
			Directory.CreateDirectory(dataDir);
			Directory.CreateDirectory(refDir);
			Directory.CreateDirectory(analyzerDir);

			System.Text.StringBuilder sb = new();
			sb.Append("<FileList>");
			foreach (string m in managed)
			{
				sb.Append($"<File Type=\"Managed\" Path=\"ref/net10.0/{m}\" AssemblyName=\"{Path.GetFileNameWithoutExtension(m)}\" />");
				File.WriteAllText(Path.Combine(refDir, m), string.Empty);
			}
			foreach (string a in analyzers)
			{
				sb.Append($"<File Type=\"Analyzer\" Language=\"cs\" Path=\"analyzers/dotnet/cs/{a}\" />");
				File.WriteAllText(Path.Combine(analyzerDir, a), string.Empty);
			}
			sb.Append("</FileList>");
			File.WriteAllText(Path.Combine(dataDir, "FrameworkList.xml"), sb.ToString());

			return new TempPack(root, dotnetRoot);
		}

		public void Dispose()
		{
			try { Directory.Delete(this.Root, recursive: true); } catch { }
		}
	}

	private sealed class TempNuGetPack : IDisposable
	{
		public string Root { get; }
		public string NuGetRoot { get; }
		public string SdkPath { get; }

		private TempNuGetPack(string root, string nugetRoot, string sdkPath)
		{
			this.Root = root;
			this.NuGetRoot = nugetRoot;
			this.SdkPath = sdkPath;
		}

		public static TempNuGetPack Create(string packName, string packVersion, string targetFramework, IEnumerable<string> managed, IEnumerable<string> analyzers)
		{
			string root = Path.Combine(Path.GetTempPath(), "lscache-nuget-rt-" + Guid.NewGuid().ToString("N"));
			string nugetRoot = Path.Combine(root, "nuget");
			string sdkPath = Path.Combine(root, "dotnet", "sdk", "10.0.100");
			Directory.CreateDirectory(sdkPath);
			File.WriteAllText(
				Path.Combine(sdkPath, "Microsoft.NETCoreSdk.BundledVersions.props"),
				$"""
				<Project>
				  <ItemGroup>
				    <KnownFrameworkReference Include="Microsoft.NETCore.App" TargetFramework="{targetFramework}" TargetingPackName="{packName}" TargetingPackVersion="{packVersion}" />
				  </ItemGroup>
				</Project>
				""");

			string packDir = Path.Combine(nugetRoot, packName.ToLowerInvariant(), packVersion);
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

			return new TempNuGetPack(root, nugetRoot, sdkPath);
		}

		public void Dispose()
		{
			try { Directory.Delete(this.Root, recursive: true); } catch { }
		}
	}
}
