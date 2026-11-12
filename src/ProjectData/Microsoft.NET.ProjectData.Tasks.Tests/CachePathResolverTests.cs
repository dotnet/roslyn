// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.NET.ProjectData.Tasks.Tests;

public class CachePathResolverTests
{
	// Helper: create a resolver with explicit roots so tests are hermetic.
	private static CachePathResolver Make(
		string projectDir,
		string[]? nugetFolders = null,
		string[]? dotnetRoots = null,
		string? netFxRefRoot = null)
		=> new CachePathResolver(
			projectDir,
			nugetFolders ?? [],
			dotnetRoots ?? [],
			netFxRefRoot);

	private static string TempPath(params string[] parts)
	{
		string path = Path.GetTempPath();
		foreach (string part in parts)
			path = Path.Combine(path, part);
		return path;
	}

	[Fact]
	public void ToPortable_NuGetPath_Substitutes()
	{
		string nuget = CachePathResolver.NormalizeFolderPath(Path.Combine(Path.GetTempPath(), "packages"));
		CachePathResolver resolver = Make(@"C:\project", nugetFolders: [nuget]);

		string result = resolver.ToPortable(Path.Combine(nuget, "Newtonsoft.Json", "13.0.3", "lib", "netstandard2.0", "Newtonsoft.Json.dll").TrimEnd(Path.DirectorySeparatorChar));

		Assert.StartsWith("<NUGET>/", result);
		Assert.Contains("Newtonsoft.Json", result);
		Assert.DoesNotContain("\\", result);
	}

	[Fact]
	public void ToPortable_DotNetRootPath_Substitutes()
	{
		string dotnet = CachePathResolver.NormalizeFolderPath(TempPath("dotnet"));
		CachePathResolver resolver = Make(TempPath("project"), dotnetRoots: [dotnet]);

		string result = resolver.ToPortable(Path.Combine(dotnet, "shared", "Microsoft.NETCore.App", "8.0.0", "System.dll").TrimEnd(Path.DirectorySeparatorChar));

		Assert.StartsWith("<DOTNET>/", result);
		Assert.Contains("System.dll", result);
	}

	[Fact]
	public void TryGetDotNetRootFromHostPath_ReturnsContainingDirectory()
	{
		string dotnetRoot = TempPath("dotnet");
		string hostPath = Path.Combine(dotnetRoot, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");

		Assert.Equal(Path.GetFullPath(dotnetRoot), CachePathResolver.TryGetDotNetRootFromHostPath(hostPath));
	}

	[Fact]
	public void TryGetDotNetRootFromHostPath_RejectsRelativePath()
	{
		Assert.Null(CachePathResolver.TryGetDotNetRootFromHostPath("dotnet"));
	}

	[Fact]
	public void TryGetDotNetRootFromSdkPath_ReturnsParentOfSdkDirectory()
	{
		string dotnetRoot = TempPath("dotnet");
		string sdkPath = Path.Combine(dotnetRoot, "sdk", "11.0.100");

		Assert.Equal(Path.GetFullPath(dotnetRoot), CachePathResolver.TryGetDotNetRootFromSdkPath(sdkPath));
	}

	[Fact]
	public void TryGetDotNetRootFromSdkPath_AcceptsMixedCaseSdkDirectory()
	{
		string dotnetRoot = TempPath("dotnet");
		string sdkPath = Path.Combine(dotnetRoot, "SdK", "11.0.100");

		Assert.Equal(Path.GetFullPath(dotnetRoot), CachePathResolver.TryGetDotNetRootFromSdkPath(sdkPath));
	}

	[Fact]
	public void TryGetDotNetRootFromSdkPath_RejectsUnrelatedDirectory()
	{
		Assert.Null(CachePathResolver.TryGetDotNetRootFromSdkPath(TempPath("project", "bin")));
	}

	[Fact]
	public void ToPortable_ProjectRelativePath_ReturnsForwardSlashRelative()
	{
		string projectDir = TempPath("project", "src");
		CachePathResolver resolver = Make(projectDir);

		string result = resolver.ToPortable(Path.Combine(projectDir, "Program.cs"));

		Assert.Equal("Program.cs", result);
	}

	[Fact]
	public void ToPortable_ParentRelativePath_ReturnsDoubleDot()
	{
		string projectDir = TempPath("project", "src");
		CachePathResolver resolver = Make(projectDir);

		string result = resolver.ToPortable(TempPath("project", "Common.cs"));

		Assert.Equal("../Common.cs", result);
	}

	[Fact]
	public void MakePortable_NoAbsolutePath_Unchanged()
	{
		CachePathResolver resolver = Make(@"C:\project");

		string result = resolver.MakePortable("/nologo");

		Assert.Equal("/nologo", result);
	}

	[Fact]
	public void MakePortable_RelativeBackslashPath_NormalizedToForwardSlash()
	{
		// Csc emits relative /out: and /refout: arguments with backslashes on Windows.
		// MakePortable must normalize them so the output is cross-platform identical.
		CachePathResolver resolver = Make(@"C:\project");

		string result = resolver.MakePortable(@"/out:obj\Debug\net8.0\MyApp.dll");

		Assert.Equal("/out:obj/Debug/net8.0/MyApp.dll", result);
	}

	[Fact]
	public void MakePortable_RefOutBackslashPath_NormalizedToForwardSlash()
	{
		CachePathResolver resolver = Make(@"C:\project");

		string result = resolver.MakePortable(@"/refout:obj\Debug\net8.0\refint\MyApp.dll");

		Assert.Equal("/refout:obj/Debug/net8.0/refint/MyApp.dll", result);
	}

	[Fact]
	public void MakePortable_PlainBackslashText_NormalizedToForwardSlash()
	{
		CachePathResolver resolver = Make(@"C:\project");

		string result = resolver.MakePortable(@"some\relative\path.txt");

		Assert.Equal("some/relative/path.txt", result);
	}

	[Fact]
	public void MakePortable_EmbeddedNuGetPath_Substitutes()
	{
		string nuget = CachePathResolver.NormalizeFolderPath(TempPath("Users", "user", ".nuget", "packages"));
		CachePathResolver resolver = Make(TempPath("project"), nugetFolders: [nuget]);

		string result = resolver.MakePortable("/doc:" + Path.Combine(nuget, "foo", "1.0", "lib", "net8.0", "foo.xml").TrimEnd(Path.DirectorySeparatorChar));

		Assert.StartsWith("/doc:<NUGET>/", result);
	}

	[Fact]
	public void MakePortable_WindowsDrivePath_EmitsPathSentinel()
	{
		CachePathResolver resolver = Make(@"C:\project");

		string result = resolver.MakePortable(@"-out:C:\project\bin\Debug\app.dll");

		Assert.StartsWith("-out:<PATH>", result);
		Assert.Contains("bin/Debug/app.dll", result);
	}

	[Fact]
	public void FindSharedDirPrefix_CommonDir_ReturnsWithTrailingSlash()
	{
		string? prefix = CachePathResolver.FindSharedDirPrefix(
			"<NUGET>/newtonsoft.json/13.0.3/lib/net8.0/Newtonsoft.Json.dll",
			"<NUGET>/newtonsoft.json/13.0.3/lib/net8.0/Newtonsoft.Json.xml");

		Assert.Equal("<NUGET>/newtonsoft.json/13.0.3/lib/net8.0/", prefix);
	}

	[Fact]
	public void FindSharedDirPrefix_NoDirInCommon_ReturnsNull()
	{
		string? prefix = CachePathResolver.FindSharedDirPrefix(
			"<NUGET>/foo/1.0/lib.dll",
			"<DOTNET>/shared/app.dll");

		Assert.Null(prefix);
	}

	[Fact]
	public void MakeRelative_SubDir_ReturnsRelative()
	{
		string result = CachePathResolver.MakeRelative(@"C:\project\src", @"C:\project\src\Program.cs");
		Assert.Equal("Program.cs", result);
	}

	[Fact]
	public void MakeRelative_ParentDir_ReturnsDoubleDot()
	{
		string result = CachePathResolver.MakeRelative(@"C:\project\src", @"C:\project\Shared.cs");
		Assert.Equal("../Shared.cs", result);
	}

	[Fact]
	public void ToPortable_DotnetSdkPath_RewritesToNetSdk()
	{
		string dotnet = CachePathResolver.NormalizeFolderPath(Path.Combine(Path.GetTempPath(), "dotnet"));
		CachePathResolver resolver = Make(@"C:\project", dotnetRoots: [dotnet]);

		string analyzer = Path.Combine(dotnet, "sdk", "10.0.202", "Sdks", "Microsoft.NET.Sdk", "analyzers", "Microsoft.CodeAnalysis.NetAnalyzers.dll").TrimEnd(Path.DirectorySeparatorChar);
		string result = resolver.ToPortable(analyzer);

		// Expect the version segment dropped — anyone reading the cache binds the
		// SDK version they care about and the resolver expands <NETSDK> against it.
		Assert.Equal("<NETSDK>/Sdks/Microsoft.NET.Sdk/analyzers/Microsoft.CodeAnalysis.NetAnalyzers.dll", result);
	}

	[Fact]
	public void ToPortable_DotnetNonSdkPath_StaysUnderDotnetSentinel()
	{
		// <DOTNET>/packs/... and <DOTNET>/host/... must NOT be rewritten — they aren't
		// the per-version SDK content the rewrite is targeting.
		string dotnet = CachePathResolver.NormalizeFolderPath(Path.Combine(Path.GetTempPath(), "dotnet"));
		CachePathResolver resolver = Make(@"C:\project", dotnetRoots: [dotnet]);

		string pack = Path.Combine(dotnet, "packs", "Microsoft.NETCore.App.Ref", "10.0.7", "ref", "net10.0", "System.Runtime.dll").TrimEnd(Path.DirectorySeparatorChar);
		string result = resolver.ToPortable(pack);

		Assert.StartsWith("<DOTNET>/packs/", result);
		Assert.DoesNotContain("<NETSDK>", result);
	}

	[Fact]
	public void MakePortable_EmbeddedDotnetSdkPath_RewritesToNetSdk()
	{
		// Property values and command-line args may embed an absolute SDK path
		// (e.g. /globalconfig:<DOTNET>/sdk/10.0.202/...). The embedded form
		// must be rewritten the same way as standalone paths.
		string dotnet = CachePathResolver.NormalizeFolderPath(Path.Combine(Path.GetTempPath(), "dotnet"));
		CachePathResolver resolver = Make(@"C:\project", dotnetRoots: [dotnet]);

		string analyzerCfg = Path.Combine(dotnet, "sdk", "10.0.202", "Sdks", "Microsoft.NET.Sdk", "analyzers", "build", "config", "analysislevel_10_default.globalconfig");
		string text = "/globalconfig:" + analyzerCfg;
		string result = resolver.MakePortable(text);

		Assert.Equal("/globalconfig:<NETSDK>/Sdks/Microsoft.NET.Sdk/analyzers/build/config/analysislevel_10_default.globalconfig", result);
	}

	[Fact]
	public void RewriteSdkPath_NonSdkPortable_LeavesAlone()
	{
		Assert.Equal("<DOTNET>/packs/Foo/1.0/file", CachePathResolver.RewriteSdkPath("<DOTNET>/packs/Foo/1.0/file"));
		Assert.Equal("<NUGET>/foo", CachePathResolver.RewriteSdkPath("<NUGET>/foo"));
		Assert.Equal("anything", CachePathResolver.RewriteSdkPath("anything"));
	}

	[Fact]
	public void RewriteSdkPath_SdkPortable_DropsVersion()
	{
		Assert.Equal("<NETSDK>/Sdks/X/Y.dll",
			CachePathResolver.RewriteSdkPath("<DOTNET>/sdk/10.0.202/Sdks/X/Y.dll"));
	}
}
