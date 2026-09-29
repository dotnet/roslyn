// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.NET.ProjectData.Tasks.Tests;

/// <summary>
/// Invariants that any well-formed <c>.lscache</c> file produced by the
/// <c>ProjectDataBuild</c> targets and <c>ProjectDataWriter</c> must satisfy.
/// Used by writer regression tests to guard against bug shapes that produce
/// internally inconsistent caches, regardless of the specific input that
/// surfaced the bug.
/// </summary>
internal static class LscacheInvariants
{
	/// <summary>
	/// Every line in the <c>[sdkAnalyzerConfigPolicy]</c> section refers to an
	/// SDK analyzer pack subfolder (e.g. <c>Microsoft.NET.Sdk/analyzers</c> or
	/// <c>Microsoft.NET.Sdk/codestyle/cs|...</c>). For each such line there
	/// must be at least one analyzer DLL listed under the matching
	/// <c>/Sdks/&lt;sdkPack&gt;/&lt;segment&gt;/</c> path in
	/// <c>[analyzerReferences]</c> — otherwise the cache claims to apply
	/// analyzer configuration to DLLs the SDK never produced.
	/// </summary>
	/// <remarks>
	/// Guards against regressing the
	/// <c>Gate [sdkAnalyzerConfigPolicy] lines on the SDK's analyzer-pack
	/// property gates</c> fix. The fix gated emission on
	/// <c>EnableNETAnalyzers</c> and <c>EnforceCodeStyleInBuild</c> to
	/// mirror the SDK's own conditions; any future code path that bypasses
	/// those gates (in the targets file, the writer, or a new policy type)
	/// will produce orphan policy lines that this assertion catches.
	/// </remarks>
	public static void AssertNoOrphanAnalyzerPolicyLines(string lscacheContent)
	{
		IReadOnlyList<string> policyLines = ExtractSection(lscacheContent, "sdkAnalyzerConfigPolicy", preserveIndent: false);
		if (policyLines.Count == 0)
		{
			return;
		}

		// [analyzerReferences] encodes paths as an indented tree where children
		// share their parent's prefix (e.g. parent `<NETSDK>/Sdks/Microsoft.NET.Sdk/`,
		// children `analyzers/`, `codestyle/cs/`). Rebuild the flat list of
		// full file paths so the invariant can be checked with a simple
		// substring match regardless of how the writer compresses prefixes.
		IReadOnlyList<string> analyzerReferencePaths = ReconstructIndentedTreePaths(
			ExtractSection(lscacheContent, "analyzerReferences", preserveIndent: true));
		string analyzerReferencesFlat = string.Join("\n", analyzerReferencePaths);

		List<string> orphans = [];
		foreach (string line in policyLines)
		{
			// Policy lines look like "Microsoft.NET.Sdk/analyzers" or
			// "Microsoft.NET.Sdk/codestyle/cs|AnalysisLevel=...". Strip the
			// argument segment after '|' and look for a matching SDK pack
			// subfolder in [analyzerReferences].
			int barIndex = line.IndexOf('|');
			string folderKey = barIndex >= 0 ? line[..barIndex] : line;
			string expectedFragment = $"/Sdks/{folderKey}/";
			if (!analyzerReferencesFlat.Contains(expectedFragment))
			{
				orphans.Add(line);
			}
		}

		Assert.True(
			orphans.Count == 0,
			$"Found {orphans.Count} orphan [sdkAnalyzerConfigPolicy] line(s) without matching analyzer DLLs in [analyzerReferences]: {string.Join("; ", orphans)}");
	}

	private static IReadOnlyList<string> ExtractSection(string lscacheContent, string sectionName, bool preserveIndent)
	{
		string[] lines = lscacheContent.Replace("\r\n", "\n").Split('\n');
		List<string> result = [];
		bool inSection = false;
		string sectionHeader = $"[{sectionName}]";
		foreach (string line in lines)
		{
			if (line.StartsWith('['))
			{
				inSection = line == sectionHeader;
				continue;
			}

			if (!inSection || line.Length == 0)
			{
				continue;
			}

			result.Add(preserveIndent ? line : line.TrimStart());
		}

		return result;
	}

	private static IReadOnlyList<string> ReconstructIndentedTreePaths(IReadOnlyList<string> indentedLines)
	{
		// Each line's leading-space count is its depth in the tree. A line ending
		// in '/' is a directory: its full path becomes the prefix for any deeper
		// lines until we return to that depth or shallower. A line not ending in
		// '/' is a file: emit its reconstructed full path.
		Stack<(int Depth, string Prefix)> stack = new();
		List<string> filePaths = [];
		foreach (string raw in indentedLines)
		{
			int depth = 0;
			while (depth < raw.Length && raw[depth] == ' ')
			{
				depth++;
			}

			string text = raw[depth..];
			while (stack.Count > 0 && stack.Peek().Depth >= depth)
			{
				stack.Pop();
			}

			string prefix = stack.Count > 0 ? stack.Peek().Prefix : string.Empty;
			string full = prefix + text;
			if (text.EndsWith('/'))
			{
				stack.Push((depth, full));
			}
			else
			{
				filePaths.Add(full);
			}
		}

		return filePaths;
	}
}

public sealed class LscacheInvariantsTests
{
	[Fact]
	public void AssertNoOrphanAnalyzerPolicyLines_NoPolicySection_Passes()
	{
		// Trivial: a cache with no policy section can't have orphan lines.
		const string content = "[someOtherSection]\nfoo=bar\n";
		LscacheInvariants.AssertNoOrphanAnalyzerPolicyLines(content);
	}

	[Fact]
	public void AssertNoOrphanAnalyzerPolicyLines_EveryPolicyLineHasMatchingReference_Passes()
	{
		const string content = """
			[sdkAnalyzerConfigPolicy]
			Microsoft.NET.Sdk/analyzers
			Microsoft.NET.Sdk/codestyle/cs|AnalysisLevel=latest
			[analyzerReferences]
			<NETSDK>/Sdks/Microsoft.NET.Sdk/analyzers/
			 Microsoft.CodeAnalysis.NetAnalyzers.dll
			<NETSDK>/Sdks/Microsoft.NET.Sdk/codestyle/cs/
			 Microsoft.CodeAnalysis.CSharp.CodeStyle.dll
			""";
		LscacheInvariants.AssertNoOrphanAnalyzerPolicyLines(content);
	}

	[Fact]
	public void AssertNoOrphanAnalyzerPolicyLines_OrphanAnalyzerLine_Throws()
	{
		// Mirrors the exact pre-fix bug shape: policy line exists but the
		// corresponding analyzer pack subfolder has no DLLs in [analyzerReferences].
		const string content = """
			[sdkAnalyzerConfigPolicy]
			Microsoft.NET.Sdk/analyzers
			[analyzerReferences]
			<NETSDK>/Sdks/Microsoft.NET.Sdk/codestyle/cs/
			 Microsoft.CodeAnalysis.CSharp.CodeStyle.dll
			""";
		Xunit.Sdk.XunitException ex = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
			() => LscacheInvariants.AssertNoOrphanAnalyzerPolicyLines(content));
		Assert.Contains("Microsoft.NET.Sdk/analyzers", ex.Message);
	}

	[Fact]
	public void AssertNoOrphanAnalyzerPolicyLines_OrphanCodeStyleLine_Throws()
	{
		const string content = """
			[sdkAnalyzerConfigPolicy]
			Microsoft.NET.Sdk/codestyle/cs|AnalysisLevel=latest
			[analyzerReferences]
			<NETSDK>/Sdks/Microsoft.NET.Sdk/analyzers/
			 Microsoft.CodeAnalysis.NetAnalyzers.dll
			""";
		Xunit.Sdk.XunitException ex = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
			() => LscacheInvariants.AssertNoOrphanAnalyzerPolicyLines(content));
		Assert.Contains("Microsoft.NET.Sdk/codestyle/cs", ex.Message);
	}

	[Fact]
	public void AssertNoOrphanAnalyzerPolicyLines_CompressedTreeWithSharedParent_Passes()
	{
		// The writer compresses common path prefixes in [analyzerReferences].
		// When both analyzers/ and codestyle/cs/ are present under the same SDK
		// pack, they are emitted as siblings under a shared parent line. The
		// invariant must reconstruct the tree before substring-matching the
		// folder key, otherwise it would spuriously fail on this real-world
		// output shape.
		const string content = """
			[sdkAnalyzerConfigPolicy]
			Microsoft.NET.Sdk/analyzers
			Microsoft.NET.Sdk/codestyle/cs|AnalysisLevel=latest
			[analyzerReferences]
			<NETSDK>/Sdks/Microsoft.NET.Sdk/
			 analyzers/
			  Microsoft.CodeAnalysis.NetAnalyzers.dll
			 codestyle/cs/
			  Microsoft.CodeAnalysis.CSharp.CodeStyle.dll
			""";
		LscacheInvariants.AssertNoOrphanAnalyzerPolicyLines(content);
	}
}
