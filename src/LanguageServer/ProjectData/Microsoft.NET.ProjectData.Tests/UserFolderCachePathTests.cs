// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.NET.ProjectData.Tests;

/// <summary>
/// Tests for <see cref="UserFolderCachePath"/>. The same source file is linked
/// into the MSBuild Tasks assembly via <c>&lt;Compile Include=...&gt;</c>; this fixture
/// guards against accidental drift.
/// </summary>
[Collection(DotnetRootEnvCollection.Name)]
public sealed class UserFolderCachePathTests
{
	[Fact]
	public void Compute_IsDeterministic()
	{
		const string project = "/repo/src/MyApp/MyApp.csproj";
		Assert.Equal(UserFolderCachePath.Compute(project), UserFolderCachePath.Compute(project));
	}

	[Fact]
	public void Compute_ProducesDifferentPathsForDifferentInputs()
	{
		string a = UserFolderCachePath.Compute("/repo/src/Foo/Foo.csproj");
		string b = UserFolderCachePath.Compute("/repo/src/Bar/Bar.csproj");
		Assert.NotEqual(a, b);
	}

	[Fact]
	public void Compute_LayoutIsTwoSegmentsAfterBase()
	{
		// override base to a known temp root so the layout shape can be asserted
		// without depending on the user's dotnet home.
		string root = Path.Combine(Path.GetTempPath(), "userfolder-cache-test-" + Guid.NewGuid().ToString("N"));
		string? previous = Environment.GetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR");
		Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", root);
		try
		{
			string actual = UserFolderCachePath.Compute("/repo/src/Sample/Sample.csproj");
			Assert.StartsWith(root, actual, StringComparison.Ordinal);
			string relative = actual.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string[] parts = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			Assert.Equal(2, parts.Length);
			Assert.Equal(2, parts[0].Length);
			Assert.Equal(38, parts[1].Length); // SHA-1 is 40 hex chars; first 2 form the bucket, remaining 38 form the leaf
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", previous);
		}
	}

	[Fact]
	public void GetCacheBaseDirectory_UsesProjectDataOverride()
	{
		string root = Path.Combine(Path.GetTempPath(), "projectdata-cache-override-" + Guid.NewGuid().ToString("N"));
		string xdgCacheHome = Path.Combine(Path.GetTempPath(), "projectdata-xdg-cache-" + Guid.NewGuid().ToString("N"));
		string? previousCacheRoot = Environment.GetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR");
		string? previousXdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
		Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", root);
		Environment.SetEnvironmentVariable("XDG_CACHE_HOME", xdgCacheHome);
		try
		{
			Assert.Equal(root, UserFolderCachePath.GetCacheBaseDirectory());
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", previousCacheRoot);
			Environment.SetEnvironmentVariable("XDG_CACHE_HOME", previousXdgCacheHome);
		}
	}

	[Fact]
	public void GetCacheBaseDirectory_DefaultsUnderWindowsLocalAppData()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return;
		}

		string? previousCacheRoot = Environment.GetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR");
		Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", null);
		try
		{
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			Assert.Equal(Path.Combine(localAppData, "Microsoft", "dotnet-projectdata"), UserFolderCachePath.GetCacheBaseDirectory());
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", previousCacheRoot);
		}
	}

	[Fact]
	public void GetCacheBaseDirectory_DefaultsUnderXdgCacheHomeOnLinux()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return;
		}

		string xdgCacheHome = Path.Combine(Path.GetTempPath(), "projectdata-xdg-cache-" + Guid.NewGuid().ToString("N"));
		string? previousCacheRoot = Environment.GetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR");
		string? previousXdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
		Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", null);
		Environment.SetEnvironmentVariable("XDG_CACHE_HOME", xdgCacheHome);
		try
		{
			Assert.Equal(Path.Combine(xdgCacheHome, "dotnet-projectdata"), UserFolderCachePath.GetCacheBaseDirectory());
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", previousCacheRoot);
			Environment.SetEnvironmentVariable("XDG_CACHE_HOME", previousXdgCacheHome);
		}
	}

	[Fact]
	public void GetCacheBaseDirectory_DefaultsUnderPlatformCacheDirectory()
	{
		string? previousCacheRoot = Environment.GetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR");
		string? previousXdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
		Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", null);
		Environment.SetEnvironmentVariable("XDG_CACHE_HOME", null);
		try
		{
			string expected;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				expected = Path.Combine(localAppData, "Microsoft", "dotnet-projectdata");
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				expected = Path.Combine(home, "Library", "Caches", "dotnet-projectdata");
			}
			else
			{
				string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				expected = Path.Combine(home, ".cache", "dotnet-projectdata");
			}

			Assert.Equal(expected, UserFolderCachePath.GetCacheBaseDirectory());
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", previousCacheRoot);
			Environment.SetEnvironmentVariable("XDG_CACHE_HOME", previousXdgCacheHome);
		}
	}

	[Fact]
	public void Compute_CaseSensitivityMatchesPlatform()
	{
		string lower = UserFolderCachePath.Compute("/repo/src/sample/sample.csproj");
		string upper = UserFolderCachePath.Compute("/REPO/SRC/SAMPLE/SAMPLE.CSPROJ");

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			// Linux paths are case-sensitive, so the cache layout follows.
			Assert.NotEqual(lower, upper);
		}
		else
		{
			// Windows / macOS: case-insensitive filesystems → identical hashes.
			Assert.Equal(lower, upper);
		}
	}

	[Fact]
	public void Compute_ThrowsOnEmpty()
	{
		Assert.Throws<ArgumentException>(() => UserFolderCachePath.Compute(string.Empty));
	}
}
