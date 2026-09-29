// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.NET.ProjectData.Tasks.Tests;

/// <summary>
/// Tests for forward/backward compatibility of the <c>.lscache</c> format: an older writer that
/// re-generates a file authored by a newer minor version must carry the unknown data through
/// losslessly (so the file does not churn between team members), while the reader simply ignores it.
///
/// <para>The tests deliberately use a <see cref="FutureCacheBuilder"/> that injects <em>fake</em>
/// unknown sections / properties / item metadata and a <em>fake</em> higher minor version. There is
/// no real forward-compat field yet, so this is the only way to validate the machinery — and it is
/// the durable harness a future field-addition reuses: when a field becomes "known", flip it out of
/// the builder's unknown set and these same tests guard the transition.</para>
/// </summary>
public class ForwardCompatTests
{
	private const int CurrentMajor = 2;

	// --- Reusable fake-future fixture ------------------------------------------------------------

	/// <summary>
	/// Synthesizes single- and multi-target <c>.lscache</c> content with optional forward-compatible
	/// (unknown-to-the-current-writer) additions. Lines are assembled explicitly so leading-space
	/// significant constructs (indentation-compressed paths, <c>@metadata</c>) stay byte-exact.
	/// </summary>
	internal static class FutureCacheBuilder
	{
		public const string UnknownSectionName = "futureSection";
		public const string UnknownSectionPayload = "future-section-payload";
		public const string UnknownProperty = "FutureProp=onward";
		public const string UnknownMetadata = "@futureMeta=42";

		/// <summary>A self-consistent single-target cache body (headerless, LF-terminated).</summary>
		public static string SingleTarget(
			string version = "version=2",
			bool unknownSection = false,
			bool unknownProperty = false,
			bool unknownMetadata = false)
		{
			var lines = new List<string>
			{
				version,
				string.Empty,
				"[project]",
				"project=Sample.csproj",
				"language=C#",
				"lastDtbSucceeded",
				string.Empty,
				"[properties]",
				"AssemblyName=Sample",
			};
			if (unknownProperty) lines.Add(UnknownProperty);
			lines.Add("TargetFramework=net8.0");
			lines.Add(string.Empty);
			lines.Add("[sourceFiles]");
			lines.Add("Program.cs");
			if (unknownMetadata) lines.Add(" " + UnknownMetadata);
			lines.Add("Helpers/");
			lines.Add(" Util.cs");
			if (unknownSection)
			{
				lines.Add(string.Empty);
				lines.Add("[" + UnknownSectionName + "]");
				lines.Add(UnknownSectionPayload);
			}

			return string.Join("\n", lines) + "\n";
		}
	}

	// Replaces the leading version header line with <paramref name="newVersionLine"/>, mirroring how
	// a newer-minor writer stamps a file that carries data this (older) writer does not understand.
	private static string BumpPrimaryVersion(string text, string newVersionLine)
	{
		int nl = text.IndexOf('\n');
		return nl < 0 ? newVersionLine : newVersionLine + text.Substring(nl);
	}

	// --- ForwardCompat.PreserveUnknownData: unit tests -------------------------------------------

	[Fact]
	public void Preserves_Unknown_Section()
	{
		string existing = FutureCacheBuilder.SingleTarget(version: "version=2.3", unknownSection: true);
		string candidate = FutureCacheBuilder.SingleTarget();

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		Assert.Contains("[" + FutureCacheBuilder.UnknownSectionName + "]", merged);
		Assert.Contains(FutureCacheBuilder.UnknownSectionPayload, merged);
		// Known content is untouched.
		Assert.Contains("AssemblyName=Sample", merged);
		Assert.Contains("Helpers/\n Util.cs", merged);
	}

	// Multiple unknown sections must round-trip in the newer writer's file order, NOT be re-sorted.
	// Re-sorting would move sections relative to where the newer minor put them, so an older minor
	// and a newer minor would fight over the layout and churn the file on every alternating write.
	// Encounter order here is z-before-a (the reverse of ordinal), so a stray re-sort would flip it.
	[Fact]
	public void Preserves_Unknown_Sections_In_File_Order_Not_Resorted()
	{
		string existing = string.Join("\n", new[]
		{
			"version=2.3",
			"",
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"lastDtbSucceeded",
			"",
			"[zSection]",
			"z-payload",
			"",
			"[aSection]",
			"a-payload",
		}) + "\n";
		string candidate = FutureCacheBuilder.SingleTarget();

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		int z = merged.IndexOf("[zSection]", StringComparison.Ordinal);
		int a = merged.IndexOf("[aSection]", StringComparison.Ordinal);
		Assert.True(z >= 0 && a >= 0, "both unknown sections must be preserved");
		Assert.True(z < a, "unknown sections must keep their original file order, not be re-sorted");
	}

	[Fact]
	public void Preserves_Unknown_Section_Between_Known_Sections_In_Same_Gap()
	{
		string existing = string.Join("\n", new[]
		{
			"version=2.3",
			"",
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"",
			"[properties]",
			"AssemblyName=Sample",
			"",
			"[futureCopyToOutputItems]",
			"content.txt",
			"",
			"[projectReferences]",
			"Referenced.csproj",
			"",
			"[capabilities]",
			"CSharp",
		}) + "\n";
		string candidate = string.Join("\n", new[]
		{
			"version=2",
			"",
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"",
			"[properties]",
			"AssemblyName=Sample",
			"",
			"[projectReferences]",
			"Referenced.csproj",
			"",
			"[capabilities]",
			"CSharp",
		}) + "\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		int properties = merged.IndexOf("[properties]", StringComparison.Ordinal);
		int unknown = merged.IndexOf("[futureCopyToOutputItems]", StringComparison.Ordinal);
		int projectReferences = merged.IndexOf("[projectReferences]", StringComparison.Ordinal);
		int capabilities = merged.IndexOf("[capabilities]", StringComparison.Ordinal);
		Assert.True(properties >= 0 && unknown >= 0 && projectReferences >= 0 && capabilities >= 0, "all sections must be present");
		Assert.True(properties < unknown, "the unknown section should remain after its preceding known section");
		Assert.True(unknown < projectReferences, "the unknown section should remain before its following known section instead of moving to the segment end");
		Assert.True(projectReferences < capabilities, "later known sections should not move ahead of the preserved unknown section's original gap");
	}

	[Fact]
	public void Preserves_Multiple_Unknown_Sections_In_Same_Gap_In_File_Order()
	{
		string existing = string.Join("\n", new[]
		{
			"version=2.3",
			"",
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"",
			"[properties]",
			"AssemblyName=Sample",
			"",
			"[futureCopyToOutputItems]",
			"content.txt",
			"",
			"[futureUpToDateCheckBuilt]",
			"bin/Debug/Sample.dll",
			"",
			"[projectReferences]",
			"Referenced.csproj",
			"",
			"[capabilities]",
			"CSharp",
		}) + "\n";
		string candidate = string.Join("\n", new[]
		{
			"version=2",
			"",
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"",
			"[properties]",
			"AssemblyName=Sample",
			"",
			"[projectReferences]",
			"Referenced.csproj",
			"",
			"[capabilities]",
			"CSharp",
		}) + "\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		int copyToOutputItems = merged.IndexOf("[futureCopyToOutputItems]", StringComparison.Ordinal);
		int upToDateCheckBuilt = merged.IndexOf("[futureUpToDateCheckBuilt]", StringComparison.Ordinal);
		int projectReferences = merged.IndexOf("[projectReferences]", StringComparison.Ordinal);
		Assert.True(copyToOutputItems >= 0 && upToDateCheckBuilt >= 0 && projectReferences >= 0, "all sections must be present");
		Assert.True(copyToOutputItems < upToDateCheckBuilt, "unknown sections in the same gap must keep their original encounter order");
		Assert.True(upToDateCheckBuilt < projectReferences, "all unknown sections in the gap should stay before the following known section");
	}

	[Fact]
	public void Preserves_Unknown_Section_Before_First_Known_Section_In_PerSliceSegment()
	{
		string existing = string.Join("\n", new[]
		{
			"version=2.3",
			"[project]",
			"language=C#",
			"",
			"[sliceDimensions]",
			"TargetFramework=net8.0",
			"---",
			"[futureSection]",
			"future-payload",
			"",
			"[project]",
			"language=C#",
			"",
			"[sliceDimensions]",
			"TargetFramework=net9.0",
		}) + "\n";
		string candidate = string.Join("\n", new[]
		{
			"version=2",
			"[project]",
			"language=C#",
			"",
			"[sliceDimensions]",
			"TargetFramework=net8.0",
			"---",
			"[project]",
			"language=C#",
			"",
			"[sliceDimensions]",
			"TargetFramework=net9.0",
		}) + "\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		int separator = merged.IndexOf("---", StringComparison.Ordinal);
		int futureSection = merged.IndexOf("[futureSection]", StringComparison.Ordinal);
		int secondProject = merged.IndexOf("[project]", separator, StringComparison.Ordinal);
		int secondProjectLanguage = merged.IndexOf("language=C#", secondProject, StringComparison.Ordinal);
		Assert.True(separator >= 0 && futureSection >= 0 && secondProject >= 0 && secondProjectLanguage >= 0, "the second slice and unknown section must be present");
		Assert.True(separator < futureSection, "the unknown section should be inserted after the slice separator");
		Assert.True(futureSection < secondProject, "the unknown section should stay before the following known section header");
		Assert.True(secondProject < secondProjectLanguage, "the known section's content should stay under its own header");
	}

	// The last item in the file carries unknown @metadata AND there is an unknown whole section to
	// append. Both anchor at the file's last content line; the metadata must stay attached to its
	// item, with the appended section AFTER it — otherwise the @metadata would re-parse under the
	// appended section on the next read (wrong item/section).
	[Fact]
	public void Preserves_Last_Item_Metadata_Before_Appended_Unknown_Section()
	{
		string existing = string.Join("\n", new[]
		{
			"version=2.1",
			"",
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"lastDtbSucceeded",
			"",
			"[sourceFiles]",
			"Program.cs",
			" @futureMeta=42",
			"",
			"[futureSection]",
			"future-payload",
		}) + "\n";
		string candidate = string.Join("\n", new[]
		{
			"version=2",
			"",
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"lastDtbSucceeded",
			"",
			"[sourceFiles]",
			"Program.cs",
		}) + "\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		int meta = merged.IndexOf("@futureMeta=42", StringComparison.Ordinal);
		int section = merged.IndexOf("[futureSection]", StringComparison.Ordinal);
		Assert.True(meta >= 0 && section >= 0, "both the metadata and the unknown section must be preserved");
		Assert.True(meta < section, "the item's @metadata must stay before the appended unknown section");
		Assert.Contains("Program.cs\n @futureMeta=42", merged);
	}

	// Two unknown @metadata lines on one item, in non-ordinal order (@zMeta before @aMeta). A newer
	// writer emits them in its own order; an older writer must preserve that file order, not re-sort
	// by content (which would churn the cache in mixed-version teams).
	[Fact]
	public void Preserves_Unknown_Metadata_In_File_Order_Not_Resorted()
	{
		string existing = string.Join("\n", new[]
		{
			"version=2.1",
			"",
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"lastDtbSucceeded",
			"",
			"[sourceFiles]",
			"Program.cs",
			" @zMeta=1",
			" @aMeta=2",
		}) + "\n";
		string candidate = string.Join("\n", new[]
		{
			"version=2",
			"",
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"lastDtbSucceeded",
			"",
			"[sourceFiles]",
			"Program.cs",
		}) + "\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		int z = merged.IndexOf("@zMeta=1", StringComparison.Ordinal);
		int a = merged.IndexOf("@aMeta=2", StringComparison.Ordinal);
		Assert.True(z >= 0 && a >= 0, "both unknown metadata lines must be preserved");
		Assert.True(z < a, "unknown metadata must keep its original file order, not be re-sorted");
	}

	[Fact]
	public void Preserves_Unknown_Property_In_Sorted_Position()
	{
		string existing = FutureCacheBuilder.SingleTarget(version: "version=2.3", unknownProperty: true);
		string candidate = FutureCacheBuilder.SingleTarget();

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		// Inserted into the already-sorted [properties] block between AssemblyName and TargetFramework.
		Assert.Contains("AssemblyName=Sample\nFutureProp=onward\nTargetFramework=net8.0", merged);
	}

	// Regression: when the candidate has NO [properties] section and the chosen anchor section is
	// header-only, its LastLineIndex is -1. Reassemble drops insertions keyed at -1, so the created
	// [properties] block must anchor on the header line instead — otherwise the preserved unknown
	// property is silently lost.
	[Fact]
	public void Preserves_Unknown_Property_When_Candidate_Lacks_Properties_And_Anchor_Is_HeaderOnly()
	{
		string existing = string.Join("\n", new[]
		{
			"version=2.1",
			"",
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"lastDtbSucceeded",
			"",
			"[properties]",
			"FutureProp=onward",
		}) + "\n";
		// Degenerate candidate: a header-only [project] (LastLineIndex == -1) and no [properties].
		string candidate = "version=2\n\n[project]\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		Assert.Contains("[properties]", merged);
		Assert.Contains("FutureProp=onward", merged);
	}

	// Defensive: a candidate segment with no sections at all must neither throw (the old code indexed
	// Sections[Count - 1]) nor drop the preserved property (no anchor → segment last content line).
	[Fact]
	public void Preserves_Unknown_Property_When_Candidate_Has_No_Sections()
	{
		string existing = string.Join("\n", new[]
		{
			"version=2.1",
			"",
			"[properties]",
			"FutureProp=onward",
		}) + "\n";
		string candidate = "version=2\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		Assert.Contains("FutureProp=onward", merged);
	}

	[Fact]
	public void Preserves_Unknown_Metadata_On_Matching_Item()
	{
		string existing = FutureCacheBuilder.SingleTarget(version: "version=2.3", unknownMetadata: true);
		string candidate = FutureCacheBuilder.SingleTarget();

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		// Reattached at metadata indentation (one space) directly under Program.cs.
		Assert.Contains("Program.cs\n @futureMeta=42", merged);
	}

	[Fact]
	public void Drops_Unknown_Metadata_When_Item_Removed_From_Candidate()
	{
		// Existing has @futureMeta on a source file the candidate no longer lists.
		string existing = string.Join("\n", new[]
		{
			"version=2.3",
			string.Empty,
			"[project]",
			"language=C#",
			string.Empty,
			"[sourceFiles]",
			"Gone.cs",
			" @futureMeta=42",
		}) + "\n";
		string candidate = string.Join("\n", new[]
		{
			"version=2",
			string.Empty,
			"[project]",
			"language=C#",
			string.Empty,
			"[sourceFiles]",
			"Program.cs",
		}) + "\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		Assert.DoesNotContain("@futureMeta", merged);
		Assert.DoesNotContain("Gone.cs", merged);
	}

	[Fact]
	public void Does_Not_Resurrect_Known_Data_The_Current_Build_Did_Not_Produce()
	{
		// The cross-environment hazard the preservation design must NOT introduce: env A built the
		// project, so its cache lists a generated source (Generated.g.cs) and a known [properties]
		// key (RootNamespace). Env B regenerates the cache WITHOUT those outputs — e.g. the project
		// was not (fully) built there, so the generated file and the property are legitimately
		// absent. Both are data the current writer KNOWS HOW TO EMIT, so the freshly generated
		// candidate is authoritative. Preservation only carries forward data the writer CANNOT
		// regenerate; it must never splice known-but-currently-absent values back in, or it would
		// mask the fact that env B's build produced nothing and resurrect stale outputs.
		string existing = string.Join("\n", new[]
		{
			"version=2",
			string.Empty,
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"lastDtbSucceeded",
			string.Empty,
			"[properties]",
			"AssemblyName=Sample",
			"RootNamespace=Sample.App", // known property present in A, absent in B
			"TargetFramework=net8.0",
			string.Empty,
			"[sourceFiles]",
			"Program.cs",
			"Generated.g.cs", // known item: a generated output present only in A
		}) + "\n";
		string candidate = string.Join("\n", new[]
		{
			"version=2",
			string.Empty,
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			"lastDtbSucceeded",
			string.Empty,
			"[properties]",
			"AssemblyName=Sample",
			"TargetFramework=net8.0",
			string.Empty,
			"[sourceFiles]",
			"Program.cs",
		}) + "\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		// Nothing the current writer can emit is resurrected: the candidate wins verbatim (same
		// reference, since there is no forward-compatible data to splice).
		Assert.Same(candidate, merged);
		Assert.DoesNotContain("Generated.g.cs", merged);
		Assert.DoesNotContain("RootNamespace", merged);
	}

	[Fact]
	public void Preserves_Known_Metadata_Name_Reused_On_A_Different_Item_Type()
	{
		// Forward-compat hazard the per-section metadata gate exists to close: a newer minor reuses
		// an EXISTING metadata name (@link — today emitted only on [sourceFiles]/Compile items) on a
		// DIFFERENT item type ([metadataReferences]/MetadataReference, which never emits @link). The
		// older writer does not produce @link there, so it must treat it as unknown and carry it
		// forward. A flattened "known metadata" union would mistake it for regenerable data and drop
		// it (resurfacing the exact churn this feature prevents).
		string existing = string.Join("\n", new[]
		{
			"version=2.4",
			string.Empty,
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			string.Empty,
			"[metadataReferences]",
			"Ref.dll",
			" @link=carried/forward",
		}) + "\n";
		string candidate = string.Join("\n", new[]
		{
			"version=2",
			string.Empty,
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			string.Empty,
			"[metadataReferences]",
			"Ref.dll",
		}) + "\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		Assert.Contains("Ref.dll\n @link=carried/forward", merged);
	}

	[Fact]
	public void Drops_Metadata_That_Is_Known_For_The_Section()
	{
		// The flip side of the test above: @aliases IS known for [metadataReferences], so an existing
		// @aliases the candidate did not regenerate is data the writer KNOWS HOW TO EMIT and must not
		// resurrect (the candidate is authoritative — the reference legitimately has no alias now).
		string existing = string.Join("\n", new[]
		{
			"version=2.4",
			string.Empty,
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			string.Empty,
			"[metadataReferences]",
			"Ref.dll",
			" @aliases=Old",
		}) + "\n";
		string candidate = string.Join("\n", new[]
		{
			"version=2",
			string.Empty,
			"[project]",
			"project=Sample.csproj",
			"language=C#",
			string.Empty,
			"[metadataReferences]",
			"Ref.dll",
		}) + "\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		Assert.DoesNotContain("@aliases", merged);
	}

	[Fact]
	public void Ignores_Unknown_Data_When_Existing_Major_Differs()
	{
		string existing = FutureCacheBuilder.SingleTarget(version: "version=3.0", unknownSection: true, unknownProperty: true, unknownMetadata: true);
		string candidate = FutureCacheBuilder.SingleTarget();

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		// A different major is an incompatible format: nothing is preserved and the candidate is
		// returned unchanged (the writer then overwrites the file wholesale).
		Assert.Same(candidate, merged);
	}

	[Fact]
	public void Returns_Same_Reference_When_Nothing_To_Preserve()
	{
		string existing = FutureCacheBuilder.SingleTarget();
		string candidate = FutureCacheBuilder.SingleTarget();

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		Assert.Same(candidate, merged);
	}

	[Fact]
	public void Carries_Forward_Higher_Minor_Version_Stamp()
	{
		// Existing is a newer minor with no other unknown data; the stamp must be carried forward so
		// the file does not flip-flop minors as different versions open it.
		string existing = FutureCacheBuilder.SingleTarget(version: "version=2.5");
		string candidate = FutureCacheBuilder.SingleTarget(version: "version=2");

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		Assert.StartsWith("version=2.5\n", merged);
	}

	[Fact]
	public void Does_Not_Downgrade_Version_Stamp()
	{
		// Existing is an OLDER (or equal) minor: keep the candidate's own stamp.
		string existing = FutureCacheBuilder.SingleTarget(version: "version=2", unknownSection: true);
		string candidate = FutureCacheBuilder.SingleTarget(version: "version=2");

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		Assert.StartsWith("version=2\n", merged);
	}

	[Theory]
	// existing newer minor, same major → gate open (preserve can carry data forward)
	[InlineData("version=2.4", "version=2", 2, true)]
	[InlineData("version=2.1", "version=2.0", 2, true)]
	// same or older minor → gate closed (nothing this writer can't already produce)
	[InlineData("version=2", "version=2", 2, false)]
	[InlineData("version=2.0", "version=2", 2, false)]
	[InlineData("version=2", "version=2.4", 2, false)]
	// present-but-unorderable existing minor (multi-part / non-numeric) → gate OPEN, conservatively,
	// so a newer writer that stamps a non-integer minor never causes us to skip preservation
	[InlineData("version=2.0.5", "version=2", 2, true)]
	[InlineData("version=2.3-preview", "version=2", 2, true)]
	// different / unparseable major → gate closed (incompatible or no header)
	[InlineData("version=3.0", "version=2", 2, false)]
	[InlineData("garbage", "version=2", 2, false)]
	public void ExistingHasNewerMinor_MirrorsPreservationGate(string existingVersion, string candidateVersion, int currentMajor, bool expected)
	{
		// The byte-level pre-check must agree with PreserveUnknownData's own version gate: it opens
		// (returns true) for exactly — and only — the existing-newer-minor, same-major case.
		byte[] existing = Encoding.UTF8.GetBytes(FutureCacheBuilder.SingleTarget(version: existingVersion));
		byte[] candidate = Encoding.UTF8.GetBytes(FutureCacheBuilder.SingleTarget(version: candidateVersion));

		bool gate = ForwardCompat.ExistingHasNewerMinor(existing, candidate, currentMajor);

		Assert.Equal(expected, gate);

		// Soundness: the gate must never be more restrictive than the actual transform. Whenever the
		// gate is closed, PreserveUnknownData must return the candidate unchanged (no data to carry).
		if (!gate)
		{
			string existingText = Encoding.UTF8.GetString(existing);
			string candidateText = Encoding.UTF8.GetString(candidate);
			Assert.Same(candidateText, ForwardCompat.PreserveUnknownData(existingText, candidateText, currentMajor));
		}
	}

	[Fact]
	public void ExistingHasNewerMinor_SkipsLeadingCommentLines()
	{
		// A leading comment line must not hide the version header from the byte probe.
		byte[] existing = Encoding.UTF8.GetBytes("# leading comment\nversion=2.4\n");
		byte[] candidate = Encoding.UTF8.GetBytes(FutureCacheBuilder.SingleTarget(version: "version=2"));

		Assert.True(ForwardCompat.ExistingHasNewerMinor(existing, candidate, CurrentMajor));
	}

	[Fact]
	public void ExistingHasNewerMinor_SkipsLeadingLegacyHashLine()
	{
		// A legacy committed cache still leads with a hash= header before version=. The byte probe
		// must skip it (like the reader) so a newer-minor legacy file is still recognized for
		// preservation instead of being treated as an unparseable header (which would drop data).
		byte[] existing = Encoding.UTF8.GetBytes(
			"hash=0000000000000000000000000000000000000000000000000000000000000000\nversion=2.4\n");
		byte[] candidate = Encoding.UTF8.GetBytes(FutureCacheBuilder.SingleTarget(version: "version=2"));

		Assert.True(ForwardCompat.ExistingHasNewerMinor(existing, candidate, CurrentMajor));
	}

	[Fact]
	public void Preserves_All_Levels_Together_Deterministically()
	{
		string existing = FutureCacheBuilder.SingleTarget(version: "version=2.4", unknownSection: true, unknownProperty: true, unknownMetadata: true);
		string candidate = FutureCacheBuilder.SingleTarget();

		string first = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);
		// Feeding the merged output back in as the existing file is a no-op: byte-stable, no churn.
		string second = ForwardCompat.PreserveUnknownData(first, candidate, CurrentMajor);

		Assert.Equal(first, second);
		Assert.Contains("[futureSection]", first);
		Assert.Contains("FutureProp=onward", first);
		Assert.Contains("Program.cs\n @futureMeta=42", first);
		Assert.StartsWith("version=2.4\n", first);
	}

	[Fact]
	public void Preserves_Unknown_Data_When_Existing_Minor_Is_Malformed()
	{
		// A newer writer that stamps a non-integer / multi-part minor (e.g. "2.0.5", "2.3-preview")
		// must not cause an older writer to silently drop its unknown data. The gate treats an
		// unorderable minor conservatively (preserve), so the future data survives.
		string existing = FutureCacheBuilder.SingleTarget(version: "version=2.0.5", unknownSection: true, unknownProperty: true);
		string candidate = FutureCacheBuilder.SingleTarget(version: "version=2");

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		Assert.Contains("[" + FutureCacheBuilder.UnknownSectionName + "]", merged);
		Assert.Contains(FutureCacheBuilder.UnknownProperty, merged);
	}

	[Fact]
	public void Preserves_PerSlice_Metadata_On_The_Correct_Slice_Only()
	{
		// Multi-TFM attribution: both slices list the same item path (Program.cs) but carry DISTINCT
		// unknown metadata. Segments are matched by their [sliceDimensions] identity, so each slice's
		// forward-compat metadata must land back on THAT slice's item — never bleed across slices,
		// even though the item paths collide.
		string slice(string tfm, string? meta) => string.Join("\n", new[]
		{
			"[project]",
			"language=C#",
			string.Empty,
			"[sliceDimensions]",
			$"TargetFramework={tfm}",
			string.Empty,
			"[sourceFiles]",
			"Program.cs",
		}.Concat(meta is null ? Array.Empty<string>() : new[] { " " + meta }));

		string existing = "version=2.4\n" + slice("net8.0", "@futureMetaA=8") + "\n---\n" + slice("net9.0", "@futureMetaB=9") + "\n";
		string candidate = "version=2\n" + slice("net8.0", null) + "\n---\n" + slice("net9.0", null) + "\n";

		string merged = ForwardCompat.PreserveUnknownData(existing, candidate, CurrentMajor);

		// Each metadatum appears exactly once, attached under its own slice.
		int metaACount = Regex.Matches(merged, "@futureMetaA").Count;
		int metaBCount = Regex.Matches(merged, "@futureMetaB").Count;
		Assert.Equal(1, metaACount);
		Assert.Equal(1, metaBCount);

		int net8 = merged.IndexOf("TargetFramework=net8.0", StringComparison.Ordinal);
		int net9 = merged.IndexOf("TargetFramework=net9.0", StringComparison.Ordinal);
		int metaA = merged.IndexOf("@futureMetaA", StringComparison.Ordinal);
		int metaB = merged.IndexOf("@futureMetaB", StringComparison.Ordinal);

		// @futureMetaA belongs to the net8.0 segment, @futureMetaB to the net9.0 segment.
		Assert.InRange(metaA, net8, net9);
		Assert.True(metaB > net9);
	}

	// --- End-to-end through the writer (single-TFM path) -----------------------------------------

	[Fact]
	public void AtomicWriteStreamed_PreservesUnknownData_AndStripsLegacyHash()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			Directory.CreateDirectory(dir);

			// On disk: a file a NEWER minor authored, still carrying a legacy hash header.
			string newer = FutureCacheBuilder.SingleTarget(version: "version=2.4", unknownSection: true, unknownProperty: true, unknownMetadata: true);
			File.WriteAllText(outputPath, $"hash={new string('0', 64)}\n{newer}", new UTF8Encoding(false));

			// The current (older) writer regenerates the candidate it knows about.
			string candidate = FutureCacheBuilder.SingleTarget();
			ProjectDataWriter.AtomicWriteStreamed(outputPath, w => w.Write(candidate));

			string result = File.ReadAllText(outputPath);

			Assert.DoesNotContain("hash=", result);
			Assert.StartsWith("version=2.4\n", result);   // newer minor stamp carried forward
			Assert.Contains("[futureSection]", result);
			Assert.Contains("FutureProp=onward", result);
			Assert.Contains("Program.cs\n @futureMeta=42", result);
			Assert.Contains("AssemblyName=Sample", result);
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWriteStreamed_IsNoOp_OnRepeatedWrite_WithPreservedData()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			Directory.CreateDirectory(dir);
			string newer = FutureCacheBuilder.SingleTarget(version: "version=2.4", unknownSection: true, unknownProperty: true, unknownMetadata: true);
			File.WriteAllText(outputPath, $"hash={new string('0', 64)}\n{newer}", new UTF8Encoding(false));

			string candidate = FutureCacheBuilder.SingleTarget();
			ProjectDataWriter.AtomicWriteStreamed(outputPath, w => w.Write(candidate)); // migrates + preserves
			DateTime afterMigration = File.GetLastWriteTimeUtc(outputPath);

			Thread.Sleep(1100);
			ProjectDataWriter.AtomicWriteStreamed(outputPath, w => w.Write(candidate)); // must skip

			Assert.Equal(afterMigration, File.GetLastWriteTimeUtc(outputPath));
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void AtomicWriteStreamed_OverwritesIncompatibleMajor()
	{
		string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string outputPath = Path.Combine(dir, "test.lscache");
		try
		{
			Directory.CreateDirectory(dir);
			string future = FutureCacheBuilder.SingleTarget(version: "version=3.0", unknownSection: true);
			File.WriteAllText(outputPath, future, new UTF8Encoding(false));

			string candidate = FutureCacheBuilder.SingleTarget();
			ProjectDataWriter.AtomicWriteStreamed(outputPath, w => w.Write(candidate));

			string result = File.ReadAllText(outputPath);
			Assert.Equal(candidate, result);
			Assert.DoesNotContain("futureSection", result);
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	// --- End-to-end through the merger (multi-TFM path) ------------------------------------------

	[Fact]
	public void Merge_PreservesUnknownData_FromExistingMergedFile()
	{
		string dir = Path.Combine(Path.GetTempPath(), "lscache-fc-" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(dir);

			string net8Dir = Path.Combine(dir, "obj", "Debug", "net8.0");
			string net9Dir = Path.Combine(dir, "obj", "Debug", "net9.0");
			Directory.CreateDirectory(net8Dir);
			Directory.CreateDirectory(net9Dir);

			// Both slices share Program.cs and a shared property, so the merged file has a shared block.
			string slice(string tfm) => string.Join("\n", new[]
			{
				"[project]",
				"language=C#",
				"[sliceDimensions]",
				$"TargetFramework={tfm}",
				"[properties]",
				"AssemblyName=Sample",
				"[sourceFiles]",
				"Program.cs",
			}) + "\n";
			File.WriteAllText(Path.Combine(net8Dir, "Sample.csproj.slice"), slice("net8.0"));
			File.WriteAllText(Path.Combine(net9Dir, "Sample.csproj.slice"), slice("net9.0"));

			string outPath = Path.Combine(dir, "out.lscache");

			// First merge to capture the canonical merged shape, then inject unknown data into the
			// shared block AND bump the stamp to a newer minor — exactly how a newer writer would
			// have authored this file (forward-compatible data always rides a newer-minor stamp).
			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"), "net8.0;net9.0");
			string merged = File.ReadAllText(outPath).Replace("\r\n", "\n");

			string newer = BumpPrimaryVersion(merged, "version=2.4")
				.Replace("AssemblyName=Sample", "AssemblyName=Sample\nFutureProp=onward")
				.Replace("Program.cs\n", "Program.cs\n @futureMeta=42\n");
			File.WriteAllText(outPath, newer, new UTF8Encoding(false));

			// Re-merge with the current (older) writer: it must carry the unknown data forward.
			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"), "net8.0;net9.0");
			string result = File.ReadAllText(outPath).Replace("\r\n", "\n");

			Assert.Contains("FutureProp=onward", result);
			Assert.Contains("Program.cs\n @futureMeta=42", result);
			Assert.DoesNotContain("hash=", result);
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void Merge_PreservesUnknownSection_InPerTargetSlice()
	{
		string dir = Path.Combine(Path.GetTempPath(), "lscache-fc-" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(dir);
			string net8Dir = Path.Combine(dir, "obj", "Debug", "net8.0");
			string net9Dir = Path.Combine(dir, "obj", "Debug", "net9.0");
			Directory.CreateDirectory(net8Dir);
			Directory.CreateDirectory(net9Dir);

			// Distinct command-line arguments keep the slices from collapsing into a shared block,
			// so each TargetFramework gets its own segment.
			string slice(string tfm, string arg) => string.Join("\n", new[]
			{
				"[project]",
				"language=C#",
				"[sliceDimensions]",
				$"TargetFramework={tfm}",
				"[commandLineArguments]",
				arg,
			}) + "\n";
			File.WriteAllText(Path.Combine(net8Dir, "Sample.csproj.slice"), slice("net8.0", "/define:NET8"));
			File.WriteAllText(Path.Combine(net9Dir, "Sample.csproj.slice"), slice("net9.0", "/define:NET9"));

			string outPath = Path.Combine(dir, "out.lscache");
			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"), "net8.0;net9.0");
			string merged = File.ReadAllText(outPath).Replace("\r\n", "\n");

			// Inject an unknown section AND bump the stamp to a newer minor — forward-compatible data
			// is only ever authored by a newer-minor writer, which stamps the file accordingly.
			int net9Index = merged.IndexOf("TargetFramework=net9.0", StringComparison.Ordinal);
			Assert.True(net9Index >= 0);
			string newer = BumpPrimaryVersion(merged, "version=2.4") + "\n[futureSection]\nfuture-section-payload\n";
			File.WriteAllText(outPath, newer, new UTF8Encoding(false));

			ProjectDataMerger.Merge(outPath, Path.Combine(dir, "obj", "**", "Sample.csproj.slice"), "net8.0;net9.0");
			string result = File.ReadAllText(outPath).Replace("\r\n", "\n");

			Assert.Contains("[futureSection]", result);
			Assert.Contains("future-section-payload", result);
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	// --- Guard: the generated .props allow-list must match ProjectProperties.All ----------------

	[Fact]
	public void GeneratedProps_Match_ProjectProperties_All()
	{
		// The writer's [properties] allow-list is every schema property, generated into BOTH
		// ProjectProperties.All (C#, consumed here and by forward-compat) and the committed
		// Microsoft.NET.ProjectData.Schema.props (_ProjectDataProperties, consumed by MSBuild). They
		// are generated from the same schema by different generators (C# source generator vs the Node
		// script), so this guard fails the build if one was regenerated and the other was not — i.e.
		// it catches a stale committed .props.
		string propsPath = Path.Combine(AppContext.BaseDirectory, "Microsoft.NET.ProjectData.Schema.props");
		Assert.True(File.Exists(propsPath), $"Generated props file not found at {propsPath}.");

		string props = File.ReadAllText(propsPath);
		var fromProps = new HashSet<string>(
			Regex.Matches(props, "_ProjectDataProperties Include=\"(?<name>[^\"]+)\"")
				.Select(m => m.Groups["name"].Value),
			StringComparer.Ordinal);

		var all = new HashSet<string>(ProjectProperties.All, StringComparer.Ordinal);

		Assert.NotEmpty(fromProps);
		Assert.True(
			fromProps.SetEquals(all),
			"Microsoft.NET.ProjectData.Schema.props is out of sync with ProjectProperties.All. " +
			"Regenerate with `node tools/generate-schema-types.js`.\n" +
			$"In .props but not All: {string.Join(", ", fromProps.Except(all))}\n" +
			$"In All but not .props: {string.Join(", ", all.Except(fromProps))}");
	}

	// --- Guard: growing the emittable set must bump the writer's version minor -------------------

	[Fact]
	public void AddingEmittableField_RequiresMinorBump()
	{
		// Soundness guard for the forward-compat scan's minor gate (ForwardCompat.PreserveUnknownData):
		// the gate SKIPS the unknown-data scan whenever the existing file's minor <= the writer's
		// minor, trusting that a same-or-older minor cannot contain anything the writer does not
		// already emit. That holds ONLY if the writer's version minor is bumped every time the
		// emittable set (sections / [properties] keys / item @metadata) grows. This test fails the
		// moment that set changes without a matching minor bump, so the gate can never silently drop
		// forward-compatible data.
		//
		// If this fails because you intentionally changed the emittable set: bump the minor in the
		// cache schema (which flows to CacheFormat.VersionHeader) and update ExpectedMinor +
		// ExpectedEmittableHash below to the values printed in the failure message.
		const int ExpectedMinor = 2;
		const string ExpectedEmittableHash = "4F7E8056D1E53C2805BA30975E889F762D97752B24BD69F77CFFF6233D1604E6";

		// Metadata is hashed AS item-type:key pairs (not a flattened union) so that reusing an
		// existing metadata NAME on a different item type — which leaves ProjectItems.AllMetadata
		// unchanged — still changes this hash and forces a minor bump. That closes the gap where the
		// per-section preservation gate would otherwise be trusted to skip data it cannot regenerate.
		IEnumerable<string> metadataPairs = ProjectItems.MetadataByItemType
			.SelectMany(kv => kv.Value.Select(m => $"{kv.Key}:{m}"));

		string joined = string.Join(
			"|",
			CacheFormat.Sections.All
				.Concat(ForwardCompat.KnownPropertyKeys)
				.Concat(metadataPairs)
				.Select(s => s.ToLowerInvariant())
				.OrderBy(s => s, StringComparer.Ordinal));
		string actualHash = Convert.ToHexString(
			System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(joined)));

		Assert.True(
			ForwardCompat.TryParseVersion(CacheFormat.VersionHeader, out _, out int actualMinor),
			$"CacheFormat.VersionHeader '{CacheFormat.VersionHeader}' is not a valid version header.");

		string detail = $"Emittable set ({joined.Length} chars):\n{joined}\n" +
			$"Re-pin with: ExpectedMinor={actualMinor}, ExpectedEmittableHash=\"{actualHash}\".";

		if (actualHash == ExpectedEmittableHash)
		{
			Assert.Equal(ExpectedMinor, actualMinor);
			return;
		}

		Assert.True(
			actualMinor > ExpectedMinor,
			"The emittable cache field set changed but the writer's version minor was NOT bumped " +
			$"(still {actualMinor}). Adding forward-compatible fields REQUIRES a minor bump so older " +
			$"writers preserve them instead of silently dropping them.\n{detail}");

		Assert.Fail(
			$"The emittable cache field set changed and the minor was correctly bumped to {actualMinor}. " +
			$"Re-pin this guard so steady-state runs pass again.\n{detail}");
	}

	[Fact]
	public void EveryEmittedMetadataKey_IsKnownForItsSection()
	{
		// Forward-compat judges "known @metadata" per cache section via the schema-generated
		// CacheFormat.Sections.MetadataBySection (built from each section's itemType link). If a
		// future schema change gives metadata to an item type whose section lacks an itemType link,
		// that metadata would be absent from MetadataBySection and the section would be treated as
		// emitting no metadata. That is the SAFE direction (the metadata is then preserved rather than
		// dropped), but this guard makes the omission explicit so the section->itemType links stay
		// complete and the per-section gate keeps dropping genuinely-known metadata.
		var knownAcrossSections = new HashSet<string>(
			CacheFormat.Sections.MetadataBySection.Values.SelectMany(m => m),
			StringComparer.OrdinalIgnoreCase);

		string[] missing = ProjectItems.MetadataByItemType.Values
			.SelectMany(m => m)
			.Where(meta => !knownAcrossSections.Contains(meta))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(s => s, StringComparer.Ordinal)
			.ToArray();

		Assert.True(
			missing.Length == 0,
			"Metadata key(s) the writer emits are not reachable through any cache section's itemType " +
			$"link (CacheFormat.Sections.MetadataBySection): {string.Join(", ", missing)}. Add the " +
			"\"itemType\" link to the owning section in server/src/Microsoft.NET.ProjectData.Generators/project-data-schema.json.");
	}
}
