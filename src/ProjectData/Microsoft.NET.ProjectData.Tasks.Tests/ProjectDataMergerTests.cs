// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.NET.ProjectData.Tasks.Tests;

public class ProjectDataMergerTests
{
	private const string SliceNet8 = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net8.0\n";
	private const string SliceNet9 = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net9.0\n";
	private const string SliceNet472 = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net472\n";

	private static string MakeTempDir()
	{
		string dir = Path.Combine(Path.GetTempPath(), "lscache-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static string CreateSliceDir(string baseDir, string tfm)
	{
		string dir = Path.Combine(baseDir, "obj", "Debug", tfm);
		Directory.CreateDirectory(dir);
		return dir;
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

	[Fact]
	public void Merge_SingleSlice_CreatesFileWithBanner()
	{
		string dir = MakeTempDir();
		try
		{
			string sliceDir = CreateSliceDir(dir, "net8.0");
			File.WriteAllText(Path.Combine(sliceDir, "Sample.csproj.slice"), SliceNet8);
			string outPath = Path.Combine(dir, "out.lscache");

			int count = ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			Assert.Equal(1, count);
			Assert.True(File.Exists(outPath));
			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			string[] lines = content.Split('\n');
			Assert.Equal("version=2.2", lines[0]);
			Assert.DoesNotContain("hash=", content);
			Assert.Contains("# This file caches", content);
			Assert.DoesNotContain("TargetFramework=net8.0", content);
			Assert.DoesNotContain("\n---\n", content);
			Assert.DoesNotContain("[sliceDimensions]", content);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_PreservesSlicesAfterSuccess()
	{
		string dir = MakeTempDir();
		try
		{
			string sliceDir = CreateSliceDir(dir, "net8.0");
			string slicePath = Path.Combine(sliceDir, "Sample.csproj.slice");
			File.WriteAllText(slicePath, SliceNet8);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			Assert.True(File.Exists(slicePath), "slice should be preserved so later builds can skip when it is up-to-date");
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_ExplicitSliceFiles_IgnoresUnlistedStaleSlice()
	{
		string dir = MakeTempDir();
		try
		{
			string net8Slice = Path.Combine(CreateSliceDir(dir, "net8.0"), "Sample.csproj.slice");
			string staleNet9Slice = Path.Combine(CreateSliceDir(dir, "net9.0"), "Sample.csproj.slice");
			File.WriteAllText(net8Slice, SliceNet8);
			File.WriteAllText(staleNet9Slice, SliceNet9);
			string outPath = Path.Combine(dir, "out.lscache");

			int count = ProjectDataMerger.Merge(outPath, [net8Slice], "net8.0");

			Assert.Equal(1, count);
			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			Assert.DoesNotContain("\n---\n", content);
			Assert.DoesNotContain("TargetFramework=net9.0", content);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_PreserveExistingSlices_KeepsUnevaluatedSliceFromOutput()
	{
		string dir = MakeTempDir();
		try
		{
			string net10Slice = Path.Combine(CreateSliceDir(dir, "net10.0"), "Sample.csproj.slice");
			File.WriteAllText(net10Slice, "[project]\nlanguage=C#\nlastDtbSucceeded\n[sliceDimensions]\nTargetFramework=net10.0\n[commandLineArguments]\n/langversion:preview\n");
			string outPath = Path.Combine(dir, "out.lscache");
			File.WriteAllText(
				outPath,
				"""
                hash=0000000000000000000000000000000000000000000000000000000000000000
                version=2

                # Existing committed cache.

                [project]
                project=Sample.csproj
                language=C#
                lastDtbSucceeded

                [commandLineArguments]
                /noconfig

                ---

                [project]
                primary

                [sliceDimensions]
                TargetFramework=net10.0

                ---

                [project]

                [sliceDimensions]
                TargetFramework=net472

                [metadataReferences]
                <NETFXREF>/v4.7.2/
                 mscorlib.dll
                """);

			int count = ProjectDataMerger.Merge(outPath, [net10Slice], "net10.0", preserveExistingSlices: true);

			Assert.Equal(1, count);
			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			Assert.Contains("TargetFramework=net10.0", content);
			Assert.Contains("/langversion:preview", content);
			Assert.DoesNotContain("/noconfig", GetSliceBlock(content, "net10.0"));

			string preservedNetFrameworkBlock = GetSliceBlock(content, "net472");
			Assert.Contains("<NETFXREF>/v4.7.2/", preservedNetFrameworkBlock);
			Assert.Contains(" mscorlib.dll", preservedNetFrameworkBlock);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_PreserveExistingSlices_DoesNotPreserveSlicesFromIncompatibleMajorVersion()
	{
		string dir = MakeTempDir();
		try
		{
			string net10Slice = Path.Combine(CreateSliceDir(dir, "net10.0"), "Sample.csproj.slice");
			File.WriteAllText(net10Slice, "[project]\nlanguage=C#\nlastDtbSucceeded\n[sliceDimensions]\nTargetFramework=net10.0\n[commandLineArguments]\n/langversion:preview\n");
			string outPath = Path.Combine(dir, "out.lscache");

			// The existing merged cache is a FUTURE major (version=3): its grammar may differ from this
			// writer's, so its slices must NOT be parsed-and-re-emitted under the current version= header.
			File.WriteAllText(
				outPath,
				"""
                version=3

                [project]
                project=Sample.csproj
                language=C#
                lastDtbSucceeded

                ---

                [project]

                [sliceDimensions]
                TargetFramework=net472

                [metadataReferences]
                <NETFXREF>/v4.7.2/
                 future_major_payload.dll
                """);

			int count = ProjectDataMerger.Merge(outPath, [net10Slice], "net10.0", preserveExistingSlices: true);

			Assert.Equal(1, count);
			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			Assert.StartsWith("version=2", content);
			Assert.Contains("/langversion:preview", content);
			// The incompatible-major slice content must be gone — not re-emitted under version=2.
			Assert.DoesNotContain("future_major_payload.dll", content);
			Assert.DoesNotContain("TargetFramework=net472", content);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_NoSlices_ReturnsZero()
	{
		string dir = MakeTempDir();
		try
		{
			Directory.CreateDirectory(Path.Combine(dir, "obj"));
			string outPath = Path.Combine(dir, "out.lscache");

			int count = ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			Assert.Equal(0, count);
			Assert.False(File.Exists(outPath));
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_TwoSlices_AddsSeparator()
	{
		string dir = MakeTempDir();
		try
		{
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net8.0"), "Sample.csproj.slice"), SliceNet8);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net9.0"), "Sample.csproj.slice"), SliceNet9);
			string outPath = Path.Combine(dir, "out.lscache");

			int count = ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			Assert.Equal(2, count);
			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			Assert.Contains("TargetFramework=net8.0", content);
			Assert.Contains("TargetFramework=net9.0", content);

			int sepCount = 0;
			int idx = 0;
			while ((idx = content.IndexOf("\n---\n", idx, StringComparison.Ordinal)) >= 0)
			{
				sepCount++;
				idx += 5;
			}
			// Shared section + 2 per-TFM sections = 2 separators
			Assert.Equal(2, sepCount);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_SortsSlicesByPath()
	{
		string dir = MakeTempDir();
		try
		{
			// Write net9.0 first, then net472 — output should sort alphabetically by path
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net9.0"), "Sample.csproj.slice"), SliceNet9 + "MARK_NET9\n");
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net472"), "Sample.csproj.slice"), SliceNet472 + "MARK_NET472\n");
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			string content = File.ReadAllText(outPath);
			int idx472 = content.IndexOf("MARK_NET472", StringComparison.Ordinal);
			int idx9 = content.IndexOf("MARK_NET9", StringComparison.Ordinal);
			Assert.True(idx472 >= 0 && idx9 >= 0);
			Assert.True(idx472 < idx9, "net472 slice should come before net9.0");
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Theory]
	[InlineData("net8.0;netstandard2.0", "net8.0", "netstandard2.0")]
	[InlineData("netstandard2.0;net8.0", "netstandard2.0", "net8.0")]
	public void Merge_PrimaryFollowsFirstNonNetFrameworkTargetFramework(string targetFrameworks, string expectedPrimary, string expectedNonPrimary)
	{
		string dir = MakeTempDir();
		try
		{
			string sliceNet8 = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net8.0\n";
			string sliceNetStandard = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=netstandard2.0\n";

			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net8.0"), "Sample.csproj.slice"), sliceNet8);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "netstandard2.0"), "Sample.csproj.slice"), sliceNetStandard);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"), targetFrameworks);

			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			Assert.Equal(1, CountOccurrences(content, "\nprimary\n"));
			Assert.Contains("\nprimary\n", GetSliceBlock(content, expectedPrimary));
			Assert.DoesNotContain("\nprimary\n", GetSliceBlock(content, expectedNonPrimary));
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_PrimaryPrefersNetCoreAppOverNetFramework()
	{
		string dir = MakeTempDir();
		try
		{
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net472"), "Sample.csproj.slice"), SliceNet472);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net8.0"), "Sample.csproj.slice"), SliceNet8);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"), "net472;net8.0");

			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			Assert.Equal(1, CountOccurrences(content, "\nprimary\n"));
			Assert.Contains("\nprimary\n", GetSliceBlock(content, "net8.0"));
			Assert.DoesNotContain("\nprimary\n", GetSliceBlock(content, "net472"));
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_PrimaryMarkerOmittedForSingleSliceBecauseReaderFallsBackToFirstSlice()
	{
		// Single-slice output intentionally omits ``primary``: the data-model reader's
		// ``ToProjectDto`` treats ``slices[0]`` as primary when no slice carries the
		// marker. Skipping the marker keeps the cache stable when a project transitions
		// from multi-targeting to single-targeting.
		string dir = MakeTempDir();
		try
		{
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net472"), "Sample.csproj.slice"), SliceNet472);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"), "net472");

			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			Assert.Equal(0, CountOccurrences(content, "\nprimary\n"));
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_ThreeSlices_TwoSeparators()
	{
		string dir = MakeTempDir();
		try
		{
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net8.0"), "Sample.csproj.slice"), SliceNet8);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net9.0"), "Sample.csproj.slice"), SliceNet9);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net472"), "Sample.csproj.slice"), SliceNet472);
			string outPath = Path.Combine(dir, "out.lscache");

			int count = ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			Assert.Equal(3, count);
			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");

			int sepCount = 0;
			int idx = 0;
			while ((idx = content.IndexOf("\n---\n", idx, StringComparison.Ordinal)) >= 0)
			{
				sepCount++;
				idx += 5;
			}
			// Shared section + 3 per-TFM sections = 3 separators
			Assert.Equal(3, sepCount);
			Assert.Contains("TargetFramework=net472", content);
			Assert.Contains("TargetFramework=net8.0", content);
			Assert.Contains("TargetFramework=net9.0", content);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_OverwritesExistingOutput()
	{
		string dir = MakeTempDir();
		try
		{
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net8.0"), "Sample.csproj.slice"), SliceNet8);
			string outPath = Path.Combine(dir, "out.lscache");
			File.WriteAllText(outPath, "OLD_CONTENT");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			string content = File.ReadAllText(outPath);
			Assert.DoesNotContain("OLD_CONTENT", content);
			Assert.Contains("[project]", content);
			Assert.DoesNotContain("\n---\n", content);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_DeduplicatesSharedContent()
	{
		string dir = MakeTempDir();
		try
		{
			// Two slices with mostly identical content but different TFMs and one different property.
			string slice8 = "[project]\nlanguage=C#\nlastDtbSucceeded\n" +
				"[sliceDimensions]\nTargetFramework=net8.0\n" +
				"[properties]\nAssemblyName=MyApp\nTargetPath=bin/net8.0/MyApp.dll\n" +
				"[sourceFiles]\nProgram.cs\nHelper.cs\n" +
				"[metadataReferences]\nSystem.Runtime.dll\nSystem.Collections.dll\n";
			string slice9 = "[project]\nlanguage=C#\nlastDtbSucceeded\n" +
				"[sliceDimensions]\nTargetFramework=net9.0\n" +
				"[properties]\nAssemblyName=MyApp\nTargetPath=bin/net9.0/MyApp.dll\n" +
				"[sourceFiles]\nProgram.cs\nHelper.cs\n" +
				"[metadataReferences]\nSystem.Runtime.dll\nSystem.Collections.dll\nSystem.Text.Json.dll\n";

			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net8.0"), "Sample.csproj.slice"), slice8);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net9.0"), "Sample.csproj.slice"), slice9);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");

			// Shared properties should appear before the first --- separator.
			int firstSep = content.IndexOf("\n---\n", StringComparison.Ordinal);
			string sharedPart = content.Substring(0, firstSep);
			Assert.Contains("AssemblyName=MyApp", sharedPart);
			Assert.Contains("Program.cs", sharedPart);
			Assert.Contains("Helper.cs", sharedPart);
			Assert.Contains("System.Runtime.dll", sharedPart);
			Assert.Contains("System.Collections.dll", sharedPart);
			Assert.Contains("lastDtbSucceeded", sharedPart);

			// TFM-specific content should only be in per-TFM sections.
			Assert.DoesNotContain("TargetFramework=", sharedPart);
			string perTfmPart = content.Substring(firstSep);
			Assert.Contains("TargetPath=bin/net8.0/MyApp.dll", perTfmPart);
			Assert.Contains("TargetPath=bin/net9.0/MyApp.dll", perTfmPart);
			Assert.Contains("System.Text.Json.dll", perTfmPart);

			// Shared content should NOT be repeated in per-TFM sections.
			// Count occurrences of "Program.cs" in the whole file — should be exactly 1.
			int programCount = 0;
			int searchIdx = 0;
			while ((searchIdx = content.IndexOf("Program.cs", searchIdx, StringComparison.Ordinal)) >= 0)
			{
				programCount++;
				searchIdx += 10;
			}
			Assert.Equal(1, programCount);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void ParseSlice_ParsesAllSections()
	{
		string slice = "[project]\nlanguage=C#\nlastDtbSucceeded\nprimary\n" +
			"[sliceDimensions]\nTargetFramework=net8.0\n" +
			"[properties]\nAssemblyName=Test\nTargetPath=bin/Test.dll\n" +
			"[commandLineArguments]\n/noconfig\n/unsafe-\n" +
			"[sourceFiles]\nProgram.cs\n" +
			"[metadataReferences]\nSystem.dll\n" +
			"[analyzerReferences]\nMyAnalyzer.dll\n";

		ProjectDataMerger.SliceData data = ProjectDataMerger.ParseSlice(slice);

		Assert.Contains("lastDtbSucceeded", data.ProjectLines);
		Assert.DoesNotContain("primary", data.ProjectLines);
		Assert.True(data.IsPrimary);
		Assert.Single(data.SliceDimensions);
		Assert.Equal("TargetFramework=net8.0", data.SliceDimensions[0]);
		Assert.Equal(2, data.Properties.Count);
		Assert.Equal(2, data.ListSections["commandLineArguments"].Count);
		Assert.Single(data.ListSections["sourceFiles"]);
		Assert.Single(data.ListSections["metadataReferences"]);
		Assert.Single(data.ListSections["analyzerReferences"]);
	}

	[Fact]
	public void Merge_IsStableWhenSharedSdkAnalyzerConfigPolicyIsReParsed()
	{
		// Round-trip stability guard for the shared block's ``[sdkAnalyzerConfigPolicy]``
		// entries. ``ProjectDataMerger.ParseSlice`` calls ``CanonicalizeSdkAnalyzerConfigPolicyLine``
		// while parsing the merged file's shared block, where ``GetTargetFramework()``
		// returns ``null``. The canonicalizer is currently a no-op under null TFM, so a
		// line that lands in the shared block (because every per-TFM slice produced the
		// same value) survives a parse/emit round-trip unchanged. Pin that contract — if
		// the canonicalizer ever mutates already-canonical data under null TFM, this test
		// catches it before the cache starts thrashing on round-trip.
		string dir = MakeTempDir();
		try
		{
			string sharedPolicy = "Microsoft.NET.Sdk/analyzers|AnalysisMode=Default";
			string sliceNet8 =
				"[project]\nlanguage=C#\n" +
				"[sliceDimensions]\nTargetFramework=net8.0\n" +
				"[sdkAnalyzerConfigPolicy]\n" + sharedPolicy + "\n";
			string sliceNet9 =
				"[project]\nlanguage=C#\n" +
				"[sliceDimensions]\nTargetFramework=net9.0\n" +
				"[sdkAnalyzerConfigPolicy]\n" + sharedPolicy + "\n";

			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net8.0"), "Sample.csproj.slice"), sliceNet8);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net9.0"), "Sample.csproj.slice"), sliceNet9);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"), "net8.0;net9.0");
			string firstMerge = File.ReadAllText(outPath).Replace("\r\n", "\n");

			// Sanity-check the shared block actually contains the policy line.
			Assert.Contains($"\n{sharedPolicy}\n", firstMerge);

			// The merged file is headerless now (no leading hash line); the whole file is the
			// banner + structured content produced by ``WriteMergedContent``.
			string firstMergeBody = firstMerge;

			// Round-trip the merged file: parse it back to ``SliceData`` (which canonicalizes
			// the shared block under null TFM) and re-emit through ``WriteMergedContent``.
			// The result must be byte-identical to the original ``WriteMergedContent`` output.
			List<ProjectDataMerger.SliceData> reparsed = ProjectDataMerger.ParseMergedContent(firstMerge);
			using var sw = new StringWriter { NewLine = "\n" };
			ProjectDataMerger.WriteMergedContent(sw, reparsed, "net8.0;net9.0");

			Assert.Equal(firstMergeBody, sw.ToString());
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_IndentedSourceFilesUnderTfmSpecificDir_AreNotHoistedToShared()
	{
		// Regression test for: a child line ' ConsoleApp2.AssemblyInfo.cs' from
		// the per-TFM compressed group
		//   obj/Debug/net10.0/
		//    ConsoleApp2.AssemblyInfo.cs
		//    ConsoleApp2.GlobalUsings.g.cs
		// was being hoisted into the shared block (line-level intersection) without
		// its parent prefix line, producing a corrupt cache file like
		//   [sourceFiles]
		//    ConsoleApp2.AssemblyInfo.cs   <-- orphaned indented line
		//    ConsoleApp2.GlobalUsings.g.cs
		//   Program.cs
		// The fix groups indent-0 lines with their indented continuations and
		// intersects at the group level.
		string dir = MakeTempDir();
		try
		{
			string slice10 = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net10.0\n" +
				"[sourceFiles]\nProgram.cs\nobj/Debug/net10.0/\n ConsoleApp2.AssemblyInfo.cs\n ConsoleApp2.GlobalUsings.g.cs\n";
			string slice9 = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net9.0\n" +
				"[sourceFiles]\nProgram.cs\nobj/Debug/net9.0/\n ConsoleApp2.AssemblyInfo.cs\n ConsoleApp2.GlobalUsings.g.cs\n";

			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net10.0"), "Sample.csproj.slice"), slice10);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net9.0"), "Sample.csproj.slice"), slice9);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			int firstSep = content.IndexOf("\n---\n", StringComparison.Ordinal);
			string sharedPart = content.Substring(0, firstSep);
			string perTfmPart = content.Substring(firstSep);

			// Program.cs is the only line legitimately shared: it has no parent prefix.
			Assert.Contains("\nProgram.cs\n", sharedPart);

			// The orphaned indented child lines must not appear in the shared block
			// — they would be meaningless without their per-TFM directory header.
			Assert.DoesNotContain("\n ConsoleApp2.AssemblyInfo.cs\n", sharedPart);
			Assert.DoesNotContain("\n ConsoleApp2.GlobalUsings.g.cs\n", sharedPart);
			Assert.DoesNotContain("obj/Debug/net10.0/", sharedPart);
			Assert.DoesNotContain("obj/Debug/net9.0/", sharedPart);

			// Each per-TFM block keeps its complete group: prefix line + children together.
			Assert.Contains("obj/Debug/net10.0/\n ConsoleApp2.AssemblyInfo.cs\n ConsoleApp2.GlobalUsings.g.cs\n", perTfmPart);
			Assert.Contains("obj/Debug/net9.0/\n ConsoleApp2.AssemblyInfo.cs\n ConsoleApp2.GlobalUsings.g.cs\n", perTfmPart);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_IndentedGroupIdenticalAcrossTfms_IsShared()
	{
		// When both slices share the *same* compressed group (same prefix line +
		// same children), the entire group should be hoisted to the shared block.
		string dir = MakeTempDir();
		try
		{
			string sliceA = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net8.0\n" +
				"[metadataReferences]\n<NUGET>/\n foo/1.0/lib/net8.0/Foo.dll\n bar/2.0/lib/net8.0/Bar.dll\n";
			string sliceB = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net9.0\n" +
				"[metadataReferences]\n<NUGET>/\n foo/1.0/lib/net8.0/Foo.dll\n bar/2.0/lib/net8.0/Bar.dll\n";

			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net8.0"), "Sample.csproj.slice"), sliceA);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net9.0"), "Sample.csproj.slice"), sliceB);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			int firstSep = content.IndexOf("\n---\n", StringComparison.Ordinal);
			string sharedPart = content.Substring(0, firstSep);
			string perTfmPart = content.Substring(firstSep);

			// The whole group (header + both children) appears once, in shared.
			Assert.Contains("[metadataReferences]\n<NUGET>/\n foo/1.0/lib/net8.0/Foo.dll\n bar/2.0/lib/net8.0/Bar.dll\n", sharedPart);
			Assert.DoesNotContain("[metadataReferences]", perTfmPart);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_FrameworkPacksSection_IsPropagatedAndDeduplicated()
	{
		string dir = MakeTempDir();
		try
		{
			string sliceA = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net8.0\n" +
				"[frameworkPacks]\nMicrosoft.NETCore.App.Ref\n";
			string sliceB = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net9.0\n" +
				"[frameworkPacks]\nMicrosoft.NETCore.App.Ref\nMicrosoft.AspNetCore.App.Ref\n";

			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net8.0"), "Sample.csproj.slice"), sliceA);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net9.0"), "Sample.csproj.slice"), sliceB);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			int firstSep = content.IndexOf("\n---\n", StringComparison.Ordinal);
			string sharedPart = content.Substring(0, firstSep);
			string perTfmPart = content.Substring(firstSep);

			// NETCore is shared; AspNetCore is per-TFM.
			Assert.Contains("[frameworkPacks]\nMicrosoft.NETCore.App.Ref\n", sharedPart);
			Assert.DoesNotContain("Microsoft.AspNetCore.App.Ref", sharedPart);
			Assert.Contains("Microsoft.AspNetCore.App.Ref", perTfmPart);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_NetFxRefMetadataReferences_ArePropagatedAndDeduplicated()
	{
		string dir = MakeTempDir();
		try
		{
			string sliceA = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net472\n" +
				"[metadataReferences]\n<NETFXREF>/v4.7.2/mscorlib.dll\n";
			string sliceB = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net472-windows\n" +
				"[metadataReferences]\n<NETFXREF>/v4.7.2/mscorlib.dll\n<NETFXREF>/v4.7.2/System.dll\n";

			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net472"), "Sample.csproj.slice"), sliceA);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net472-windows"), "Sample.csproj.slice"), sliceB);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			int firstSep = content.IndexOf("\n---\n", StringComparison.Ordinal);
			string sharedPart = content.Substring(0, firstSep);
			string perTfmPart = content.Substring(firstSep);

			Assert.Contains("[metadataReferences]\n<NETFXREF>/v4.7.2/mscorlib.dll\n", sharedPart);
			Assert.DoesNotContain("<NETFXREF>/v4.7.2/System.dll", sharedPart);
			Assert.Contains("<NETFXREF>/v4.7.2/System.dll", perTfmPart);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_SdkAnalyzerSections_ArePropagatedAndDeduplicated()
	{
		string dir = MakeTempDir();
		try
		{
			string sliceA = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net10.0\n" +
				"[sdkAnalyzerPacks]\nMicrosoft.NET.ILLink.Tasks\n" +
				"[sdkAnalyzerConfigPolicy]\nMicrosoft.NET.Sdk/analyzers\n";
			string sliceB = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net11.0\n" +
				"[sdkAnalyzerPacks]\nMicrosoft.NET.ILLink.Tasks\nAnother.Sdk.AnalyzerPack\n" +
				"[sdkAnalyzerConfigPolicy]\nMicrosoft.NET.Sdk/analyzers\nMicrosoft.NET.Sdk/codestyle/cs\n";

			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net10.0"), "Sample.csproj.slice"), sliceA);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net11.0"), "Sample.csproj.slice"), sliceB);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			int firstSep = content.IndexOf("\n---\n", StringComparison.Ordinal);
			string sharedPart = content.Substring(0, firstSep);
			string perTfmPart = content.Substring(firstSep);

			Assert.Contains("[sdkAnalyzerPacks]\nMicrosoft.NET.ILLink.Tasks\n", sharedPart);
			Assert.DoesNotContain("Another.Sdk.AnalyzerPack", sharedPart);
			Assert.Contains("Another.Sdk.AnalyzerPack", perTfmPart);
			Assert.Contains("[sdkAnalyzerConfigPolicy]\nMicrosoft.NET.Sdk/analyzers\n", sharedPart);
			Assert.DoesNotContain("Microsoft.NET.Sdk/codestyle/cs", sharedPart);
			Assert.Contains("Microsoft.NET.Sdk/codestyle/cs", perTfmPart);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Fact]
	public void Merge_SdkAnalyzerConfigPolicy_CanonicalizesNumericDefaultAndLatest()
	{
		string dir = MakeTempDir();
		try
		{
			string sliceA = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net10.0\n" +
					"[properties]\nTargetFramework=net10.0\nTargetFrameworkIdentifier=.NETCoreApp\nTargetFrameworkVersion=v10.0\n" +
				"[sdkAnalyzerConfigPolicy]\nMicrosoft.NET.Sdk/analyzers|AnalysisLevel=10.0\nMicrosoft.NET.Sdk/codestyle/cs|AnalysisLevel=Latest|AnalysisMode=Default\n";
			string sliceB = "[project]\nlanguage=C#\n[sliceDimensions]\nTargetFramework=net8.0\n" +
					"[properties]\nTargetFramework=net8.0\nTargetFrameworkIdentifier=.NETCoreApp\nTargetFrameworkVersion=v8.0\n" +
				"[sdkAnalyzerConfigPolicy]\nMicrosoft.NET.Sdk/analyzers\nMicrosoft.NET.Sdk/codestyle/cs\n";

			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net10.0"), "Sample.csproj.slice"), sliceA);
			File.WriteAllText(Path.Combine(CreateSliceDir(dir, "net8.0"), "Sample.csproj.slice"), sliceB);
			string outPath = Path.Combine(dir, "out.lscache");

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"));

			string content = File.ReadAllText(outPath).Replace("\r\n", "\n");
			Assert.DoesNotContain("AnalysisLevel=10.0", content);
			Assert.Contains("Microsoft.NET.Sdk/analyzers\n", content);
			Assert.Contains("Microsoft.NET.Sdk/codestyle/cs|AnalysisMode=Default\n", content);
			Assert.DoesNotContain("AnalysisLevel=Latest", content);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}
}
