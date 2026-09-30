// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Moq;
using Xunit;

namespace Microsoft.NET.ProjectData.Tasks.Tests;

public class AnalyzerConfigFileFilterTests
{
	[Theory]
	[InlineData("<NETSDK>/Sdks/Microsoft.NET.Sdk/analyzers/build/config/analysislevel_10_default.globalconfig")]
	[InlineData("<NETSDK>/Sdks/Microsoft.NET.Sdk/analyzers/build/config/analysislevel_11_recommended_warnaserror.globalconfig")]
	[InlineData("<NETSDK>/Sdks/Microsoft.NET.Sdk/codestyle/cs/build/config/analysislevelstyle_default.globalconfig")]
	public void IsSdkAnalyzerConfigFilePath_RecognizesSdkGlobalConfigs(string portablePath)
	{
		Assert.True(AnalyzerConfigFileFilter.IsSdkAnalyzerConfigFilePath(portablePath));
	}

	[Fact]
	public void Prepare_FiltersSdkGlobalConfigsWhenPolicyIsAvailable()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj");
		(CachePathResolver resolver, string dotnetRoot) = MakeSyntheticResolver(projectDir);
		string analysisConfig = Path.Combine(dotnetRoot, "sdk", "10.0.202", "Sdks", "Microsoft.NET.Sdk", "analyzers", "build", "config", "analysislevel_10_default.globalconfig");
		string styleConfig = Path.Combine(dotnetRoot, "sdk", "10.0.202", "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "build", "config", "analysislevelstyle_default.globalconfig");
		string projectConfig = Path.Combine(projectDir, "Directory.Build.globalconfig");

		List<string> prepared = AnalyzerConfigFileFilter.Prepare(
			[analysisConfig, styleConfig, projectConfig],
			resolver,
			sourceFiles: null,
			filterSdkAnalyzerConfigFiles: true);

		Assert.Single(prepared);
		Assert.Equal("Directory.Build.globalconfig", prepared[0]);
	}

	[Fact]
	public void Prepare_IgnoresNullSourceItems()
	{
		string projectDir = Path.Combine(Path.GetTempPath(), "proj");
		var resolver = new CachePathResolver(projectDir, [], [], null);
		string projectConfig = Path.Combine(projectDir, "Directory.Build.globalconfig");

		List<string> prepared = AnalyzerConfigFileFilter.Prepare(
			[projectConfig],
			resolver,
			sourceFiles: [null!, MakeItem(Path.Combine(projectDir, "Program.cs"))],
			filterSdkAnalyzerConfigFiles: false);

		Assert.Equal(["Directory.Build.globalconfig"], prepared);
	}

	[Fact]
	public void Prepare_StopsAtNearestRootEditorConfig()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "lscache-editorconfig-root-" + Guid.NewGuid().ToString("N"));
		try
		{
			string repoDir = Path.Combine(tempDir, "parent", "repo");
			string nestedWorktreeDir = Path.Combine(repoDir, "worktrees", "nested");
			string projectDir = Path.Combine(nestedWorktreeDir, "src", "App");
			Directory.CreateDirectory(projectDir);

			string parentConfig = Path.Combine(tempDir, "parent", ".editorconfig");
			string repoConfig = Path.Combine(repoDir, ".editorconfig");
			string nestedConfig = Path.Combine(nestedWorktreeDir, ".editorconfig");
			string projectConfig = Path.Combine(projectDir, ".editorconfig");
			string sourceFile = Path.Combine(projectDir, "Program.cs");
			string generatedConfig = Path.Combine(projectDir, "obj", "Debug", "net8.0", "App.GeneratedMSBuildEditorConfig.editorconfig");
			Directory.CreateDirectory(Path.GetDirectoryName(generatedConfig)!);

			File.WriteAllText(parentConfig, "[*.cs]\ndotnet_diagnostic.PARENT9999.severity = error\n");
			File.WriteAllText(repoConfig, "root = true\n\n[*.cs]\ndotnet_diagnostic.REPO0001.severity = warning\n");
			File.WriteAllText(nestedConfig, "root = true\n\n[*.cs]\ndotnet_diagnostic.NESTED0001.severity = silent\n");
			File.WriteAllText(projectConfig, "[*.cs]\ndotnet_diagnostic.PROJECT0001.severity = silent\n");
			File.WriteAllText(sourceFile, "Console.WriteLine(\"Hello\");\n");
			File.WriteAllText(generatedConfig, "is_global = true\n");

			var resolver = new CachePathResolver(projectDir, [], [], null);

			List<string> prepared = AnalyzerConfigFileFilter.Prepare(
				[parentConfig, repoConfig, nestedConfig, projectConfig, generatedConfig],
				resolver,
				sourceFiles: [MakeItem(sourceFile)],
				filterSdkAnalyzerConfigFiles: false);

			Assert.DoesNotContain(resolver.ToPortable(parentConfig), prepared);
			Assert.DoesNotContain(resolver.ToPortable(repoConfig), prepared);
			Assert.Contains(resolver.ToPortable(nestedConfig), prepared);
			Assert.Contains(resolver.ToPortable(projectConfig), prepared);
			Assert.Contains(resolver.ToPortable(generatedConfig), prepared);
		}
		finally
		{
			try { Directory.Delete(tempDir, recursive: true); } catch { }
		}
	}

	[Fact]
	public void Prepare_KeepsAncestorEditorConfigForLinkedSourceOutsideRoot()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "lscache-linked-editorconfig-root-" + Guid.NewGuid().ToString("N"));
		try
		{
			string parentDir = Path.Combine(tempDir, "parent");
			string repoDir = Path.Combine(parentDir, "repo");
			string nestedWorktreeDir = Path.Combine(repoDir, "worktrees", "nested");
			string projectDir = Path.Combine(nestedWorktreeDir, "src", "App");
			string linkedSourceDir = Path.Combine(parentDir, "shared");
			Directory.CreateDirectory(projectDir);
			Directory.CreateDirectory(linkedSourceDir);

			string parentConfig = Path.Combine(parentDir, ".editorconfig");
			string repoConfig = Path.Combine(repoDir, ".editorconfig");
			string nestedConfig = Path.Combine(nestedWorktreeDir, ".editorconfig");
			string projectConfig = Path.Combine(projectDir, ".editorconfig");
			string projectSource = Path.Combine(projectDir, "Program.cs");
			string linkedSource = Path.Combine(linkedSourceDir, "Shared.cs");

			File.WriteAllText(parentConfig, "[*.cs]\ndotnet_diagnostic.PARENT9999.severity = error\n");
			File.WriteAllText(repoConfig, "root = true\n\n[*.cs]\ndotnet_diagnostic.REPO0001.severity = warning\n");
			File.WriteAllText(nestedConfig, "root = true\n\n[*.cs]\ndotnet_diagnostic.NESTED0001.severity = silent\n");
			File.WriteAllText(projectConfig, "[*.cs]\ndotnet_diagnostic.PROJECT0001.severity = silent\n");
			File.WriteAllText(projectSource, "Console.WriteLine(\"Hello\");\n");
			File.WriteAllText(linkedSource, "public class Shared { }\n");

			var resolver = new CachePathResolver(projectDir, [], [], null);

			List<string> prepared = AnalyzerConfigFileFilter.Prepare(
				[parentConfig, repoConfig, nestedConfig, projectConfig],
				resolver,
				sourceFiles: [MakeItem(projectSource), MakeItem(linkedSource)],
				filterSdkAnalyzerConfigFiles: false);

			Assert.Contains(resolver.ToPortable(parentConfig), prepared);
			Assert.DoesNotContain(resolver.ToPortable(repoConfig), prepared);
			Assert.Contains(resolver.ToPortable(nestedConfig), prepared);
			Assert.Contains(resolver.ToPortable(projectConfig), prepared);
		}
		finally
		{
			try { Directory.Delete(tempDir, recursive: true); } catch { }
		}
	}

	private static ITaskItem MakeItem(string identity)
	{
		var mock = new Mock<ITaskItem>();
		mock.Setup(i => i.ItemSpec).Returns(identity);
		return mock.Object;
	}

	private static (CachePathResolver Resolver, string DotNetRoot) MakeSyntheticResolver(string projectDir)
	{
		string dotnetRoot = Path.Combine(projectDir, "fakedotnet") + Path.DirectorySeparatorChar;
		var resolver = new CachePathResolver(
			projectDir: projectDir,
			nugetFolders: [Path.Combine(projectDir, "fakenuget") + Path.DirectorySeparatorChar],
			dotnetRoots: [dotnetRoot],
			netFxRefRoot: null);
		return (resolver, dotnetRoot);
	}
}
