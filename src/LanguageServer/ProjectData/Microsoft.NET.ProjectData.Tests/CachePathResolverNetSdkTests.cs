// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.NET.ProjectData.Tests;

/// <summary>
/// Tests for the <c>&lt;NETSDK&gt;</c> sentinel introduced for SDK-shipped content
/// (analyzers, global configs). The cache file is environment-agnostic — it stores
/// paths under <c>&lt;DOTNET&gt;/sdk/&lt;ver&gt;/...</c> as <c>&lt;NETSDK&gt;/...</c>
/// without a version, and the resolver requires a caller-supplied SDK binding.
/// </summary>
[Collection(DotnetRootEnvCollection.Name)]
public sealed class CachePathResolverNetSdkTests
{
	[Fact]
	public void Resolve_WithoutBinding_IsNotBound()
	{
		var resolver = new CachePathResolver();
		Assert.False(resolver.IsNetSdkBound);
	}

	[Fact]
	public void Resolve_WithoutBinding_ThrowsWhenToAbsoluteCalledOnNetSdk()
	{
		// When unbound, callers must check IsNetSdkBound and not call ToAbsolute
		// on <NETSDK> paths. This verifies the contract is enforced.
		var resolver = new CachePathResolver();
		Assert.False(resolver.IsNetSdkBound);
		Assert.Throws<InvalidOperationException>(
			() => resolver.ToAbsolute("<NETSDK>/Sdks/Microsoft.NET.Sdk/analyzers/Foo.dll", projectDirectory: "/p"));
	}

	[Fact]
	public void Resolve_WithSdkPath_UsesPathDirectly_EvenIfNotOnDisk()
	{
		string sdkPath = OperatingSystem.IsWindows()
			? @"C:\does\not\exist\dotnet\sdk\10.0.202"
			: "/does/not/exist/dotnet/sdk/10.0.202";
		var resolver = new CachePathResolver("10.0.202", sdkPath);
		string result = resolver.ToAbsolute("<NETSDK>/Sdks/Microsoft.NET.Sdk/analyzers/Foo.dll", projectDirectory: "/p");
		Assert.Equal(JoinPlat(sdkPath, "Sdks", "Microsoft.NET.Sdk", "analyzers", "Foo.dll"), result);
	}

	[Fact]
	public void Resolve_WithSdkVersion_LocatesUnderInstalledRoot()
	{
		// Build a synthetic dotnet root and point DOTNET_ROOT at it.
		string root = Path.Combine(Path.GetTempPath(), "lscache-netsdk-" + Guid.NewGuid().ToString("N"));
		string sdkDir = Path.Combine(root, "dotnet", "sdk", "10.0.202", "Sdks", "Microsoft.NET.Sdk", "analyzers");
		Directory.CreateDirectory(sdkDir);
		File.WriteAllText(Path.Combine(sdkDir, "Foo.dll"), string.Empty);

		string previous = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty;
		try
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", Path.Combine(root, "dotnet"));
			var resolver = new CachePathResolver("10.0.202");
			string result = resolver.ToAbsolute("<NETSDK>/Sdks/Microsoft.NET.Sdk/analyzers/Foo.dll", projectDirectory: "/p");
			Assert.Equal(
				JoinPlat(root, "dotnet", "sdk", "10.0.202", "Sdks", "Microsoft.NET.Sdk", "analyzers", "Foo.dll"),
				result);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", previous);
			try { Directory.Delete(root, recursive: true); } catch { }
		}
	}

	[Fact]
	public void Resolve_WithSdkVersion_NotInstalled_ReturnsBestEffortPath()
	{
		// No matching SDK on disk: the resolver constructs a path under the first known
		// dotnet root anyway, mirroring the existing <DOTNET> "file not found" behavior.
		// Caller deals with missing-on-disk via DTB regeneration.
		string root = Path.Combine(Path.GetTempPath(), "lscache-netsdk-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(root, "dotnet"));
		string previous = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty;
		try
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", Path.Combine(root, "dotnet"));
			var resolver = new CachePathResolver("99.99.999");
			string result = resolver.ToAbsolute("<NETSDK>/global.config", projectDirectory: "/p");
			Assert.Contains("99.99.999", result);
			Assert.EndsWith("global.config", result);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", previous);
			try { Directory.Delete(root, recursive: true); } catch { }
		}
	}

	[Fact]
	public void MakeAbsolute_EmbeddedNetSdk_Resolves()
	{
		string sdkPath = OperatingSystem.IsWindows()
			? @"C:\dotnet\sdk\10.0.202"
			: "/dotnet/sdk/10.0.202";
		var resolver = new CachePathResolver("10.0.202", sdkPath);
		string text = "/keyfile:<NETSDK>/keys/strong.snk";
		string result = resolver.MakeAbsolute(text, projectDirectory: "/p");
		Assert.Equal("/keyfile:" + JoinPlat(sdkPath, "keys", "strong.snk"), result);
	}

	[Fact]
	public void Ctor_NullOrEmptyVersion_Throws()
	{
		Assert.Throws<ArgumentException>(() => new CachePathResolver(sdkVersion: ""));
		Assert.Throws<ArgumentNullException>(() => new CachePathResolver(sdkVersion: null!));
	}

	/// <summary>
	/// Regression for the test-isolation seam. The public constructors discover dotnet
	/// roots from <c>DOTNET_ROOT</c> + ambient probes (<c>C:\Program Files\dotnet</c>, etc.).
	/// The internal seam constructor must use the caller-supplied roots verbatim and
	/// never consult the environment, even when <c>DOTNET_ROOT</c> points at a directory
	/// that contains the pack being looked up.
	/// </summary>
	[Fact]
	public void Ctor_ExplicitRoots_IgnoresAmbientDotNetRootEnvironment()
	{
		// Build a synthetic dotnet root that DOES contain Microsoft.NETCore.App.Ref/8.0.26.
		// If the seam leaked to the ambient env, FindRefPackDirectory would return this path.
		string ambientRoot = Path.Combine(Path.GetTempPath(), "lscache-ambient-leak-" + Guid.NewGuid().ToString("N"));
		string ambientDotnet = Path.Combine(ambientRoot, "dotnet");
		string ambientPack = Path.Combine(ambientDotnet, "packs", "Microsoft.NETCore.App.Ref", "8.0.26", "ref", "net8.0");
		Directory.CreateDirectory(ambientPack);
		File.WriteAllText(Path.Combine(ambientPack, "Ambient.dll"), string.Empty);

		// Unrelated explicit root. Even though it exists, it doesn't contain the pack.
		string explicitRoot = Path.Combine(Path.GetTempPath(), "lscache-explicit-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(explicitRoot);

		string previous = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty;
		try
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", ambientDotnet);

			CachePathResolver resolver = new(
				sdkVersion: null,
				sdkPath: null,
				dotnetRoots: [explicitRoot],
				nugetFolders: [],
				netFxRefRoot: null);

			Assert.Null(resolver.FindRefPackDirectory("Microsoft.NETCore.App.Ref", "net8.0"));
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_ROOT", previous);
			try { Directory.Delete(ambientRoot, recursive: true); } catch { }
			try { Directory.Delete(explicitRoot, recursive: true); } catch { }
		}
	}

	private static string JoinPlat(params string[] parts) => Path.Combine(parts);
}
