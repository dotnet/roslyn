// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.NET.ProjectData;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// Forward-compatibility support for the <c>.lscache</c> format.
///
/// <para>The cache version is <c>version=&lt;major&gt;[.&lt;minor&gt;]</c>. A newer <em>minor</em>
/// only adds data an older build does not understand — new sections, new <c>[properties]</c> keys,
/// or new item <c>@metadata</c>. So a teammate on an older C# Dev Kit must, when it regenerates a
/// file authored by a newer minor, <strong>carry that unknown data through losslessly</strong> (so
/// the file does not churn back and forth between versions) while otherwise writing exactly the
/// content it knows about.</para>
///
/// <para>The candidate content an older build produces never contains future data (it generates
/// from MSBuild inputs only). Preservation therefore means: extract the fragments of the
/// <em>existing</em> file this build does not recognize and splice them, verbatim, into the freshly
/// generated candidate. Known content is never reserialized, so it is byte-stable.</para>
///
/// <para>"Unknown" is defined against what the current writer can <em>emit</em> — the schema's
/// emittable subset: a section not in <see cref="CacheFormat.Sections.All"/>, a <c>[properties]</c>
/// key not in <see cref="KnownPropertyKeys"/> (the schema's <c>cached</c> properties), or an item
/// <c>@metadata</c> key not emitted for that section's item type (see
/// <see cref="KnownMetadataBySection"/>). Metadata is judged <em>per section</em>, not as a flattened
/// union, so a newer minor that reuses an existing metadata name on a different item type is still
/// preserved rather than mistaken for regenerable data.</para>
/// </summary>
internal static class ForwardCompat
{
	// Sections the current writer emits. Schema-generated; matched case-sensitively because
	// section names are emitted and matched verbatim.
	private static readonly HashSet<string> KnownSections = new HashSet<string>(CacheFormat.Sections.All, StringComparer.Ordinal);

	// Item @metadata keys the writer emits, resolved PER cache section rather than as a flattened
	// union across all item types. Schema-generated from each section's itemType link
	// (CacheFormat.Sections.MetadataBySection). Case-insensitive because the wire form diverges from
	// the schema casing (the writer emits "@link" for the "Link" metadata). A section absent from
	// this map emits no metadata, so every @metadata found under it is unknown (and therefore
	// preserved). Keying per section is what lets a newer minor reuse a known metadata NAME on a
	// different item type: the older writer that does not emit that name there still treats it as
	// unknown and carries it forward, instead of mistaking it for data it can regenerate and
	// dropping it.
	internal static readonly Dictionary<string, HashSet<string>> KnownMetadataBySection = BuildKnownMetadataBySection();

	private static Dictionary<string, HashSet<string>> BuildKnownMetadataBySection()
	{
		var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, string[]> entry in CacheFormat.Sections.MetadataBySection)
			map[entry.Key] = new HashSet<string>(entry.Value, StringComparer.OrdinalIgnoreCase);

		return map;
	}

	/// <summary>
	/// The exact set of <c>[properties]</c> keys the writer can emit: every schema property
	/// (<see cref="ProjectProperties.All"/>), which the generated <c>_ProjectDataProperties</c>
	/// MSBuild allow-list mirrors. Using the emittable set is what makes "a brand-new key appeared"
	/// detectable as forward-compat data rather than as a known-but-currently-absent property.
	/// </summary>
	internal static readonly HashSet<string> KnownPropertyKeys = new HashSet<string>(ProjectProperties.All, StringComparer.OrdinalIgnoreCase);

	private const string VersionLinePrefix = "version=";

	// Identity assigned to the shared/primary segment (the block before the first "---", or the
	// sole segment of a single-target file). It has no [sliceDimensions], so it cannot collide
	// with a per-TFM slice identity.
	private const string SharedSegmentIdentity = "\u0000shared";

	/// <summary>
	/// Returns <paramref name="candidateText"/> augmented with any forward-compatible data found in
	/// <paramref name="existingText"/> that the current writer does not produce, or the same
	/// <paramref name="candidateText"/> reference when there is nothing to preserve.
	/// </summary>
	/// <param name="existingText">The current on-disk cache content (with any legacy <c>hash=</c>
	/// line already stripped).</param>
	/// <param name="candidateText">The freshly generated cache content.</param>
	/// <param name="currentMajor">The major version this build understands. Preservation only runs
	/// when the existing file's major matches; a different major is an incompatible format and the
	/// caller overwrites it wholesale.</param>
	internal static string PreserveUnknownData(string existingText, string candidateText, int currentMajor)
	{
		// Forward-compat preservation is pay-for-play. The expensive parse + scan below only ever
		// needs to run when the existing file was authored by a NEWER minor than this writer emits.
		// By the "bump the minor whenever the emittable section / [properties] key / item @metadata
		// set grows" invariant (guarded by ForwardCompatTests.AddingEmittableField_RequiresMinorBump),
		// a same-or-older minor cannot contain anything this writer does not already produce: there
		// is nothing to preserve and no higher stamp to carry forward. Probing just the two version
		// headers is a zero-allocation span scan — splitting and parsing the whole file (hundreds of
		// KB of transient allocation on a large cache) is reserved for the rare newer-minor case.
		// Today, with no minor field shipped, every file takes this fast path.
		if (!TryReadVersionHeader(existingText, out int existingMajor, out int existingMinor)
			|| existingMajor != currentMajor)
		{
			// A different major (or unrecognized header) is an incompatible format the caller
			// overwrites wholesale; an empty/headerless existing file has nothing to preserve.
			return candidateText;
		}

		int candidateMinor = 0;
		if (TryReadVersionHeader(candidateText, out int candidateMajor, out int parsedCandidateMinor)
			&& candidateMajor == currentMajor)
		{
			candidateMinor = parsedCandidateMinor;
		}

		if (existingMinor <= candidateMinor)
			return candidateText;

		// ---- Newer-minor path: the existing file may carry data this writer cannot regenerate. ----
		List<Segment> existingSegments = ParseSegments(existingText);
		if (existingSegments.Count == 0)
			return candidateText;

		Segment existingPrimary = existingSegments[0];

		// Index existing segments by slice identity so we can match them to candidate segments.
		var existingByIdentity = new Dictionary<string, Segment>(StringComparer.Ordinal);
		foreach (Segment seg in existingSegments)
			existingByIdentity[seg.Identity] = seg;

		// Split the candidate once into raw lines, shared between the splice target (candidateLines,
		// which must preserve exact bytes for round-trip) and the structural parse below. Splitting
		// on '\n' and re-joining on '\n' round-trips the exact bytes, so known content is never
		// reformatted; the parser only reads the array, so one split serves both consumers.
		string[] candidateRawLines = candidateText.Split('\n');
		var candidateLines = new List<string>(candidateRawLines);
		List<Segment> candidateSegments = ParseSegments(candidateRawLines);

		var insertions = new Dictionary<int, List<string>>();
		// Whole-section appends are tracked separately from item-local insertions (metadata,
		// properties). At a shared anchor — e.g. when the last item in the file both carries unknown
		// @metadata and is the line we append trailing unknown sections after — the item-local
		// insertions must come FIRST so the @metadata stays attached to its item; the appended
		// section then follows. Reassemble emits `insertions` before `appends` at each line.
		var appends = new Dictionary<int, List<string>>();
		bool changed = false;

		foreach (Segment candidateSeg in candidateSegments)
		{
			if (!existingByIdentity.TryGetValue(candidateSeg.Identity, out Segment? existingSeg))
				continue;

			changed |= PreserveSegment(existingSeg, candidateSeg, candidateLines, insertions, appends);
		}

		// Carry the newer minor-version stamp forward so the file does not flip-flop between minors
		// as different versions open it. We only reach here when existingMinor > candidateMinor, so
		// existingMinor is always >= 1; the parsed primary stamp is authoritative and the reconstructed
		// fallback (used only if the primary segment somehow lacks a version line) always carries it.
		if (candidatePrimary(candidateSegments) is { VersionLineIndex: >= 0 } cp)
		{
			candidateLines[cp.VersionLineIndex] = existingPrimary.VersionLine
				?? $"{VersionLinePrefix}{existingMajor}.{existingMinor}";
			changed = true;
		}

		if (!changed)
			return candidateText;

		return Reassemble(candidateLines, insertions, appends);

		static Segment? candidatePrimary(List<Segment> segs) => segs.Count > 0 ? segs[0] : null;
	}

	/// <summary>
	/// Parses the first <c>version=</c> header in <paramref name="text"/> directly off the underlying
	/// span without allocating, so the preservation fast path never splits the whole file. Blank lines,
	/// leading comments, and a leading legacy <c>hash=</c> header are skipped (mirroring the reader);
	/// the first <c>version=</c> line wins. Returns <see langword="false"/> when the first non-blank,
	/// non-comment, non-hash line is not a version line.
	/// </summary>
	internal static bool TryReadVersionHeader(string text, out int major, out int minor)
	{
		major = -1;
		minor = 0;
		int pos = 0;
		int len = text.Length;
		while (pos < len)
		{
			int nl = text.IndexOf('\n', pos);
			int lineEnd = nl < 0 ? len : nl;
			int trimmedEnd = lineEnd > pos && text[lineEnd - 1] == '\r' ? lineEnd - 1 : lineEnd;

			if (trimmedEnd > pos)
			{
				ReadOnlySpan<char> line = text.AsSpan(pos, trimmedEnd - pos);
				if (line.StartsWith(VersionLinePrefix.AsSpan(), StringComparison.Ordinal))
					return TryParseVersion(line, out major, out minor);
				if (line.StartsWith("hash=".AsSpan(), StringComparison.Ordinal))
				{
					// Legacy header: skip it and keep looking for the version line (mirrors the reader).
					if (nl < 0)
						break;
					pos = nl + 1;
					continue;
				}
				if (line[0] != CacheFormat.CommentChar)
					return false; // first real content is not a version line
			}

			if (nl < 0)
				break;
			pos = nl + 1;
		}

		return false;
	}

	/// <summary>
	/// Byte-level precondition for <see cref="PreserveUnknownData"/>: returns <see langword="true"/>
	/// only when <paramref name="existingContent"/> was authored by a strictly-NEWER minor of the
	/// SAME major as this writer — the one case in which <see cref="PreserveUnknownData"/> would do
	/// any work (see its version gate at lines ~98-114). It mirrors that gate exactly, but reads the
	/// two <c>version=</c> headers straight off the UTF-8 bytes, so the writer can decide whether to
	/// preserve <em>without</em> decoding the whole existing/candidate file to <see cref="string"/>.
	/// On the overwhelmingly common same-version change this skips two ~file-sized string
	/// allocations (and the LOH traffic they cause on large caches).
	/// </summary>
	/// <remarks>
	/// Conservative by construction: any header it cannot parse, or a different major, yields
	/// <see langword="false"/> — matching <see cref="PreserveUnknownData"/>'s own "unrecognized
	/// header / different major → nothing to preserve" branch. The cache header is ASCII and both the
	/// byte and char parsers apply identical blank-line / comment-skip rules, so the two parses always
	/// agree for the same content. <see cref="PreserveUnknownData"/> re-checks the gate itself, so even
	/// an over-permissive result here only wastes the rare-path allocation; it can never drop data.
	/// </remarks>
	internal static bool ExistingHasNewerMinor(ReadOnlySpan<byte> existingContent, ReadOnlySpan<byte> candidateContent, int currentMajor)
	{
		if (!TryReadVersionHeader(existingContent, out int existingMajor, out int existingMinor)
			|| existingMajor != currentMajor)
		{
			return false;
		}

		int candidateMinor = 0;
		if (TryReadVersionHeader(candidateContent, out int candidateMajor, out int parsedCandidateMinor)
			&& candidateMajor == currentMajor)
		{
			candidateMinor = parsedCandidateMinor;
		}

		return existingMinor > candidateMinor;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the existing content was authored by an older minor of
	/// the current major and is otherwise byte-for-byte identical to the candidate.
	/// </summary>
	/// <remarks>
	/// A minor version is a compatibility marker for the file's payload, not a mandatory stamp of the
	/// writer binary that last evaluated the project. An existing older stamp remains valid when no
	/// new data was emitted, avoiding a rewrite of every cache after an additive schema change.
	/// </remarks>
	internal static bool MatchesExceptForOlderMinorVersion(
		ReadOnlySpan<byte> existingContent,
		ReadOnlySpan<byte> candidateContent,
		int currentMajor)
	{
		if (!TryReadVersionHeader(
				existingContent,
				out int existingMajor,
				out int existingMinor,
				out int existingVersionStart,
				out int existingVersionLength)
			|| existingMajor != currentMajor
			|| !TryReadVersionHeader(
				candidateContent,
				out int candidateMajor,
				out int candidateMinor,
				out int candidateVersionStart,
				out int candidateVersionLength)
			|| candidateMajor != currentMajor
			|| existingMinor >= candidateMinor)
		{
			return false;
		}

		return existingContent.Slice(0, existingVersionStart)
				.SequenceEqual(candidateContent.Slice(0, candidateVersionStart))
			&& existingContent.Slice(existingVersionStart + existingVersionLength)
				.SequenceEqual(candidateContent.Slice(candidateVersionStart + candidateVersionLength));
	}

	/// <summary>
	/// UTF-8 byte counterpart of <see cref="TryReadVersionHeader(string, out int, out int)"/>: finds
	/// the first <c>version=</c> header, skipping blank lines, comment lines, and a leading legacy
	/// <c>hash=</c> header, without allocating. Returns <see langword="false"/> when the first real
	/// content line is not a version line.
	/// </summary>
	private static bool TryReadVersionHeader(ReadOnlySpan<byte> text, out int major, out int minor)
		=> TryReadVersionHeader(text, out major, out minor, out _, out _);

	private static bool TryReadVersionHeader(
		ReadOnlySpan<byte> text,
		out int major,
		out int minor,
		out int versionStart,
		out int versionLength)
	{
		major = -1;
		minor = 0;
		versionStart = -1;
		versionLength = 0;
		int pos = 0;
		int len = text.Length;
		while (pos < len)
		{
			int rel = text.Slice(pos).IndexOf((byte)'\n');
			int nl = rel < 0 ? -1 : pos + rel;
			int lineEnd = nl < 0 ? len : nl;
			int trimmedEnd = lineEnd > pos && text[lineEnd - 1] == (byte)'\r' ? lineEnd - 1 : lineEnd;

			if (trimmedEnd > pos)
			{
				ReadOnlySpan<byte> line = text.Slice(pos, trimmedEnd - pos);
				if (line.StartsWith(VersionLinePrefixUtf8))
				{
					if (!TryParseVersion(line, out major, out minor))
						return false;

					versionStart = pos;
					versionLength = trimmedEnd - pos;
					return true;
				}
				if (line.StartsWith("hash="u8))
				{
					// Legacy header: skip it and keep looking for the version line (mirrors the reader).
					if (nl < 0)
						break;
					pos = nl + 1;
					continue;
				}
				if (line[0] != (byte)CacheFormat.CommentChar)
					return false; // first real content is not a version line
			}

			if (nl < 0)
				break;
			pos = nl + 1;
		}

		return false;
	}

	private static bool TryParseVersion(ReadOnlySpan<byte> versionLine, out int major, out int minor)
	{
		major = -1;
		minor = 0;
		if (!versionLine.StartsWith(VersionLinePrefixUtf8))
			return false;

		ReadOnlySpan<byte> value = versionLine.Slice(VersionLinePrefixUtf8.Length);
		int dot = value.IndexOf((byte)'.');
		ReadOnlySpan<byte> majorPart = dot >= 0 ? value.Slice(0, dot) : value;
		ReadOnlySpan<byte> minorPart = dot >= 0 ? value.Slice(dot + 1) : "0"u8;

		if (!TryParseNonNegativeInt(majorPart, out major) || major <= 0)
		{
			major = -1;
			return false;
		}

		// A present-but-malformed or multi-part minor (e.g. "0.5", "3-preview") cannot be ordered as a
		// plain integer. Treat it as the newest-possible minor so the preservation gate stays
		// CONSERVATIVE: it runs the full preserve rather than skipping, and so never drops unknown
		// data it cannot prove is older. The writer only ever emits an integer minor; this guards
		// against a future/rogue writer that doesn't. The major still governs read compatibility.
		if (!TryParseNonNegativeInt(minorPart, out minor))
			minor = int.MaxValue;

		return true;
	}

	// netstandard2.0 lacks int.TryParse(ReadOnlySpan<byte>, ...); this zero-allocation parser keeps the
	// preservation pre-check off the heap. Accepts only non-negative decimal integers.
	private static bool TryParseNonNegativeInt(ReadOnlySpan<byte> span, out int value)
	{
		value = 0;
		if (span.Length == 0)
			return false;

		long acc = 0;
		foreach (byte b in span)
		{
			if (b < (byte)'0' || b > (byte)'9')
				return false;
			acc = (acc * 10) + (b - '0');
			if (acc > int.MaxValue)
				return false;
		}

		value = (int)acc;
		return true;
	}

	private static ReadOnlySpan<byte> VersionLinePrefixUtf8 => "version="u8;

	private static bool PreserveSegment(
		Segment existing,
		Segment candidate,
		List<string> candidateLines,
		Dictionary<int, List<string>> insertions,
		Dictionary<int, List<string>> appends)
	{
		bool changed = false;
		changed |= PreserveUnknownSections(existing, candidate, appends);
		changed |= PreserveUnknownProperties(existing, candidate, candidateLines, insertions);
		changed |= PreserveUnknownMetadata(existing, candidate, insertions);
		return changed;
	}

	// --- Unknown whole sections ------------------------------------------------------------------

	private static bool PreserveUnknownSections(Segment existing, Segment candidate, Dictionary<int, List<string>> appends)
	{
		// Preserve unknown sections near their existing known-section neighbors rather than appending
		// everything to the segment end or re-sorting. Re-sorting would rewrite the newer writer's
		// canonical layout and produce mixed-version flip-flop churn: a newer minor emits an additive
		// section in its own order, an older minor here would move it, the newer minor moves it back,
		// and so on. Round-tripping the bytes we found keeps the file stable.
		var candidateSectionsByName = new Dictionary<string, Section>(StringComparer.Ordinal);
		foreach (Section section in candidate.Sections)
		{
			if (section.Name is not null && !candidateSectionsByName.ContainsKey(section.Name))
				candidateSectionsByName.Add(section.Name, section);
		}

		List<string>? pendingUnknownSections = null;
		bool changed = false;
		foreach (Section section in existing.Sections)
		{
			if (section.Name is null)
				continue;

			if (!KnownSections.Contains(section.Name))
			{
				// Defensive: never duplicate a section the candidate somehow already has.
				if (candidateSectionsByName.ContainsKey(section.Name))
					continue;

				pendingUnknownSections ??= new List<string>();
				pendingUnknownSections.Add(string.Empty);
				pendingUnknownSections.Add(CacheFormat.SectionHeader(section.Name));
				pendingUnknownSections.AddRange(section.Lines);
				changed = true;
				continue;
			}

			if (pendingUnknownSections is not null
				&& candidateSectionsByName.TryGetValue(section.Name, out Section? candidateSection))
			{
				AddInsertion(appends, LineBeforeSectionWithOptionalLeadingBlank(candidate, candidateSection), pendingUnknownSections);
				pendingUnknownSections = null;
			}
		}

		if (pendingUnknownSections is not null)
		{
			AddInsertion(appends, candidate.LastContentLineIndex, pendingUnknownSections);
		}

		return changed;
	}

	private static int LineBeforeSectionWithOptionalLeadingBlank(Segment candidate, Section section)
	{
		int anchor = section.HeaderLineIndex == candidate.Start && candidate.Start > 0
			? candidate.Start - 1
			: candidate.Start;

		if (candidate.VersionLineIndex >= candidate.Start && candidate.VersionLineIndex < section.HeaderLineIndex)
			anchor = candidate.VersionLineIndex;

		foreach (Section candidateSection in candidate.Sections)
		{
			if (candidateSection.HeaderLineIndex >= section.HeaderLineIndex)
				break;

			if (candidateSection.HeaderLineIndex > anchor)
				anchor = candidateSection.HeaderLineIndex;
			if (candidateSection.LastLineIndex > anchor && candidateSection.LastLineIndex < section.HeaderLineIndex)
				anchor = candidateSection.LastLineIndex;
		}

		return anchor;
	}

	// --- Unknown [properties] keys ---------------------------------------------------------------

	private static bool PreserveUnknownProperties(
		Segment existing,
		Segment candidate,
		List<string> candidateLines,
		Dictionary<int, List<string>> insertions)
	{
		Section? existingProps = existing.Sections.FirstOrDefault(s => s.Name == CacheFormat.Sections.Properties);
		if (existingProps is null)
			return false;

		var unknown = new List<string>();
		foreach (string line in existingProps.Lines)
		{
			string key = PropertyKey(line);
			if (key.Length > 0 && !KnownPropertyKeys.Contains(key))
				unknown.Add(line);
		}

		if (unknown.Count == 0)
			return false;

		Section? candidateProps = candidate.Sections.FirstOrDefault(s => s.Name == CacheFormat.Sections.Properties);
		if (candidateProps is null)
		{
			// Candidate has no [properties] section (rare — required properties almost always force
			// one). Create one right after the [project]/[sliceDimensions] header block.
			Section? anchorSection = candidate.Sections.FirstOrDefault(s => s.Name == CacheFormat.Sections.SliceDimensions)
				?? candidate.Sections.FirstOrDefault(s => s.Name == CacheFormat.Sections.Project)
				?? (candidate.Sections.Count > 0 ? candidate.Sections[candidate.Sections.Count - 1] : null);

			var block = new List<string> { string.Empty, CacheFormat.SectionHeader(CacheFormat.Sections.Properties) };
			block.AddRange(unknown.OrderBy(PropertyKey, StringComparer.OrdinalIgnoreCase));

			// Insert after the anchor's last content line. For a header-only anchor LastLineIndex is
			// -1; fall back to its header line. With no sections at all, anchor at the segment's last
			// content line. Reassemble only applies insertions keyed to a real candidate line (>= 0),
			// so an insertion keyed at -1 would be silently dropped — never key there.
			int anchorIndex = anchorSection is null
				? candidate.LastContentLineIndex
				: anchorSection.LastLineIndex >= 0 ? anchorSection.LastLineIndex : anchorSection.HeaderLineIndex;

			AddInsertion(insertions, anchorIndex, block);
			return true;
		}

		// Merge each unknown key into the candidate's already-sorted [properties] block, skipping
		// any the candidate already contains.
		var present = new HashSet<string>(candidateProps.Lines.Select(PropertyKey), StringComparer.OrdinalIgnoreCase);
		bool changed = false;
		foreach (string line in unknown)
		{
			string key = PropertyKey(line);
			if (present.Contains(key))
				continue;

			int anchor = candidateProps.HeaderLineIndex;
			for (int i = 0; i < candidateProps.Lines.Count; i++)
			{
				if (string.Compare(PropertyKey(candidateProps.Lines[i]), key, StringComparison.OrdinalIgnoreCase) < 0)
					anchor = candidateProps.ContentLineIndices[i];
				else
					break;
			}

			AddInsertion(insertions, anchor, new List<string> { line });
			present.Add(key);
			changed = true;
		}

		return changed;
	}

	// --- Unknown item @metadata ------------------------------------------------------------------

	private static bool PreserveUnknownMetadata(Segment existing, Segment candidate, Dictionary<int, List<string>> insertions)
	{
		bool changed = false;
		foreach (Section existingSection in existing.Sections)
		{
			if (existingSection.Name is null || !KnownSections.Contains(existingSection.Name))
				continue;
			if (existingSection.Name is CacheFormat.Sections.Project
				or CacheFormat.Sections.SliceDimensions
				or CacheFormat.Sections.Properties)
			{
				continue;
			}

			// Resolve "known" @metadata for THIS section. A section with no entry emits no metadata,
			// so its known set is empty and every @metadata under it is unknown (and preserved).
			HashSet<string>? knownForSection = KnownMetadataBySection.TryGetValue(existingSection.Name, out HashSet<string>? ks)
				? ks
				: null;

			List<Leaf> existingLeaves = ExpandLeaves(existingSection);
			bool anyUnknown = existingLeaves.Any(l => l.Metadata.Any(m => !IsKnownMetadata(knownForSection, m.Content)));
			if (!anyUnknown)
				continue;

			Section? candidateSection = candidate.Sections.FirstOrDefault(s => s.Name == existingSection.Name);
			if (candidateSection is null)
				continue;

			List<Leaf> candidateLeaves = ExpandLeaves(candidateSection);
			var candidateByPath = new Dictionary<string, Leaf>(StringComparer.Ordinal);
			foreach (Leaf leaf in candidateLeaves)
				candidateByPath[leaf.Path] = leaf;

			foreach (Leaf existingLeaf in existingLeaves)
			{
				List<MetaLine> unknownMeta = existingLeaf.Metadata
					.Where(m => !IsKnownMetadata(knownForSection, m.Content))
					.ToList();
				if (unknownMeta.Count == 0)
					continue;
				if (!candidateByPath.TryGetValue(existingLeaf.Path, out Leaf? candidateLeaf))
					continue; // item removed from the candidate — drop its forward-compat metadata.

				var present = new HashSet<string>(
					candidateLeaf.Metadata.Select(m => MetadataKey(m.Content)),
					StringComparer.OrdinalIgnoreCase);

				string indent = new string(' ', candidateLeaf.Indent + 1);
				var toInsert = new List<string>();
				// Preserve unknown @metadata in the existing file's encounter order — a newer writer
				// emits an item's metadata in its own schema order, and re-sorting here would rewrite
				// that layout and churn the cache in mixed-version teams. Dedup by key is order-free.
				foreach (MetaLine meta in unknownMeta)
				{
					if (present.Add(MetadataKey(meta.Content)))
						toInsert.Add(indent + meta.Content);
				}

				if (toInsert.Count == 0)
					continue;

				int anchor = candidateLeaf.Metadata.Count > 0
					? candidateLeaf.Metadata[candidateLeaf.Metadata.Count - 1].LineIndex
					: candidateLeaf.LineIndex;
				AddInsertion(insertions, anchor, toInsert);
				changed = true;
			}
		}

		return changed;
	}

	// --- Parsing ---------------------------------------------------------------------------------

	private static List<Segment> ParseSegments(string text) => ParseSegments(text.Split('\n'));

	// Overload that reuses an already-split line array, so a caller that also needs the raw lines
	// (e.g. the splice target) does not pay for a second Split of the same text. The parser trims
	// lines internally for its own decisions but never mutates the array, so sharing it is safe.
	private static List<Segment> ParseSegments(string[] lines)
	{
		var segments = new List<Segment>();
		int segStart = 0;
		for (int i = 0; i <= lines.Length; i++)
		{
			bool atSeparator = i < lines.Length && Trim(lines[i]) == CacheFormat.SliceSeparator;
			bool atEnd = i == lines.Length;
			if (!atSeparator && !atEnd)
				continue;

			segments.Add(ParseSegment(lines, segStart, i));
			segStart = i + 1;
		}

		return segments;
	}

	private static Segment ParseSegment(string[] lines, int start, int end)
	{
		var segment = new Segment { Start = start, End = end };
		Section? current = null;
		var sliceDimensionLines = new List<string>();

		for (int i = start; i < end; i++)
		{
			string raw = Trim(lines[i]);
			if (raw.Length == 0)
				continue;

			if (raw.StartsWith(VersionLinePrefix, StringComparison.Ordinal))
			{
				segment.VersionLine = raw;
				segment.VersionLineIndex = i;
				continue;
			}

			if (raw[0] == CacheFormat.CommentChar)
				continue;

			if (raw.Length >= 2 && raw[0] == '[' && raw[raw.Length - 1] == ']')
			{
				string name = raw.Substring(1, raw.Length - 2);
				current = new Section { Name = name, HeaderLineIndex = i };
				segment.Sections.Add(current);
				continue;
			}

			if (segment.LastContentLineIndex < i)
				segment.LastContentLineIndex = i;

			if (current is null)
				continue;

			current.Lines.Add(raw);
			current.ContentLineIndices.Add(i);
			current.LastLineIndex = i;

			if (current.Name == CacheFormat.Sections.SliceDimensions)
				sliceDimensionLines.Add(raw);
		}

		segment.Identity = sliceDimensionLines.Count == 0
			? SharedSegmentIdentity
			: string.Join("\n", sliceDimensionLines.OrderBy(l => l, StringComparer.OrdinalIgnoreCase));

		if (segment.LastContentLineIndex < start)
			segment.LastContentLineIndex = Math.Max(start, end - 1);

		return segment;
	}

	/// <summary>
	/// Expands a path section's indentation-compressed lines into leaves, recording each leaf's full
	/// path, its line index, indentation, and the <c>@metadata</c> lines attached to it. Mirrors the
	/// reader's expansion so existing/candidate leaves match by full path regardless of how sibling
	/// changes alter compression.
	/// </summary>
	private static List<Leaf> ExpandLeaves(Section section)
	{
		var leaves = new List<Leaf>();
		var prefixStack = new Stack<(int Indent, string Prefix)>();
		Leaf? lastLeaf = null;

		for (int i = 0; i < section.Lines.Count; i++)
		{
			string line = section.Lines[i];
			int globalIndex = section.ContentLineIndices[i];
			int indent = CountIndent(line);
			string content = line.Substring(indent);

			if (content.Length > 0 && content[0] == '@')
			{
				lastLeaf?.Metadata.Add(new MetaLine(content, globalIndex));
				continue;
			}

			while (prefixStack.Count > 0 && prefixStack.Peek().Indent >= indent)
				prefixStack.Pop();

			string prefix = prefixStack.Count > 0 ? prefixStack.Peek().Prefix : string.Empty;

			if (content.Length > 0 && content[content.Length - 1] == '/')
			{
				prefixStack.Push((indent, prefix + content));
				lastLeaf = null;
			}
			else
			{
				lastLeaf = new Leaf(prefix + content, globalIndex, indent);
				leaves.Add(lastLeaf);
			}
		}

		return leaves;
	}

	// --- Reassembly ------------------------------------------------------------------------------

	private static string Reassemble(
		List<string> candidateLines,
		Dictionary<int, List<string>> insertions,
		Dictionary<int, List<string>> appends)
	{
		int extraCount = insertions.Sum(kvp => kvp.Value.Count) + appends.Sum(kvp => kvp.Value.Count);
		var output = new List<string>(candidateLines.Count + extraCount);
		for (int i = 0; i < candidateLines.Count; i++)
		{
			output.Add(candidateLines[i]);
			// Item-local insertions (metadata, properties) first, then whole-section appends, so a
			// section appended at the same anchor as the last item's @metadata never gets between the
			// item and its metadata.
			if (insertions.TryGetValue(i, out List<string>? extra))
				output.AddRange(extra);
			if (appends.TryGetValue(i, out List<string>? appended))
				output.AddRange(appended);
		}

		return string.Join("\n", output);
	}

	private static void AddInsertion(Dictionary<int, List<string>> insertions, int afterLineIndex, List<string> lines)
	{
		if (!insertions.TryGetValue(afterLineIndex, out List<string>? existing))
		{
			existing = new List<string>();
			insertions[afterLineIndex] = existing;
		}

		existing.AddRange(lines);
	}

	// --- Small helpers ---------------------------------------------------------------------------

	internal static bool TryParseVersion(string versionLine, out int major, out int minor)
	{
		major = -1;
		minor = 0;
		return versionLine is not null && TryParseVersion(versionLine.AsSpan(), out major, out minor);
	}

	private static bool TryParseVersion(ReadOnlySpan<char> versionLine, out int major, out int minor)
	{
		major = -1;
		minor = 0;
		if (!versionLine.StartsWith(VersionLinePrefix.AsSpan(), StringComparison.Ordinal))
			return false;

		ReadOnlySpan<char> value = versionLine.Slice(VersionLinePrefix.Length);
		int dot = value.IndexOf('.');
		ReadOnlySpan<char> majorPart = dot >= 0 ? value.Slice(0, dot) : value;
		ReadOnlySpan<char> minorPart = dot >= 0 ? value.Slice(dot + 1) : "0".AsSpan();

		if (!TryParseNonNegativeInt(majorPart, out major) || major <= 0)
		{
			major = -1;
			return false;
		}

		// A present-but-malformed or multi-part minor (e.g. "0.5", "3-preview") cannot be ordered as a
		// plain integer. Treat it as the newest-possible minor so the preservation gate stays
		// CONSERVATIVE: it runs the full preserve rather than skipping, and so never drops unknown
		// data it cannot prove is older. The writer only ever emits an integer minor; this guards
		// against a future/rogue writer that doesn't. The major still governs read compatibility.
		if (!TryParseNonNegativeInt(minorPart, out minor))
			minor = int.MaxValue;

		return true;
	}

	// netstandard2.0 lacks int.TryParse(ReadOnlySpan&lt;char&gt;, out int); this zero-allocation parser
	// keeps the preservation fast path off the heap. Accepts only non-negative decimal integers.
	private static bool TryParseNonNegativeInt(ReadOnlySpan<char> span, out int value)
	{
		value = 0;
		if (span.Length == 0)
			return false;

		long acc = 0;
		foreach (char c in span)
		{
			if (c < '0' || c > '9')
				return false;
			acc = (acc * 10) + (c - '0');
			if (acc > int.MaxValue)
				return false;
		}

		value = (int)acc;
		return true;
	}

	private static string Trim(string line) => line.Length > 0 && line[line.Length - 1] == '\r' ? line.Substring(0, line.Length - 1) : line;

	private static string PropertyKey(string line)
	{
		int eq = line.IndexOf('=');
		return eq < 0 ? line : line.Substring(0, eq);
	}

	private static string MetadataKey(string content)
	{
		// content starts with '@'.
		string body = content.Substring(1);
		int eq = body.IndexOf('=');
		return eq < 0 ? body : body.Substring(0, eq);
	}

	// A @metadata line is "known" only when the section that contains it actually emits that key.
	// A null set means the section emits no metadata at all, so nothing under it is known.
	private static bool IsKnownMetadata(HashSet<string>? knownForSection, string content)
		=> knownForSection is not null && knownForSection.Contains(MetadataKey(content));

	private static int CountIndent(string line)
	{
		int i = 0;
		while (i < line.Length && line[i] == ' ')
			i++;
		return i;
	}

	private sealed class Segment
	{
		public int Start { get; set; }
		public int End { get; set; }
		public string Identity { get; set; } = SharedSegmentIdentity;
		public string? VersionLine { get; set; }
		public int VersionLineIndex { get; set; } = -1;
		public List<Section> Sections { get; } = new List<Section>();

		// Index of the last non-blank line in the segment — the anchor for appending whole sections.
		public int LastContentLineIndex { get; set; } = -1;
	}

	private sealed class Section
	{
		public string? Name { get; set; }
		public int HeaderLineIndex { get; set; } = -1;
		public List<string> Lines { get; } = new List<string>();
		public List<int> ContentLineIndices { get; } = new List<int>();
		public int LastLineIndex { get; set; } = -1;
	}

	private sealed class Leaf
	{
		public Leaf(string path, int lineIndex, int indent)
		{
			this.Path = path;
			this.LineIndex = lineIndex;
			this.Indent = indent;
		}

		public string Path { get; }
		public int LineIndex { get; }
		public int Indent { get; }
		public List<MetaLine> Metadata { get; } = new List<MetaLine>();
	}

	private readonly struct MetaLine
	{
		public MetaLine(string content, int lineIndex)
		{
			this.Content = content;
			this.LineIndex = lineIndex;
		}

		public string Content { get; }
		public int LineIndex { get; }
	}
}
