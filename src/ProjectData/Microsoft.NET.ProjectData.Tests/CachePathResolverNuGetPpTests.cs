// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.NET.ProjectData.Tests;

/// <summary>
/// Tests for the <c>&lt;NUGETPP&gt;</c> sentinel used for NuGet preprocessed content assets.
/// The writer emits <c>&lt;NUGETPP&gt;/{PackageId}/{Version}/...</c> and the reader resolves
/// it by scanning <c>obj/**/NuGet/*/</c> for the matching package-relative path.
/// </summary>
public sealed class CachePathResolverNuGetPpTests : IDisposable
{
	private readonly string tempRoot;
	private readonly string projectDir;

	public CachePathResolverNuGetPpTests()
	{
		this.tempRoot = Path.Combine(Path.GetTempPath(), "lscache-nugetpp-" + Guid.NewGuid().ToString("N"));
		this.projectDir = Path.Combine(this.tempRoot, "proj");
		Directory.CreateDirectory(this.projectDir);
	}

	public void Dispose()
	{
		try { Directory.Delete(this.tempRoot, recursive: true); } catch { }
	}

	[Fact]
	public void ToAbsolute_NuGetPpSentinel_ResolvesWhenFileExists()
	{
		// Simulate: obj/Debug/net8.0/NuGet/7E7D116BF0B1C551/Nullable/1.3.0/Nullable/NullableAttributes.cs
		string hashDir = Path.Combine(this.projectDir, "obj", "Debug", "net8.0", "NuGet", "7E7D116BF0B1C551", "Nullable", "1.3.0", "Nullable");
		Directory.CreateDirectory(hashDir);
		string expectedFile = Path.Combine(hashDir, "NullableAttributes.cs");
		File.WriteAllText(expectedFile, string.Empty);

		var resolver = new CachePathResolver();
		string result = resolver.ToAbsolute("<NUGETPP>/Nullable/1.3.0/Nullable/NullableAttributes.cs", this.projectDir);

		Assert.Equal(expectedFile, result);
	}

	[Fact]
	public void ToAbsolute_NuGetPpSentinel_ResolvesWithDifferentHash()
	{
		// Different hash value — reader should still find the file
		string hashDir = Path.Combine(this.projectDir, "obj", "Release", "net6.0", "NuGet", "ABCDEF0123456789", "Pkg", "2.0.0");
		Directory.CreateDirectory(hashDir);
		string expectedFile = Path.Combine(hashDir, "File.cs");
		File.WriteAllText(expectedFile, string.Empty);

		var resolver = new CachePathResolver();
		string result = resolver.ToAbsolute("<NUGETPP>/Pkg/2.0.0/File.cs", this.projectDir);

		Assert.Equal(expectedFile, result);
	}

	[Fact]
	public void ToAbsolute_NuGetPpSentinel_FallsBackWhenNoObjExists()
	{
		// No obj/ directory — should return a best-effort path without throwing
		var resolver = new CachePathResolver();
		string result = resolver.ToAbsolute("<NUGETPP>/Nullable/1.3.0/Nullable/NullableAttributes.cs", this.projectDir);

		Assert.Contains("NuGet", result);
		Assert.EndsWith("NullableAttributes.cs", result);
		Assert.StartsWith(Path.Combine(this.projectDir, "obj"), result);
	}

	[Fact]
	public void ToAbsolute_NuGetPpSentinel_FallsBackWhenFileNotFound()
	{
		// obj/ exists but no matching file under any hash
		string nugetDir = Path.Combine(this.projectDir, "obj", "Debug", "net8.0", "NuGet", "AAAAAAAAAAAAAAAA", "OtherPkg", "1.0.0");
		Directory.CreateDirectory(nugetDir);

		var resolver = new CachePathResolver();
		string result = resolver.ToAbsolute("<NUGETPP>/Nullable/1.3.0/Nullable/NullableAttributes.cs", this.projectDir);

		// Falls back to best-effort path
		Assert.EndsWith("NullableAttributes.cs", result);
	}

	[Fact]
	public void MakeAbsolute_NuGetPpSentinel_ResolvesInlineOccurrence()
	{
		// Simulate the file on disk
		string hashDir = Path.Combine(this.projectDir, "obj", "Debug", "net8.0", "NuGet", "1234567890ABCDEF", "Nullable", "1.3.0", "Nullable");
		Directory.CreateDirectory(hashDir);
		string expectedFile = Path.Combine(hashDir, "NullableAttributes.cs");
		File.WriteAllText(expectedFile, string.Empty);

		var resolver = new CachePathResolver();
		string result = resolver.MakeAbsolute("/some/prefix<NUGETPP>/Nullable/1.3.0/Nullable/NullableAttributes.cs", this.projectDir);

		Assert.StartsWith("/some/prefix", result);
		Assert.EndsWith("NullableAttributes.cs", result);
		Assert.Contains("NuGet", result);
	}

	[Fact]
	public void ToAbsolute_NuGetPpSentinel_MultipleHashDirs_FindsCorrectOne()
	{
		// Two hash dirs exist, only one has the target file
		string wrongHash = Path.Combine(this.projectDir, "obj", "Debug", "net8.0", "NuGet", "0000000000000000", "Nullable", "1.3.0", "Nullable");
		string rightHash = Path.Combine(this.projectDir, "obj", "Debug", "net8.0", "NuGet", "FFFFFFFFFFFFFFFF", "Nullable", "1.3.0", "Nullable");
		Directory.CreateDirectory(wrongHash);
		Directory.CreateDirectory(rightHash);
		string expectedFile = Path.Combine(rightHash, "NullableAttributes.cs");
		File.WriteAllText(expectedFile, string.Empty);

		var resolver = new CachePathResolver();
		string result = resolver.ToAbsolute("<NUGETPP>/Nullable/1.3.0/Nullable/NullableAttributes.cs", this.projectDir);

		Assert.Equal(expectedFile, result);
	}

	[Fact]
	public void ToAbsolute_NuGetPpSentinel_PrefersDebugOverOtherConfigurations()
	{
		// Both obj/Debug/.../NuGet and obj/Release/.../NuGet contain the file under
		// different hashes. The resolver must prefer Debug even when Release is
		// chronologically newer, because the committed lscaches are generated in
		// Debug (the refresh-lscache skill always builds Debug, and Aspire / Roslyn
		// / CPS all treat Debug as the universal local-dev configuration). Without
		// this preference, a stale Release build on the developer's machine could
		// be picked first purely by OS enumeration order, returning content that
		// doesn't match what the writer recorded.
		string releaseHashDir = Path.Combine(this.projectDir, "obj", "Release", "net8.0", "NuGet", "AAAAAAAAAAAAAAAA");
		string debugHashDir = Path.Combine(this.projectDir, "obj", "Debug", "net8.0", "NuGet", "BBBBBBBBBBBBBBBB");
		string releasePackageDir = Path.Combine(releaseHashDir, "Nullable", "1.3.0", "Nullable");
		string debugPackageDir = Path.Combine(debugHashDir, "Nullable", "1.3.0", "Nullable");
		Directory.CreateDirectory(releasePackageDir);
		Directory.CreateDirectory(debugPackageDir);
		string releaseFile = Path.Combine(releasePackageDir, "NullableAttributes.cs");
		string debugFile = Path.Combine(debugPackageDir, "NullableAttributes.cs");
		File.WriteAllText(releaseFile, "// from release");
		File.WriteAllText(debugFile, "// from debug");

		// Make Release deliberately newer so the test fails if we accidentally fall
		// back to "newest wins across all configurations" instead of preferring Debug.
		DateTime baseTime = DateTime.UtcNow.AddHours(-1);
		Directory.SetLastWriteTimeUtc(debugHashDir, baseTime);
		Directory.SetLastWriteTimeUtc(releaseHashDir, baseTime.AddMinutes(30));

		var resolver = new CachePathResolver();
		string result = resolver.ToAbsolute("<NUGETPP>/Nullable/1.3.0/Nullable/NullableAttributes.cs", this.projectDir);

		Assert.Equal(debugFile, result);
	}

	[Fact]
	public void ToAbsolute_NuGetPpSentinel_RootedSuffix_ReturnsFallbackInsideObjNuGet()
	{
		// Defense in depth: lscache content is writer-generated, but if a malformed
		// or stray cache ever supplied a rooted suffix, naive Path.Combine would
		// discard the obj prefix and return an absolute path outside the project.
		// The resolver should reject and return a non-existent placeholder under
		// `obj/NuGet` so File.Exists fails cleanly downstream.
		var resolver = new CachePathResolver();
		string result = resolver.ToAbsolute("<NUGETPP>//etc/passwd", this.projectDir);

		Assert.StartsWith(Path.Combine(this.projectDir, "obj"), result);
		Assert.DoesNotContain("etc", result, StringComparison.OrdinalIgnoreCase);
		Assert.False(File.Exists(result));
	}

	[Fact]
	public void ToAbsolute_NuGetPpSentinel_BackslashRootedSuffix_ReturnsFallbackInsideObjNuGet()
	{
		var resolver = new CachePathResolver();
		string result = resolver.ToAbsolute("<NUGETPP>/\\Windows\\System32\\evil.dll", this.projectDir);

		Assert.StartsWith(Path.Combine(this.projectDir, "obj"), result);
		Assert.DoesNotContain("System32", result, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ToAbsolute_NuGetPpSentinel_TraversalSegment_ReturnsFallbackInsideObjNuGet()
	{
		// `..` segments could escape the hash directory if combined naively.
		var resolver = new CachePathResolver();
		string result = resolver.ToAbsolute("<NUGETPP>/Pkg/1.0.0/../../../../../etc/shadow", this.projectDir);

		Assert.StartsWith(Path.Combine(this.projectDir, "obj"), result);
		Assert.DoesNotContain("shadow", result, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("..", result);
	}

	[Fact]
	public void ToAbsolute_NuGetPpSentinel_DriveLetterRootedSuffix_ReturnsFallbackInsideObjNuGet()
	{
		// Windows-only: `Path.IsPathRooted("C:/...")` is true only on Windows.
		// On Linux/macOS, `C:` is just a weirdly-named relative path segment
		// — the resolver treats it as a normal package id and the suffix is
		// safe by definition. This test asserts the resolver's drive-letter
		// rejection on the platforms where that rejection is meaningful.
		if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
		{
			return;
		}

		var resolver = new CachePathResolver();
		string result = resolver.ToAbsolute("<NUGETPP>/C:/Windows/win.ini", this.projectDir);

		Assert.StartsWith(Path.Combine(this.projectDir, "obj"), result);
		Assert.DoesNotContain("win.ini", result, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ToAbsolute_NuGetPpSentinel_MultipleHashDirsBothContainFile_PrefersNewest()
	{
		// Both hash dirs contain the same file. The resolver should return the
		// file from the most recently written hash dir so the reader sees the
		// output of the latest restore, not a stale leftover. Without explicit
		// ordering, enumeration order is OS-defined (alphabetical on NTFS,
		// inode/insertion order elsewhere) and would non-deterministically
		// pick one or the other.
		string olderHashDir = Path.Combine(this.projectDir, "obj", "Debug", "net8.0", "NuGet", "0000000000000000");
		string newerHashDir = Path.Combine(this.projectDir, "obj", "Debug", "net8.0", "NuGet", "FFFFFFFFFFFFFFFF");
		string olderPackageDir = Path.Combine(olderHashDir, "Nullable", "1.3.0", "Nullable");
		string newerPackageDir = Path.Combine(newerHashDir, "Nullable", "1.3.0", "Nullable");
		Directory.CreateDirectory(olderPackageDir);
		Directory.CreateDirectory(newerPackageDir);
		string olderFile = Path.Combine(olderPackageDir, "NullableAttributes.cs");
		string newerFile = Path.Combine(newerPackageDir, "NullableAttributes.cs");
		File.WriteAllText(olderFile, "// stale");
		File.WriteAllText(newerFile, "// fresh");

		// Force a stable mtime ordering. We set the hash-dir mtimes directly
		// because the resolver compares hash-dir level, not file level, and
		// `Directory.CreateDirectory` mtimes can land within the same FS tick
		// in fast tests. Note: the "wrong" alphabetical winner would be the
		// '0000...' hash, so the test would silently regress to NTFS-order
		// passing if we didn't deliberately make 'FFFF...' newer.
		DateTime baseTime = DateTime.UtcNow.AddHours(-1);
		Directory.SetLastWriteTimeUtc(olderHashDir, baseTime);
		Directory.SetLastWriteTimeUtc(newerHashDir, baseTime.AddMinutes(30));

		var resolver = new CachePathResolver();
		string result = resolver.ToAbsolute("<NUGETPP>/Nullable/1.3.0/Nullable/NullableAttributes.cs", this.projectDir);

		Assert.Equal(newerFile, result);
	}
}
