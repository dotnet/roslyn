// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Build.Framework;
using Moq;
using Xunit;

namespace Microsoft.NET.ProjectData.Tasks.Tests;

public class ProjectDataWriterTests
{
	private static string Build(
		string? projectPath = null,
		bool writeHeader = true,
		bool isPrimary = false,
		bool lastDtbSucceeded = false,
		ITaskItem[]? sliceDimensions = null,
		ITaskItem[]? properties = null,
		string[]? commandLineArguments = null,
		ITaskItem[]? sourceFiles = null,
		ITaskItem[]? metadataReferences = null,
		ITaskItem[]? analyzerReferences = null,
		string[]? analyzerConfigFiles = null,
		string[]? additionalFiles = null,
		ITaskItem[]? embeddedResources = null,
		ITaskItem[]? projectReferences = null,
		string[]? capabilities = null,
		ITaskItem[]? sdkKnownAnalyzerPacks = null,
		ITaskItem[]? sdkAnalyzerConfigPolicy = null,
		Action<ProjectDataDuplicateItemDiagnostic>? duplicateItemReporter = null)
		=> ProjectDataWriter.BuildContent(
			projectPath ?? Path.Combine(Path.GetTempPath(), "projectdata-writer-tests", "App.csproj"), writeHeader, isPrimary, lastDtbSucceeded,
			sliceDimensions, properties, commandLineArguments,
			sourceFiles, metadataReferences, analyzerReferences,
			analyzerConfigFiles, additionalFiles, embeddedResources, projectReferences, capabilities, sdkKnownAnalyzerPacks, sdkAnalyzerConfigPolicy, duplicateItemReporter);

	private static ITaskItem MakeItem(
		string identity,
		string? value = null,
		string? nuGetPackageId = null,
		string? nuGetPackageVersion = null,
		string? frameworkReferenceName = null,
		string? referenceOutputAssembly = null)
	{
		var mock = new Mock<ITaskItem>();
		mock.Setup(i => i.ItemSpec).Returns(identity);
		mock.Setup(i => i.GetMetadata("Value")).Returns(value ?? string.Empty);
		mock.Setup(i => i.GetMetadata("NuGetPackageId")).Returns(nuGetPackageId ?? string.Empty);
		mock.Setup(i => i.GetMetadata("NuGetPackageVersion")).Returns(nuGetPackageVersion ?? string.Empty);
		mock.Setup(i => i.GetMetadata("FrameworkReferenceName")).Returns(frameworkReferenceName ?? string.Empty);
		mock.Setup(i => i.GetMetadata("Aliases")).Returns(string.Empty);
		mock.Setup(i => i.GetMetadata("EmbedInteropTypes")).Returns(string.Empty);
		mock.Setup(i => i.GetMetadata("Link")).Returns(string.Empty);
		mock.Setup(i => i.GetMetadata("ReferenceOutputAssembly")).Returns(referenceOutputAssembly ?? string.Empty);
		return mock.Object;
	}

	private static ITaskItem MakeSdkKnownAnalyzerPack(string packageId, string targetFramework, string packageVersion)
	{
		var mock = new Mock<ITaskItem>();
		mock.Setup(i => i.ItemSpec).Returns(packageId);
		mock.Setup(i => i.GetMetadata("PackageId")).Returns(packageId);
		mock.Setup(i => i.GetMetadata("PackageVersion")).Returns(packageVersion);
		mock.Setup(i => i.GetMetadata("TargetFramework")).Returns(targetFramework);
		return mock.Object;
	}

	private static ITaskItem MakeSdkAnalyzerConfigPolicy(params (string Name, string Value)[] metadata)
	{
		Dictionary<string, string> values = metadata.ToDictionary(static item => item.Name, static item => item.Value, StringComparer.OrdinalIgnoreCase);
		Mock<ITaskItem> mock = new Mock<ITaskItem>();
		mock.Setup(i => i.ItemSpec).Returns("Microsoft.NET.Sdk");
		mock.Setup(i => i.GetMetadata(It.IsAny<string>())).Returns((string name) => values.TryGetValue(name, out string? value) ? value : string.Empty);
		return mock.Object;
	}

	private static ITaskItem MakeDefaultSdkAnalyzerConfigPolicy(string analysisLevel = "latest", string effectiveAnalysisLevel = "10.0")
		=> MakeSdkAnalyzerConfigPolicy(
			("Language", "C#"),
			("EnableNETAnalyzers", "true"),
			("EnforceCodeStyleInBuild", "true"),
			("AnalysisLevel", analysisLevel),
			("AnalysisLevelStyle", analysisLevel),
			("EffectiveAnalysisLevel", effectiveAnalysisLevel),
			("EffectiveAnalysisLevelStyle", effectiveAnalysisLevel),
			("MicrosoftCodeAnalysisNetAnalyzersRulesVersion", effectiveAnalysisLevel.Split('.')[0]));

	[Fact]
	public void BuildSdkAnalyzerConfigPolicy_OmitsNetAnalyzersLine_WhenEnableNETAnalyzersFalse()
	{
		// netstandard2.0 default: EnableNETAnalyzers=false, EnforceCodeStyleInBuild=false.
		// SDK does not add NetAnalyzer DLLs to @(Analyzer), so we must not emit the
		// `Microsoft.NET.Sdk/analyzers|...` policy line either (orphan policy entry).
		ITaskItem item = MakeSdkAnalyzerConfigPolicy(
			("Language", "C#"),
			("EnableNETAnalyzers", "false"),
			("EnforceCodeStyleInBuild", "false"),
			("AnalysisLevel", "latest"),
			("EffectiveAnalysisLevel", "4.0"));

		SortedSet<string> policies = ProjectDataWriter.BuildSdkAnalyzerConfigPolicy([item], new ProjectDataWriter.TargetFramework("netstandard2.0", ".NETStandard", "v2.0"));

		Assert.Empty(policies);
	}

	[Fact]
	public void BuildSdkAnalyzerConfigPolicy_EmitsNetAnalyzersLineOnly_WhenOnlyEnableNETAnalyzersTrue()
	{
		// netcoreapp default with codestyle suppressed: only NetAnalyzers policy expected.
		ITaskItem item = MakeSdkAnalyzerConfigPolicy(
			("Language", "C#"),
			("EnableNETAnalyzers", "true"),
			("EnforceCodeStyleInBuild", "false"),
			("AnalysisLevel", "latest"),
			("EffectiveAnalysisLevel", "10.0"));

		SortedSet<string> policies = ProjectDataWriter.BuildSdkAnalyzerConfigPolicy([item], new ProjectDataWriter.TargetFramework("net10.0", ".NETCoreApp", "v10.0"));

		Assert.Single(policies);
		Assert.Contains(policies, p => p.StartsWith("Microsoft.NET.Sdk/analyzers", StringComparison.Ordinal));
		Assert.DoesNotContain(policies, p => p.StartsWith("Microsoft.NET.Sdk/codestyle/", StringComparison.Ordinal));
	}

	[Fact]
	public void BuildSdkAnalyzerConfigPolicy_EmitsCodeStyleLineOnly_WhenOnlyEnforceCodeStyleInBuildTrue()
	{
		// Repo case (e.g. vs-validation test asset on netstandard2.0): codestyle DLLs included
		// via Directory.Build.props setting EnforceCodeStyleInBuild=true, but NetAnalyzers stay
		// off because EffectiveAnalysisLevel<5.0 leaves EnableNETAnalyzers=false.
		ITaskItem item = MakeSdkAnalyzerConfigPolicy(
			("Language", "C#"),
			("EnableNETAnalyzers", "false"),
			("EnforceCodeStyleInBuild", "true"),
			("AnalysisLevel", "latest"),
			("AnalysisLevelStyle", "latest"),
			("EffectiveAnalysisLevel", "4.0"),
			("EffectiveAnalysisLevelStyle", "4.0"));

		SortedSet<string> policies = ProjectDataWriter.BuildSdkAnalyzerConfigPolicy([item], new ProjectDataWriter.TargetFramework("netstandard2.0", ".NETStandard", "v2.0"));

		Assert.Single(policies);
		Assert.DoesNotContain(policies, p => p.StartsWith("Microsoft.NET.Sdk/analyzers", StringComparison.Ordinal));
		Assert.Contains(policies, p => p.StartsWith("Microsoft.NET.Sdk/codestyle/cs", StringComparison.Ordinal));
	}

	[Fact]
	public void BuildSdkAnalyzerConfigPolicy_EmitsBothLines_WhenBothPropertiesTrue()
	{
		// Modern netcoreapp default: both NetAnalyzers and CodeStyle policies.
		SortedSet<string> policies = ProjectDataWriter.BuildSdkAnalyzerConfigPolicy(
			[MakeDefaultSdkAnalyzerConfigPolicy()], new ProjectDataWriter.TargetFramework("net10.0", ".NETCoreApp", "v10.0"));

		Assert.Equal(2, policies.Count);
		Assert.Contains(policies, p => p.StartsWith("Microsoft.NET.Sdk/analyzers", StringComparison.Ordinal));
		Assert.Contains(policies, p => p.StartsWith("Microsoft.NET.Sdk/codestyle/cs", StringComparison.Ordinal));
	}

	[Fact]
	public void WriteHeader_EmitsVersionAndBanner()
	{
		string content = Build();

		Assert.StartsWith("version=2.2", content);
		Assert.Contains("aka.ms/lscache", content);
		Assert.Contains("dotnet.projectsystem.cacheInProjectFolder", content);
	}

	[Fact]
	public void WriteHeader_False_NoVersionLine()
	{
		string content = Build(writeHeader: false);

		Assert.DoesNotContain("version=2", content);
		Assert.StartsWith("[project]", content.TrimStart('\r', '\n'));
	}

	[Fact]
	public void WriteProjectSection_EmitsProjectPrimaryAndDtbFlags()
	{
		string content = Build(isPrimary: true, lastDtbSucceeded: true);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("[project]\nproject=App.csproj\nlanguage=C#\n", normalized);
		Assert.Contains("\nprimary\n", normalized);
		Assert.Contains("\nlastDtbSucceeded\n", normalized);
	}

	[Fact]
	public void WriteProjectSection_OmitsDtbFlag_WhenFalse()
	{
		string content = Build(lastDtbSucceeded: false);

		string normalized = content.Replace("\r\n", "\n");
		Assert.DoesNotContain("\nprimary\n", normalized);
		Assert.DoesNotContain("\nlastDtbSucceeded\n", normalized);
	}

	[Fact]
	public void ProjectReferences_EmitsReferenceOutputAssemblyFalseMetadata()
	{
		string projectPath = Path.Combine(Path.GetTempPath(), "projectdata-writer-tests", "App.csproj");
		string referencePath = Path.Combine(Path.GetDirectoryName(projectPath)!, "BuildOnly", "BuildOnly.csproj");
		string content = Build(
			projectPath,
			projectReferences:
			[
				MakeItem(referencePath, referenceOutputAssembly: "false"),
				MakeItem(Path.Combine(Path.GetDirectoryName(projectPath)!, "Library", "Library.csproj")),
			]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains(
			"[projectReferences]\nBuildOnly/BuildOnly.csproj\n @ReferenceOutputAssembly=false\nLibrary/Library.csproj\n",
			normalized);
		Assert.DoesNotContain("@ReferenceOutputAssembly=true", normalized);
	}

	[Fact]
	public void WritePropertiesSection_SortedOrdinalIgnoreCase()
	{
		ITaskItem[] props = [
			MakeItem("ZProperty", "z"),
			MakeItem("aProperty", "a"),
			MakeItem("MProperty", "m"),
		];
		string content = Build(properties: props);

		int aIdx = content.IndexOf("aProperty=", StringComparison.OrdinalIgnoreCase);
		int mIdx = content.IndexOf("MProperty=", StringComparison.OrdinalIgnoreCase);
		int zIdx = content.IndexOf("ZProperty=", StringComparison.OrdinalIgnoreCase);
		Assert.True(aIdx < mIdx && mIdx < zIdx, "Properties should be sorted OrdinalIgnoreCase");
	}

	[Fact]
	public void WritePropertiesSection_EmptySkipped()
	{
		string content = Build(properties: []);

		Assert.DoesNotContain("[properties]", content);
	}

	[Fact]
	public void WritePropertiesSection_SkipsEmptyAndWhitespaceValues()
	{
		ITaskItem[] props = [
			MakeItem("HasValue", "real"),
			MakeItem("EmptyValue", ""),
			MakeItem("WhitespaceValue", "   "),
		];
		string content = Build(properties: props);

		Assert.Contains("HasValue=real", content);
		Assert.DoesNotContain("EmptyValue=", content);
		Assert.DoesNotContain("WhitespaceValue=", content);
	}

	[Fact]
	public void WritePropertiesSection_SkipsUndefinedSentinel()
	{
		ITaskItem[] props = [
			MakeItem("AssemblyName", "Foo"),
			MakeItem("SolutionPath", "*Undefined*"),
		];
		string content = Build(properties: props);

		Assert.Contains("AssemblyName=Foo", content);
		Assert.DoesNotContain("SolutionPath=", content);
	}

	[Fact]
	public void WritePropertiesSection_ExcludesSolutionPath_EvenWhenValueIsReal()
	{
		ITaskItem[] props = [
			MakeItem("AssemblyName", "Foo"),
			MakeItem("SolutionPath", @"C:\Users\dev\MySolution.sln"),
		];
		string content = Build(properties: props);

		Assert.Contains("AssemblyName=Foo", content);
		Assert.DoesNotContain("SolutionPath=", content);
	}

	[Fact]
	public void WritePropertiesSection_HeaderOmittedWhenAllValuesSkipped()
	{
		ITaskItem[] props = [
			MakeItem("EmptyA", ""),
			MakeItem("EmptyB", "   "),
			MakeItem("Sentinel", "*Undefined*"),
		];
		string content = Build(properties: props);

		Assert.DoesNotContain("[properties]", content);
	}

	[Fact]
	public void WriteCommandLineArgSection_FiltersFileArgs()
	{
		string[] args = [
			"/nologo",
			"/langversion:preview",
			"/reference:C:\\ref\\System.dll",   // excluded
            "/analyzer:C:\\Analyzers\\Foo.dll",  // excluded
            "C:\\project\\Program.cs",           // bare path, excluded
            "/Users/dev/project/Generated.cs",   // Unix absolute source path, excluded
            "<PATH>GeneratedFromRsp.cs",          // already-portable source path, excluded
            "/doc:obj/Debug/App.xml",             // output path, preserved
            "/out:obj/Debug/App.dll",             // output path, preserved
            "/refout:obj/Debug/refint/App.dll",   // output path, preserved
            "/pdb:obj/Debug/App.pdb",             // output path, preserved
        ];
		string content = Build(commandLineArguments: args);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("[commandLineArguments]", normalized);
		Assert.Contains("/nologo", normalized);
		Assert.Contains("/langversion:preview", normalized);
		Assert.DoesNotContain("/reference:", normalized);
		Assert.DoesNotContain("/analyzer:", normalized);
		Assert.DoesNotContain("Program.cs\n", normalized.Substring(normalized.IndexOf("[commandLineArguments]")));
		Assert.DoesNotContain("Generated.cs\n", normalized.Substring(normalized.IndexOf("[commandLineArguments]")));
		Assert.DoesNotContain("GeneratedFromRsp.cs\n", normalized.Substring(normalized.IndexOf("[commandLineArguments]")));
		Assert.Contains("/doc:obj/Debug/App.xml", normalized);
		Assert.Contains("/out:obj/Debug/App.dll", normalized);
		Assert.Contains("/refout:obj/Debug/refint/App.dll", normalized);
		Assert.Contains("/pdb:obj/Debug/App.pdb", normalized);
	}

	[Fact]
	public void WriteCommandLineArgSection_FiltersPlatformArgs()
	{
		string content = Build(commandLineArguments: ["/nologo", "/platform:AnyCPU", "-platform:x86", "/langversion:preview"]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("/nologo", normalized);
		Assert.Contains("/langversion:preview", normalized);
		Assert.DoesNotContain("/platform:", normalized);
		Assert.DoesNotContain("-platform:", normalized);
	}

	[Fact]
	public void WriteCommandLineArgSection_OrderPreserved()
	{
		string[] args = ["/langversion:preview", "/nologo", "/nullable+"];
		string content = Build(commandLineArguments: args);

		int lIdx = content.IndexOf("/langversion");
		int nIdx = content.IndexOf("/nologo");
		int qIdx = content.IndexOf("/nullable");
		Assert.True(lIdx < nIdx && nIdx < qIdx, "Argument order should be preserved");
	}

	[Fact]
	public void WriteCommandLineArgSection_ExcludesMachineSpecificArgs()
	{
		string[] args = [
			"/nologo",
			"/preferreduilang:en",        // machine-specific, excluded
            "/langversion:preview",
			"-preferreduilang:de",         // dash-prefix variant, excluded
            "/nullable+",
		];
		string content = Build(commandLineArguments: args);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("/nologo", normalized);
		Assert.Contains("/langversion:preview", normalized);
		Assert.Contains("/nullable+", normalized);
		Assert.DoesNotContain("preferreduilang", normalized);
	}

	[Fact]
	public void WriteCommandLineArgSection_NormalizesNetCoreAppNoWarn8002()
	{
		string content = Build(
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETCoreApp")],
			commandLineArguments: ["/nologo", "/nowarn:1701,1702"]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("/nowarn:1701,1702,8002", normalized);
	}

	[Fact]
	public void WriteCommandLineArgSection_DoesNotDuplicateNoWarn8002()
	{
		string content = Build(
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETCoreApp")],
			commandLineArguments: ["/nowarn:1701,8002,1702"]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("/nowarn:1701,8002,1702", normalized);
		Assert.DoesNotContain("/nowarn:1701,8002,1702,8002", normalized);
	}

	[Fact]
	public void WriteCommandLineArgSection_DoesNotNormalizeNonNetCoreAppNoWarn8002()
	{
		string content = Build(
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETStandard")],
			commandLineArguments: ["/nowarn:1701,1702"]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("/nowarn:1701,1702", normalized);
		Assert.DoesNotContain("8002", normalized);
	}

	[Theory]
	[InlineData("/platform:AnyCPU")]
	[InlineData("/platform:x86")]
	[InlineData("/platform:x64")]
	[InlineData("/platform:anycpu32bitpreferred")]
	[InlineData("-platform:arm64")]
	public void WriteCommandLineArgSection_SkipsNetFrameworkPlatform(string platformArgument)
	{
		string content = Build(
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETFramework")],
			commandLineArguments: ["/nologo", platformArgument]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("/nologo", normalized);
		Assert.DoesNotContain(platformArgument, normalized);
	}

	[Fact]
	public void WriteCommandLineArgSection_SkipsNetCoreAppPlatform()
	{
		string content = Build(
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETCoreApp")],
			commandLineArguments: ["/platform:x64"]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.DoesNotContain("/platform:x64", normalized);
	}

	[Fact]
	public void EmitCompressed_SharedPrefix_CollapsesSingleChildChain()
	{
		var paths = new List<string>
		{
			"<NUGET>/foo/1.0/lib/net8.0/a.dll",
			"<NUGET>/foo/1.0/lib/net8.0/b.dll",
		};
		var sb = new StringBuilder();
		ProjectDataWriter.EmitCompressed(sb, paths, 0);
		string result = sb.ToString().Replace("\r\n", "\n");

		// Single-child directory chain collapses onto one header line; the two
		// sibling files nest under it.
		string expected =
			"<NUGET>/foo/1.0/lib/net8.0/\n" +
			" a.dll\n" +
			" b.dll\n";
		Assert.Equal(expected, result);
	}

	[Fact]
	public void EmitCompressed_NoSharedPrefix_EmitsFlatCollapsedSingletonsInOrder()
	{
		var paths = new List<string> { "<NUGET>/a.dll", "<DOTNET>/b.dll" };
		var sb = new StringBuilder();
		ProjectDataWriter.EmitCompressed(sb, paths, 0);
		string result = sb.ToString().Replace("\r\n", "\n");

		// A path that is a single linear directory chain ending in one file
		// collapses back to one file line.
		string expected =
			"<DOTNET>/b.dll\n" +
			"<NUGET>/a.dll\n";
		Assert.Equal(expected, result);
	}

	[Fact]
	public void EmitCompressed_SiblingFileAndSubdirectory_EmitsDirectoriesFirstThenFiles()
	{
		var paths = new List<string>
		{
			"Contracts/CultureInfoFormatter.cs",
			"Contracts/DataModel/DataModelReadiness.cs",
			"Contracts/DataModel/Project.cs",
			"Contracts/EnvironmentMutationResult.cs",
		};
		paths.Sort(StringComparer.OrdinalIgnoreCase);
		var sb = new StringBuilder();
		ProjectDataWriter.EmitCompressed(sb, paths, 0);
		string result = sb.ToString().Replace("\r\n", "\n");

		// Single Contracts/ block; directories first then files, with each group sorted.
		string expected =
			"Contracts/\n" +
			" DataModel/\n" +
			"  DataModelReadiness.cs\n" +
			"  Project.cs\n" +
			" CultureInfoFormatter.cs\n" +
			" EnvironmentMutationResult.cs\n";
		Assert.Equal(expected, result);
		Assert.Equal(1, CountOccurrences(result, "Contracts/\n"));
	}

	[Fact]
	public void EmitCompressed_SingleFileDeepPath_CollapsesToFileLine()
	{
		var paths = new List<string> { "a/b/c/only.cs" };
		var sb = new StringBuilder();
		ProjectDataWriter.EmitCompressed(sb, paths, 0);
		string result = sb.ToString().Replace("\r\n", "\n");

		// Single-child directory chain ending in one file collapses to one file line.
		string expected = "a/b/c/only.cs\n";
		Assert.Equal(expected, result);
	}

	[Fact]
	public void EmitCompressed_CollapsedDirectoryEntries_SortWithDirectories()
	{
		var paths = new List<string>
		{
			"<NUGET>/system.drawing.common/10.0.5/lib/net10.0/System.Drawing.Common.dll",
			"<NUGET>/system.drawing.common/10.0.5/lib/net10.0/System.Private.Windows.Core.dll",
			"<NUGET>/google.protobuf/3.22.5/lib/net5.0/Google.Protobuf.dll",
			"<NUGET>/microsoft.dotnet.cecil/0.11.5-preview.26160.112/lib/netstandard2.0/Mono.Cecil.dll",
			"<NUGET>/microsoft.dotnet.cecil/0.11.5-preview.26160.112/lib/netstandard2.0/Mono.Cecil.Rocks.dll",
		};
		paths.Sort(StringComparer.OrdinalIgnoreCase);
		var sb = new StringBuilder();
		ProjectDataWriter.EmitCompressed(sb, paths, 0);
		string result = sb.ToString().Replace("\r\n", "\n");

		string expected =
			"<NUGET>/\n" +
			" google.protobuf/3.22.5/lib/net5.0/Google.Protobuf.dll\n" +
			" microsoft.dotnet.cecil/0.11.5-preview.26160.112/lib/netstandard2.0/\n" +
			"  Mono.Cecil.dll\n" +
			"  Mono.Cecil.Rocks.dll\n" +
			" system.drawing.common/10.0.5/lib/net10.0/\n" +
			"  System.Drawing.Common.dll\n" +
			"  System.Private.Windows.Core.dll\n";
		Assert.Equal(expected, result);
	}

	[Fact]
	public void EmitCompressed_RemovingFile_DoesNotReshapeOtherBranches()
	{
		var withExtra = new List<string>
		{
			"alpha/one.cs",
			"alpha/two.cs",
			"beta/three.cs",
		};
		var withoutExtra = new List<string>
		{
			"alpha/one.cs",
			"beta/three.cs",
		};
		withExtra.Sort(StringComparer.OrdinalIgnoreCase);
		withoutExtra.Sort(StringComparer.OrdinalIgnoreCase);

		var sb1 = new StringBuilder();
		ProjectDataWriter.EmitCompressed(sb1, withExtra, 0);
		var sb2 = new StringBuilder();
		ProjectDataWriter.EmitCompressed(sb2, withoutExtra, 0);
		string r1 = sb1.ToString().Replace("\r\n", "\n");
		string r2 = sb2.ToString().Replace("\r\n", "\n");

		// beta/ has a single file in both shapes and collapses identically.
		Assert.Contains("beta/three.cs\n", r1);
		Assert.Contains("beta/three.cs\n", r2);
		// alpha/ remains grouped while it has two files, and collapses to a file
		// line when only one file remains.
		Assert.Equal(1, CountOccurrences(r1, "alpha/\n"));
		Assert.Contains("alpha/one.cs\n", r2);
	}

	[Fact]
	public void EmitCompressed_RoundTripsThroughReader()
	{
		var paths = new List<string>
		{
			"Contracts/CultureInfoFormatter.cs",
			"Contracts/DataModel/DataModelReadiness.cs",
			"Contracts/DataModel/Project.cs",
			"Contracts/EnvironmentMutationResult.cs",
			"Program.cs",
			"<NUGET>/foo/1.0/lib/net8.0/Foo.dll",
			"<NUGET>/foo/1.0/lib/net8.0/Bar.dll",
		};
		paths.Sort(StringComparer.OrdinalIgnoreCase);
		var sb = new StringBuilder();
		ProjectDataWriter.EmitCompressed(sb, paths, 0);
		string emitted = sb.ToString().Replace("\r\n", "\n");

		// Inline expansion mirroring CacheFileReader.ExpandCompressedPaths so the
		// writer-tests project does not need to depend on the cache-reader assembly.
		var stack = new Stack<(int Indent, string Prefix)>();
		var rebuilt = new List<string>();
		foreach (string raw in emitted.Split('\n', StringSplitOptions.RemoveEmptyEntries))
		{
			int indent = 0;
			while (indent < raw.Length && raw[indent] == ' ') indent++;
			string content = raw[indent..];
			while (stack.Count > 0 && stack.Peek().Indent >= indent) stack.Pop();
			string prefix = stack.Count > 0 ? stack.Peek().Prefix : "";
			if (content.Length > 0 && content[^1] == '/')
				stack.Push((indent, prefix + content));
			else
				rebuilt.Add(prefix + content);
		}
		rebuilt.Sort(StringComparer.OrdinalIgnoreCase);
		Assert.Equal(paths, rebuilt);
	}

	private static int CountOccurrences(string haystack, string needle)
	{
		int count = 0;
		int idx = 0;
		while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
		{
			count++;
			idx += needle.Length;
		}
		return count;
	}

	[Fact]
	public void BuildContent_UsesLfLineEndings()
	{
		string content = Build(commandLineArguments: ["/noconfig"]);

		Assert.Contains("\n", content);
		Assert.DoesNotContain("\r\n", content);
	}

	[Fact]
	public void AtomicWrite_TmpFileGoneAfterSuccess()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			ProjectDataWriter.AtomicWrite(outputPath, "hello");

			Assert.True(File.Exists(outputPath));
			Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
			Assert.Equal("hello", File.ReadAllText(outputPath));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWrite_PlacesTempFileInRequestedDirectory_NotNextToOutput()
	{
		string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputDir = Path.Combine(root, "src");
		string tempDir = Path.Combine(root, "obj"); // does not exist yet
		string outputPath = Path.Combine(outputDir, "test.lscache");
		try
		{
			Directory.CreateDirectory(outputDir);

			ProjectDataWriter.AtomicWrite(outputPath, "hello", tempDir);

			// The requested temp directory is created and used, so the transient .tmp side-file never
			// appears next to the (committed, watched) output file.
			Assert.True(Directory.Exists(tempDir));
			Assert.Empty(Directory.GetFiles(outputDir, "*.tmp"));
			Assert.Empty(Directory.GetFiles(tempDir, "*.tmp"));
			Assert.Equal("hello", File.ReadAllText(outputPath));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void AtomicWriteStreamed_PlacesTempFileInRequestedDirectory_NotNextToOutput()
	{
		string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputDir = Path.Combine(root, "src");
		string tempDir = Path.Combine(root, "obj"); // does not exist yet
		string outputPath = Path.Combine(outputDir, "test.lscache");
		try
		{
			Directory.CreateDirectory(outputDir);

			ProjectDataWriter.AtomicWriteStreamed(outputPath, writer => writer.Write("version=2\n"), tempDir);

			Assert.True(Directory.Exists(tempDir));
			Assert.Empty(Directory.GetFiles(outputDir, "*.tmp"));
			Assert.Empty(Directory.GetFiles(tempDir, "*.tmp"));
			Assert.Equal("version=2\n", File.ReadAllText(outputPath));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void AtomicWrite_FallsBackToOutputDirectory_WhenTempDirectoryUnusable()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			Directory.CreateDirectory(dir);

			// An unusable temp directory (invalid path) must not throw: the write degrades to the
			// output directory and still succeeds.
			ProjectDataWriter.AtomicWrite(outputPath, "hello", "bad\0dir");

			Assert.Equal("hello", File.ReadAllText(outputPath));
			Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWrite_ReplacesExistingFile()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			Directory.CreateDirectory(dir);
			File.WriteAllText(outputPath, "old");

			ProjectDataWriter.AtomicWrite(outputPath, "new");

			Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
			Assert.Equal("new", File.ReadAllText(outputPath));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWrite_SkipsRewrite_WhenContentMatches()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			ProjectDataWriter.AtomicWrite(outputPath, "same");
			DateTime lastWriteTime = File.GetLastWriteTimeUtc(outputPath);

			Thread.Sleep(1100);
			ProjectDataWriter.AtomicWrite(outputPath, "same");

			Assert.Equal(lastWriteTime, File.GetLastWriteTimeUtc(outputPath));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWriteStreamed_SkipsRewrite_WhenOnlyMinorVersionIncreases()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		const string ExistingContent = "version=2.1\n[project]\nlanguage=C#\n";
		try
		{
			Directory.CreateDirectory(dir);
			File.WriteAllText(outputPath, ExistingContent, new UTF8Encoding(false));
			File.SetLastWriteTimeUtc(outputPath, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
			DateTime lastWriteTime = File.GetLastWriteTimeUtc(outputPath);

			ProjectDataWriter.AtomicWriteStreamed(outputPath, writer =>
			{
				writer.WriteLine("version=2.2");
				writer.WriteLine("[project]");
				writer.WriteLine("language=C#");
			});

			Assert.Equal(ExistingContent, File.ReadAllText(outputPath));
			Assert.Equal(lastWriteTime, File.GetLastWriteTimeUtc(outputPath));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWriteStreamed_Rewrites_WhenNewMinorAddsData()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			Directory.CreateDirectory(dir);
			File.WriteAllText(outputPath, "version=2.1\n[properties]\nAssemblyName=Sample\n", new UTF8Encoding(false));

			ProjectDataWriter.AtomicWriteStreamed(outputPath, writer =>
			{
				writer.WriteLine("version=2.2");
				writer.WriteLine("[properties]");
				writer.WriteLine("AssemblyName=Sample");
				writer.WriteLine("IsTestProject=true");
			});

			Assert.Equal(
				"version=2.2\n[properties]\nAssemblyName=Sample\nIsTestProject=true\n",
				File.ReadAllText(outputPath));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWrite_StripsLegacyHashHeader_ThenStable()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			Directory.CreateDirectory(dir);
			// A pre-migration file still carries a leading "hash=" line.
			File.WriteAllText(outputPath, $"hash={new string('0', 64)}\nsame", new UTF8Encoding(false));

			// First write strips the legacy header even though the body is unchanged.
			ProjectDataWriter.AtomicWrite(outputPath, "same");
			Assert.Equal("same", File.ReadAllText(outputPath));

			// Second write is a no-op now that the header is gone.
			DateTime lastWriteTime = File.GetLastWriteTimeUtc(outputPath);
			Thread.Sleep(1100);
			ProjectDataWriter.AtomicWrite(outputPath, "same");
			Assert.Equal(lastWriteTime, File.GetLastWriteTimeUtc(outputPath));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWrite_NormalizesLineEndingsBeforeWriting()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			ProjectDataWriter.AtomicWrite(outputPath, "hello\r\nworld\r\n");

			Assert.Equal("hello\nworld\n", File.ReadAllText(outputPath));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWriteStreamed_UsesLfLineEndings_NoHashHeader()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			ProjectDataWriter.AtomicWriteStreamed(outputPath, writer =>
			{
				writer.WriteLine("version=2");
				writer.WriteLine("[project]");
			});

			Assert.Equal("version=2\n[project]\n", File.ReadAllText(outputPath));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Theory]
	[InlineData("a\r\nb\r\nc", "a\nb\nc")]      // CRLF
	[InlineData("a\rb\rc", "a\nb\nc")]          // lone CR
	[InlineData("a\r\r\nb", "a\n\nb")]          // CR immediately followed by CRLF
	[InlineData("\r\n\r\n", "\n\n")]            // only line endings
	[InlineData("no-endings-here", "no-endings-here")] // fast path: no CR at all
	[InlineData("", "")]                         // empty
	public void AtomicWriteStreamed_NormalizesEmbeddedLineEndings(string input, string expected)
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			ProjectDataWriter.AtomicWriteStreamed(outputPath, writer => writer.Write(input));

			Assert.Equal(expected, File.ReadAllText(outputPath));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWriteStreamed_NormalizesAndGrowsBeyondInitialBuffer()
	{
		// Render well past the pooled writer's initial capacity so the buffer-growth path is
		// exercised alongside in-place line-ending normalization.
		var sb = new StringBuilder();
		for (int i = 0; i < 5000; i++)
			sb.Append("reference/path/segment/Some.Package.Name.").Append(i).Append(".dll\r\n");
		string input = sb.ToString();
		string expected = input.Replace("\r\n", "\n");

		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			ProjectDataWriter.AtomicWriteStreamed(outputPath, writer => writer.Write(input));

			Assert.Equal(expected, File.ReadAllText(outputPath));
			Assert.True(input.Length > 4096, "input should exceed the initial pooled buffer to exercise growth");
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWriteStreamed_StripsLegacyHashHeader()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		string body = "version=2\n[project]\n";
		try
		{
			Directory.CreateDirectory(dir);
			File.WriteAllText(outputPath, $"hash={new string('0', 64)}\n{body}", new UTF8Encoding(false));

			ProjectDataWriter.AtomicWriteStreamed(outputPath, writer =>
			{
				writer.WriteLine("version=2");
				writer.WriteLine("[project]");
			});

			Assert.Equal(body, File.ReadAllText(outputPath));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void WriteSourceFileSection_EmitsLinkMetadata()
	{
		var item = new Mock<ITaskItem>();
		item.Setup(i => i.ItemSpec).Returns(@"C:\project\Shared\Feature.cs");
		item.Setup(i => i.GetMetadata("Link")).Returns(@"Linked\Feature.cs");

		string content = Build(sourceFiles: [item.Object]);
		string normalized = content.Replace("\r\n", "\n");

		Assert.Contains("[sourceFiles]", normalized);
		Assert.Contains("Shared/Feature.cs\n", normalized);
		Assert.Contains(" @link=Linked/Feature.cs\n", normalized);
	}

	[Fact]
	public void WriteSourceFileSection_OmitsLinkMetadata_WhenLinkIsEmpty()
	{
		string content = Build(sourceFiles: [MakeItem(@"C:\project\Program.cs")]);
		string normalized = content.Replace("\r\n", "\n");

		Assert.Contains("[sourceFiles]", normalized);
		Assert.Contains("Program.cs\n", normalized);
		Assert.DoesNotContain("@link=", normalized);
	}

	[Fact]
	public void WriteSourceFileSection_DedupesItemsCollapsingToSamePortableForm()
	{
		// MSBuild evaluation can produce two ``ITaskItem``s with different ``ItemSpec``s
		// that resolve to the same portable form (e.g. a wildcard ``<Compile Include="**/*.cs">``
		// plus an explicit ``<Compile Include="Program.cs">`` with custom metadata, or two
		// items differing only in casing on a case-insensitive file system). Without
		// deduplication the same path is emitted twice — bloating the cache and causing
		// the reader to materialize duplicate ``CachedSourceFile`` entries.
		string projectPath = Path.Combine(Path.GetTempPath(), "projectdata-writer-tests", "App.csproj");
		string projectDir = Path.GetDirectoryName(projectPath)!;
		ITaskItem first = MakeItem(Path.Combine(projectDir, "Program.cs"));
		ITaskItem duplicate = MakeItem(Path.Combine(projectDir, "program.cs"));
		List<ProjectDataDuplicateItemDiagnostic> diagnostics = [];

		string content = Build(projectPath: projectPath, sourceFiles: [first, duplicate], duplicateItemReporter: diagnostics.Add);
		string normalized = content.Replace("\r\n", "\n");

		int matches = CountOccurrencesInSection(normalized, "[sourceFiles]", "Program.cs");
		Assert.Equal(OperatingSystem.IsLinux() ? 2 : 1, matches);
		if (OperatingSystem.IsLinux())
		{
			Assert.Empty(diagnostics);
		}
		else
		{
			ProjectDataDuplicateItemDiagnostic diagnostic = Assert.Single(diagnostics);
			Assert.Equal(projectPath, diagnostic.ProjectFilePath);
			Assert.Equal("sourceFiles", diagnostic.Section);
			Assert.EndsWith("Program.cs", diagnostic.ItemSpec, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public void WriteMetadataRefSection_DedupesItemsCollapsingToSamePortableForm()
	{
		// Same concern as the source-file case: two ``MetadataReference`` items that
		// collapse to the same portable form should be emitted only once. While
		// ``PrepareMetadataRefs`` upstream handles most dedup, ``EmitMetadataRefSection``
		// is the wire-format boundary and must not assume its caller dedups.
		string projectDir = Path.GetDirectoryName(Path.Combine(Path.GetTempPath(), "projectdata-writer-tests", "App.csproj"))!;
		ITaskItem first = MakeItem(Path.Combine(projectDir, "ref", "Foo.dll"));
		ITaskItem duplicate = MakeItem(Path.Combine(projectDir, "ref", "foo.dll"));

		string content = Build(metadataReferences: [first, duplicate]);
		string normalized = content.Replace("\r\n", "\n");

		int matches = CountOccurrencesInSection(normalized, "[metadataReferences]", "Foo.dll");
		Assert.Equal(OperatingSystem.IsLinux() ? 2 : 1, matches);
	}

	[Fact]
	public void WriteAnalyzerReferenceSection_DedupesItemsCollapsingToSamePortableForm()
	{
		string projectDir = Path.GetDirectoryName(Path.Combine(Path.GetTempPath(), "projectdata-writer-tests", "App.csproj"))!;
		ITaskItem first = MakeItem(Path.Combine(projectDir, "analyzers", "PolyType.SourceGenerator.dll"));
		ITaskItem duplicate = MakeItem(Path.Combine(projectDir, "analyzers", "PolyType.SourceGenerator.dll"));

		string content = Build(analyzerReferences: [first, duplicate]);
		string normalized = content.Replace("\r\n", "\n");

		int matches = CountOccurrencesInSection(normalized, "[analyzerReferences]", "PolyType.SourceGenerator.dll");
		Assert.Equal(1, matches);
	}

	[Fact]
	public void WriteAnalyzerReferenceSection_ReportsDuplicateItems()
	{
		string projectPath = Path.Combine(Path.GetTempPath(), "projectdata-writer-tests", "App.csproj");
		string projectDir = Path.GetDirectoryName(projectPath)!;
		ITaskItem first = MakeItem(Path.Combine(projectDir, "analyzers", "PolyType.SourceGenerator.dll"));
		ITaskItem duplicate = MakeItem(Path.Combine(projectDir, "analyzers", "PolyType.SourceGenerator.dll"));
		List<ProjectDataDuplicateItemDiagnostic> diagnostics = [];

		Build(projectPath: projectPath, analyzerReferences: [first, duplicate], duplicateItemReporter: diagnostics.Add);

		ProjectDataDuplicateItemDiagnostic diagnostic = Assert.Single(diagnostics);
		Assert.Equal(projectPath, diagnostic.ProjectFilePath);
		Assert.Equal("analyzerReferences", diagnostic.Section);
		Assert.EndsWith("analyzers/PolyType.SourceGenerator.dll", diagnostic.ItemSpec, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void WriteAnalyzerReferenceSection_UsesPlatformPathCaseSensitivityForDedupe()
	{
		string projectPath = Path.Combine(Path.GetTempPath(), "projectdata-writer-tests", "App.csproj");
		string projectDir = Path.GetDirectoryName(projectPath)!;
		ITaskItem first = MakeItem(Path.Combine(projectDir, "analyzers", "CaseSensitive.dll"));
		ITaskItem second = MakeItem(Path.Combine(projectDir, "analyzers", "casesensitive.dll"));
		List<ProjectDataDuplicateItemDiagnostic> diagnostics = [];

		string content = Build(projectPath: projectPath, analyzerReferences: [first, second], duplicateItemReporter: diagnostics.Add);
		string normalized = content.Replace("\r\n", "\n");

		int matches = CountOccurrencesInSection(normalized, "[analyzerReferences]", "casesensitive.dll");
		if (OperatingSystem.IsLinux())
		{
			Assert.Equal(2, matches);
			Assert.Empty(diagnostics);
		}
		else
		{
			Assert.Equal(1, matches);
			Assert.Single(diagnostics);
		}
	}

	[Fact]
	public void WriteAnalyzerReferenceSection_UsesPlatformPathCaseSensitivityForCompressedDirectories()
	{
		string projectPath = Path.Combine(Path.GetTempPath(), "projectdata-writer-tests", "App.csproj");
		string projectDir = Path.GetDirectoryName(projectPath)!;
		ITaskItem first = MakeItem(Path.Combine(projectDir, "Analyzers", "CaseSensitive.dll"));
		ITaskItem second = MakeItem(Path.Combine(projectDir, "analyzers", "casesensitive.dll"));
		List<ProjectDataDuplicateItemDiagnostic> diagnostics = [];

		string content = Build(projectPath: projectPath, analyzerReferences: [first, second], duplicateItemReporter: diagnostics.Add);
		string normalized = content.Replace("\r\n", "\n");

		Assert.Contains("Analyzers/CaseSensitive.dll\n", normalized);
		if (OperatingSystem.IsLinux())
		{
			Assert.Contains("analyzers/casesensitive.dll\n", normalized);
			Assert.Empty(diagnostics);
		}
		else
		{
			Assert.DoesNotContain("analyzers/casesensitive.dll\n", normalized);
			Assert.Single(diagnostics);
		}
	}

	// Counts case-insensitive occurrences of ``needle`` between ``sectionHeader`` and
	// the next section header (or end of content).
	private static int CountOccurrencesInSection(string content, string sectionHeader, string needle)
	{
		int sectionStart = content.IndexOf(sectionHeader + "\n", StringComparison.Ordinal);
		Assert.NotEqual(-1, sectionStart);
		string sectionTail = content[(sectionStart + sectionHeader.Length + 1)..];
		int sectionEnd = sectionTail.IndexOf("\n[", StringComparison.Ordinal);
		string section = sectionEnd >= 0 ? sectionTail[..sectionEnd] : sectionTail;

		int count = 0;
		int idx = 0;
		while ((idx = section.IndexOf(needle, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
		{
			count++;
			idx += needle.Length;
		}
		return count;
	}

	[Fact]
	public void WriteMetadataRefSection_EmitsAliases()
	{
		var item = new Mock<ITaskItem>();
		item.Setup(i => i.ItemSpec).Returns(@"C:\project\ref\Interop.dll");
		item.Setup(i => i.GetMetadata("Aliases")).Returns("MyAlias");
		item.Setup(i => i.GetMetadata("EmbedInteropTypes")).Returns("false");
		item.Setup(i => i.GetMetadata("Value")).Returns(string.Empty);

		string content = Build(metadataReferences: [item.Object]);
		string normalized = content.Replace("\r\n", "\n");

		Assert.Contains("[metadataReferences]", normalized);
		Assert.Contains("@aliases=MyAlias", normalized);
	}

	[Fact]
	public void WriteMetadataRefSection_EmitsEmbedInteropTypes()
	{
		var item = new Mock<ITaskItem>();
		item.Setup(i => i.ItemSpec).Returns(@"C:\project\ref\Interop.dll");
		item.Setup(i => i.GetMetadata("Aliases")).Returns("global");
		item.Setup(i => i.GetMetadata("EmbedInteropTypes")).Returns("true");
		item.Setup(i => i.GetMetadata("Value")).Returns(string.Empty);

		string content = Build(metadataReferences: [item.Object]);
		string normalized = content.Replace("\r\n", "\n");

		Assert.Contains("@embedInteropTypes", normalized);
		Assert.DoesNotContain("@aliases=", normalized); // "global" is not emitted
	}

	#region Framework packs

	[Fact]
	public void TryExtractRefPackName_RecognizesValidPath()
	{
		Assert.Equal(
			"Microsoft.NETCore.App.Ref",
			ProjectDataWriter.TryExtractRefPackName("<DOTNET>/packs/Microsoft.NETCore.App.Ref/10.0.7/ref/net10.0/System.Runtime.dll"));
	}

	[Fact]
	public void TryExtractRefPackName_ReturnsNullForNonPackPaths()
	{
		Assert.Null(ProjectDataWriter.TryExtractRefPackName("<NUGET>/foo/1.0/lib/net10.0/Foo.dll"));
		Assert.Null(ProjectDataWriter.TryExtractRefPackName("<NUGET>/microsoft.netcore.app.ref/8.0.26/ref/net8.0/System.Runtime.dll"));
		Assert.Null(ProjectDataWriter.TryExtractRefPackName("<DOTNET>/sdk/9.0.100/Sdks/Microsoft.NET.Sdk/analyzers/x.dll"));
		Assert.Null(ProjectDataWriter.TryExtractRefPackName("<DOTNET>/packs/Microsoft.Android.Ref.36/36.0.0/ref/net10.0/Mono.Android.dll"));
		Assert.Null(ProjectDataWriter.TryExtractRefPackName("<DOTNET>/packs/Microsoft.iOS.Ref.net10.0_26.5/26.5.0/ref/net10.0/Microsoft.iOS.dll"));
		Assert.Null(ProjectDataWriter.TryExtractRefPackName("<DOTNET>/packs/Microsoft.MacCatalyst.Ref.net10.0_26.5/26.5.0/ref/net10.0/Microsoft.MacCatalyst.dll"));
		Assert.Null(ProjectDataWriter.TryExtractRefPackName("<DOTNET>/packs/Foo")); // no version segment
		Assert.Null(ProjectDataWriter.TryExtractRefPackName("<DOTNET>/packs/Foo/1.0")); // no path under version
		Assert.Null(ProjectDataWriter.TryExtractRefPackName(""));
		Assert.Null(ProjectDataWriter.TryExtractRefPackName(null));
	}

	[Fact]
	public void WriteFrameworkPacksSection_EmitsSortedDistinctNames()
	{
		var sb = new StringBuilder();
		var packs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"Microsoft.NETCore.App.Ref",
			"Microsoft.AspNetCore.App.Ref",
		};
		ProjectDataWriter.WriteFrameworkPacksSection(sb, packs);
		string content = sb.ToString().Replace("\r\n", "\n");

		Assert.Contains("[frameworkPacks]\n", content);
		int aspIdx = content.IndexOf("Microsoft.AspNetCore.App.Ref");
		int netIdx = content.IndexOf("Microsoft.NETCore.App.Ref");
		Assert.True(aspIdx > 0 && netIdx > aspIdx, "Pack names must be sorted OrdinalIgnoreCase");
	}

	[Fact]
	public void WriteFrameworkPacksSection_EmptyPacks_OmitsSection()
	{
		var sb = new StringBuilder();
		ProjectDataWriter.WriteFrameworkPacksSection(sb, new SortedSet<string>());
		Assert.Equal(string.Empty, sb.ToString());
	}

	// Builds a resolver wired to a synthetic dotnet root so paths under that root
	// get rewritten to <DOTNET>/... portable form for testing the prepare helpers.
	private static (CachePathResolver Resolver, string DotNetRoot) MakeSyntheticResolver(string projectDir)
	{
		// Use the project dir's drive root as the synthetic dotnet root: any path under it
		// that starts with "<root>\\packs\\..." will be classified as <DOTNET>/packs/...
		string dotnetRoot = Path.Combine(projectDir, "fakedotnet") + Path.DirectorySeparatorChar;
		var resolver = new CachePathResolver(
			projectDir: projectDir,
			nugetFolders: [Path.Combine(projectDir, "fakenuget") + Path.DirectorySeparatorChar],
			dotnetRoots: [dotnetRoot],
			netFxRefRoot: null);
		return (resolver, dotnetRoot);
	}

	private static (CachePathResolver Resolver, string DotNetRoot, string NuGetRoot) MakeSyntheticResolverWithRoots(string projectDir)
	{
		string dotnetRoot = Path.Combine(projectDir, "fakedotnet") + Path.DirectorySeparatorChar;
		string nugetRoot = Path.Combine(projectDir, "fakenuget") + Path.DirectorySeparatorChar;
		var resolver = new CachePathResolver(
			projectDir: projectDir,
			nugetFolders: [nugetRoot],
			dotnetRoots: [dotnetRoot],
			netFxRefRoot: null);
		return (resolver, dotnetRoot, nugetRoot);
	}

	[Theory]
	[InlineData("")]
	[InlineData(null)]
	public void ToPortable_PreservesNullOrEmptyInput(string? inputPath)
	{
		var resolver = new CachePathResolver(Path.Combine(Path.GetTempPath(), "proj"), [], [], null);

		Assert.Equal(inputPath, resolver.ToPortable(inputPath!));
	}

	[Fact]
	public void PrepareMetadataRefs_DivertsPackEntriesAndKeepsOthers()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj");
		(CachePathResolver resolver, string dotnetRoot) = MakeSyntheticResolver(projectDir);
		ITaskItem[] items =
		[
			MakeItem(Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref", "10.0.7", "ref", "net10.0", "System.Runtime.dll")),
			MakeItem(Path.Combine(dotnetRoot, "packs", "Microsoft.AspNetCore.App.Ref", "10.0.7", "ref", "net10.0", "Microsoft.AspNetCore.dll")),
			MakeItem(Path.Combine(projectDir, "bin", "App.dll")),
		];
		var packs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
		List<KeyValuePair<string, ITaskItem>> prepared = ProjectDataWriter.PrepareMetadataRefs(items, resolver, packs);

		Assert.Equal(2, packs.Count);
		Assert.Contains("Microsoft.NETCore.App.Ref", packs);
		Assert.Contains("Microsoft.AspNetCore.App.Ref", packs);
		Assert.Single(prepared); // only the non-pack ref survives
		Assert.DoesNotContain("packs/", prepared[0].Key, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void PrepareMetadataRefs_KeepsWorkloadPackEntriesExplicit()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj");
		(CachePathResolver resolver, string dotnetRoot) = MakeSyntheticResolver(projectDir);
		ITaskItem[] items =
		[
			MakeItem(Path.Combine(dotnetRoot, "packs", "Microsoft.Android.Ref.36", "36.0.0", "ref", "net10.0", "Mono.Android.dll")),
			MakeItem(Path.Combine(dotnetRoot, "packs", "Microsoft.iOS.Ref.net10.0_26.5", "26.5.0", "ref", "net10.0", "Microsoft.iOS.dll")),
			MakeItem(Path.Combine(dotnetRoot, "packs", "Microsoft.MacCatalyst.Ref.net10.0_26.5", "26.5.0", "ref", "net10.0", "Microsoft.MacCatalyst.dll")),
		];
		var packs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

		List<KeyValuePair<string, ITaskItem>> prepared = ProjectDataWriter.PrepareMetadataRefs(items, resolver, packs);

		Assert.Empty(packs);
		Assert.Equal(3, prepared.Count);
		Assert.Contains(prepared, item => item.Key.EndsWith("Mono.Android.dll", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(prepared, item => item.Key.EndsWith("Microsoft.iOS.dll", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(prepared, item => item.Key.EndsWith("Microsoft.MacCatalyst.dll", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void PrepareAnalyzerRefs_DivertsPackEntriesAndKeepsOthers()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj");
		(CachePathResolver resolver, string dotnetRoot) = MakeSyntheticResolver(projectDir);
		ITaskItem[] items =
		[
			MakeItem(Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref", "10.0.7", "analyzers", "dotnet", "cs", "x.dll")),
			MakeItem(Path.Combine(dotnetRoot, "sdk", "9.0.100", "Sdks", "Microsoft.NET.Sdk", "analyzers", "Foo.dll")),
		];
		var packs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> prepared = ProjectDataWriter.PrepareAnalyzerRefs(items, resolver, packs);

		Assert.Single(packs);
		Assert.Contains("Microsoft.NETCore.App.Ref", packs);
		Assert.Single(prepared); // only the SDK-folder analyzer survives
								 // The SDK analyzer path is rewritten via the <NETSDK> sentinel
								 // (the version segment is dropped — see CachePathResolver.RewriteSdkPath).
		Assert.StartsWith("<NETSDK>/", prepared[0]);
		Assert.DoesNotContain("9.0.100", prepared[0]);
	}

	[Fact]
	public void PrepareRefs_UnifiesSdkAndNuGetResolvedFrameworkPacks()
	{
		// Regression test: the same canonical framework pack must end up in
		// [frameworkPacks] regardless of whether MSBuild resolved it from
		// <DOTNET>/packs/ (SDK install) or <NUGET>/ (NuGet download). Otherwise
		// the cache contents would depend on which dotnet SDKs are installed
		// on the writer's machine, producing environment-dependent churn.
		string projectDir = Path.Combine(Path.GetTempPath(), "proj-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		try
		{
			(CachePathResolver resolver, string dotnetRoot, string nugetRoot) = MakeSyntheticResolverWithRoots(projectDir);

			// One pack resolved from SDK install, another (same canonical name) resolved from NuGet.
			string sdkRef = Path.Combine(dotnetRoot, "packs", "Microsoft.AspNetCore.App.Ref", "8.0.20", "ref", "net8.0", "Microsoft.AspNetCore.dll");
			string nugetRef = Path.Combine(nugetRoot, "microsoft.netcore.app.ref", "8.0.26", "ref", "net8.0", "System.Runtime.dll");

			var frameworkPacks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			List<KeyValuePair<string, ITaskItem>> prepared = ProjectDataWriter.PrepareMetadataRefs(
				[MakeItem(sdkRef), MakeItem(nugetRef, nuGetPackageId: "Microsoft.NETCore.App.Ref", nuGetPackageVersion: "8.0.26", frameworkReferenceName: "Microsoft.NETCore.App")],
				resolver,
				frameworkPacks,
				new ProjectDataWriter.TargetFramework("net8.0", null, "v8.0"));

			Assert.Equal(2, frameworkPacks.Count);
			Assert.Contains("Microsoft.AspNetCore.App.Ref", frameworkPacks);
			Assert.Contains("Microsoft.NETCore.App.Ref", frameworkPacks);
			Assert.Empty(prepared);
		}
		finally
		{
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void PrepareRefs_ClassifiesNuGetResolvedFrameworkPacksAsFrameworkPacks()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		try
		{
			(CachePathResolver resolver, _, string nugetRoot) = MakeSyntheticResolverWithRoots(projectDir);
			string packageRoot = Path.Combine(nugetRoot, "microsoft.netcore.app.ref", "8.0.26");
			string metadataRef = Path.Combine(packageRoot, "ref", "net8.0", "System.Runtime.dll");
			string analyzerRef = Path.Combine(packageRoot, "analyzers", "dotnet", "cs", "FrameworkAnalyzer.dll");
			string arbitraryNuGetRef = Path.Combine(nugetRoot, "some.package", "1.0.0", "lib", "net8.0", "Some.Package.dll");
			ITaskItem packMetadataRef = MakeItem(metadataRef, nuGetPackageId: "Microsoft.NETCore.App.Ref", nuGetPackageVersion: "8.0.26", frameworkReferenceName: "Microsoft.NETCore.App");
			ITaskItem packAnalyzerRef = MakeItem(analyzerRef, nuGetPackageId: "Microsoft.NETCore.App.Ref", nuGetPackageVersion: "8.0.26", frameworkReferenceName: "Microsoft.NETCore.App");

			var frameworkPacks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			var targetFramework = new ProjectDataWriter.TargetFramework("net8.0", null, "v8.0");
			List<KeyValuePair<string, ITaskItem>> preparedMetadata = ProjectDataWriter.PrepareMetadataRefs(
				[packMetadataRef, MakeItem(arbitraryNuGetRef)],
				resolver,
				frameworkPacks,
				targetFramework);
			List<string> preparedAnalyzers = ProjectDataWriter.PrepareAnalyzerRefs(
				[packAnalyzerRef],
				resolver,
				frameworkPacks,
				new SortedSet<string>(StringComparer.OrdinalIgnoreCase),
				null,
				targetFramework);

			Assert.Single(frameworkPacks);
			Assert.Contains("Microsoft.NETCore.App.Ref", frameworkPacks);
			Assert.Single(preparedMetadata);
			Assert.Contains("some.package", preparedMetadata[0].Key, StringComparison.OrdinalIgnoreCase);
			Assert.Empty(preparedAnalyzers);
		}
		finally
		{
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void PrepareRefs_ClassifiesNuGetResolvedFrameworkPacksAsFrameworkPacks_WhenFrameworkReferenceMetadataIsMissing()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		try
		{
			(CachePathResolver resolver, _, string nugetRoot) = MakeSyntheticResolverWithRoots(projectDir);
			string packageRoot = Path.Combine(nugetRoot, "microsoft.netcore.app.ref", "8.0.26");
			string metadataRef = Path.Combine(packageRoot, "ref", "net8.0", "System.Runtime.dll");
			string arbitraryNuGetRef = Path.Combine(nugetRoot, "some.package", "1.0.0", "lib", "net8.0", "Some.Package.dll");

			var frameworkPacks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			List<KeyValuePair<string, ITaskItem>> preparedMetadata = ProjectDataWriter.PrepareMetadataRefs(
				[MakeItem(metadataRef), MakeItem(arbitraryNuGetRef)],
				resolver,
				frameworkPacks,
				new ProjectDataWriter.TargetFramework("net8.0", null, "v8.0"));

			Assert.Single(frameworkPacks);
			Assert.Contains("Microsoft.NETCore.App.Ref", frameworkPacks);
			Assert.Single(preparedMetadata);
			Assert.Contains("some.package", preparedMetadata[0].Key, StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void PrepareRefs_NormalizesNuGetPackageIdCasingToCanonicalName()
	{
		// Regression: NuGet preserves the casing from the package's .nuspec, which has
		// historically varied across SDK versions and feeds (e.g. lowercase
		// `microsoft.netcore.app.ref` on some restore paths vs PascalCase
		// `Microsoft.NETCore.App.Ref` on others). Echoing that casing through to the
		// cache reintroduces the very environment dependence this PR is eliminating.
		// `TryExtractNuGetRefPackName` must always emit the canonical pack id regardless
		// of what `NuGetPackageId` metadata supplies.
		string projectDir = Path.Combine(Path.GetTempPath(), "proj-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		try
		{
			(CachePathResolver resolver, _, string nugetRoot) = MakeSyntheticResolverWithRoots(projectDir);
			string packageRoot = Path.Combine(nugetRoot, "microsoft.netcore.app.ref", "8.0.26");
			string metadataRef = Path.Combine(packageRoot, "ref", "net8.0", "System.Runtime.dll");

			var frameworkPacks = new SortedSet<string>(StringComparer.Ordinal);
			List<KeyValuePair<string, ITaskItem>> preparedMetadata = ProjectDataWriter.PrepareMetadataRefs(
				[
					MakeItem(
						metadataRef,
						nuGetPackageId: "microsoft.netcore.app.ref",
						nuGetPackageVersion: "8.0.26",
						frameworkReferenceName: "Microsoft.NETCore.App"),
				],
				resolver,
				frameworkPacks,
				new ProjectDataWriter.TargetFramework("net8.0", null, "v8.0"));

			// The frameworkPacks set is `StringComparer.Ordinal`, so the assertion below
			// would fail if we emitted lowercase `microsoft.netcore.app.ref` from the
			// metadata casing.
			Assert.Single(frameworkPacks);
			Assert.Contains("Microsoft.NETCore.App.Ref", frameworkPacks);
			Assert.DoesNotContain("microsoft.netcore.app.ref", frameworkPacks);
		}
		finally
		{
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void PrepareRefs_ClassifiesNuGetResolvedFrameworkPacksAsFrameworkPacks_WhenTargetFrameworkUsesCompactTfm()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		try
		{
			(CachePathResolver resolver, _, string nugetRoot) = MakeSyntheticResolverWithRoots(projectDir);
			string packageRoot = Path.Combine(nugetRoot, "microsoft.netcore.app.ref", "8.0.26");
			string metadataRef = Path.Combine(packageRoot, "ref", "net8.0", "System.Runtime.dll");

			var frameworkPacks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			List<KeyValuePair<string, ITaskItem>> preparedMetadata = ProjectDataWriter.PrepareMetadataRefs(
				[MakeItem(metadataRef)],
				resolver,
				frameworkPacks,
				new ProjectDataWriter.TargetFramework("net8", null, "v8.0"));

			Assert.Single(frameworkPacks);
			Assert.Contains("Microsoft.NETCore.App.Ref", frameworkPacks);
			Assert.Empty(preparedMetadata);
		}
		finally
		{
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void BuildContent_NuGetResolvedFrameworkPacksAppearInFrameworkPacksSectionBeforeMetadataReferences_AndPackEntriesAreFiltered()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "lscache-nuget-fpacks-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		string previousNuGet = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? string.Empty;
		try
		{
			string nugetRoot = Path.Combine(projectDir, "nuget");
			Environment.SetEnvironmentVariable("NUGET_PACKAGES", nugetRoot);
			string projectFile = Path.Combine(projectDir, "App.csproj");
			string packDll = Path.Combine(nugetRoot, "microsoft.netcore.app.ref", "8.0.26", "ref", "net8.0", "System.Runtime.dll");
			string nugetDll = Path.Combine(nugetRoot, "foo", "1.0.0", "lib", "net8.0", "Foo.dll");

			string content = ProjectDataWriter.BuildContent(
				projectFilePath: projectFile,
				writeHeader: true,
				isPrimary: false,
				lastDtbSucceeded: false,
				sliceDimensions: [MakeItem("TargetFramework", "net8.0")],
				properties: null,
				commandLineArguments: null,
				sourceFiles: null,
				metadataReferences: [MakeItem(packDll, nuGetPackageId: "Microsoft.NETCore.App.Ref", nuGetPackageVersion: "8.0.26", frameworkReferenceName: "Microsoft.NETCore.App"), MakeItem(nugetDll)],
				analyzerReferences: null,
				analyzerConfigFiles: null,
				additionalFiles: null,
				projectReferences: null,
				capabilities: null);

			int packsIdx = content.IndexOf("[frameworkPacks]");
			int metaIdx = content.IndexOf("[metadataReferences]");
			Assert.True(packsIdx > 0, "[frameworkPacks] section expected");
			Assert.True(metaIdx > packsIdx, "[frameworkPacks] must precede [metadataReferences]");
			Assert.Contains("Microsoft.NETCore.App.Ref", content);
			Assert.DoesNotContain("[nugetFrameworkPacks]", content);
			Assert.DoesNotContain("System.Runtime.dll", content);
			Assert.DoesNotContain("8.0.26/ref/net8.0", content);
			Assert.Contains("Foo.dll", content);
		}
		finally
		{
			Environment.SetEnvironmentVariable("NUGET_PACKAGES", previousNuGet);
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void PrepareMetadataRefs_CanonicalizesNetFrameworkReferenceAssembliesToNetFxRefMetadata()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		try
		{
			string nugetRoot = Path.Combine(projectDir, "fakenuget") + Path.DirectorySeparatorChar;
			string netFxRoot = Path.Combine(projectDir, "Reference Assemblies", "Microsoft", "Framework", ".NETFramework") + Path.DirectorySeparatorChar;
			var resolver = new CachePathResolver(projectDir, [nugetRoot], [], netFxRoot);
			ITaskItem developerPackRef = MakeItem(Path.Combine(netFxRoot, "v4.7.2", "mscorlib.dll"));
			ITaskItem nugetRef = MakeItem(Path.Combine(nugetRoot, "microsoft.netframework.referenceassemblies.net472", "1.0.3", "build", ".NETFramework", "v4.7.2", "System.dll"));
			ITaskItem packageRef = MakeItem(Path.Combine(nugetRoot, "some.package", "1.0.0", "lib", "net472", "Some.Package.dll"));

			var frameworkPacks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			List<KeyValuePair<string, ITaskItem>> prepared = ProjectDataWriter.PrepareMetadataRefs(
				[developerPackRef, nugetRef, packageRef],
				resolver,
				frameworkPacks,
				new ProjectDataWriter.TargetFramework("net472", ".NETFramework", "v4.7.2"));

			Assert.Empty(frameworkPacks);
			Assert.Equal(["<NETFXREF>/v4.7.2/mscorlib.dll", "<NETFXREF>/v4.7.2/System.dll", "<NUGET>/some.package/1.0.0/lib/net472/Some.Package.dll"], prepared.Select(reference => reference.Key));
		}
		finally
		{
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void BuildContent_NetFrameworkReferenceAssembliesAreCanonicalMetadataReferences()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "lscache-netfx-refs-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		try
		{
			string netFxRoot = Path.Combine(projectDir, "refs", ".NETFramework") + Path.DirectorySeparatorChar;
			string projectFile = Path.Combine(projectDir, "App.csproj");
			string mscorlib = Path.Combine(netFxRoot, "v4.7.2", "mscorlib.dll");
			string packageRef = Path.Combine(projectDir, "packages", "Some.Package.dll");

			string content = ProjectDataWriter.BuildContent(
				projectFilePath: projectFile,
				writeHeader: true,
				isPrimary: false,
				lastDtbSucceeded: false,
				sliceDimensions: [MakeItem("TargetFramework", "net472")],
				properties: [MakeItem("TargetFrameworkIdentifier", ".NETFramework"), MakeItem("TargetFrameworkVersion", "v4.7.2")],
				commandLineArguments: null,
				sourceFiles: null,
				metadataReferences: [MakeItem(mscorlib), MakeItem(packageRef)],
				analyzerReferences: null,
				analyzerConfigFiles: null,
				additionalFiles: null,
				projectReferences: null,
				capabilities: null);

			int metaIdx = content.IndexOf("[metadataReferences]");
			Assert.True(metaIdx > 0, "[metadataReferences] section expected");
			Assert.DoesNotContain("[netFrameworkReferenceAssemblies]", content);
			Assert.Contains("<NETFXREF>/v4.7.2/mscorlib.dll", content);
			Assert.DoesNotContain("refs/.NETFramework/v4.7.2/mscorlib.dll", content);
			Assert.Contains("Some.Package.dll", content);
		}
		finally
		{
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void TryValidateNetFrameworkReferences_RejectsMissingBareFrameworkReferences()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "lscache-netfx-validation-" + Guid.NewGuid().ToString("N"));
		string projectFile = Path.Combine(projectDir, "App.csproj");
		Directory.CreateDirectory(projectDir);
		try
		{
			bool valid = ProjectDataWriter.TryValidateNetFrameworkReferences(
				projectFile,
				sliceDimensions: null,
				properties: [MakeItem("TargetFrameworkIdentifier", ".NETFramework"), MakeItem("TargetFramework", "net472")],
				metadataReferences: [MakeItem("mscorlib.dll")],
				out string unsupportedReason);

			Assert.False(valid);
			Assert.Equal("MissingNetFrameworkReferenceAssemblies", unsupportedReason);
		}
		finally
		{
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void TryValidateNetFrameworkReferences_AcceptsExistingCanonicalReferences()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "lscache-netfx-validation-" + Guid.NewGuid().ToString("N"));
		string projectFile = Path.Combine(projectDir, "App.csproj");
		string reference = Path.Combine(projectDir, "refs", ".NETFramework", "v4.7.2", "mscorlib.dll");
		Directory.CreateDirectory(Path.GetDirectoryName(reference)!);
		File.WriteAllText(reference, string.Empty);
		try
		{
			bool valid = ProjectDataWriter.TryValidateNetFrameworkReferences(
				projectFile,
				sliceDimensions: null,
				properties: [MakeItem("TargetFrameworkIdentifier", ".NETFramework"), MakeItem("TargetFramework", "net472"), MakeItem("TargetFrameworkVersion", "v4.7.2")],
				metadataReferences: [MakeItem(reference)],
				out string unsupportedReason);

			Assert.True(valid);
			Assert.Equal(string.Empty, unsupportedReason);
		}
		finally
		{
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void PrepareAnalyzerRefs_DivertsSdkKnownAnalyzerPacks_WithoutRequiringSdkKnownVersionMatch()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		try
		{
			(CachePathResolver resolver, _, string nugetRoot) = MakeSyntheticResolverWithRoots(projectDir);
			string illinkAnalyzer = Path.Combine(nugetRoot, "microsoft.net.illink.tasks", "10.0.8", "analyzers", "dotnet", "cs", "ILLink.RoslynAnalyzer.dll");
			string otherAnalyzer = Path.Combine(nugetRoot, "some.analyzer", "1.0.0", "analyzers", "dotnet", "cs", "Some.Analyzer.dll");
			ITaskItem illinkItem = MakeItem(illinkAnalyzer, nuGetPackageId: "Microsoft.NET.ILLink.Tasks", nuGetPackageVersion: "10.0.8");
			ITaskItem otherItem = MakeItem(otherAnalyzer, nuGetPackageId: "Some.Analyzer", nuGetPackageVersion: "1.0.0");

			var frameworkPacks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			var sdkAnalyzerPacks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> prepared = ProjectDataWriter.PrepareAnalyzerRefs(
				[illinkItem, otherItem],
				resolver,
				frameworkPacks,
				sdkAnalyzerPacks,
				[MakeSdkKnownAnalyzerPack("Microsoft.NET.ILLink.Tasks", "net10.0", "10.0.7")],
				new ProjectDataWriter.TargetFramework("net10.0", null, "v10.0"));

			Assert.Empty(frameworkPacks);
			Assert.Single(sdkAnalyzerPacks);
			Assert.Contains("Microsoft.NET.ILLink.Tasks", sdkAnalyzerPacks);
			Assert.Single(prepared);
			Assert.Contains("some.analyzer", prepared[0], StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("microsoft.net.illink.tasks", prepared[0], StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void PrepareAnalyzerRefs_KeepsNuGetAnalyzerPackage_WhenNotSdkKnown()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		try
		{
			(CachePathResolver resolver, _, string nugetRoot) = MakeSyntheticResolverWithRoots(projectDir);
			string analyzer = Path.Combine(nugetRoot, "some.analyzer", "1.0.0", "analyzers", "dotnet", "cs", "Some.Analyzer.dll");
			ITaskItem analyzerItem = MakeItem(analyzer, nuGetPackageId: "Some.Analyzer", nuGetPackageVersion: "1.0.0");

			var frameworkPacks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			var sdkAnalyzerPacks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> prepared = ProjectDataWriter.PrepareAnalyzerRefs(
				[analyzerItem],
				resolver,
				frameworkPacks,
				sdkAnalyzerPacks,
				[MakeSdkKnownAnalyzerPack("Microsoft.NET.ILLink.Tasks", "net10.0", "10.0.7")],
				new ProjectDataWriter.TargetFramework("net10.0", null, "v10.0"));

			Assert.Empty(sdkAnalyzerPacks);
			Assert.Single(prepared);
			Assert.Contains("some.analyzer", prepared[0], StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void BuildContent_SdkAnalyzerPacksAppearsBeforeAnalyzerReferences_AndPackEntriesAreFiltered()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "lscache-sdk-apacks-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		string previousNuGet = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? string.Empty;
		try
		{
			string nugetRoot = Path.Combine(projectDir, "nuget");
			Environment.SetEnvironmentVariable("NUGET_PACKAGES", nugetRoot);
			string projectFile = Path.Combine(projectDir, "App.csproj");
			string illinkAnalyzer = Path.Combine(nugetRoot, "microsoft.net.illink.tasks", "10.0.8", "analyzers", "dotnet", "cs", "ILLink.RoslynAnalyzer.dll");
			string otherAnalyzer = Path.Combine(nugetRoot, "some.analyzer", "1.0.0", "analyzers", "dotnet", "cs", "Some.Analyzer.dll");

			string content = ProjectDataWriter.BuildContent(
				projectFilePath: projectFile,
				writeHeader: true,
				isPrimary: false,
				lastDtbSucceeded: false,
				sliceDimensions: [MakeItem("TargetFramework", "net10.0")],
				properties: [MakeItem("TargetFrameworkIdentifier", ".NETCoreApp"), MakeItem("TargetFrameworkVersion", "v10.0")],
				commandLineArguments: null,
				sourceFiles: null,
				metadataReferences: null,
				analyzerReferences:
				[
					MakeItem(illinkAnalyzer, nuGetPackageId: "Microsoft.NET.ILLink.Tasks", nuGetPackageVersion: "10.0.8"),
					MakeItem(otherAnalyzer, nuGetPackageId: "Some.Analyzer", nuGetPackageVersion: "1.0.0"),
				],
				analyzerConfigFiles: null,
				additionalFiles: null,
				projectReferences: null,
				capabilities: null,
				sdkKnownAnalyzerPacks: [MakeSdkKnownAnalyzerPack("Microsoft.NET.ILLink.Tasks", "net10.0", "10.0.7")]);

			int packsIdx = content.IndexOf("[sdkAnalyzerPacks]");
			int analyzerIdx = content.IndexOf("[analyzerReferences]");
			Assert.True(packsIdx > 0, "[sdkAnalyzerPacks] section expected");
			Assert.True(analyzerIdx > packsIdx, "[sdkAnalyzerPacks] must precede [analyzerReferences]");
			Assert.Contains("Microsoft.NET.ILLink.Tasks", content);
			Assert.DoesNotContain("ILLink.RoslynAnalyzer.dll", content);
			Assert.DoesNotContain("10.0.8/analyzers", content);
			Assert.Contains("Some.Analyzer.dll", content);
		}
		finally
		{
			Environment.SetEnvironmentVariable("NUGET_PACKAGES", previousNuGet);
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void BuildContent_SdkAnalyzerConfigPolicyAppearsBeforeAnalyzerConfigFiles()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "lscache-sdk-configs-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		string fakeDotnet = Path.Combine(projectDir, "dotnet");
		Directory.CreateDirectory(fakeDotnet);
		string previous = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty;
		try
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", fakeDotnet);
			string projectFile = Path.Combine(projectDir, "App.csproj");
			string sdkConfig = Path.Combine(fakeDotnet, "sdk", "10.0.202", "Sdks", "Microsoft.NET.Sdk", "analyzers", "build", "config", "analysislevel_10_default.globalconfig");
			string styleConfig = Path.Combine(
				fakeDotnet,
				"sdk",
				"10.0.202",
				"Sdks",
				"Microsoft.NET.Sdk",
				"codestyle",
				"cs",
				"build",
				"config",
				"analysislevelstyle_default.globalconfig");
			string projectConfig = Path.Combine(projectDir, "Directory.Build.globalconfig");

			string content = ProjectDataWriter.BuildContent(
				projectFilePath: projectFile,
				writeHeader: true,
				isPrimary: false,
				lastDtbSucceeded: false,
				sliceDimensions: [MakeItem("TargetFramework", "net10.0")],
				properties: [MakeItem("TargetFrameworkIdentifier", ".NETCoreApp"), MakeItem("TargetFrameworkVersion", "v10.0")],
				commandLineArguments: null,
				sourceFiles: null,
				metadataReferences: null,
				analyzerReferences: null,
				analyzerConfigFiles: [sdkConfig, styleConfig, projectConfig],
				additionalFiles: null,
				projectReferences: null,
				sdkAnalyzerConfigPolicy: [MakeDefaultSdkAnalyzerConfigPolicy()]);

			int sdkConfigIdx = content.IndexOf("[sdkAnalyzerConfigPolicy]");
			int configIdx = content.IndexOf("[analyzerConfigFiles]");
			Assert.True(sdkConfigIdx > 0, "[sdkAnalyzerConfigPolicy] section expected");
			Assert.True(configIdx > sdkConfigIdx, "[sdkAnalyzerConfigPolicy] must precede [analyzerConfigFiles]");
			string normalized = content.Replace("\r\n", "\n");
			Assert.Contains("Microsoft.NET.Sdk/analyzers\n", normalized);
			Assert.Contains("Microsoft.NET.Sdk/codestyle/cs\n", normalized);
			Assert.DoesNotContain("AnalysisLevel=latest", normalized);
			Assert.DoesNotContain("analysislevel_10_default.globalconfig", content);
			Assert.DoesNotContain("analysislevelstyle_default.globalconfig", content);
			Assert.Contains("Directory.Build.globalconfig", content);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", previous);
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Theory]
	[InlineData("net10.0", "10.0", "10", ".NETCoreApp", "v10.0")]
	[InlineData("net10", "10.0", "10", ".NETCoreApp", "v10.0")]
	[InlineData("net8", "8.0", "8", ".NETCoreApp", "v8.0")]
	public void BuildContent_SdkAnalyzerConfigPolicyCanonicalizesNumericDefaults(string targetFramework, string numericAnalysisLevel, string shortAnalysisLevel, string tfmIdentifier, string tfmVersion)
	{
		ITaskItem[] targetFrameworkDimension = [MakeItem("TargetFramework", targetFramework)];
		ITaskItem[] tfmProperties = [MakeItem("TargetFrameworkIdentifier", tfmIdentifier), MakeItem("TargetFrameworkVersion", tfmVersion)];

		string numeric = Build(
			sliceDimensions: targetFrameworkDimension,
			properties: tfmProperties,
			sdkAnalyzerConfigPolicy: [MakeDefaultSdkAnalyzerConfigPolicy(numericAnalysisLevel, numericAnalysisLevel)]);
		string numericShort = Build(
			sliceDimensions: targetFrameworkDimension,
			properties: tfmProperties,
			sdkAnalyzerConfigPolicy: [MakeDefaultSdkAnalyzerConfigPolicy(shortAnalysisLevel, numericAnalysisLevel)]);

		Assert.Equal(numeric, numericShort);
		string normalized = numeric.Replace("\r\n", "\n");
		Assert.Contains("[sdkAnalyzerConfigPolicy]\nMicrosoft.NET.Sdk/analyzers\nMicrosoft.NET.Sdk/codestyle/cs\n", normalized);
		Assert.DoesNotContain($"AnalysisLevel={numericAnalysisLevel}", normalized);
	}

	[Fact]
	public void BuildContent_SdkAnalyzerConfigPolicyCanonicalizesLatestWhenEffectiveLevelMatchesTargetFramework()
	{
		string content = Build(
			sliceDimensions: [MakeItem("TargetFramework", "net10.0")],
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETCoreApp"), MakeItem("TargetFrameworkVersion", "v10.0")],
			sdkAnalyzerConfigPolicy: [MakeDefaultSdkAnalyzerConfigPolicy("latest", "10.0")]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("Microsoft.NET.Sdk/analyzers\n", normalized);
		Assert.Contains("Microsoft.NET.Sdk/codestyle/cs\n", normalized);
		Assert.DoesNotContain("AnalysisLevel=latest", normalized);
	}

	[Fact]
	public void BuildContent_SdkAnalyzerConfigPolicyCanonicalizesCapitalLatestWhenEffectiveLevelMatchesTargetFramework()
	{
		// Windows SDK evaluation yields AnalysisLevel="Latest" (capital L) + EffectiveAnalysisLevel="10.0"
		// for a net10.0 project with <AnalysisLevel>Latest</AnalysisLevel> in Directory.Build.props.
		// Verify this is treated identically to lowercase "latest".
		string content = Build(
			sliceDimensions: [MakeItem("TargetFramework", "net10.0")],
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETCoreApp"), MakeItem("TargetFrameworkVersion", "v10.0")],
			sdkAnalyzerConfigPolicy: [MakeDefaultSdkAnalyzerConfigPolicy("Latest", "10.0")]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("Microsoft.NET.Sdk/analyzers\n", normalized);
		Assert.Contains("Microsoft.NET.Sdk/codestyle/cs\n", normalized);
		Assert.DoesNotContain("AnalysisLevel=Latest", normalized);
	}

	[Fact]
	public void BuildContent_SdkAnalyzerConfigPolicyCanonicalizesLatestWhenEffectiveLevelIsNewerThanTargetFramework()
	{
		// The extension-driven design-time build can evaluate <AnalysisLevel>Latest</AnalysisLevel>
		// on a net10.0 project as EffectiveAnalysisLevel="11.0" when running on a newer SDK.
		string content = Build(
			sliceDimensions: [MakeItem("TargetFramework", "net10.0")],
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETCoreApp"), MakeItem("TargetFrameworkVersion", "v10.0")],
			sdkAnalyzerConfigPolicy: [MakeDefaultSdkAnalyzerConfigPolicy("Latest", "11.0")]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("Microsoft.NET.Sdk/analyzers\n", normalized);
		Assert.Contains("Microsoft.NET.Sdk/codestyle/cs\n", normalized);
		Assert.DoesNotContain("AnalysisLevel=Latest", normalized);
	}

	[Fact]
	public void BuildContent_SdkAnalyzerConfigPolicyCanonicalizesLatestWithoutEffectiveLevel()
	{
		ITaskItem policy = MakeSdkAnalyzerConfigPolicy(
			("Language", "C#"),
			("EnableNETAnalyzers", "true"),
			("EnforceCodeStyleInBuild", "true"),
			("AnalysisLevel", "Latest"),
			("AnalysisLevelStyle", "Latest"),
			("AnalysisMode", "Default"),
			("AnalysisModeStyle", "Default"));

		string content = Build(
			sliceDimensions: [MakeItem("TargetFramework", "net10.0")],
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETCoreApp"), MakeItem("TargetFrameworkVersion", "v10.0")],
			sdkAnalyzerConfigPolicy: [policy]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("Microsoft.NET.Sdk/analyzers|AnalysisMode=Default\n", normalized);
		Assert.Contains("Microsoft.NET.Sdk/codestyle/cs|AnalysisMode=Default\n", normalized);
		Assert.DoesNotContain("AnalysisLevel=Latest", normalized);
	}

	[Fact]
	public void BuildContent_SdkAnalyzerConfigPolicyCanonicalizesLatestWithEffectiveLatest()
	{
		string content = Build(
			properties: [MakeItem("TargetFramework", "net10.0"), MakeItem("TargetFrameworkIdentifier", ".NETCoreApp"), MakeItem("TargetFrameworkVersion", "v10.0")],
			sdkAnalyzerConfigPolicy: [MakeDefaultSdkAnalyzerConfigPolicy("Latest", "Latest")]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("Microsoft.NET.Sdk/analyzers\n", normalized);
		Assert.Contains("Microsoft.NET.Sdk/codestyle/cs\n", normalized);
		Assert.DoesNotContain("AnalysisLevel=Latest", normalized);
	}

	[Fact]
	public void BuildContent_SdkAnalyzerConfigPolicyCanonicalizesNetFrameworkLatestWithoutEffectiveLevel()
	{
		ITaskItem policy = MakeSdkAnalyzerConfigPolicy(
			("Language", "C#"),
			("EnableNETAnalyzers", "true"),
			("EnforceCodeStyleInBuild", "true"),
			("AnalysisLevel", "Latest"),
			("AnalysisLevelStyle", "Latest"),
			("AnalysisMode", "Default"),
			("AnalysisModeStyle", "Default"));

		string content = Build(
			sliceDimensions: [MakeItem("TargetFramework", "net472")],
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETFramework"), MakeItem("TargetFrameworkVersion", "v4.7.2")],
			sdkAnalyzerConfigPolicy: [policy]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("Microsoft.NET.Sdk/analyzers|AnalysisMode=Default\n", normalized);
		Assert.Contains("Microsoft.NET.Sdk/codestyle/cs|AnalysisMode=Default\n", normalized);
		Assert.DoesNotContain("AnalysisLevel=Latest", normalized);
	}

	[Theory]
	[InlineData("preview", "11.0")]
	[InlineData("none", "4.0")]
	[InlineData("9.0", "9.0")]
	public void BuildContent_SdkAnalyzerConfigPolicyPreservesNonDefaultAnalysisLevel(string analysisLevel, string effectiveAnalysisLevel)
	{
		string content = Build(
			sliceDimensions: [MakeItem("TargetFramework", "net10.0")],
			sdkAnalyzerConfigPolicy: [MakeDefaultSdkAnalyzerConfigPolicy(analysisLevel, effectiveAnalysisLevel)]);

		Assert.Contains($"AnalysisLevel={analysisLevel}", content);
	}

	[Theory]
	[InlineData("net472")]
	[InlineData("net48")]
	public void BuildContent_SdkAnalyzerConfigPolicyPreservesLatestForNetFrameworkTfms(string targetFramework)
	{
		string content = Build(
			sliceDimensions: [MakeItem("TargetFramework", targetFramework)],
			sdkAnalyzerConfigPolicy: [MakeDefaultSdkAnalyzerConfigPolicy("latest", "4.8")]);

		Assert.Contains("Microsoft.NET.Sdk/analyzers|AnalysisLevel=latest", content);
		Assert.Contains("Microsoft.NET.Sdk/codestyle/cs|AnalysisLevel=latest", content);
	}

	[Fact]
	public void BuildContent_SdkAnalyzerConfigPolicyPreservesLatestSuffix()
	{
		ITaskItem policy = MakeSdkAnalyzerConfigPolicy(
			("Language", "C#"),
			("EnableNETAnalyzers", "true"),
			("EnforceCodeStyleInBuild", "true"),
			("AnalysisLevel", "latest-all"),
			("AnalysisLevelStyle", "latest-all"),
			("AnalysisLevelSuffix", "all"),
			("AnalysisLevelSuffixStyle", "all"),
			("EffectiveAnalysisLevel", "10.0"),
			("EffectiveAnalysisLevelStyle", "10.0"),
			("MicrosoftCodeAnalysisNetAnalyzersRulesVersion", "10"));

		string content = Build(
			sliceDimensions: [MakeItem("TargetFramework", "net10.0")],
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETCoreApp"), MakeItem("TargetFrameworkVersion", "v10.0")],
			sdkAnalyzerConfigPolicy: [policy]);

		string normalized = content.Replace("\r\n", "\n");
		Assert.Contains("Microsoft.NET.Sdk/analyzers|AnalysisLevelSuffix=all", normalized);
		Assert.Contains("Microsoft.NET.Sdk/codestyle/cs|AnalysisLevelSuffix=all", normalized);
		Assert.DoesNotContain("AnalysisLevel=latest-all", normalized);
	}

	[Fact]
	public void BuildContent_SdkAnalyzerConfigPolicyPreservesStyleLevelWhenItDiffersFromCoreSpelling()
	{
		ITaskItem policy = MakeSdkAnalyzerConfigPolicy(
			("Language", "C#"),
			("EnableNETAnalyzers", "true"),
			("EnforceCodeStyleInBuild", "true"),
			("AnalysisLevel", "latest"),
			("AnalysisLevelStyle", "10.0"),
			("EffectiveAnalysisLevel", "10.0"),
			("EffectiveAnalysisLevelStyle", "10.0"),
			("MicrosoftCodeAnalysisNetAnalyzersRulesVersion", "10"));

		string content = Build(
			sliceDimensions: [MakeItem("TargetFramework", "net10.0")],
			properties: [MakeItem("TargetFrameworkIdentifier", ".NETCoreApp"), MakeItem("TargetFrameworkVersion", "v10.0")],
			sdkAnalyzerConfigPolicy: [policy]);

		Assert.Contains("Microsoft.NET.Sdk/codestyle/cs|AnalysisLevelStyle=10.0", content);
		Assert.DoesNotContain("AnalysisLevel=latest", content);
	}

	[Theory]
	[InlineData("Microsoft.NET.Sdk/analyzers")]
	[InlineData("Microsoft.NET.Sdk/codestyle/cs")]
	[InlineData("Microsoft.NET.Sdk/analyzers|AnalysisMode=Default")]
	[InlineData("Microsoft.NET.Sdk/codestyle/cs|AnalysisLevelStyle=10.0")]
	[InlineData("Microsoft.NET.Sdk/analyzers|AnalysisLevelSuffix=all")]
	[InlineData("not-an-sdk-policy-line")]
	public void CanonicalizeSdkAnalyzerConfigPolicyLine_IsNoOpWhenTargetFrameworkIsNull(string alreadyCanonicalLine)
	{
		// ``ProjectDataMerger.ParseSlice`` invokes ``CanonicalizeSdkAnalyzerConfigPolicyLine`` while
		// parsing the shared block of a merged ``.lscache`` — at that point ``SliceDimensions``
		// is empty and ``GetTargetFramework()`` returns ``null``. Since lines in the shared
		// block already agreed across every per-TFM slice (otherwise they would not have been
		// hoisted), the canonicalizer must leave already-canonical input unchanged when the
		// TFM context is unavailable. Pin that contract so a future refactor of
		// ``CanonicalizeAnalysisLevel``'s null-TFM branch cannot silently mutate shared-block
		// data on round-trip.
		string result = ProjectDataWriter.CanonicalizeSdkAnalyzerConfigPolicyLine(alreadyCanonicalLine, targetFrameworkIdentifier: null, targetFrameworkVersion: null);
		Assert.Equal(alreadyCanonicalLine, result);
	}

	[Fact]
	public void BuildContent_SdkAnalyzerConfigPolicyIsStableWhenSdkAddsCodeStyleConfig()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "lscache-sdk-config-policy-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		string fakeDotnet = Path.Combine(projectDir, "dotnet");
		Directory.CreateDirectory(fakeDotnet);
		string previous = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty;
		try
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", fakeDotnet);
			string projectFile = Path.Combine(projectDir, "App.csproj");
			string sdkConfig = Path.Combine(fakeDotnet, "sdk", "10.0.202", "Sdks", "Microsoft.NET.Sdk", "analyzers", "build", "config", "analysislevel_10_default.globalconfig");
			string styleConfig = Path.Combine(
				fakeDotnet,
				"sdk",
				"10.0.202",
				"Sdks",
				"Microsoft.NET.Sdk",
				"codestyle",
				"cs",
				"build",
				"config",
				"analysislevelstyle_default.globalconfig");
			string projectConfig = Path.Combine(projectDir, "Directory.Build.globalconfig");
			ITaskItem policy = MakeDefaultSdkAnalyzerConfigPolicy();

			string withoutCodeStyleConfig = ProjectDataWriter.BuildContent(
				projectFilePath: projectFile,
				writeHeader: true,
				isPrimary: false,
				lastDtbSucceeded: false,
				sliceDimensions: [MakeItem("TargetFramework", "net10.0")],
				properties: null,
				commandLineArguments: null,
				sourceFiles: null,
				metadataReferences: null,
				analyzerReferences: null,
				analyzerConfigFiles: [sdkConfig, projectConfig],
				additionalFiles: null,
				projectReferences: null,
				sdkAnalyzerConfigPolicy: [policy]);

			string withCodeStyleConfig = ProjectDataWriter.BuildContent(
				projectFilePath: projectFile,
				writeHeader: true,
				isPrimary: false,
				lastDtbSucceeded: false,
				sliceDimensions: [MakeItem("TargetFramework", "net10.0")],
				properties: null,
				commandLineArguments: null,
				sourceFiles: null,
				metadataReferences: null,
				analyzerReferences: null,
				analyzerConfigFiles: [sdkConfig, styleConfig, projectConfig],
				additionalFiles: null,
				projectReferences: null,
				sdkAnalyzerConfigPolicy: [policy]);

			Assert.Equal(withoutCodeStyleConfig, withCodeStyleConfig);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", previous);
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void PrepareRefs_SharedPackBetweenMetadataAndAnalyzersIsDeduplicated()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj");
		(CachePathResolver resolver, string dotnetRoot) = MakeSyntheticResolver(projectDir);
		ITaskItem[] meta = [MakeItem(Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref", "10.0.7", "ref", "net10.0", "System.Runtime.dll"))];
		ITaskItem[] analyzers = [MakeItem(Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref", "10.0.7", "analyzers", "dotnet", "cs", "x.dll"))];

		var packs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
		ProjectDataWriter.PrepareMetadataRefs(meta, resolver, packs);
		ProjectDataWriter.PrepareAnalyzerRefs(analyzers, resolver, packs);

		Assert.Single(packs);
	}

	[Fact]
	public void BuildContent_FrameworkPacksAppearsBeforeMetadataReferences_AndPackEntriesAreFiltered()
	{
		// Use a custom DOTNET_ROOT so the live writer's resolver picks up our fake pack paths.
		string projectDir = Path.Combine(Path.GetTempPath(), "lscache-fpacks-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(projectDir);
		string fakeDotnet = Path.Combine(projectDir, "dotnet");
		Directory.CreateDirectory(fakeDotnet);
		string previous = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty;
		try
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", fakeDotnet);
			string projectFile = Path.Combine(projectDir, "App.csproj");
			string packDll = Path.Combine(fakeDotnet, "packs", "Microsoft.NETCore.App.Ref", "10.0.7", "ref", "net10.0", "System.Runtime.dll");
			string nugetDll = @"C:\nuget\foo\1.0\lib\net10.0\Foo.dll";

			string content = ProjectDataWriter.BuildContent(
				projectFilePath: projectFile,
				writeHeader: true,
				isPrimary: false,
				lastDtbSucceeded: false,
				sliceDimensions: null,
				properties: null,
				commandLineArguments: null,
				sourceFiles: null,
				metadataReferences: [MakeItem(packDll), MakeItem(nugetDll)],
				analyzerReferences: null,
				analyzerConfigFiles: null,
				additionalFiles: null,
				projectReferences: null);

			int packsIdx = content.IndexOf("[frameworkPacks]");
			int metaIdx = content.IndexOf("[metadataReferences]");
			Assert.True(packsIdx > 0, "[frameworkPacks] section expected");
			Assert.True(metaIdx > packsIdx, "[frameworkPacks] must precede [metadataReferences]");
			Assert.Contains("Microsoft.NETCore.App.Ref", content);
			Assert.DoesNotContain("System.Runtime.dll", content);
			// The non-pack ref still appears.
			Assert.Contains("Foo.dll", content);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", previous);
			try { Directory.Delete(projectDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void WriteCapabilitiesSection_ExcludesUniversalCapabilities()
	{
		string content = Build(capabilities: ["Aspire", "CSharp", "TestingPlatformServer", "OutputGroups", "SupportsHotReload"]);
		string normalized = content.Replace("\r\n", "\n");

		Assert.Contains("[capabilities]\nAspire\nSupportsHotReload\nTestingPlatformServer\n", normalized);
		Assert.DoesNotContain("\nCSharp\n", normalized);
		Assert.DoesNotContain("\nOutputGroups\n", normalized);
	}

	[Fact]
	public void WriteCapabilitiesSection_ExcludesNewDenylistEntries()
	{
		string content = Build(capabilities:
		[
			"Aspire", "AppServicePublish", "AspNetCoreInProcessHosting",
			"BuildWindowsDesktopTarget", "DeclaredSourceItems", "DotNetCoreRazorConfiguration",
			"DynamicDependentFile", "DynamicFileNesting", "GenerateDocumentationFile",
			"NetSdkOCIImageBuild", "SupportHierarchyContextSvc", "SupportsComputeRunCommand",
			"SupportsTypeScriptNuGet", "UserSourceItems", "WebNestingDefaults",
		]);
		string normalized = content.Replace("\r\n", "\n");

		Assert.Contains("[capabilities]\nAspire\n", normalized);
		Assert.DoesNotContain("\nAppServicePublish\n", normalized);
		Assert.DoesNotContain("\nAspNetCoreInProcessHosting\n", normalized);
		Assert.DoesNotContain("\nBuildWindowsDesktopTarget\n", normalized);
		Assert.DoesNotContain("\nDeclaredSourceItems\n", normalized);
		Assert.DoesNotContain("\nDotNetCoreRazorConfiguration\n", normalized);
		Assert.DoesNotContain("\nDynamicDependentFile\n", normalized);
		Assert.DoesNotContain("\nDynamicFileNesting\n", normalized);
		Assert.DoesNotContain("\nGenerateDocumentationFile\n", normalized);
		Assert.DoesNotContain("\nNetSdkOCIImageBuild\n", normalized);
		Assert.DoesNotContain("\nSupportHierarchyContextSvc\n", normalized);
		Assert.DoesNotContain("\nSupportsComputeRunCommand\n", normalized);
		Assert.DoesNotContain("\nSupportsTypeScriptNuGet\n", normalized);
		Assert.DoesNotContain("\nUserSourceItems\n", normalized);
		Assert.DoesNotContain("\nWebNestingDefaults\n", normalized);
	}

	[Fact]
	public void WriteCapabilitiesSection_NullCapabilities_OmitsSection()
	{
		string content = Build(capabilities: null);
		Assert.DoesNotContain("[capabilities]", content);
	}

	[Fact]
	public void WriteCapabilitiesSection_EmptyCapabilities_OmitsSection()
	{
		string content = Build(capabilities: []);
		Assert.DoesNotContain("[capabilities]", content);
	}

	[Fact]
	public void WriteCapabilitiesSection_SortsAlphabetically()
	{
		string content = Build(capabilities: ["Zebra", "Alpha", "Middle"]);
		string normalized = content.Replace("\r\n", "\n");

		Assert.Contains("[capabilities]\nAlpha\nMiddle\nZebra\n", normalized);
	}

	[Fact]
	public void WriteCapabilitiesSection_Deduplicates()
	{
		string content = Build(capabilities: ["Aspire", "Aspire", "TestContainer"]);
		string normalized = content.Replace("\r\n", "\n");

		Assert.Contains("[capabilities]\nAspire\nTestContainer\n", normalized);
		int count = normalized.Split("\nAspire\n").Length - 1;
		Assert.Equal(1, count);
	}

	[Fact]
	public void WriteCapabilitiesSection_CaseInsensitiveExclusion()
	{
		string content = Build(capabilities: ["csharp", "OUTPUTGROUPS", "Aspire"]);
		string normalized = content.Replace("\r\n", "\n");

		Assert.Contains("[capabilities]\nAspire\n", normalized);
		Assert.DoesNotContain("csharp", normalized, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("OUTPUTGROUPS", normalized, StringComparison.OrdinalIgnoreCase);
	}

	#endregion

	#region TryRewriteAsNuGetPp

	[Theory]
	[InlineData("obj/Debug/net8.0/NuGet/7E7D116BF0B1C551/Nullable/1.3.0/Nullable/NullableAttributes.cs",
		"<NUGETPP>/Nullable/1.3.0/Nullable/NullableAttributes.cs")]
	[InlineData("obj/Release/net6.0/NuGet/ABCDEF0123456789/SomePackage/2.0.0/File.cs",
		"<NUGETPP>/SomePackage/2.0.0/File.cs")]
	[InlineData("obj/Debug/net8.0/NuGet/abcdef0123456789/Pkg/1.0.0/Dir/File.cs",
		"<NUGETPP>/Pkg/1.0.0/Dir/File.cs")]
	public void TryRewriteAsNuGetPp_RewritesToSentinel(string input, string expected)
	{
		string? result = CachePathResolver.TryRewriteAsNuGetPp(input);
		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("src/Program.cs")]
	[InlineData("obj/Debug/net8.0/SomeFile.cs")]
	[InlineData("obj/Debug/net8.0/NuGet/SHORT/Pkg/1.0/File.cs")]  // Hash too short
	[InlineData("obj/Debug/net8.0/NuGet/ZZZZZZZZZZZZZZZZ/Pkg/1.0/File.cs")]  // Non-hex chars
	[InlineData("obj/Debug/net8.0/NuGet/7E7D116BF0B1C551")]  // No trailing path after hash
	public void TryRewriteAsNuGetPp_ReturnsNullForNonMatchingPaths(string input)
	{
		string? result = CachePathResolver.TryRewriteAsNuGetPp(input);
		Assert.Null(result);
	}

	#endregion
}
