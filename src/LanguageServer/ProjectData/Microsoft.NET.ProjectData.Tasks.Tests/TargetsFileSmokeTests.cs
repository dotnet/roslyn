// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Microsoft.NET.ProjectData;
using Xunit;

namespace Microsoft.NET.ProjectData.Tasks.Tests;

/// <summary>
/// Smoke tests for the <c>Microsoft.NET.ProjectData.targets</c> file. Spawns
/// <c>dotnet msbuild</c> against a fixture project with the targets file wired
/// in via <c>CustomAfterMicrosoftCommonTargets</c>, mirroring how the extension
/// activates the writer at runtime.
/// </summary>
public sealed class TargetsFileSmokeTests : IDisposable
{
	private static readonly string TargetsFile = Path.Combine(
		AppContext.BaseDirectory,
		"Microsoft.NET.ProjectData.targets");
	private static readonly string ExtensionMSBuildDir = AppContext.BaseDirectory;

	/// <summary>
	/// Safety-net timeout for each spawned <c>dotnet msbuild</c> invocation. If a child MSBuild/restore
	/// process (or a lingering MSBuild worker node / build server that inherited the redirected stdout/stderr
	/// pipe) fails to exit, the read below would otherwise never reach EOF and the test would hang forever.
	/// That escalated a transient child hang into a 180-minute CI job timeout on macOS (the vstest
	/// <c>--blame-hang</c> dump collection also hangs on macOS, so it never recovered). Bounding the wait here
	/// and killing the whole process tree fails the individual test in minutes with captured output instead.
	/// Overridable via <c>PROJECTDATA_SMOKE_TEST_PROCESS_TIMEOUT_SECONDS</c> for slower agents.
	/// </summary>
	private static readonly TimeSpan ProcessTimeout = GetProcessTimeout();

	private readonly string workDir;

	public TargetsFileSmokeTests()
	{
		this.workDir = Path.Combine(Path.GetTempPath(), "projectdata-targets-smoke-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(this.workDir);
	}

	public void Dispose()
	{
		try { Directory.Delete(this.workDir, recursive: true); }
		catch { /* best-effort cleanup */ }
	}

	[Fact]
	public void TargetsFileShipsAlongsideTaskAssembly()
	{
		Assert.True(
			File.Exists(TargetsFile),
			$"Expected the targets file to be next to the test binary so MSBuild can resolve it. Looked for: {TargetsFile}");
	}

	[Theory]
	[InlineData("NuGet.Versioning.dll")]
	[InlineData("System.Buffers.dll")]
	[InlineData("System.Collections.Immutable.dll")]
	[InlineData("System.Memory.dll")]
	[InlineData("System.Numerics.Vectors.dll")]
	[InlineData("System.Runtime.CompilerServices.Unsafe.dll")]
	public void ExtensionDistShipsTaskRuntimeDependencies(string fileName)
	{
		string path = Path.Combine(ExtensionMSBuildDir, fileName);

		Assert.True(
			File.Exists(path),
			$"Expected the ProjectData MSBuild task runtime dependency to be copied to extension dist. Looked for: {path}");
	}

	[Fact]
	public async Task SingleTfmProject_EvaluatesProjectDataPathNextToCsproj()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false, writeAssetsFile: false);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs: ["/getProperty:_ProjectDataPath", "/p:EnableProjectDataInProjectFolder=true"]);

		Assert.True(result.ExitCode == 0, result.Output);

		// /getProperty prints the evaluated value on its own line.
		string expected = projectFile + ".lscache";
		Assert.Contains(expected, result.Output, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ExistingProjectFolderCache_ForcesProjectDataPathNextToCsproj()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false, writeAssetsFile: false);
		string expected = projectFile + ".lscache";
		await File.WriteAllTextAsync(expected, "existing cache", TestContext.Current.CancellationToken);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs: ["/getProperty:_ProjectDataPath"]);

		Assert.True(result.ExitCode == 0, result.Output);

		// Even though the unset default is user-folder mode, committed/in-project
		// caches keep using the project-folder path so they stay up to date.
		Assert.Contains(expected, result.Output, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task DefaultMode_DisablesProjectFolderStorage()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false, writeAssetsFile: false);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs: ["/getProperty:EnableProjectDataInProjectFolder"]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.Contains(
			result.Output.Replace("\r\n", "\n").Split('\n'),
			line => string.Equals(line.Trim(), "false", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task SingleTfmProject_DTBProducesProjectDataFile()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		string expected = projectFile + ".lscache";
		Assert.True(File.Exists(expected), $"Expected the writer to produce {expected}.\n{result.Output}");
		AssertNoUnsupportedMarker(projectFile);

		string content = File.ReadAllText(expected);
		Assert.Contains("OutputType=Exe", content);
		Assert.Contains("[commandLineArguments]", content);
	}

	[Fact]
	public async Task ProjectDataBuild_PersistsIsTestProjectOptOut()
	{
		string projectFile = this.WriteProject(
			"App.csproj",
			multiTargeting: false,
			extraProperties: "<IsTestProject>false</IsTestProject>");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		string content = File.ReadAllText(projectFile + ".lscache").Replace("\r\n", "\n");
		Assert.Contains("\nIsTestProject=false\n", content);
	}

	[Fact]
	public async Task ProjectDataBuild_AfterSdkImport_DoesNotSuppressGeneratedAssemblyInfoAndFiltersItFromCache()
	{
		string projectFile = this.WriteProject(
			"App.csproj",
			multiTargeting: false,
			extraProperties: "<Company>ProjectDataAuditCompany</Company>");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				$"/p:AfterMicrosoftNETSdkTargets={TargetsFile}",
			],
			wireProjectDataTargets: false);

		Assert.True(result.ExitCode == 0, result.Output);
		string assemblyInfoFile = Path.Combine(this.workDir, "obj", "Debug", "net8.0", "App.AssemblyInfo.cs");
		Assert.True(File.Exists(assemblyInfoFile), $"Expected the SDK to generate {assemblyInfoFile}.\n{result.Output}");
		Assert.Contains("ProjectDataAuditCompany", File.ReadAllText(assemblyInfoFile));

		string cacheContent = File.ReadAllText(projectFile + ".lscache");
		Assert.DoesNotContain("App.AssemblyInfo.cs", cacheContent);
	}

	[Fact]
	public async Task ProjectDataBuild_DoesNotFilterUserOwnedCompileItemAtGeneratedAssemblyInfoPath()
	{
		string projectFile = this.WriteProject(
			"App.csproj",
			multiTargeting: false,
			extraProperties:
			"""
			<GenerateAssemblyInfo>false</GenerateAssemblyInfo>
			<GeneratedAssemblyInfoFile>$(IntermediateOutputPath)Manual.AssemblyInfo.cs</GeneratedAssemblyInfoFile>
			""",
			extraXml:
			"""
			<ItemGroup>
			  <Compile Include="$(GeneratedAssemblyInfoFile)" />
			</ItemGroup>
			<Target Name="WriteManualAssemblyInfo" BeforeTargets="CoreCompile">
			  <MakeDir Directories="$([System.IO.Path]::GetDirectoryName('$(GeneratedAssemblyInfoFile)'))" />
			  <WriteLinesToFile File="$(GeneratedAssemblyInfoFile)" Lines="namespace ManualAssemblyInfoInput { internal static class Marker { } }" Overwrite="true" />
			</Target>
			""");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		string cacheContent = File.ReadAllText(projectFile + ".lscache");
		Assert.Contains("Manual.AssemblyInfo.cs", cacheContent);
	}

	[Fact]
	public async Task SingleTfmProject_DTBRunsCompileDependsOnTargetsForKeyFile()
	{
		string keyFile = Path.Combine(this.workDir, "TestKey.snk");
		await File.WriteAllTextAsync(keyFile, "test-key", TestContext.Current.CancellationToken);
		string projectFile = this.WriteProject(
			"App.csproj",
			multiTargeting: false,
			extraProperties:
			$"""
                <SignAssembly>true</SignAssembly>
                <PublicSign>true</PublicSign>
                <AssemblyOriginatorKeyFile>{keyFile}</AssemblyOriginatorKeyFile>
            """);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		string content = File.ReadAllText(projectFile + ".lscache").Replace("\r\n", "\n");
		Assert.Contains("/keyfile:", content);
		Assert.Contains("TestKey.snk", content);
	}

	[Fact]
	public async Task SingleTfmProject_ProjectDataBuildCommandLineArgumentsMatchDesignTimeCompile()
	{
		string baselineArgsFile = Path.Combine(this.workDir, "compile.args");
		string projectFile = this.WriteProject(
			"App.csproj",
			multiTargeting: false,
			extraXml:
			$$"""
              <Target Name="ProjectDataTestBeforeCompile" BeforeTargets="BeforeCompile">
                <PropertyGroup>
                  <DefineConstants>$(DefineConstants);PROJECTDATA_BEFORE_COMPILE</DefineConstants>
                </PropertyGroup>
              </Target>

              <Target Name="ProjectDataTestCaptureCscArgs" AfterTargets="CoreCompile">
                <WriteLinesToFile File="{{baselineArgsFile}}" Lines="@(CscCommandLineArgs)" Overwrite="true" />
              </Target>
            """);

		ProcessResult compileResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:Compile",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:ProvideCommandLineArgs=true",
			]);

		Assert.True(compileResult.ExitCode == 0, compileResult.Output);
		Assert.True(File.Exists(baselineArgsFile), $"Expected direct Compile to capture CscCommandLineArgs at {baselineArgsFile}.\n{compileResult.Output}");
		string[] compileArgs = File.ReadAllLines(baselineArgsFile);

		ProcessResult projectDataResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:ProvideCommandLineArgs=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(projectDataResult.ExitCode == 0, projectDataResult.Output);
		string[] cacheArgs = ExtractCommandLineArguments(File.ReadAllText(projectFile + ".lscache"));

		string[] expected = NormalizeForStableArgumentParity(compileArgs);
		string[] actual = NormalizeForStableArgumentParity(cacheArgs);
		Assert.Equal(expected, actual);
		Assert.Contains(actual, arg => arg.Contains("PROJECTDATA_BEFORE_COMPILE", StringComparison.Ordinal));
	}

	[Fact]
	public async Task SingleTfmProject_DTBProducesProjectDataFile_WhenCoreCompileIsUpToDate()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);
		string expected = projectFile + ".lscache";

		ProcessResult firstResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(firstResult.ExitCode == 0, firstResult.Output);
		Assert.True(File.Exists(expected), $"Expected the first DTB to produce {expected}.\n{firstResult.Output}");

		File.Delete(expected);

		ProcessResult secondResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(secondResult.ExitCode == 0, secondResult.Output);
		Assert.True(File.Exists(expected), $"Expected the second DTB to produce {expected} even when CoreCompile is up-to-date.\n{secondResult.Output}");
		Assert.Contains("[commandLineArguments]", File.ReadAllText(expected));
	}

	[Fact]
	public async Task DirectWriteTarget_DoesNotProduceCacheWithoutCommandLineArguments()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_WriteProjectData",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataOnBuild=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.False(File.Exists(projectFile + ".lscache"), $"Direct _WriteProjectData should not silently create a cache without CscCommandLineArgs.\n{result.Output}");

		// The opportunistic EnableProjectDataOnBuild hook did NOT force CoreCompile, so an empty
		// CscCommandLineArgs here means CoreCompile was skipped as up-to-date (its AfterTargets hook
		// still fires) — a perfectly good project. The writer must NOT poison the shared cache with a
		// spurious unsupported marker; doing so makes projects silently vanish on the next non-forced
		// workspace refresh (the aspire-starter regression).
		AssertNoUnsupportedMarker(projectFile);
	}

	[Fact]
	public async Task DirectWriteTarget_WritesUnsupportedMarker_WhenCoreCompileForcedAndArgumentsEmpty()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_WriteProjectData",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataOnBuild=true",
				"/p:EnableProjectDataInProjectFolder=true",

				// The authoritative ProjectDataBuild graph forces CoreCompile to run, so an empty
				// CscCommandLineArgs genuinely means the project produces no C# compilation and should
				// be marked unsupported.
				"/p:_ProjectDataBuildActive=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.False(File.Exists(projectFile + ".lscache"), $"Direct _WriteProjectData should not silently create a cache without CscCommandLineArgs.\n{result.Output}");
		AssertUnsupportedMarker(projectFile, "CompilerCommandLineArgumentsEmpty");
	}

	[Fact]
	public async Task DirectWriteTarget_PreservesExistingCache_WhenNotForcedAndArgumentsEmpty()
	{
		// Regression test for the aspire-starter "projects vanish" bug. An authoritative ProjectDataBuild
		// writes a good `.lscache`; then an ordinary incremental build's opportunistic
		// EnableProjectDataOnBuild hook fires AfterTargets="CoreCompile" with empty CscCommandLineArgs
		// (CoreCompile was skipped as up-to-date). The writer must leave the good cache in place and NOT
		// poison the shared cache with an unsupported marker.
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);
		string cache = projectFile + ".lscache";

		ProcessResult dtbResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(dtbResult.ExitCode == 0, dtbResult.Output);
		Assert.True(File.Exists(cache), $"Expected ProjectDataBuild to produce {cache}.\n{dtbResult.Output}");
		string originalContent = File.ReadAllText(cache);

		// Simulate the ordinary incremental build hook: EnableProjectDataOnBuild without the
		// ProjectDataBuild graph (so CoreCompile is not forced) and empty CscCommandLineArgs.
		ProcessResult hookResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_WriteProjectData",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataOnBuild=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(hookResult.ExitCode == 0, hookResult.Output);
		Assert.True(File.Exists(cache), $"The non-forced empty-args hook must NOT delete the existing good cache {cache}.\n{hookResult.Output}");
		Assert.Equal(originalContent, File.ReadAllText(cache));
		AssertNoUnsupportedMarker(projectFile);
	}

	[Fact]
	public async Task ProjectDataBuild_NonSdkProject_IsNoOp()
	{
		string projectFile = this.WriteLegacyProject("LegacyApp.csproj");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.False(File.Exists(projectFile + ".lscache"), $"Non-SDK projects should not produce project data.\n{result.Output}");
		AssertUnsupportedMarker(projectFile, "UsingMicrosoftNETSdkFalse");
	}

	[Fact]
	public async Task ProjectDataBuild_SdkFSharpProject_IsNoOp()
	{
		string projectFile = this.WriteProject("FSharpApp.fsproj", multiTargeting: false, extraProperties: "<Language>F#</Language>");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.False(File.Exists(projectFile + ".lscache"), $"Non-C# projects should not produce project data.\n{result.Output}");
		AssertUnsupportedMarker(projectFile, "LanguageNotCSharp");
	}

	[Fact]
	public async Task ProjectDataBuild_NoTargetsSdkIdentity_IsNoOp()
	{
		// Microsoft.Build.NoTargets sets this property in its Sdk.props. Set the
		// identity bit directly so the smoke test does not have to resolve an
		// external MSBuild SDK package.
		string projectFile = this.WriteProject(
			"NoTargets.csproj",
			multiTargeting: false,
			extraProperties:
			"""
                <UsingMicrosoftNoTargetsSdk>true</UsingMicrosoftNoTargetsSdk>
            """);
		await File.WriteAllTextAsync(projectFile + ".lscache", "stale", TestContext.Current.CancellationToken);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.False(File.Exists(projectFile + ".lscache"), $"Microsoft.Build.NoTargets projects should not produce or retain project data.\n{result.Output}");
		AssertUnsupportedMarker(projectFile, "MicrosoftBuildNoTargetsSdk");
	}

	[Fact]
	public async Task ProjectDataBuild_ExcludedProject_PreservesProjectFolderCache()
	{
		string projectFile = this.WriteProject(
			"Excluded.csproj",
			multiTargeting: false,
			extraProperties:
			"""
                <ExcludeFromBuild>true</ExcludeFromBuild>
            """);
		await File.WriteAllTextAsync(projectFile + ".lscache", "committed cache", TestContext.Current.CancellationToken);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				"/p:OS=Unix",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.Equal("committed cache", File.ReadAllText(projectFile + ".lscache"));
		AssertUnsupportedMarker(projectFile, "ExcludeFromBuildTrue");
	}

	[Fact]
	public async Task MultiTfmProject_DTBProducesMergedProjectDataFile()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		string expected = projectFile + ".lscache";
		Assert.True(File.Exists(expected), $"Expected the merge target to produce {expected}.\n{result.Output}");

		string content = File.ReadAllText(expected);
		Assert.Contains("[commandLineArguments]", content);
	}

	[Fact]
	public async Task MultiTfmProject_ProjectDataBuildWritesSdkLayoutSlices()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true, targetFrameworks: "net8.0;net9.0");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(
			File.Exists(Path.Combine(this.workDir, "obj", "Debug", "net8.0", "App.csproj.slice")),
			$"Expected ProjectData to write the net8.0 slice to the deterministic TFM path.\n{result.Output}");
		Assert.True(
			File.Exists(Path.Combine(this.workDir, "obj", "Debug", "net9.0", "App.csproj.slice")),
			$"Expected ProjectData to write the net9.0 slice to the deterministic TFM path.\n{result.Output}");

		string expected = projectFile + ".lscache";
		Assert.True(File.Exists(expected), $"Expected the merge target to produce {expected}.\n{result.Output}");
		AssertNoUnsupportedMarker(projectFile);

		string content = File.ReadAllText(expected).Replace("\r\n", "\n");
		Assert.Equal(2, CountOccurrences(content, "\n[sliceDimensions]\n"));
		Assert.Contains("TargetFramework=net8.0", content);
		Assert.Contains("TargetFramework=net9.0", content);
	}

	[Fact]
	public async Task MultiTfmUserFolderProject_RegeneratesCacheWhenStampExists()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true);
		string cacheRoot = GetTestCacheRoot(projectFile);
		string stampPath = Path.Combine(this.workDir, "obj", "Debug", "App.csproj.lscache.stamp");
		string[] args =
		[
			"/t:ProjectDataBuild",
			"/p:DesignTimeBuild=true",
			"/p:BuildingProject=false",
			"/p:SkipCompilerExecution=true",
			"/p:EnableProjectDataInProjectFolder=false",
		];

		ProcessResult firstResult = await RunDotnetMsbuildAsync(projectFile, extraArgs: args);

		Assert.True(firstResult.ExitCode == 0, firstResult.Output);
		Assert.True(File.Exists(stampPath), $"Expected user-folder multi-TFM merge stamp at {stampPath}.\n{firstResult.Output}");
		string[] cacheFiles = Directory.GetFiles(cacheRoot, "*", SearchOption.AllDirectories);
		string cacheFile = Assert.Single(cacheFiles);
		Assert.Contains("version=2", File.ReadAllText(cacheFile));
		File.Delete(cacheFile);

		ProcessResult secondResult = await RunDotnetMsbuildAsync(projectFile, extraArgs: args);

		Assert.True(secondResult.ExitCode == 0, secondResult.Output);
		string regeneratedCacheFile = Assert.Single(Directory.GetFiles(cacheRoot, "*", SearchOption.AllDirectories));
		Assert.Contains("version=2", File.ReadAllText(regeneratedCacheFile));
		AssertNoUnsupportedMarker(projectFile);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task MultiTfmUserFolderProject_WhenMergeFails_DoesNotRefreshStamp(bool force)
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true);
		string stampPath = Path.Combine(this.workDir, "obj", "Debug", "App.csproj.lscache.stamp");
		string cacheRootFile = Path.Combine(this.workDir, "user-cache-file");
		await File.WriteAllTextAsync(cacheRootFile, "not a directory", TestContext.Current.CancellationToken);
		List<string> args =
		[
			"/t:ProjectDataBuild",
			"/p:DesignTimeBuild=true",
			"/p:BuildingProject=false",
			"/p:SkipCompilerExecution=true",
			"/p:EnableProjectDataInProjectFolder=false",
		];
		if (force)
		{
			args.Add("/p:_ProjectDataBuildForce=true");
		}

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs: [.. args],
			extraEnv: new() { ["DOTNET_PROJECTDATA_CACHE_DIR"] = cacheRootFile });

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.Contains("ProjectData: failed to merge slices", result.Output);
		Assert.False(File.Exists(stampPath), $"A failed user-folder merge must not mark ProjectData as fresh.\n{result.Output}");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task MultiTfmRidProject_ProjectDataBuildProducesMergedProjectDataFile(bool force)
	{
		const string runtimeIdentifier = "win-x64";
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true, runtimeIdentifier: runtimeIdentifier);
		List<string> args =
		[
			"/t:ProjectDataBuild",
			"/p:DesignTimeBuild=true",
			"/p:BuildingProject=false",
			"/p:SkipCompilerExecution=true",
			"/p:EnableProjectDataInProjectFolder=true",
		];
		if (force)
		{
			args.Add("/p:_ProjectDataBuildForce=true");
		}

		ProcessResult result = await RunDotnetMsbuildAsync(projectFile, extraArgs: [.. args]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(
			File.Exists(Path.Combine(this.workDir, "obj", "Debug", "net8.0", runtimeIdentifier, "App.csproj.slice")),
			$"Expected the net8.0 RID-specific inner slice.\n{result.Output}");
		Assert.True(
			File.Exists(Path.Combine(this.workDir, "obj", "Debug", "net9.0", runtimeIdentifier, "App.csproj.slice")),
			$"Expected the net9.0 RID-specific inner slice.\n{result.Output}");
		Assert.False(
			File.Exists(Path.Combine(this.workDir, "obj", "Debug", "net8.0", "App.csproj.slice")),
			$"The SDK should place RID-specific slices under the runtime identifier directory.\n{result.Output}");

		string expected = projectFile + ".lscache";
		Assert.True(File.Exists(expected), $"Expected the merge target to produce {expected}.\n{result.Output}");
		AssertNoUnsupportedMarker(projectFile);

		string content = File.ReadAllText(expected).Replace("\r\n", "\n");
		Assert.Equal(2, CountOccurrences(content, "\n[sliceDimensions]\n"));
		Assert.Contains("TargetFramework=net8.0", content);
		Assert.Contains("TargetFramework=net9.0", content);
	}

	[Fact]
	public async Task MultiTfmRidProject_WhenRuntimeIdentifierOutputPathAppendDisabled_ProducesMergedProjectDataFile()
	{
		const string runtimeIdentifier = "win-x64";
		string projectFile = this.WriteProject(
			"App.csproj",
			multiTargeting: true,
			runtimeIdentifier: runtimeIdentifier,
			extraProperties: "<AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(
			File.Exists(Path.Combine(this.workDir, "obj", "Debug", "net8.0", "App.csproj.slice")),
			$"Expected the net8.0 non-RID inner slice when AppendRuntimeIdentifierToOutputPath=false.\n{result.Output}");
		Assert.True(
			File.Exists(Path.Combine(this.workDir, "obj", "Debug", "net9.0", "App.csproj.slice")),
			$"Expected the net9.0 non-RID inner slice when AppendRuntimeIdentifierToOutputPath=false.\n{result.Output}");
		Assert.False(
			File.Exists(Path.Combine(this.workDir, "obj", "Debug", "net8.0", runtimeIdentifier, "App.csproj.slice")),
			$"The SDK should not place slices under the runtime identifier directory when AppendRuntimeIdentifierToOutputPath=false.\n{result.Output}");

		string expected = projectFile + ".lscache";
		Assert.True(File.Exists(expected), $"Expected the merge target to produce {expected}.\n{result.Output}");
		AssertNoUnsupportedMarker(projectFile);

		string content = File.ReadAllText(expected).Replace("\r\n", "\n");
		Assert.Equal(2, CountOccurrences(content, "\n[sliceDimensions]\n"));
		Assert.Contains("TargetFramework=net8.0", content);
		Assert.Contains("TargetFramework=net9.0", content);
	}

	[Fact]
	public async Task MultiTfmProject_ProjectDataBuildWithUnchangedSlicesPreservesMergedCache()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true);
		string cacheFile = projectFile + ".lscache";

		ProcessResult firstResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(firstResult.ExitCode == 0, firstResult.Output);
		Assert.True(File.Exists(cacheFile), $"Expected the first DTB to produce {cacheFile}.\n{firstResult.Output}");
		string expectedContent = File.ReadAllText(cacheFile);
		File.SetLastWriteTimeUtc(cacheFile, DateTime.UtcNow.AddMinutes(5));

		ProcessResult secondResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(secondResult.ExitCode == 0, secondResult.Output);
		Assert.Equal(expectedContent, File.ReadAllText(cacheFile));
		AssertNoUnsupportedMarker(projectFile);
	}

	[Fact]
	public async Task MultiTfmProject_ForceProjectDataBuildRefreshesMergedOutputWithoutDeletingCache()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true);
		string cacheFile = projectFile + ".lscache";

		ProcessResult firstResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(firstResult.ExitCode == 0, firstResult.Output);
		Assert.True(File.Exists(cacheFile), $"Expected the first DTB to produce {cacheFile}.\n{firstResult.Output}");

		await File.WriteAllTextAsync(cacheFile, "stale-cache-content", TestContext.Current.CancellationToken);
		File.SetLastWriteTimeUtc(cacheFile, DateTime.UtcNow.AddMinutes(5));

		ProcessResult secondResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				"/p:_ProjectDataBuildForce=true",
			]);

		Assert.True(secondResult.ExitCode == 0, secondResult.Output);
		string content = File.ReadAllText(cacheFile).Replace("\r\n", "\n");
		Assert.DoesNotContain("stale-cache-content", content);
		Assert.Contains("TargetFramework=net8.0", content);
		Assert.Contains("TargetFramework=net9.0", content);
	}

	[Fact]
	public async Task MultiTfmProject_ProjectDataBuildRefreshesCommandLineArgumentChanges()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true, targetFrameworks: "net8.0;net10.0");

		ProcessResult firstResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				"/p:NoWarn=1111",
			]);

		Assert.True(firstResult.ExitCode == 0, firstResult.Output);
		string content = File.ReadAllText(projectFile + ".lscache");
		Assert.Contains("1111", content);

		ProcessResult secondResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				"/p:NoWarn=2222",
			]);

		Assert.True(secondResult.ExitCode == 0, secondResult.Output);
		content = File.ReadAllText(projectFile + ".lscache");
		Assert.DoesNotContain("1111", content);
		Assert.Contains("2222", content);
	}

	[Fact]
	public async Task MultiTfmProject_RemovedTargetFrameworkStaleSliceIsNotMerged()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true, targetFrameworks: "net8.0;net9.0");

		ProcessResult firstResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(firstResult.ExitCode == 0, firstResult.Output);
		string staleSlice = Path.Combine(this.workDir, "obj", "Debug", "net9.0", "App.csproj.slice");
		Assert.True(File.Exists(staleSlice), $"Expected first build to preserve the net9.0 slice.\n{firstResult.Output}");

		projectFile = this.WriteProject("App.csproj", multiTargeting: true, targetFrameworks: "net8.0");

		ProcessResult secondResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				"/p:OS=Windows_NT",
			]);

		Assert.True(secondResult.ExitCode == 0, secondResult.Output);
		string content = File.ReadAllText(projectFile + ".lscache").Replace("\r\n", "\n");
		Assert.Contains("TargetFramework=net8.0", content);
		Assert.DoesNotContain("TargetFramework=net9.0", content);
	}

	[Fact]
	public async Task MultiTfmProject_NonWindowsPreservesExistingUnevaluatedSlice()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true, targetFrameworks: "net8.0;net9.0");

		ProcessResult firstResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(firstResult.ExitCode == 0, firstResult.Output);

		projectFile = this.WriteProject("App.csproj", multiTargeting: true, targetFrameworks: "net8.0");

		ProcessResult secondResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				"/p:OS=Unix",
			]);

		Assert.True(secondResult.ExitCode == 0, secondResult.Output);
		string content = File.ReadAllText(projectFile + ".lscache").Replace("\r\n", "\n");
		Assert.Contains("TargetFramework=net8.0", content);
		Assert.Contains("TargetFramework=net9.0", content);
	}

	[Theory]
	[InlineData("net8.0;net9.0", "net8.0", "net9.0")]
	[InlineData("net9.0;net8.0", "net9.0", "net8.0")]
	public async Task MultiTfmProject_DTBMarksFirstTargetFrameworkAsPrimary(
		string targetFrameworks,
		string expectedPrimary,
		string expectedNonPrimary)
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true, targetFrameworks: targetFrameworks);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		string cacheFile = projectFile + ".lscache";
		Assert.True(File.Exists(cacheFile), $"Expected the merge target to produce {cacheFile}.\n{result.Output}");

		string content = File.ReadAllText(cacheFile).Replace("\r\n", "\n");
		Assert.Equal(1, CountOccurrences(content, "\nprimary\n"));
		Assert.Contains("\nprimary\n", GetSliceBlock(content, expectedPrimary));
		Assert.DoesNotContain("\nprimary\n", GetSliceBlock(content, expectedNonPrimary));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Compile_WritesProjectDataOnlyWhenBuildHookIsEnabled(bool enableProjectDataOnBuild)
	{
		await File.WriteAllTextAsync(
			Path.Combine(this.workDir, "Program.cs"),
			"using System; Console.WriteLine(\"hello\");",
			TestContext.Current.CancellationToken);
		string projectFile = this.WriteProject(
			"App.csproj",
			multiTargeting: false,
			extraXml:
			"""
			  <ItemGroup>
			    <Compile Include="Program.cs" />
			  </ItemGroup>
			""");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:Compile",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:UseAppHost=false",
				"/p:EnableProjectDataInProjectFolder=true",
				$"/p:EnableProjectDataOnBuild={enableProjectDataOnBuild.ToString().ToLowerInvariant()}",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		string cacheFile = projectFile + ".lscache";
		Assert.Equal(enableProjectDataOnBuild, File.Exists(cacheFile));
	}

	[Fact]
	public async Task DefaultMode_WritesToUserFolderCacheLocation()
	{
		// Default user-folder mode writes under DOTNET_PROJECTDATA_CACHE_DIR
		// (test-only override) using the same SHA-1 layout the reader uses.
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);
		string cacheRoot = Path.Combine(this.workDir, "user-cache");
		Directory.CreateDirectory(cacheRoot);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/v:n",
			],
			extraEnv: new() { ["DOTNET_PROJECTDATA_CACHE_DIR"] = cacheRoot });

		Assert.True(result.ExitCode == 0, result.Output);

		// Confirm at least one cache file appeared somewhere under the override
		// root, and nothing was written next to the .csproj.
		string[] cacheFiles = Directory.GetFiles(cacheRoot, "*", SearchOption.AllDirectories);
		Assert.True(cacheFiles.Length > 0, $"Expected user-folder cache file under {cacheRoot}.\n--- MSBUILD OUTPUT ---\n{result.Output}");
		Assert.False(
			File.Exists(projectFile + ".lscache"),
			$"Expected the cache to live under the user-folder root, not next to the .csproj.\n{result.Output}");
	}

	[Fact]
	public async Task ExplicitUserFolderMode_RefreshesStampOnProjectDataBuild()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);
		string cacheRoot = Path.Combine(this.workDir, "user-cache");
		Directory.CreateDirectory(cacheRoot);

		string[] args =
		[
			"/t:ProjectDataBuild",
			"/p:DesignTimeBuild=true",
			"/p:BuildingProject=false",
			"/p:SkipCompilerExecution=true",
			"/p:EnableProjectDataInProjectFolder=false",
		];

		ProcessResult firstResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs: args,
			extraEnv: new() { ["DOTNET_PROJECTDATA_CACHE_DIR"] = cacheRoot });

		Assert.True(firstResult.ExitCode == 0, firstResult.Output);
		string stampPath = Path.Combine(this.workDir, "obj", "Debug", "net8.0", "App.csproj.lscache.stamp");
		Assert.True(File.Exists(stampPath), $"Expected user-folder stamp at {stampPath}.\n{firstResult.Output}");
		DateTime firstStampTime = File.GetLastWriteTimeUtc(stampPath);

		await Task.Delay(TimeSpan.FromSeconds(1.1), TestContext.Current.CancellationToken);

		ProcessResult secondResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs: args,
			extraEnv: new() { ["DOTNET_PROJECTDATA_CACHE_DIR"] = cacheRoot });

		Assert.True(secondResult.ExitCode == 0, secondResult.Output);
		Assert.True(File.GetLastWriteTimeUtc(stampPath) > firstStampTime);
	}

	[Fact]
	public async Task NoAssetsFile_DoesNotIncludeRestoreInDependsOn()
	{
		// ProjectDataBuild requires a restored evaluation. It must not run Restore
		// inside the same MSBuild invocation because generated NuGet imports would
		// be written after this project was already evaluated.
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false, writeAssetsFile: false);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/getProperty:_ProjectDataBuildDependsOn",
				"/p:DesignTimeBuild=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.DoesNotContain("Restore;", result.Output);
		Assert.Contains("Compile", result.Output);
	}

	[Fact]
	public async Task FreshAssetsFile_OmitsRestoreFromDependsOn()
	{
		// Steady-state path: when obj/project.assets.json exists AND is newer than
		// the project file, Restore is dropped from the dependsOn list. The host
		// owns restore lifecycle and re-running it on every DTB is wasted work
		// (~150-300ms warm).
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);

		// Place a fresh assets file (mtime later than the .csproj).
		string objDir = Path.Combine(Path.GetDirectoryName(projectFile)!, "obj");
		Directory.CreateDirectory(objDir);
		string assetsFile = Path.Combine(objDir, "project.assets.json");
		await File.WriteAllTextAsync(assetsFile, "{}", TestContext.Current.CancellationToken);
		File.SetLastWriteTime(assetsFile, DateTime.Now.AddMinutes(1));

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/getProperty:_ProjectDataBuildDependsOn",
				"/p:DesignTimeBuild=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.DoesNotContain("Restore;", result.Output);
		// Inner-build target list must still be there.
		Assert.Contains("Compile", result.Output);
	}

	[Fact]
	public async Task UnsupportedSdkProject_UsesMarkerOnlyDependsOn()
	{
		string projectFile = this.WriteProject("FSharpApp.fsproj", multiTargeting: false, extraProperties: "<Language>F#</Language>");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/getProperty:_ProjectDataBuildDependsOn",
				"/p:DesignTimeBuild=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.Contains("_DeleteUnsupportedProjectData;_WriteUnsupportedProjectDataMarker", result.Output);
		Assert.DoesNotContain("Compile", result.Output);
		Assert.DoesNotContain("DispatchToInnerBuilds", result.Output);
	}

	[Fact]
	public async Task MultiTfmOuterBuild_DispatchesInnerBuildsInDependsOn()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: true);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/getProperty:_ProjectDataBuildDependsOn",
				"/p:DesignTimeBuild=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.Contains("DispatchToInnerBuilds", result.Output);
		Assert.DoesNotContain("_DeleteUnsupportedProjectData;_WriteUnsupportedProjectDataMarker", result.Output);
	}

	[Fact]
	public async Task MultiTfmUnsupportedInnerBuild_UsesMarkerOnlyDependsOn()
	{
		string projectFile = this.WriteProject("FSharpApp.fsproj", multiTargeting: true, extraProperties: "<Language>F#</Language>");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/getProperty:_ProjectDataBuildDependsOn",
				"/p:DesignTimeBuild=true",
				"/p:EnableProjectDataInProjectFolder=true",
				"/p:TargetFramework=net8.0",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.Contains("_DeleteUnsupportedProjectData;_WriteUnsupportedProjectDataMarker", result.Output);
		Assert.DoesNotContain("Compile", result.Output);
		Assert.DoesNotContain("DispatchToInnerBuilds", result.Output);
	}

	[Fact]
	public async Task StaleAssetsFile_DoesNotIncludeRestoreInDependsOn()
	{
		// The ProjectDataBuild target must never add Restore to its target graph:
		// generated NuGet imports would be written after this project was already
		// evaluated. Restore orchestration belongs to the caller.
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);

		// Place an assets file with mtime BEFORE the .csproj (simulate user edit
		// after a previous successful restore).
		string objDir = Path.Combine(Path.GetDirectoryName(projectFile)!, "obj");
		Directory.CreateDirectory(objDir);
		string assetsFile = Path.Combine(objDir, "project.assets.json");
		await File.WriteAllTextAsync(assetsFile, "{}", TestContext.Current.CancellationToken);
		File.SetLastWriteTime(assetsFile, DateTime.Now.AddMinutes(-5));
		File.SetLastWriteTime(projectFile, DateTime.Now);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/getProperty:_ProjectDataBuildDependsOn",
				"/p:DesignTimeBuild=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.DoesNotContain("Restore;", result.Output);
		Assert.Contains("Compile", result.Output);
	}

	[Fact]
	public async Task ForceIncludeRestore_DoesNotAddRestoreToDependsOn()
	{
		// Historical compatibility: the old private switch no longer makes
		// ProjectDataBuild run Restore inside this evaluation.
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);
		string objDir = Path.Combine(Path.GetDirectoryName(projectFile)!, "obj");
		Directory.CreateDirectory(objDir);
		string assetsFile = Path.Combine(objDir, "project.assets.json");
		await File.WriteAllTextAsync(assetsFile, "{}", TestContext.Current.CancellationToken);
		File.SetLastWriteTime(assetsFile, DateTime.Now.AddMinutes(1));

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/getProperty:_ProjectDataBuildDependsOn",
				"/p:DesignTimeBuild=true",
				"/p:EnableProjectDataInProjectFolder=true",
				"/p:_ProjectDataBuildIncludeRestore=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.DoesNotContain("Restore;", result.Output);
	}

	[Theory]
	[InlineData(".sln")]
	[InlineData(".slnx")]
	public async Task SolutionLevel_DispatchesProjectDataBuildToAllProjects(string solutionExtension)
	{
		// Verifies that running /t:ProjectDataBuild against a solution file dispatches
		// the custom target to each project, including multi-TFM outer builds.
		string projectA = this.WriteProject("App.csproj", multiTargeting: true);
		string projectBDir = Path.Combine(this.workDir, "Lib");
		Directory.CreateDirectory(projectBDir);
		string projectB = Path.Combine(projectBDir, "Lib.csproj");
		string stubRef = Path.Combine(this.workDir, "stub-reference.dll");
		File.WriteAllText(projectB,
			$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
              <Target Name="_ProjectDataTestInjectStubReference"
                      BeforeTargets="_ValidateProjectDataMetadataReferences"
                      Condition="'$(DisableImplicitFrameworkReferences)' != 'true'">
                <ItemGroup>
                  <ReferencePathWithRefAssemblies Include="{stubRef}">
                    <FrameworkReferenceName>Microsoft.NETCore.App</FrameworkReferenceName>
                  </ReferencePathWithRefAssemblies>
                </ItemGroup>
              </Target>
            </Project>
            """);
		this.WriteProjectAssetsFile(projectB, ["net8.0"]);

		string solutionPath = Path.Combine(this.workDir, "Solution" + solutionExtension);
		string receiptDirectory = Path.Combine(this.workDir, "receipts-" + solutionExtension.TrimStart('.'));
		string attemptId = Guid.NewGuid().ToString("N");
		string slnRelA = Path.GetRelativePath(this.workDir, projectA).Replace('/', '\\');
		string slnRelB = Path.GetRelativePath(this.workDir, projectB).Replace('/', '\\');
		if (solutionExtension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
		{
			File.WriteAllText(solutionPath,
				$$"""
	            Microsoft Visual Studio Solution File, Format Version 12.00
	            # Visual Studio Version 17
	            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App", "{{slnRelA}}", "{11111111-1111-1111-1111-111111111111}"
	            EndProject
	            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Lib", "{{slnRelB}}", "{22222222-2222-2222-2222-222222222222}"
	            EndProject
	            Global
	                GlobalSection(SolutionConfigurationPlatforms) = preSolution
	                    Debug|Any CPU = Debug|Any CPU
	                EndGlobalSection
	                GlobalSection(ProjectConfigurationPlatforms) = postSolution
	                    {11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
	                    {11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
	                    {22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
	                    {22222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU
	                EndGlobalSection
	            EndGlobal
	            """);
		}
		else
		{
			File.WriteAllText(solutionPath,
				$$"""
	            <Solution>
	              <Project Path="{{slnRelA}}" />
	              <Project Path="{{slnRelB}}" />
	            </Solution>
	            """);
		}

		ProcessResult result = await RunDotnetMsbuildAsync(
			solutionPath,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				$"/p:ProjectDataBuildReceiptDirectory={receiptDirectory}",
				$"/p:ProjectDataBuildReceiptAttemptId={attemptId}",
				GetCompletionLoggerArgument(receiptDirectory, attemptId),
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(File.Exists(projectA + ".lscache"), $"Expected lscache for App.csproj.\n{result.Output}");
		Assert.True(File.Exists(projectB + ".lscache"), $"Expected lscache for Lib.csproj.\n{result.Output}");
		Assert.True(ProjectDataBuildReceipt.TryRead(receiptDirectory, attemptId, projectA, out _));
		Assert.True(ProjectDataBuildReceipt.TryRead(receiptDirectory, attemptId, projectB, out _));
		Assert.True(ProjectDataBuildReceipt.TryReadAggregateCompletion(receiptDirectory, attemptId));
		Assert.True(ProjectDataBuildAttemptManifest.TryRead(receiptDirectory, attemptId, out ProjectDataBuildAttemptManifest manifest));
		Assert.True(manifest.BuildFinished);
		Assert.True(manifest.BuildSucceeded);
		Assert.NotEmpty(manifest.Contexts);
		Assert.Contains(manifest.Submissions, submission => submission.Phase == "ProjectDataBuild");

		string appCacheContent = File.ReadAllText(projectA + ".lscache").Replace("\r\n", "\n");
		Assert.Contains("TargetFramework=net8.0", appCacheContent);
		Assert.Contains("TargetFramework=net9.0", appCacheContent);
	}

	[Fact]
	public async Task ProjectDataBuild_ReceiptProtocol_NormalAggregateFailureHasManifestWithoutProjectCompletion()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false, writeAssetsFile: false);
		string receiptDirectory = Path.Combine(this.workDir, "failure-receipts");
		string attemptId = Guid.NewGuid().ToString("N");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				$"/p:ProjectDataBuildReceiptDirectory={receiptDirectory}",
				$"/p:ProjectDataBuildReceiptAttemptId={attemptId}",
				GetCompletionLoggerArgument(receiptDirectory, attemptId),
			]);

		Assert.NotEqual(0, result.ExitCode);
		Assert.False(ProjectDataBuildReceipt.TryRead(receiptDirectory, attemptId, projectFile, out _));
		Assert.True(ProjectDataBuildReceipt.TryReadAggregateCompletion(receiptDirectory, attemptId));
		Assert.True(ProjectDataBuildAttemptManifest.TryRead(receiptDirectory, attemptId, out ProjectDataBuildAttemptManifest manifest));
		Assert.True(manifest.BuildFinished);
		Assert.False(manifest.BuildSucceeded);
		ProjectDataBuildDiagnosticRecord diagnostic = Assert.Single(manifest.Diagnostics, diagnostic => diagnostic.Severity == "Error");
		Assert.True(string.Equals(
			projectFile,
			diagnostic.ProjectFilePath,
			OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
		Assert.Contains("project.assets.json", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ProjectDataBuild_StaticGraphRestorePreservesDirectFailureAttributionAndTrimmedProjectsProduceCaches()
	{
		string missingFeed = Path.Combine(this.workDir, "missing-private-feed");
		File.WriteAllText(
			Path.Combine(this.workDir, "NuGet.Config"),
			$"""
			<?xml version="1.0" encoding="utf-8"?>
			<configuration>
			  <packageSources>
			    <clear />
			    <add key="missing-private-feed" value="{missingFeed}" />
			  </packageSources>
			</configuration>
			""");

		string broken = WriteRestoreProject(
			"Broken",
			"""    <PackageReference Include="Picasso.Private.Package" Version="1.0.0" />""");
		string dependent = WriteRestoreProject(
			"Dependent",
			"""    <ProjectReference Include="..\Broken\Broken.csproj" />""");
		string healthyA = WriteRestoreProject("HealthyA");
		string healthyB = WriteRestoreProject("HealthyB");
		string fullSolution = WriteSolution("Full.sln", [broken, dependent, healthyA, healthyB]);
		string fullReceiptDirectory = Path.Combine(this.workDir, "full-restore-receipts");
		string fullAttemptId = Guid.NewGuid().ToString("N");

		ProcessResult fullResult = await RunDotnetMsbuildAsync(
			fullSolution,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/m:1",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				$"/p:ProjectDataBuildReceiptDirectory={fullReceiptDirectory}",
				$"/p:ProjectDataBuildReceiptAttemptId={fullAttemptId}",
				GetCompletionLoggerArgument(fullReceiptDirectory, fullAttemptId),
			],
			useBuildCommand: true);

		Assert.NotEqual(0, fullResult.ExitCode);
		Assert.True(
			ProjectDataBuildAttemptManifest.TryRead(fullReceiptDirectory, fullAttemptId, out ProjectDataBuildAttemptManifest fullManifest),
			fullResult.Output);
		ProjectDataBuildDiagnosticRecord directFailure = Assert.Single(
			fullManifest.Diagnostics,
			diagnostic =>
				string.Equals(diagnostic.Severity, "Error", StringComparison.OrdinalIgnoreCase) &&
				string.Equals(diagnostic.Code, "NU1301", StringComparison.OrdinalIgnoreCase) &&
				string.Equals(diagnostic.ProjectFilePath, broken, StringComparison.OrdinalIgnoreCase));
		Assert.Equal(ProjectDataBuildDiagnosticRecord.FileProjectPathSource, directFailure.ProjectFilePathSource);
		Assert.False(File.Exists(healthyA + ".lscache"));
		Assert.False(File.Exists(healthyB + ".lscache"));

		string trimmedSolution = WriteSolution("Trimmed.sln", [healthyA, healthyB]);
		string trimmedReceiptDirectory = Path.Combine(this.workDir, "trimmed-restore-receipts");
		string trimmedAttemptId = Guid.NewGuid().ToString("N");
		ProcessResult trimmedResult = await RunDotnetMsbuildAsync(
			trimmedSolution,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/m:1",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				$"/p:ProjectDataBuildReceiptDirectory={trimmedReceiptDirectory}",
				$"/p:ProjectDataBuildReceiptAttemptId={trimmedAttemptId}",
				GetCompletionLoggerArgument(trimmedReceiptDirectory, trimmedAttemptId),
			],
			useBuildCommand: true);

		Assert.True(trimmedResult.ExitCode == 0, trimmedResult.Output);
		Assert.True(ProjectDataBuildReceipt.TryRead(trimmedReceiptDirectory, trimmedAttemptId, healthyA, out _));
		Assert.True(ProjectDataBuildReceipt.TryRead(trimmedReceiptDirectory, trimmedAttemptId, healthyB, out _));
		Assert.True(File.Exists(healthyA + ".lscache"));
		Assert.True(File.Exists(healthyB + ".lscache"));
		Assert.StartsWith("version=2", File.ReadAllText(healthyA + ".lscache"));
		Assert.StartsWith("version=2", File.ReadAllText(healthyB + ".lscache"));

		string WriteRestoreProject(string name, string item = "")
		{
			string directory = Path.Combine(this.workDir, name);
			Directory.CreateDirectory(directory);
			string projectPath = Path.Combine(directory, $"{name}.csproj");
			File.WriteAllText(
				projectPath,
				$$"""
				<Project Sdk="Microsoft.NET.Sdk">
				  <PropertyGroup>
				    <TargetFramework>net10.0</TargetFramework>
				    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
				  </PropertyGroup>
				  <ItemGroup>
				{{item}}
				  </ItemGroup>
				</Project>
				""");
			return projectPath;
		}

		string WriteSolution(string fileName, IReadOnlyCollection<string> projects)
		{
			string solutionPath = Path.Combine(this.workDir, fileName);
			string[] projectEntries = projects.Select((project, index) =>
			{
				string relativePath = Path.GetRelativePath(this.workDir, project).Replace('/', '\\');
				string projectName = Path.GetFileNameWithoutExtension(project);
				string projectGuid = $"{{00000000-0000-0000-0000-{index + 1:D12}}}";
				return $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}\", \"{relativePath}\", \"{projectGuid}\"{Environment.NewLine}EndProject";
			}).ToArray();
			string[] projectConfigurations = projects.SelectMany((_, index) =>
			{
				string projectGuid = $"{{00000000-0000-0000-0000-{index + 1:D12}}}";
				return new[]
				{
					$"        {projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
					$"        {projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU",
				};
			}).ToArray();
			File.WriteAllText(
				solutionPath,
				$"""
				Microsoft Visual Studio Solution File, Format Version 12.00
				# Visual Studio Version 17
				{string.Join(Environment.NewLine, projectEntries)}
				Global
				    GlobalSection(SolutionConfigurationPlatforms) = preSolution
				        Debug|Any CPU = Debug|Any CPU
				    EndGlobalSection
				    GlobalSection(ProjectConfigurationPlatforms) = postSolution
				{string.Join(Environment.NewLine, projectConfigurations)}
				    EndGlobalSection
				EndGlobal
				""");
			return solutionPath;
		}
	}

	[Theory]
	[InlineData(1)]
	[InlineData(4)]
	public async Task ProjectDataBuild_ReceiptProtocol_EvaluationFailuresDoNotDropOtherReferencedProjects(int maxCpuCount)
	{
		string core = this.WriteGraphProject("Core");
		string broken = this.WriteGraphProject("Broken", ["Core"], failDuringProjectDataEvaluation: true);
		string dependent = this.WriteGraphProject("Dependent", ["Broken"]);
		string independent = this.WriteGraphProject("Independent", ["Core"]);
		string tail = this.WriteGraphProject("Tail", ["Independent"]);
		string[] projects = [core, broken, dependent, independent, tail];
		string solutionPath = Path.Combine(this.workDir, $"Graph-{maxCpuCount}.slnx");
		File.WriteAllText(
			solutionPath,
			$"""
			<Solution>
			{string.Join(Environment.NewLine, projects.Select(project => $"  <Project Path=\"{Path.GetRelativePath(this.workDir, project).Replace('/', '\\')}\" />"))}
			</Solution>
			""");
		string receiptDirectory = Path.Combine(this.workDir, $"graph-receipts-{maxCpuCount}");
		string attemptId = Guid.NewGuid().ToString("N");

		ProcessResult result = await RunDotnetMsbuildAsync(
			solutionPath,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				$"/m:{maxCpuCount}",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				$"/p:ProjectDataBuildReceiptDirectory={receiptDirectory}",
				$"/p:ProjectDataBuildReceiptAttemptId={attemptId}",
				GetCompletionLoggerArgument(receiptDirectory, attemptId),
			]);

		Assert.NotEqual(0, result.ExitCode);
		Assert.True(ProjectDataBuildReceipt.TryReadAggregateCompletion(receiptDirectory, attemptId));
		Assert.False(ProjectDataBuildReceipt.TryRead(receiptDirectory, attemptId, broken, out _));
		foreach (string successfulProject in projects.Except([broken], StringComparer.OrdinalIgnoreCase))
		{
			Assert.True(
				ProjectDataBuildReceipt.TryRead(receiptDirectory, attemptId, successfulProject, out _),
				$"Expected completion receipt for {successfulProject}.{Environment.NewLine}{result.Output}");
			Assert.True(File.Exists(successfulProject + ".lscache"), result.Output);
		}

		Assert.True(ProjectDataBuildAttemptManifest.TryRead(receiptDirectory, attemptId, out ProjectDataBuildAttemptManifest manifest));
		Assert.Contains(manifest.Diagnostics, diagnostic =>
			diagnostic.Severity == "Error" &&
			string.Equals(diagnostic.ProjectFilePath, broken, OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task ProjectDataBuild_ReceiptProtocol_LoadsCompletionLoggerFromQuotedDelimiterPath()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);
		string loggerDirectory = Path.Combine(this.workDir, "logger,with;delimiters");
		Directory.CreateDirectory(loggerDirectory);
		string loggerAssemblyPath = CopyAssembly(typeof(ProjectDataBuildCompletionLogger).Assembly.Location, loggerDirectory);
		string receiptDirectory = Path.Combine(this.workDir, "delimiter-receipts");
		string attemptId = Guid.NewGuid().ToString("N");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				$"/p:ProjectDataBuildReceiptDirectory={receiptDirectory}",
				$"/p:ProjectDataBuildReceiptAttemptId={attemptId}",
				GetCompletionLoggerArgument(receiptDirectory, attemptId, loggerAssemblyPath),
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(ProjectDataBuildReceipt.TryReadAggregateCompletion(receiptDirectory, attemptId));
	}

	[Fact]
	public async Task ProjectDataBuild_ReceiptProtocol_UnsupportedProjectWritesMarkerAndCompletion()
	{
		string projectFile = this.WriteProject("FSharpApp.fsproj", multiTargeting: false, extraProperties: "<Language>F#</Language>");
		string receiptDirectory = Path.Combine(this.workDir, "unsupported-receipts");
		string attemptId = Guid.NewGuid().ToString("N");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				$"/p:ProjectDataBuildReceiptDirectory={receiptDirectory}",
				$"/p:ProjectDataBuildReceiptAttemptId={attemptId}",
				GetCompletionLoggerArgument(receiptDirectory, attemptId),
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(ProjectDataBuildReceipt.TryRead(receiptDirectory, attemptId, projectFile, out _));
		AssertUnsupportedMarker(projectFile, "LanguageNotCSharp");
	}

	[Fact]
	public async Task ProjectDataBuild_ReceiptProtocol_CompletionCanProduceNoOutputOrMarker()
	{
		string projectFile = this.WriteProject(
			"App.csproj",
			multiTargeting: false,
			extraXml:
			"""
			  <Target Name="DeleteProjectDataAfterCompletion" AfterTargets="ProjectDataBuild">
			    <Delete Files="$(MSBuildProjectFullPath).lscache" />
			  </Target>
			""");
		string receiptDirectory = Path.Combine(this.workDir, "no-output-receipts");
		string attemptId = Guid.NewGuid().ToString("N");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				$"/p:ProjectDataBuildReceiptDirectory={receiptDirectory}",
				$"/p:ProjectDataBuildReceiptAttemptId={attemptId}",
				GetCompletionLoggerArgument(receiptDirectory, attemptId),
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(ProjectDataBuildReceipt.TryRead(receiptDirectory, attemptId, projectFile, out _));
		Assert.False(File.Exists(projectFile + ".lscache"));
		Assert.False(File.Exists(GetMarkerPath(projectFile)));
	}

	[Fact]
	public async Task SolutionLevel_ForcedProjectDataBuildPreservesMultiTfmSlices()
	{
		string projectA = this.WriteProject("App.csproj", multiTargeting: true);
		string projectBDir = Path.Combine(this.workDir, "Lib");
		Directory.CreateDirectory(projectBDir);
		string projectB = Path.Combine(projectBDir, "Lib.csproj");
		File.WriteAllText(projectB,
			"""
			<Project Sdk="Microsoft.NET.Sdk">
			  <PropertyGroup>
			    <TargetFramework>net8.0</TargetFramework>
			    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
			  </PropertyGroup>
			</Project>
			""");
		this.WriteProjectAssetsFile(projectB, ["net8.0"]);

		string solutionPath = Path.Combine(this.workDir, "Solution.slnx");
		string slnRelA = Path.GetRelativePath(this.workDir, projectA).Replace('/', '\\');
		string slnRelB = Path.GetRelativePath(this.workDir, projectB).Replace('/', '\\');
		File.WriteAllText(solutionPath,
			$$"""
			<Solution>
			  <Project Path="{{slnRelA}}" />
			  <Project Path="{{slnRelB}}" />
			</Solution>
			""");

		string cacheFile = projectA + ".lscache";
		await File.WriteAllTextAsync(cacheFile, "stale-cache-content", TestContext.Current.CancellationToken);
		File.SetLastWriteTimeUtc(cacheFile, DateTime.UtcNow.AddMinutes(5));

		// CI agents are firewalled away from api.nuget.org but have credentialed
		// access to the org-internal feed. Point restore at that feed so it can
		// satisfy the implicit framework-reference downloads
		// (Microsoft.NETCore.App.Ref/.AspNetCore.App.Ref/etc) that the multi-TFM
		// project requires. An empty source folder does NOT work here -- the
		// SDK 10 install only ships the net10 pack, so net8/net9 must come from
		// the feed.
		string restoreSource = "https://pkgs.dev.azure.com/devdiv/DevDiv/_packaging/vs-green/nuget/v3/index.json";

		ProcessResult result = await RunDotnetMsbuildAsync(
			solutionPath,
			extraArgs:
			[
				"/restore",
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				"/p:_ProjectDataBuildForce=true",
				$"/p:RestoreSources={restoreSource}",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(File.Exists(cacheFile), $"Expected lscache for App.csproj.\n{result.Output}");

		string appCacheContent = File.ReadAllText(cacheFile).Replace("\r\n", "\n");
		Assert.DoesNotContain("stale-cache-content", appCacheContent);
		Assert.Equal(2, CountOccurrences(appCacheContent, "\n[sliceDimensions]\n"));
		Assert.Contains("TargetFramework=net8.0", appCacheContent);
		Assert.Contains("TargetFramework=net9.0", appCacheContent);
	}

	[Fact]
	public async Task UnsupportedProject_ProjectDataBuildIsNoOp()
	{
		string projectFile = Path.Combine(this.workDir, "Unsupported.proj");
		File.WriteAllText(projectFile,
			"""
            <Project>
              <Import Project="$(MSBuildToolsPath)\Microsoft.Common.targets" />
            </Project>
            """);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.False(File.Exists(projectFile + ".lscache"), $"Unsupported project should not produce lscache.\n{result.Output}");
		AssertUnsupportedMarker(projectFile, "UsingMicrosoftNETSdkFalse");
	}

	[Fact]
	public async Task ExcludedProject_ProjectDataBuildIsNoOp()
	{
		string projectFile = this.WriteProject(
			"Excluded.csproj",
			multiTargeting: false,
			extraProperties:
			"""
                <ExcludeFromBuild>true</ExcludeFromBuild>
            """);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.False(File.Exists(projectFile + ".lscache"), $"Excluded project should not produce lscache.\n{result.Output}");
		AssertUnsupportedMarker(projectFile, "ExcludeFromBuildTrue");
	}

	[Fact]
	public async Task ProjectDataBuild_NetFrameworkProject_WithoutReferenceAssemblies_PreservesProjectFolderCacheAndWritesMissingReferenceMarker()
	{
		string projectFile = this.WriteProject(
			"NetFramework.csproj",
			multiTargeting: false,
			targetFramework: "net472",
			extraProperties: this.MissingNetFrameworkReferenceAssembliesProperties());
		await File.WriteAllTextAsync(projectFile + ".lscache", "stale", TestContext.Current.CancellationToken);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.DoesNotContain("CollectFrameworkReferences", result.Output);
		Assert.Equal("stale", File.ReadAllText(projectFile + ".lscache"));
		AssertUnsupportedMarker(projectFile, "MissingNetFrameworkReferenceAssemblies");
	}

	[Fact]
	public async Task DirectWriteTarget_NetFrameworkProjectWithoutCommandLineArguments_PreservesProjectFolderCache()
	{
		string projectFile = this.WriteProject(
			"NetFramework.csproj",
			multiTargeting: false,
			targetFramework: "net472",
			extraProperties: $"<FrameworkPathOverride>{Path.Combine(this.workDir, "missing-netfx-refs")}</FrameworkPathOverride>");
		await File.WriteAllTextAsync(projectFile + ".lscache", "stale", TestContext.Current.CancellationToken);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_WriteProjectData",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataOnBuild=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.Equal("stale", File.ReadAllText(projectFile + ".lscache"));
		AssertUnsupportedMarker(projectFile, "MissingNetFrameworkReferenceAssemblies");
	}

	[Fact]
	public async Task ProjectDataBuild_NetFrameworkProject_WithReferenceAssemblies_WritesProjectData()
	{
		string referenceAssemblyDirectory = this.WriteNet472ReferenceAssemblies();
		string projectFile = this.WriteProject(
			"NetFramework.csproj",
			multiTargeting: false,
			targetFramework: "net472",
			extraProperties: $"<FrameworkPathOverride>{referenceAssemblyDirectory}</FrameworkPathOverride>");

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.DoesNotContain("CollectFrameworkReferences", result.Output);
		string cacheFile = projectFile + ".lscache";
		Assert.True(File.Exists(cacheFile), $"Expected .NET Framework project data when reference assemblies are available.\n{result.Output}");
		string content = File.ReadAllText(cacheFile).Replace("\r\n", "\n");
		Assert.DoesNotContain("[netFrameworkReferenceAssemblies]\n", content);
		Assert.Contains("[metadataReferences]\n", content);
		Assert.Contains("<NETFXREF>/v4.7.2/\n", content);
		Assert.Contains(" mscorlib.dll", content);
		Assert.Contains(" System.dll", content);
		Assert.Contains(" System.Core.dll", content);
		Assert.DoesNotContain(referenceAssemblyDirectory.Replace('\\', '/'), content);
		AssertNoUnsupportedMarker(projectFile);
	}

	[Fact]
	public async Task MultiTfmProject_SkipsUnsupportedTargetFrameworkSlices()
	{
		string projectFile = this.WriteProject(
			"Mixed.csproj",
			multiTargeting: true,
			targetFrameworks: "net8.0;net472",
			extraProperties: this.MissingNetFrameworkReferenceAssembliesProperties());
		string staleNet472Slice = Path.Combine(this.workDir, "obj", "Debug", "net472", "Mixed.csproj.slice");
		Directory.CreateDirectory(Path.GetDirectoryName(staleNet472Slice)!);
		await File.WriteAllTextAsync(
			staleNet472Slice,
			"""
            version=2

            [project]
            project=Mixed.csproj
            language=C#

            [sliceDimensions]
            TargetFramework=net472

            [metadataReferences]
            <NETFXREF>/v4.7.2/
            """,
			TestContext.Current.CancellationToken);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		string cacheFile = projectFile + ".lscache";
		Assert.True(File.Exists(cacheFile), $"Expected project data for supported target frameworks.\n{result.Output}");
		string content = File.ReadAllText(cacheFile).Replace("\r\n", "\n");
		Assert.Contains("TargetFramework=net8.0", content);
		Assert.DoesNotContain("TargetFramework=net472", content);
		Assert.DoesNotContain("<NETFXREF>", content);
		Assert.False(File.Exists(staleNet472Slice), $"Unsupported slices should be deleted before merge.\n{result.Output}");
		AssertNoUnsupportedMarker(projectFile);
	}

	[Fact]
	public async Task MultiTfmProject_UnsupportedInnerBuild_PreservesMergedCache()
	{
		string projectFile = this.WriteProject(
			"Mixed.csproj",
			multiTargeting: true,
			targetFrameworks: "net8.0;net472",
			extraProperties: this.MissingNetFrameworkReferenceAssembliesProperties());
		string staleCache = projectFile + ".lscache";
		await File.WriteAllTextAsync(staleCache, "existing merged cache", TestContext.Current.CancellationToken);
		string staleNet472Slice = Path.Combine(this.workDir, "obj", "Debug", "net472", "Mixed.csproj.slice");
		Directory.CreateDirectory(Path.GetDirectoryName(staleNet472Slice)!);
		await File.WriteAllTextAsync(staleNet472Slice, "stale slice", TestContext.Current.CancellationToken);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				"/p:TargetFramework=net472",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(File.Exists(staleCache), $"Unsupported inner target framework builds should not delete the merged project cache.\n{result.Output}");
		Assert.Equal("existing merged cache", File.ReadAllText(staleCache));
		Assert.False(File.Exists(staleNet472Slice), $"Unsupported inner target framework builds should delete their stale slice.\n{result.Output}");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task MultiTfmProject_AllUnsupportedSlices_PreservesStaleCacheAndWritesMarker(bool force)
	{
		string projectFile = this.WriteProject(
			"NetFrameworkOnly.csproj",
			multiTargeting: true,
			targetFrameworks: "net472",
			extraProperties: this.MissingNetFrameworkReferenceAssembliesProperties());
		string staleCache = projectFile + ".lscache";
		await File.WriteAllTextAsync(staleCache, "stale", TestContext.Current.CancellationToken);
		string staleNet472Slice = Path.Combine(this.workDir, "obj", "Debug", "net472", "NetFrameworkOnly.csproj.slice");
		Directory.CreateDirectory(Path.GetDirectoryName(staleNet472Slice)!);
		await File.WriteAllTextAsync(staleNet472Slice, "stale", TestContext.Current.CancellationToken);

		List<string> args =
		[
			"/t:ProjectDataBuild",
			"/p:DesignTimeBuild=true",
			"/p:BuildingProject=false",
			"/p:SkipCompilerExecution=true",
			"/p:EnableProjectDataInProjectFolder=true",
		];
		if (force)
		{
			args.Add("/p:_ProjectDataBuildForce=true");
		}

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs: [.. args]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.False(File.Exists(staleNet472Slice), $"Unsupported slices should be deleted before merge.\n{result.Output}");
		Assert.Equal("stale", File.ReadAllText(staleCache));
		AssertUnsupportedMarker(projectFile, "AllTargetFrameworksUnsupported");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task MultiTfmUserFolderProject_AllUnsupportedSlices_WritesMarker(bool force)
	{
		string projectFile = this.WriteProject(
			"NetFrameworkOnly.csproj",
			multiTargeting: true,
			targetFrameworks: "net472",
			extraProperties: this.MissingNetFrameworkReferenceAssembliesProperties());
		string stampPath = Path.Combine(this.workDir, "obj", "Debug", "NetFrameworkOnly.csproj.lscache.stamp");

		List<string> args =
		[
			"/t:ProjectDataBuild",
			"/p:DesignTimeBuild=true",
			"/p:BuildingProject=false",
			"/p:SkipCompilerExecution=true",
			"/p:EnableProjectDataInProjectFolder=false",
		];
		if (force)
		{
			args.Add("/p:_ProjectDataBuildForce=true");
		}

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs: [.. args]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.False(File.Exists(projectFile + ".lscache"), $"User-folder mode should not write a project-folder cache.\n{result.Output}");
		Assert.True(File.Exists(stampPath), $"Expected user-folder multi-TFM merge stamp at {stampPath}.\n{result.Output}");
		AssertUnsupportedMarker(projectFile, "AllTargetFrameworksUnsupported");
	}

	[Fact]
	public async Task ProjectDataBuild_RestoreSkippedAndAssetsMissing_FailsWithoutWritingCache()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false, writeAssetsFile: false);
		string cacheFile = projectFile + ".lscache";

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.NotEqual(0, result.ExitCode);
		AssertMissingAssetsError(projectFile, result.Output);
		Assert.False(File.Exists(cacheFile), $"ProjectDataBuild should not create {cacheFile} without project.assets.json.\n{result.Output}");
	}

	[Fact]
	public async Task ProjectDataBuild_RestoreSkippedAndAssetsMissing_PreservesExistingCache()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false, writeAssetsFile: false);
		string cacheFile = projectFile + ".lscache";
		await File.WriteAllTextAsync(cacheFile, "existing-cache", TestContext.Current.CancellationToken);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.NotEqual(0, result.ExitCode);
		AssertMissingAssetsError(projectFile, result.Output);
		Assert.Equal("existing-cache", File.ReadAllText(cacheFile));
	}

	[Fact]
	public async Task ProjectDataBuild_StaleRestoreTimestampsWithResolvedInputs_WritesCache()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false);
		string cacheFile = projectFile + ".lscache";
		await File.WriteAllTextAsync(cacheFile, "existing-cache", TestContext.Current.CancellationToken);

		string objDir = Path.Combine(Path.GetDirectoryName(projectFile)!, "obj");
		string assetsFile = Path.Combine(objDir, "project.assets.json");
		string nugetCacheFile = Path.Combine(objDir, "project.nuget.cache");
		string escapedProjectFile = projectFile.Replace("\\", "\\\\");
		await File.WriteAllTextAsync(
			nugetCacheFile,
			$$"""{"version":2,"success":true,"projectFilePath":"{{escapedProjectFile}}"}""",
			TestContext.Current.CancellationToken);

		DateTime projectTimeUtc = DateTime.UtcNow.AddMinutes(-5);
		File.SetLastWriteTimeUtc(assetsFile, projectTimeUtc.AddMinutes(-5));
		File.SetLastWriteTimeUtc(projectFile, projectTimeUtc);
		File.SetLastWriteTimeUtc(nugetCacheFile, projectTimeUtc.AddMinutes(-5));

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(File.Exists(cacheFile), $"ProjectDataBuild should write {cacheFile} when assets/imports/resolved references are available.\n{result.Output}");
		Assert.NotEqual("existing-cache", File.ReadAllText(cacheFile));
	}

	[Fact]
	public async Task ProjectDataBuild_RestoreAndProjectDataBuildSameEvaluation_FailsWithoutWritingCache()
	{
		string projectFile = this.WriteProject("App.csproj", multiTargeting: false, targetFramework: "net11.0", writeAssetsFile: false);
		string cacheFile = projectFile + ".lscache";
		string restoreSource = Path.Combine(this.workDir, "empty-restore-source");
		Directory.CreateDirectory(restoreSource);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:Restore;ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				$"/p:RestoreSources={restoreSource}",
			]);

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("were not imported into the current project evaluation", result.Output);
		Assert.Contains("Do not run Restore and ProjectDataBuild in the same MSBuild evaluation", result.Output);
		Assert.False(File.Exists(cacheFile), $"ProjectDataBuild should not create {cacheFile} when Restore ran in the same evaluation.\n{result.Output}");
	}

	[Fact]
	public async Task ProjectDataBuild_NoMetadataReferencesResolved_WithoutNoStdLib_FailsWithoutWritingCache()
	{
		// Force `@(ReferencePathWithRefAssemblies)` to be empty by construction so the
		// `_ValidateProjectDataMetadataReferences` target fires deterministically, regardless
		// of which SDKs/targeting packs happen to be installed on the host machine. Using an
		// "uninstalled TFM" (e.g. net7.0) would be brittle: it would silently regress the day
		// someone installs that SDK, and it would exercise the SDK's unknown-framework error
		// path rather than our validator.
		string projectFile = this.WriteProject(
			"NoRefs.csproj",
			multiTargeting: false,
			targetFramework: "net10.0",
			extraXml:
			"""
              <Target Name="ProjectDataTestRemoveMetadataReferences"
                      DependsOnTargets="_ProjectDataTestInjectStubReference"
                      BeforeTargets="_ValidateProjectDataMetadataReferences">
                <PropertyGroup>
                  <NoStdLib>false</NoStdLib>
                </PropertyGroup>
                <ItemGroup>
                  <ReferencePathWithRefAssemblies Remove="@(ReferencePathWithRefAssemblies)" />
                </ItemGroup>
              </Target>
            """);
		string cacheFile = projectFile + ".lscache";

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("no metadata references were resolved", result.Output);
		Assert.Contains(projectFile, result.Output);
		Assert.False(File.Exists(cacheFile), $"ProjectDataBuild should not create {cacheFile} when no metadata references resolved.\n{result.Output}");
	}

	[Fact]
	public async Task ProjectDataBuild_NoMetadataReferencesResolved_WithoutNoStdLib_PreservesExistingCache()
	{
		string projectFile = this.WriteProject(
			"NoRefs.csproj",
			multiTargeting: false,
			targetFramework: "net10.0",
			extraXml:
			"""
              <Target Name="ProjectDataTestRemoveMetadataReferences"
                      DependsOnTargets="_ProjectDataTestInjectStubReference"
                      BeforeTargets="_ValidateProjectDataMetadataReferences">
                <PropertyGroup>
                  <NoStdLib>false</NoStdLib>
                </PropertyGroup>
                <ItemGroup>
                  <ReferencePathWithRefAssemblies Remove="@(ReferencePathWithRefAssemblies)" />
                </ItemGroup>
              </Target>
            """);
		string cacheFile = projectFile + ".lscache";
		await File.WriteAllTextAsync(cacheFile, "existing-cache", TestContext.Current.CancellationToken);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("no metadata references were resolved", result.Output);
		Assert.Equal("existing-cache", File.ReadAllText(cacheFile));
	}

	[Fact]
	public async Task ProjectDataBuild_NoStdLibWithoutMetadataReferences_WritesCache()
	{
		string projectFile = this.WriteProject(
			"NoStdLibCoreLib.csproj",
			multiTargeting: false,
			targetFramework: "net10.0",
			extraProperties:
			"""
			    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
			    <NoStdLib>true</NoStdLib>
			""");
		string cacheFile = projectFile + ".lscache";

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(File.Exists(cacheFile), $"NoStdLib projects with compiler command-line data should still produce project data.\n{result.Output}");
		string content = File.ReadAllText(cacheFile).Replace("\r\n", "\n");
		Assert.Contains("[commandLineArguments]\n", content);
		Assert.Contains("[metadataReferences]\n", content);
		AssertNoUnsupportedMarker(projectFile);
	}

	[Fact]
	public async Task ProjectDataBuild_ProjectReferenceOnlyMetadataReferences_WithoutNoStdLib_PreservesExistingCache()
	{
		string libraryFile = this.WriteProject("Core.csproj", multiTargeting: false);
		string projectFile = this.WriteProject(
			"App.csproj",
			multiTargeting: false,
			extraXml:
			$$"""
              <ItemGroup>
                <ProjectReference Include="{{libraryFile}}" />
              </ItemGroup>
              <Target Name="ProjectDataTestInjectProjectReferenceMetadata"
                      DependsOnTargets="_ProjectDataTestInjectStubReference"
                      BeforeTargets="_ValidateProjectDataMetadataReferences">
                <PropertyGroup>
                  <NoStdLib>false</NoStdLib>
                </PropertyGroup>
                <ItemGroup>
                  <ReferencePathWithRefAssemblies Remove="@(ReferencePathWithRefAssemblies)" />
                  <ReferencePathWithRefAssemblies Include="{{Path.Combine(this.workDir, "Core.dll")}}">
                    <ReferenceSourceTarget>ProjectReference</ReferenceSourceTarget>
                  </ReferencePathWithRefAssemblies>
                </ItemGroup>
              </Target>
            """);
		string cacheFile = projectFile + ".lscache";
		await File.WriteAllTextAsync(cacheFile, "existing-cache", TestContext.Current.CancellationToken);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("no framework reference assemblies were resolved", result.Output);
		Assert.Equal("existing-cache", File.ReadAllText(cacheFile));
	}

	[Fact]
	public async Task ProjectDataBuild_NoStdLibProjectReferenceMetadataReferences_WritesCache()
	{
		string libraryFile = this.WriteProject("Core.csproj", multiTargeting: false);
		string projectReferenceAssembly = Path.Combine(this.workDir, "artifacts", "bin", "System.Runtime", "ref", "Debug", "net10.0", "System.Runtime.dll");
		Directory.CreateDirectory(Path.GetDirectoryName(projectReferenceAssembly)!);
		await File.WriteAllTextAsync(projectReferenceAssembly, string.Empty, TestContext.Current.CancellationToken);
		string projectFile = this.WriteProject(
			"App.csproj",
			multiTargeting: false,
			targetFramework: "net10.0",
			extraProperties:
			"""
			    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
			    <NoStdLib>true</NoStdLib>
			""",
			extraXml:
			$$"""
              <ItemGroup>
                <ProjectReference Include="{{libraryFile}}" />
              </ItemGroup>
              <Target Name="ProjectDataTestInjectProjectReferenceMetadata"
                      BeforeTargets="_ValidateProjectDataMetadataReferences">
                <ItemGroup>
                  <ReferencePathWithRefAssemblies Include="{{projectReferenceAssembly}}">
                    <ReferenceSourceTarget>ProjectReference</ReferenceSourceTarget>
                  </ReferencePathWithRefAssemblies>
                </ItemGroup>
              </Target>
            """);
		string cacheFile = projectFile + ".lscache";

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(File.Exists(cacheFile), $"NoStdLib projects should accept project-reference metadata references without FrameworkReferenceName metadata.\n{result.Output}");
		string content = File.ReadAllText(cacheFile).Replace("\r\n", "\n");
		Assert.Contains("[metadataReferences]\n", content);
		Assert.Contains("System.Runtime.dll", content);
		AssertNoUnsupportedMarker(projectFile);
	}

	[Fact]
	public async Task ValidateProjectDataMetadataReferences_NetStandardReferenceWithoutFrameworkReferenceName_Succeeds()
	{
		string netstandardReference = Path.Combine(this.workDir, "netstandard.dll");
		string projectFile = this.WriteProject(
			"NetStandardApp.csproj",
			multiTargeting: false,
			targetFramework: "netstandard2.0",
			extraProperties:
			"""
			    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
			    <NoStdLib>true</NoStdLib>
			""",
			extraXml:
			$$"""
              <Target Name="ProjectDataTestInjectNetStandardMetadata"
                      BeforeTargets="_ValidateProjectDataMetadataReferences">
                <ItemGroup>
                  <ReferencePathWithRefAssemblies Include="{{netstandardReference}}" />
                </ItemGroup>
              </Target>
            """);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_ValidateProjectDataMetadataReferences",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public async Task ProjectDataBuild_AnalyzerPolicy_NeverHasOrphanLines(bool enableNetAnalyzers, bool enforceCodeStyleInBuild)
	{
		// Writer regression guard for the
		// "Gate [sdkAnalyzerConfigPolicy] lines on the SDK's analyzer-pack
		// property gates" fix. Exercises the full toolchain (targets → writer →
		// cache file) with every combination of the two gating properties and
		// asserts the output never contains a policy line for an analyzer pack
		// that has no DLLs in [analyzerReferences]. This is robust to *how* a
		// future regression might happen — at the targets layer, the writer
		// layer, a new policy type, or a new caller — because it asserts an
		// invariant on the *output* rather than the writer's internal behavior.
		string projectFile = this.WriteProject(
			"AnalyzerPolicy.csproj",
			multiTargeting: false,
			targetFramework: "net10.0",
			extraProperties:
			$"""
			    <EnableNETAnalyzers>{(enableNetAnalyzers ? "true" : "false")}</EnableNETAnalyzers>
			    <EnforceCodeStyleInBuild>{(enforceCodeStyleInBuild ? "true" : "false")}</EnforceCodeStyleInBuild>
			""");
		string cacheFile = projectFile + ".lscache";

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(File.Exists(cacheFile), $"Expected {cacheFile} to exist.\n{result.Output}");

		string content = File.ReadAllText(cacheFile);
		LscacheInvariants.AssertNoOrphanAnalyzerPolicyLines(content);
	}

	[Fact]
	public async Task ProjectDataBuild_RestoreSkippedAndPackageFilesMissing_FailsWithoutWritingCache()
	{
		const string packageId = "PackageApp.Dependency";
		const string packageVersion = "1.0.0";
		string projectFile = this.WriteProject(
			"PackageApp.csproj",
			multiTargeting: false,
			extraXml:
			$$"""
              <ItemGroup>
                <PackageReference Include="{{packageId}}" Version="{{packageVersion}}" />
              </ItemGroup>
            """,
			writeAssetsFile: false);
		string packagesPath = Path.Combine(this.workDir, "packages");
		this.WriteProjectAssetsFileWithPackage(projectFile, packageId, packageVersion, "net8.0", packagesPath);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
				"/p:ContinueOnError=true",
				$"/p:RestorePackagesPath={packagesPath}",
			]);

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("restore graph does not contain declared PackageReference items", result.Output);
		Assert.Contains(packageId, result.Output);
		Assert.False(File.Exists(projectFile + ".lscache"), $"ProjectDataBuild should not create a cache when restored package files are missing.\n{result.Output}");
	}

	[Fact]
	public async Task ProjectDataBuild_FloatingPackageVersionUsesConcreteRestoredPackageFolder()
	{
		const string packageId = "Floating.Package";
		const string requestedVersion = "11.0.0-preview.6.*";
		const string resolvedVersion = "11.0.0-preview.6.26359.118";
		const string targetFramework = "net8.0";
		string projectFile = this.WriteProject(
			"FloatingPackage.csproj",
			multiTargeting: false,
			extraXml:
			$$"""
              <ItemGroup>
                <PackageReference Include="{{packageId}}" Version="{{requestedVersion}}" />
              </ItemGroup>
            """,
			writeAssetsFile: false);
		string packagesPath = Path.Combine(this.workDir, "packages");
		string packagePath = Path.Combine(packagesPath, packageId.ToLowerInvariant(), resolvedVersion);
		string packageAssetPath = Path.Combine(packagePath, "lib", targetFramework, packageId + ".dll");
		Directory.CreateDirectory(Path.GetDirectoryName(packageAssetPath)!);
		await File.WriteAllBytesAsync(packageAssetPath, [], TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(
			Path.Combine(packagePath, packageId.ToLowerInvariant() + ".nuspec"),
			"<package><metadata><id>Floating.Package</id><version>11.0.0-preview.6.26359.118</version><authors>Test</authors><description>Test</description></metadata></package>",
			TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(
			Path.Combine(packagePath, $"{packageId.ToLowerInvariant()}.{resolvedVersion}.nupkg.sha512"),
			string.Empty,
			TestContext.Current.CancellationToken);
		this.WriteProjectAssetsFileWithPackage(
			projectFile,
			packageId,
			resolvedVersion,
			targetFramework,
			packagesPath,
			requestedVersion);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:ProjectDataBuild",
				"/p:DesignTimeBuild=true",
				"/p:BuildingProject=false",
				"/p:SkipCompilerExecution=true",
				"/p:EnableProjectDataInProjectFolder=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.True(File.Exists(projectFile + ".lscache"), $"ProjectDataBuild should use the concrete package folder selected by restore.\n{result.Output}");
	}

	[Fact]
	public async Task ValidateProjectDataResolvedPackages_MatchesCentrallyManagedReferenceWithoutVersionMetadata()
	{
		const string packageId = "Central.Package";
		const string resolvedVersion = "2.0.0";
		string packagePath = Path.Combine(this.workDir, "packages", packageId.ToLowerInvariant(), resolvedVersion);
		Directory.CreateDirectory(packagePath);
		string projectFile = this.WriteProject(
			"CentralPackage.csproj",
			multiTargeting: false,
			extraXml:
			$$"""
              <ItemGroup>
                <PackageReference Include="{{packageId}}" />
                <PackageVersion Include="{{packageId}}" Version="{{resolvedVersion}}" />
              </ItemGroup>

              <Target Name="ProjectDataTestInjectResolvedPackage"
                      BeforeTargets="_ValidateProjectDataResolvedPackages">
                <ItemGroup>
                  <_PackageDependenciesDesignTime Include="{{packageId}}/{{resolvedVersion}}">
                    <Name>{{packageId}}</Name>
                    <Version>{{resolvedVersion}}</Version>
                    <Path>{{packagePath}}</Path>
                  </_PackageDependenciesDesignTime>
                </ItemGroup>
              </Target>
            """,
			extraProperties: "<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>",
			writeAssetsFile: false);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_ValidateProjectDataResolvedPackages",
				"/p:_ProjectDataCanWriteOutput=true",
			]);

		Assert.True(result.ExitCode == 0, result.Output);
	}

	[Fact]
	public async Task ValidateProjectDataResolvedPackages_ReportsVersionChangeMissingFromRestoreGraph()
	{
		const string packageId = "Stale.Package";
		const string requestedVersion = "12.0.3";
		const string resolvedVersion = "13.0.1";
		const string targetFramework = "net8.0";
		string projectFile = this.WriteProject(
			"StalePackageVersion.csproj",
			multiTargeting: false,
			extraXml:
			$$"""
              <ItemGroup>
                <PackageReference Include="{{packageId}}" Version="{{requestedVersion}}" />
              </ItemGroup>
            """,
			writeAssetsFile: false);
		string packagesPath = Path.Combine(this.workDir, "packages");
		string packagePath = Path.Combine(packagesPath, packageId.ToLowerInvariant(), resolvedVersion);
		string packageAssetPath = Path.Combine(packagePath, "lib", targetFramework, packageId + ".dll");
		Directory.CreateDirectory(Path.GetDirectoryName(packageAssetPath)!);
		await File.WriteAllBytesAsync(packageAssetPath, [], TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(
			Path.Combine(packagePath, packageId.ToLowerInvariant() + ".nuspec"),
			"<package><metadata><id>Stale.Package</id><version>13.0.1</version><authors>Test</authors><description>Test</description></metadata></package>",
			TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(
			Path.Combine(packagePath, $"{packageId.ToLowerInvariant()}.{resolvedVersion}.nupkg.sha512"),
			string.Empty,
			TestContext.Current.CancellationToken);
		this.WriteProjectAssetsFileWithPackage(projectFile, packageId, resolvedVersion, targetFramework, packagesPath);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_ValidateProjectDataAssetsFile;ResolvePackageAssets;_ValidateProjectDataResolvedPackages",
				"/p:_ProjectDataCanWriteOutput=true",
			]);

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("declared PackageReference requests differ from the restore graph", result.Output);
		Assert.Contains(packageId, result.Output);
		Assert.Contains($"current request '{requestedVersion}'", result.Output);
		Assert.Contains("restored request", result.Output);
	}

	[Theory]
	[InlineData("runtime", "compile")]
	[InlineData("build", "runtime")]
	public async Task ValidateProjectDataResolvedPackages_ReportsAssetSelectionChangeMissingFromRestoreGraph(
		string restoredExcludeAssets,
		string currentExcludeAssets)
	{
		const string packageId = "Microsoft.Build.Utilities.Core";
		const string packageVersion = "17.14.0-preview-25119-36";

		string WriteFilteredProject(string excludeAssets)
			=> this.WriteProject(
				"StalePackageAssets.csproj",
				multiTargeting: false,
				targetFramework: "net9.0",
				extraXml:
				$$"""
			      <ItemGroup>
			        <PackageReference Include="{{packageId}}" Version="{{packageVersion}}" ExcludeAssets="{{excludeAssets}}" />
			      </ItemGroup>
			    """,
				writeAssetsFile: false);

		string projectFile = WriteFilteredProject(restoredExcludeAssets);
		ProcessResult restoreResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:Restore",
				$"/p:RestoreConfigFile={Path.Combine(FindRepoRoot(), "NuGet.config")}",
			]);
		Assert.True(restoreResult.ExitCode == 0, restoreResult.Output);

		ProcessResult matchingResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_ValidateProjectDataAssetsFile;ResolvePackageAssets;_ValidateProjectDataResolvedPackages",
				"/p:_ProjectDataCanWriteOutput=true",
			]);
		Assert.True(matchingResult.ExitCode == 0, matchingResult.Output);

		WriteFilteredProject(currentExcludeAssets);
		ProcessResult staleResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_ValidateProjectDataAssetsFile;ResolvePackageAssets;_ValidateProjectDataResolvedPackages",
				"/p:_ProjectDataCanWriteOutput=true",
			]);

		Assert.NotEqual(0, staleResult.ExitCode);
		Assert.Contains("declared PackageReference requests differ from the restore graph", staleResult.Output);
		Assert.Contains("current assets", staleResult.Output);
		Assert.Contains(packageId, staleResult.Output);
	}

	[Fact]
	public async Task ValidateProjectDataResolvedPackages_ReportsActiveTransitiveCentralPinChangeMissingFromRestoreGraph()
	{
		const string directPackageId = "Microsoft.Build.Utilities.Core";
		const string directPackageVersion = "17.14.0-preview-25119-36";
		const string transitivePackageId = "Microsoft.NET.StringTools";
		const string restoredPinVersion = "18.9.11";
		const string currentPinVersion = "18.4.0";
		const string targetFramework = "net9.0";

		string WritePinnedProject(string pinVersion)
		{
			File.WriteAllText(
				Path.Combine(this.workDir, "Directory.Packages.props"),
				$$"""
				<Project>
				  <PropertyGroup>
				    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
				    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
				  </PropertyGroup>
				  <ItemGroup>
				    <PackageVersion Include="{{directPackageId}}" Version="{{directPackageVersion}}" />
				    <PackageVersion Include="{{transitivePackageId}}" Version="{{pinVersion}}" />
				  </ItemGroup>
				</Project>
				""");
			return this.WriteProject(
				"TransitivePin.csproj",
				multiTargeting: false,
				targetFramework: targetFramework,
				extraXml:
				$$"""
			      <ItemGroup>
			        <PackageReference Include="{{directPackageId}}" />
			      </ItemGroup>
			    """,
				writeAssetsFile: false);
		}

		string projectFile = WritePinnedProject(restoredPinVersion);
		ProcessResult restoreResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:Restore",
				$"/p:RestoreConfigFile={Path.Combine(FindRepoRoot(), "NuGet.config")}",
			]);
		Assert.True(restoreResult.ExitCode == 0, restoreResult.Output);

		ProcessResult currentResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_ValidateProjectDataAssetsFile;ResolvePackageAssets;_ValidateProjectDataResolvedPackages",
				"/p:_ProjectDataCanWriteOutput=true",
			]);
		Assert.True(currentResult.ExitCode == 0, currentResult.Output);

		WritePinnedProject(currentPinVersion);
		ProcessResult staleResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_ValidateProjectDataAssetsFile;ResolvePackageAssets;_ValidateProjectDataResolvedPackages",
				"/p:_ProjectDataCanWriteOutput=true",
			]);

		Assert.NotEqual(0, staleResult.ExitCode);
		Assert.Contains("central transitive package version requests differ from the restore graph", staleResult.Output);
		Assert.Contains(transitivePackageId, staleResult.Output);
		Assert.Contains($"current request '{currentPinVersion}'", staleResult.Output);
		Assert.Contains($"restored request '[{restoredPinVersion}, )'", staleResult.Output);
	}

	[Theory]
	[InlineData(false, false, true)]
	[InlineData(true, true, true)]
	[InlineData(false, true, false)]
	[InlineData(true, false, false)]
	public async Task ValidateProjectDataResolvedPackages_ValidatesCentralTransitivePinningModeAgainstRestoreGraph(
		bool restoredPinningEnabled,
		bool currentPinningEnabled,
		bool expectedSuccess)
	{
		const string directPackageId = "Microsoft.Build.Utilities.Core";
		const string directPackageVersion = "17.14.0-preview-25119-36";
		const string transitivePackageId = "Microsoft.NET.StringTools";
		const string pinVersion = "18.9.11";
		const string targetFramework = "net9.0";

		string WritePinnedProject(bool pinningEnabled)
		{
			File.WriteAllText(
				Path.Combine(this.workDir, "Directory.Packages.props"),
				$$"""
				<Project>
				  <PropertyGroup>
				    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
				    <CentralPackageTransitivePinningEnabled>{{pinningEnabled.ToString().ToLowerInvariant()}}</CentralPackageTransitivePinningEnabled>
				  </PropertyGroup>
				  <ItemGroup>
				    <PackageVersion Include="{{directPackageId}}" Version="{{directPackageVersion}}" />
				    <PackageVersion Include="{{transitivePackageId}}" Version="{{pinVersion}}" />
				  </ItemGroup>
				</Project>
				""");
			return this.WriteProject(
				"TransitivePinMode.csproj",
				multiTargeting: false,
				targetFramework: targetFramework,
				extraXml:
				$$"""
			      <ItemGroup>
			        <PackageReference Include="{{directPackageId}}" />
			      </ItemGroup>
			    """,
				writeAssetsFile: false);
		}

		string projectFile = WritePinnedProject(restoredPinningEnabled);
		ProcessResult restoreResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:Restore",
				$"/p:RestoreConfigFile={Path.Combine(FindRepoRoot(), "NuGet.config")}",
			]);
		Assert.True(restoreResult.ExitCode == 0, restoreResult.Output);

		WritePinnedProject(currentPinningEnabled);
		ProcessResult staleResult = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_ValidateProjectDataAssetsFile;ResolvePackageAssets;_ValidateProjectDataResolvedPackages",
				"/p:_ProjectDataCanWriteOutput=true",
			]);

		if (expectedSuccess)
		{
			Assert.True(staleResult.ExitCode == 0, staleResult.Output);
			Assert.DoesNotContain("central transitive package pinning mode differs from the restore graph", staleResult.Output);
		}
		else
		{
			Assert.NotEqual(0, staleResult.ExitCode);
			Assert.Contains("central transitive package pinning mode differs from the restore graph", staleResult.Output);
			Assert.Contains($"current '{currentPinningEnabled}'", staleResult.Output, StringComparison.OrdinalIgnoreCase);
			Assert.Contains($"restored '{restoredPinningEnabled}'", staleResult.Output, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public async Task ValidateProjectDataResolvedPackages_StillValidatesImplicitPackageDependencyPaths()
	{
		string missingPackageAsset = Path.Combine(this.workDir, "missing-packages", "implicit.dependency", "1.0.0", "lib", "net8.0", "Implicit.Dependency.dll");
		string projectFile = this.WriteProject(
			"ImplicitResolvedPackage.csproj",
			multiTargeting: false,
			extraXml:
			$$"""
              <ItemGroup>
                <PackageReference Include="Implicit.Dependency" Version="1.0.0" IsImplicitlyDefined="true" />
              </ItemGroup>

              <Target Name="ProjectDataTestInjectImplicitResolvedPackage"
                      BeforeTargets="_ValidateProjectDataResolvedPackages">
                <ItemGroup>
                  <_PackageDependenciesDesignTime Include="Implicit.Dependency/1.0.0">
                    <Path>{{missingPackageAsset}}</Path>
                  </_PackageDependenciesDesignTime>
                </ItemGroup>
              </Target>
            """,
			writeAssetsFile: false);

		ProcessResult result = await RunDotnetMsbuildAsync(
			projectFile,
			extraArgs:
			[
				"/t:_ValidateProjectDataResolvedPackages",
				"/p:_ProjectDataCanWriteOutput=true",
			]);

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("package files are missing", result.Output);
		Assert.Contains("Implicit.Dependency/1.0.0", result.Output);
	}

	[Fact]
	public async Task RunProcessWithTimeoutAsync_TerminatesHungProcessInsteadOfHanging()
	{
		// Regression test for the macOS CI job that hung for 180 minutes: a spawned `dotnet msbuild`
		// child never exited, and the old harness awaited it unbounded. The harness must now bound the
		// wait, kill the whole process tree, and surface a TimeoutException with the captured output.
		ProcessStartInfo psi = CreateLongRunningProcess();
		var timeout = TimeSpan.FromSeconds(1);

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		TimeoutException ex = await Assert.ThrowsAsync<TimeoutException>(
			() => RunProcessWithTimeoutAsync(psi, timeout, "hung-test-process"));
		stopwatch.Stop();

		// It should fail promptly (well under the process' own ~5 minute lifetime), not hang.
		Assert.True(
			stopwatch.Elapsed < TimeSpan.FromMinutes(1),
			$"Expected the hung process to be terminated promptly but it took {stopwatch.Elapsed}.");
		Assert.Contains("did not complete within", ex.Message);
	}

	private static ProcessStartInfo CreateLongRunningProcess()
	{
		// A portable process that blocks for far longer than the test's timeout on every CI OS.
		ProcessStartInfo psi = OperatingSystem.IsWindows()
			? new ProcessStartInfo("cmd.exe", "/c \"ping 127.0.0.1 -n 300 > NUL\"")
			: new ProcessStartInfo("/bin/sh", "-c \"sleep 300\"");
		psi.RedirectStandardOutput = true;
		psi.RedirectStandardError = true;
		psi.UseShellExecute = false;
		psi.CreateNoWindow = true;
		return psi;
	}

	private string WriteProject(
		string fileName,
		bool multiTargeting,
		string? targetFramework = null,
		string? targetFrameworks = null,
		string? runtimeIdentifier = null,
		string? extraProperties = null,
		string? extraXml = null,
		bool writeAssetsFile = true)
	{
		string projectFile = Path.Combine(this.workDir, fileName);
		string resolvedTargetFrameworks = targetFrameworks ?? "net8.0;net9.0";
		string[] projectTargetFrameworks = multiTargeting ? resolvedTargetFrameworks.Split(';') : [targetFramework ?? "net8.0"];
		string tfm = multiTargeting
			? $"<TargetFrameworks>{resolvedTargetFrameworks}</TargetFrameworks>"
			: $"<TargetFramework>{targetFramework ?? "net8.0"}</TargetFramework>";
		string runtimeIdentifierProperty = runtimeIdentifier is null ? string.Empty : $"<RuntimeIdentifier>{runtimeIdentifier}</RuntimeIdentifier>";

		// Inject a stub framework-reference-shaped `@(ReferencePathWithRefAssemblies)`
		// item so the production `_ValidateProjectDataMetadataReferences` target
		// passes during synthetic unit tests.
		//
		// These smoke tests use a hand-crafted `project.assets.json` with empty
		// `packageFolders` and never run a real Restore, so RAR has nothing to
		// resolve from. That's an artefact of the hermetic test fixture, not a
		// real broken build — none of these tests assert on `[metadataReferences]`
		// content; they care about target dispatch, merge behaviour, slice
		// management, etc. The stub is the cheapest way to feed the validator
		// without standing up a network-dependent restore.
		//
		// The stub does NOT defeat the validator's purpose in production: the
		// injection lives in the synthetic .csproj produced here, not in the
		// targets file. Real projects continue to be validated normally.
		//
		// Two important escape hatches:
		//
		//   * `Condition="'$(DisableImplicitFrameworkReferences)' != 'true'"`
		//     lets the dedicated regression tests
		//     (`ProjectDataBuild_NoMetadataReferencesResolved_*`) force-empty
		//     `@(ReferencePathWithRefAssemblies)` by setting that property — the
		//     stub naturally opts out and the validator fires as those tests
		//     require.
		//
		//   * `BeforeTargets="_ValidateProjectDataMetadataReferences"` is enough:
		//     the stub item is in scope for the writer task too and gets written into `[metadataReferences]`,
		//     but for `.NETFramework` slices the writer's own
		//     `TryValidateNetFrameworkReferences` requires a *canonical* ref
		//     assembly (mscorlib/NETFXREF-shaped), which the stub is not, so the
		//     graceful `MissingNetFrameworkReferenceAssemblies` skip path still
		//     fires for net472 inner builds where the targeting pack is absent.
		string stubReferenceAssembly = Path.Combine(this.workDir, "stub-reference.dll");
		if (!File.Exists(stubReferenceAssembly))
		{
			File.WriteAllBytes(stubReferenceAssembly, []);
		}

		File.WriteAllText(projectFile,
$@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    {tfm}
    {runtimeIdentifierProperty}
        <OutputType>Exe</OutputType>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    {extraProperties}
  </PropertyGroup>
  {extraXml}
  <Target Name=""_ProjectDataTestInjectStubReference""
          BeforeTargets=""_ValidateProjectDataMetadataReferences""
          Condition=""'$(DisableImplicitFrameworkReferences)' != 'true'"">
    <ItemGroup>
      <ReferencePathWithRefAssemblies Include=""{stubReferenceAssembly}"">
        <FrameworkReferenceName>Microsoft.NETCore.App</FrameworkReferenceName>
      </ReferencePathWithRefAssemblies>
    </ItemGroup>
  </Target>
</Project>");

		if (writeAssetsFile)
		{
			this.WriteProjectAssetsFile(projectFile, projectTargetFrameworks, runtimeIdentifier);
		}

		return projectFile;
	}

	private string WriteGraphProject(string name, string[]? references = null, bool failDuringProjectDataEvaluation = false)
	{
		string directory = Path.Combine(this.workDir, name);
		Directory.CreateDirectory(directory);
		string projectPath = Path.Combine(directory, $"{name}.csproj");
		string projectReferences = string.Join(
			Environment.NewLine,
			(references ?? []).Select(reference => $"""    <ProjectReference Include="..\{reference}\{reference}.csproj" />"""));
		string failureProperty = failDuringProjectDataEvaluation
			? """    <ReceiptGraphEvaluationFailure Condition="'$(ProjectDataBuildReceiptAttemptId)' != ''">$([System.String]::MissingProjectDataMethod())</ReceiptGraphEvaluationFailure>"""
			: string.Empty;
		File.WriteAllText(
			projectPath,
			$$"""
			<Project Sdk="Microsoft.NET.Sdk">
			  <PropertyGroup>
			    <TargetFramework>net11.0</TargetFramework>
			    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
			{{failureProperty}}
			  </PropertyGroup>
			  <ItemGroup>
			{{projectReferences}}
			  </ItemGroup>
			</Project>
			""");
		this.WriteProjectAssetsFile(projectPath, ["net11.0"]);
		return projectPath;
	}

	private void WriteProjectAssetsFile(string projectFile, IReadOnlyList<string> targetFrameworks, string? runtimeIdentifier = null)
	{
		string objDir = Path.Combine(Path.GetDirectoryName(projectFile)!, "obj");
		Directory.CreateDirectory(objDir);
		string assetsFile = Path.Combine(objDir, "project.assets.json");
		string projectName = Path.GetFileNameWithoutExtension(projectFile);
		string escapedProjectFile = projectFile.Replace("\\", "\\\\");
		string escapedObjDir = objDir.Replace("\\", "\\\\") + "\\\\";
		IEnumerable<string> targetGraphs = runtimeIdentifier is null
			? targetFrameworks
			: targetFrameworks.Concat(targetFrameworks.Select(tfm => $"{tfm}/{runtimeIdentifier}"));
		string targets = string.Join(",\n    ", targetGraphs.Select(tfm => $"\"{tfm}\": {{}}"));
		string dependencyGroups = string.Join(",\n    ", targetFrameworks.Select(tfm => $"\"{tfm}\": []"));
		string restoreFrameworks = string.Join(",\n        ", targetFrameworks.Select(tfm => $"\"{tfm}\": {{ \"targetAlias\": \"{tfm}\", \"projectReferences\": {{}} }}"));
		string projectFrameworks = string.Join(",\n      ", targetFrameworks.Select(tfm => $"\"{tfm}\": {{ \"targetAlias\": \"{tfm}\" }}"));
		string projectRuntimes = runtimeIdentifier is null
			? string.Empty
			: $$"""
                "runtimes": {
                  "{{runtimeIdentifier}}": {
                    "#import": []
                  }
                },
              """;

		File.WriteAllText(
			assetsFile,
			$$"""
            {
              "version": 3,
              "targets": {
                {{targets}}
              },
              "libraries": {},
              "projectFileDependencyGroups": {
                {{dependencyGroups}}
              },
              "packageFolders": {},
              "project": {
                "version": "1.0.0",
                "restore": {
                  "projectUniqueName": "{{escapedProjectFile}}",
                  "projectName": "{{projectName}}",
                  "projectPath": "{{escapedProjectFile}}",
                  "packagesPath": "",
                  "outputPath": "{{escapedObjDir}}",
                  "projectStyle": "PackageReference",
                  "configFilePaths": [],
                  "originalTargetFrameworks": [{{string.Join(", ", targetFrameworks.Select(tfm => $"\"{tfm}\""))}}],
                  "sources": {},
                  "frameworks": {
                    {{restoreFrameworks}}
                  },
                  {{projectRuntimes}}
                  "warningProperties": {
                    "warnAsError": []
                  }
                },
                "frameworks": {
                  {{projectFrameworks}}
                }
              }
            }
            """);

		this.WriteNuGetGeneratedImports(projectFile);
	}

	private void WriteProjectAssetsFileWithPackage(
		string projectFile,
		string packageId,
		string packageVersion,
		string targetFramework,
		string packagesPath,
		string? requestedVersion = null)
	{
		string objDir = Path.Combine(Path.GetDirectoryName(projectFile)!, "obj");
		Directory.CreateDirectory(objDir);
		string assetsFile = Path.Combine(objDir, "project.assets.json");
		string projectName = Path.GetFileNameWithoutExtension(projectFile);
		string escapedProjectFile = projectFile.Replace("\\", "\\\\");
		string escapedObjDir = objDir.Replace("\\", "\\\\") + "\\\\";
		string escapedPackagesPath = (packagesPath + Path.DirectorySeparatorChar).Replace("\\", "\\\\");
		string packageAsset = $"lib/{targetFramework}/{packageId}.dll";
		string packageLibrary = $"{packageId}/{packageVersion}";
		string packagePath = $"{packageId.ToLowerInvariant()}/{packageVersion}";
		string dependencyVersion = requestedVersion ?? $"[{packageVersion}, )";

		File.WriteAllText(
			assetsFile,
			$$"""
            {
              "version": 3,
              "targets": {
                "{{targetFramework}}": {
                  "{{packageLibrary}}": {
                    "type": "package",
                    "compile": {
                      "{{packageAsset}}": {}
                    },
                    "runtime": {
                      "{{packageAsset}}": {}
                    }
                  }
                }
              },
              "libraries": {
                "{{packageLibrary}}": {
                  "type": "package",
                  "path": "{{packagePath}}",
                  "files": [
                    "{{packageAsset}}",
                    "{{packageId.ToLowerInvariant()}}.nuspec"
                  ]
                }
              },
              "projectFileDependencyGroups": {
                "{{targetFramework}}": [
                  "{{packageId}} >= {{packageVersion}}"
                ]
              },
              "packageFolders": {
                "{{escapedPackagesPath}}": {}
              },
              "project": {
                "version": "1.0.0",
                "restore": {
                  "projectUniqueName": "{{escapedProjectFile}}",
                  "projectName": "{{projectName}}",
                  "projectPath": "{{escapedProjectFile}}",
                  "packagesPath": "{{escapedPackagesPath}}",
                  "outputPath": "{{escapedObjDir}}",
                  "projectStyle": "PackageReference",
                  "configFilePaths": [],
                  "originalTargetFrameworks": ["{{targetFramework}}"],
                  "sources": {},
                  "frameworks": {
                    "{{targetFramework}}": {
                      "targetAlias": "{{targetFramework}}",
                      "projectReferences": {}
                    }
                  },
                  "warningProperties": {
                    "warnAsError": []
                  }
                },
                "frameworks": {
                  "{{targetFramework}}": {
                    "targetAlias": "{{targetFramework}}",
                    "dependencies": {
                      "{{packageId}}": {
                        "target": "Package",
                        "version": "{{dependencyVersion}}"
                      }
                    }
                  }
                }
              }
            }
            """);

		this.WriteNuGetGeneratedImports(projectFile);
	}

	private void WriteNuGetGeneratedImports(string projectFile)
	{
		string objDir = Path.Combine(Path.GetDirectoryName(projectFile)!, "obj");
		Directory.CreateDirectory(objDir);
		string projectName = Path.GetFileName(projectFile);
		File.WriteAllText(
			Path.Combine(objDir, projectName + ".nuget.g.props"),
			"""
            <Project>
              <PropertyGroup>
                <RestoreSuccess Condition="'$(RestoreSuccess)' == ''">True</RestoreSuccess>
                <ProjectAssetsFile Condition="'$(ProjectAssetsFile)' == ''">$(MSBuildThisFileDirectory)project.assets.json</ProjectAssetsFile>
              </PropertyGroup>
            </Project>
            """);
		File.WriteAllText(
			Path.Combine(objDir, projectName + ".nuget.g.targets"),
			"""
            <Project>
            </Project>
            """);
	}

	private string WriteNet472ReferenceAssemblies()
	{
		string referenceAssemblyDirectory = Path.Combine(this.workDir, "refs", ".NETFramework", "v4.7.2");
		Directory.CreateDirectory(referenceAssemblyDirectory);
		Directory.CreateDirectory(Path.Combine(referenceAssemblyDirectory, "RedistList"));

		File.WriteAllText(Path.Combine(referenceAssemblyDirectory, "mscorlib.dll"), string.Empty);
		File.WriteAllText(Path.Combine(referenceAssemblyDirectory, "System.dll"), string.Empty);
		File.WriteAllText(Path.Combine(referenceAssemblyDirectory, "System.Core.dll"), string.Empty);
		File.WriteAllText(
			Path.Combine(referenceAssemblyDirectory, "RedistList", "FrameworkList.xml"),
			"""
            <FileList Redist=".NET Framework 4.7.2">
              <File AssemblyName="mscorlib" Version="4.0.0.0" PublicKeyToken="b77a5c561934e089" Culture="neutral" ProcessorArchitecture="MSIL" FileVersion="4.7.0.0" />
              <File AssemblyName="System" Version="4.0.0.0" PublicKeyToken="b77a5c561934e089" Culture="neutral" ProcessorArchitecture="MSIL" FileVersion="4.7.0.0" />
              <File AssemblyName="System.Core" Version="4.0.0.0" PublicKeyToken="b77a5c561934e089" Culture="neutral" ProcessorArchitecture="MSIL" FileVersion="4.7.0.0" />
            </FileList>
            """);

		return referenceAssemblyDirectory;
	}

	private string MissingNetFrameworkReferenceAssembliesProperties()
		=> $"<FrameworkPathOverride>{Path.Combine(this.workDir, "missing-reference-assemblies")}</FrameworkPathOverride>";

	private static string FindRepoRoot()
	{
		DirectoryInfo? directory = new(AppContext.BaseDirectory);
		while (directory is not null)
		{
			if (File.Exists(Path.Combine(directory.FullName, "Roslyn.slnx")))
			{
				return directory.FullName;
			}

			directory = directory.Parent;
		}

		throw new DirectoryNotFoundException($"Could not find the repository root from {AppContext.BaseDirectory}.");
	}

	private string WriteLegacyProject(string fileName)
	{
		string projectFile = Path.Combine(this.workDir, fileName);
		File.WriteAllText(projectFile,
"""
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  </PropertyGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
""");

		return projectFile;
	}

	private static string[] ExtractCommandLineArguments(string cacheContent)
	{
		string normalized = cacheContent.Replace("\r\n", "\n");
		const string header = "\n[commandLineArguments]\n";
		int start = normalized.IndexOf(header, StringComparison.Ordinal);
		Assert.True(start >= 0, $"Could not find [commandLineArguments] section.\n{cacheContent}");
		start += header.Length;
		int end = normalized.IndexOf("\n[", start, StringComparison.Ordinal);
		if (end < 0)
			end = normalized.IndexOf("\n---\n", start, StringComparison.Ordinal);
		if (end < 0)
			end = normalized.Length;

		return normalized.Substring(start, end - start)
			.Split('\n', StringSplitOptions.RemoveEmptyEntries);
	}

	private static string[] NormalizeForStableArgumentParity(IEnumerable<string> args)
	{
		return args
			.Where(arg => !IsFileArgument(arg))
			.Where(arg => !IsPathBearingArgument(arg))
			.Select(NormalizeNetCoreAppNoWarn)
			.Select(arg => arg.Replace('\\', '/'))
			.ToArray();
	}

	private static string NormalizeNetCoreAppNoWarn(string arg)
	{
		const string noWarn = "/nowarn:";
		if (!arg.StartsWith(noWarn, StringComparison.OrdinalIgnoreCase))
			return arg;
		if (arg.Split(',', ';').Any(part => string.Equals(part.Trim(), "8002", StringComparison.OrdinalIgnoreCase)))
			return arg;
		return arg + ",8002";
	}

	private static bool IsFileArgument(string arg)
	{
		if (string.IsNullOrWhiteSpace(arg)) return false;
		if (!arg.StartsWith("/", StringComparison.Ordinal) && !arg.StartsWith("-", StringComparison.Ordinal))
			return true;
		return arg.StartsWith("/reference:", StringComparison.OrdinalIgnoreCase)
			|| arg.StartsWith("/r:", StringComparison.OrdinalIgnoreCase)
			|| arg.StartsWith("/analyzer:", StringComparison.OrdinalIgnoreCase)
			|| arg.StartsWith("/additionalfile:", StringComparison.OrdinalIgnoreCase)
			|| arg.StartsWith("/analyzerconfig:", StringComparison.OrdinalIgnoreCase)
			|| arg.StartsWith("/resource:", StringComparison.OrdinalIgnoreCase)
			|| arg.StartsWith("/linkresource:", StringComparison.OrdinalIgnoreCase)
			|| arg.StartsWith("/embed:", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsPathBearingArgument(string arg)
	{
		return arg.Contains(":\\", StringComparison.Ordinal)
			|| arg.Contains(":/", StringComparison.Ordinal)
			|| arg.Contains("<PATH>", StringComparison.Ordinal)
			|| arg.Contains("<NUGET>", StringComparison.Ordinal)
			|| arg.Contains("<DOTNET>", StringComparison.Ordinal)
			|| arg.Contains("<NETSDK>", StringComparison.Ordinal);
	}

	private static int CountOccurrences(string content, string value)
	{
		int count = 0;
		int index = 0;
		while ((index = content.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
		{
			count++;
			index += value.Length;
		}

		return count;
	}

	private static string GetSliceBlock(string content, string targetFramework)
	{
		foreach (string block in content.Split("\n---\n", StringSplitOptions.None).Skip(1))
		{
			if (block.Contains($"TargetFramework={targetFramework}\n", StringComparison.Ordinal))
				return block;
		}

		throw new InvalidOperationException($"Could not find slice block for {targetFramework}.\n{content}");
	}

	private static async Task<ProcessResult> RunDotnetMsbuildAsync(
		string projectFile,
		string[] extraArgs,
		System.Collections.Generic.Dictionary<string, string>? extraEnv = null,
		bool wireProjectDataTargets = true,
		bool useBuildCommand = false)
	{
		var psi = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = Path.GetDirectoryName(projectFile)!,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		string command = useBuildCommand ? "build" : "msbuild";
		foreach (string argument in new[] { command, projectFile, "/nologo", "/v:minimal" }.Concat(extraArgs))
		{
			psi.ArgumentList.Add(argument);
		}

		if (wireProjectDataTargets)
		{
			// Wire the targets file in the same way the extension does at runtime.
			psi.Environment["CustomAfterMicrosoftCommonTargets"] = TargetsFile;
			psi.Environment["CustomAfterMicrosoftCommonCrossTargetingTargets"] = TargetsFile;
		}
		psi.Environment["DOTNET_PROJECTDATA_CACHE_DIR"] = GetTestCacheRoot(projectFile);
		// Avoid surprising restore behavior in the smoke test.
		psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
		psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
		// Don't let the persistent MSBuild build server linger and keep the redirected
		// stdout/stderr pipe open after `dotnet msbuild` returns; that would deadlock the
		// stream reads below (observed as an intermittent hang on macOS CI).
		psi.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";

		if (extraEnv is not null)
		{
			foreach (KeyValuePair<string, string> kvp in extraEnv)
			{
				psi.Environment[kvp.Key] = kvp.Value;
			}
		}

		return await RunProcessWithTimeoutAsync(psi, ProcessTimeout, $"dotnet {string.Join(' ', psi.ArgumentList)}");
	}

	/// <summary>
	/// Starts <paramref name="psi"/>, reads stdout/stderr concurrently, and waits for the process to exit,
	/// but never for longer than <paramref name="timeout"/>. On timeout the entire process tree is killed
	/// (so lingering MSBuild worker nodes release the redirected pipe handles) and a <see cref="TimeoutException"/>
	/// is thrown with whatever output was captured, instead of hanging the test host indefinitely.
	/// </summary>
	private static async Task<ProcessResult> RunProcessWithTimeoutAsync(
		ProcessStartInfo psi,
		TimeSpan timeout,
		string commandDescription)
	{
		using Process proc = Process.Start(psi)!;

		// Read both streams concurrently so a full stderr buffer can never block stdout (and vice versa).
		Task<string> stdoutTask = proc.StandardOutput.ReadToEndAsync();
		Task<string> stderrTask = proc.StandardError.ReadToEndAsync();
		Task completion = Task.WhenAll(stdoutTask, stderrTask, proc.WaitForExitAsync());

		Task finished = await Task.WhenAny(completion, Task.Delay(timeout));
		if (finished != completion)
		{
			// Timed out. Kill the whole tree (dotnet + any lingering MSBuild worker nodes) so the redirected
			// pipes close, the reads can drain, and we don't leak processes onto the agent.
			try { proc.Kill(entireProcessTree: true); }
			catch { /* the process may have exited between the timeout check and the kill */ }

			// After the kill the pipe write handles close, so give the reads a bounded chance to drain.
			try { await completion.WaitAsync(TimeSpan.FromSeconds(30)); }
			catch { /* best-effort drain */ }

			string partialStdout = stdoutTask.IsCompletedSuccessfully ? stdoutTask.Result : string.Empty;
			string partialStderr = stderrTask.IsCompletedSuccessfully ? stderrTask.Result : string.Empty;

			throw new TimeoutException(
				$"'{commandDescription}' did not complete within {timeout.TotalMinutes:N1} minute(s) and was terminated. " +
				"This usually indicates a hung MSBuild/restore child process." + Environment.NewLine +
				"--- stdout ---" + Environment.NewLine + partialStdout + Environment.NewLine +
				"--- stderr ---" + Environment.NewLine + partialStderr);
		}

		await completion;
		return new ProcessResult(proc.ExitCode, stdoutTask.Result + Environment.NewLine + stderrTask.Result);
	}

	private static TimeSpan GetProcessTimeout()
	{
		string? raw = Environment.GetEnvironmentVariable("PROJECTDATA_SMOKE_TEST_PROCESS_TIMEOUT_SECONDS");
		if (!string.IsNullOrWhiteSpace(raw)
			&& int.TryParse(raw, out int seconds)
			&& seconds > 0)
		{
			return TimeSpan.FromSeconds(seconds);
		}

		return TimeSpan.FromMinutes(5);
	}

	private static string GetCompletionLoggerArgument(string receiptDirectory, string attemptId, string? loggerAssemblyPath = null)
	{
		string encodedReceiptDirectory = Convert.ToBase64String(Encoding.UTF8.GetBytes(receiptDirectory));
		loggerAssemblyPath ??= typeof(ProjectDataBuildCompletionLogger).Assembly.Location;
		return $"/logger:{typeof(ProjectDataBuildCompletionLogger).FullName},\"{loggerAssemblyPath}\";{encodedReceiptDirectory};{attemptId}";
	}

	private static string CopyAssembly(string sourcePath, string destinationDirectory)
	{
		string destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(sourcePath));
		File.Copy(sourcePath, destinationPath, overwrite: true);
		return destinationPath;
	}

	private sealed record ProcessResult(int ExitCode, string Output);

	private static void AssertMissingAssetsError(string projectFile, string output)
	{
		Assert.Contains("ProjectData: cannot write project data", output);
		Assert.Contains(projectFile, output);
		Assert.Contains("project.assets.json", output);
	}

	private static void AssertUnsupportedMarker(string projectFile, string expectedReason)
	{
		string markerPath = GetMarkerPath(projectFile);
		Assert.True(File.Exists(markerPath), $"Expected unsupported marker at {markerPath}.");
		string content = File.ReadAllText(markerPath);
		Assert.Contains($"reason={expectedReason}", content);
	}

	private static void AssertNoUnsupportedMarker(string projectFile)
	{
		string markerPath = GetMarkerPath(projectFile);
		Assert.False(File.Exists(markerPath), $"Expected no unsupported marker at {markerPath}.");
	}

	private static string GetMarkerPath(string projectFile)
	{
		string? previous = Environment.GetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR");
		try
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", GetTestCacheRoot(projectFile));
			return UnsupportedProjectDataMarker.GetMarkerFilePath(projectFile);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", previous);
		}
	}

	private static string GetTestCacheRoot(string projectFile)
		=> Path.Combine(Path.GetDirectoryName(projectFile)!, ".projectdata-cache");
}
