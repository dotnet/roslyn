#:sdk Microsoft.NET.Sdk
#:property LangVersion=preview
#:property PublishAot=false

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using static System.Console;

if (args.Length != 1)
{
    Error.WriteLine("Usage: ref-assembly-analyzer <root-directory>");
    return 1;
}

var rootDirectory = Path.GetFullPath(args[0]);
var outputDirectory = Path.GetFullPath(Path.Combine(rootDirectory, "output"));

if (!Directory.Exists(rootDirectory))
{
    Error.WriteLine($"Root directory not found: {rootDirectory}");
    return 1;
}

Directory.CreateDirectory(outputDirectory);

var discovery = DiscoverPairs(rootDirectory);
if (discovery.Pairs.Count == 0)
{
    Error.WriteLine("No before/after assembly pairs were found.");
    return 1;
}

var results = new List<PairAnalysisResult>(discovery.Pairs.Count);
var badDllPairCount = 0;

foreach (AssemblyPair pair in discovery.Pairs.OrderBy(static p => p.PairId, StringComparer.OrdinalIgnoreCase))
{
    WriteLine($"Analyzing {pair.PairId}");
    try
    {
        results.Add(AnalyzePair(pair));
    }
    catch (BadImageFormatException ex)
    {
        Error.WriteLine($"Skipping bad DLL pair {pair.PairId}: {ex.Message}");
        badDllPairCount++;
    }
    catch (InvalidOperationException ex) when (ex.Message.StartsWith("Assembly has no metadata:", StringComparison.Ordinal))
    {
        Error.WriteLine($"Skipping bad DLL pair {pair.PairId}: {ex.Message}");
        badDllPairCount++;
    }
}

var summary = BuildSummary(results, discovery.PartialPairCount, badDllPairCount);
var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
};

var categoryDescriptions = BuildCategoryDescriptions();
var categoryTotals = BuildCategoryTotals(results, categoryDescriptions);
var pairResults = new PairResultsDocument(
    BuildCategoryTotalDescriptions(categoryDescriptions, categoryTotals),
    categoryTotals,
    results.Select(static r => new PairResultOutput(r.PairId, r.BeforePath, r.AfterPath, r.BeforeIdentity, r.AfterIdentity, r.BeforeMvid, r.AfterMvid, r.Classification, r.Categories)).ToList());

File.WriteAllText(Path.Combine(outputDirectory, "pair-results.json"), JsonSerializer.Serialize(pairResults, jsonOptions));
File.WriteAllText(Path.Combine(outputDirectory, "summary.json"), JsonSerializer.Serialize(summary, jsonOptions));
File.WriteAllText(Path.Combine(outputDirectory, "summary.txt"), BuildSummaryText(summary));
WriteVisualDiffFiles(outputDirectory, results);

// Clean up legacy files
var legacyCsv = Path.Combine(outputDirectory, "pair-results.csv");
if (File.Exists(legacyCsv)) File.Delete(legacyCsv);

WriteLine();
WriteLine(BuildSummaryText(summary));
WriteLine();
WriteLine($"Wrote results to {outputDirectory}");

return 0;

static DiscoveryResult DiscoverPairs(string rootDirectory)
{
    var beforeFiles = Directory.EnumerateFiles(rootDirectory, "*.before.dll", SearchOption.AllDirectories)
        .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
        .ToList();
    var afterFiles = Directory.EnumerateFiles(rootDirectory, "*.after.dll", SearchOption.AllDirectories)
        .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
        .ToList();
    var beforeFileSet = beforeFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);

    var pairs = new List<AssemblyPair>();
    var partialPairCount = 0;
    foreach (var beforePath in beforeFiles)
    {
        var afterPath = beforePath.Replace(".before.dll", ".after.dll", StringComparison.OrdinalIgnoreCase);
        if (!File.Exists(afterPath))
        {
            partialPairCount++;
            continue;
        }

        var relativeBefore = Path.GetRelativePath(rootDirectory, beforePath);
        var pairId = relativeBefore.Replace(".before.dll", string.Empty, StringComparison.OrdinalIgnoreCase);
        pairs.Add(new AssemblyPair(pairId, beforePath, afterPath));
    }

    foreach (var afterPath in afterFiles)
    {
        var beforePath = afterPath.Replace(".after.dll", ".before.dll", StringComparison.OrdinalIgnoreCase);
        if (!beforeFileSet.Contains(beforePath))
        {
            partialPairCount++;
        }
    }

    return new DiscoveryResult(pairs, partialPairCount);
}

static PairAnalysisResult AnalyzePair(AssemblyPair pair)
{
    AssemblySnapshot before = AssemblySnapshot.Load(pair.BeforePath);
    AssemblySnapshot after = AssemblySnapshot.Load(pair.AfterPath);

    var memberRemoved = before.MemberItems.Except(after.MemberItems).ToList();
    var memberAdded = after.MemberItems.Except(before.MemberItems).ToList();

    var stateMachineAttributeAdded = new List<string>();
    var stateMachineAttributeRemoved = new List<string>();
    RebucketStateMachineAttributeOnlyDifferences(memberAdded, memberRemoved, stateMachineAttributeAdded, stateMachineAttributeRemoved);

    var assemblyAttributeVersionOnlyAdded = new List<(string Item, string Category)>();
    var assemblyAttributeVersionOnlyRemoved = new List<(string Item, string Category)>();
    RebucketAssemblyAttributeVersionOnlyDifferences(memberAdded, memberRemoved, assemblyAttributeVersionOnlyAdded, assemblyAttributeVersionOnlyRemoved);

    // Categorize all member diffs through a single pipeline
    var hasIvt = before.HasInternalsVisibleTo || after.HasInternalsVisibleTo;
    var categorizedAdded = new List<(string Item, string Category)>();
    var categorizedRemoved = new List<(string Item, string Category)>();

    categorizedAdded.AddRange(assemblyAttributeVersionOnlyAdded);
    categorizedRemoved.AddRange(assemblyAttributeVersionOnlyRemoved);

    foreach (var item in memberAdded)
        categorizedAdded.Add((item.Item, ClassifyMemberDiffItem(item, hasIvt)));
    foreach (var item in memberRemoved)
        categorizedRemoved.Add((item.Item, ClassifyMemberDiffItem(item, hasIvt)));

    // Add rebucketed state machine attribute items
    foreach (var item in stateMachineAttributeAdded)
        categorizedAdded.Add((item, "public-async-state-machine-attribute"));
    foreach (var item in stateMachineAttributeRemoved)
        categorizedRemoved.Add((item, "public-async-state-machine-attribute"));

    // Add non-member diffs as categorized entries
    var identityChanged = !StringComparer.Ordinal.Equals(before.AssemblyIdentity, after.AssemblyIdentity);
    if (identityChanged)
    {
        categorizedAdded.Add((after.AssemblyIdentity, "assembly-identity"));
        categorizedRemoved.Add((before.AssemblyIdentity, "assembly-identity"));
    }

    foreach (var item in after.AssemblyReferences.Except(before.AssemblyReferences, StringComparer.Ordinal))
        categorizedAdded.Add((item, "references"));
    foreach (var item in before.AssemblyReferences.Except(after.AssemblyReferences, StringComparer.Ordinal))
        categorizedRemoved.Add((item, "references"));

    foreach (var item in after.OtherMetadataItems.Except(before.OtherMetadataItems, StringComparer.Ordinal))
        categorizedAdded.Add((item, "other-metadata"));
    foreach (var item in before.OtherMetadataItems.Except(after.OtherMetadataItems, StringComparer.Ordinal))
        categorizedRemoved.Add((item, "other-metadata"));

    // Build categories dictionary
    var categoryNames = categorizedAdded.Select(e => e.Category)
        .Concat(categorizedRemoved.Select(e => e.Category))
        .Distinct(StringComparer.Ordinal);

    var categories = new Dictionary<string, PairDiffBucket>(StringComparer.Ordinal);
    foreach (var category in categoryNames)
    {
        categories[category] = new PairDiffBucket(
            categorizedAdded.Where(e => StringComparer.Ordinal.Equals(e.Category, category)).Select(e => e.Item).OrderBy(x => x, StringComparer.Ordinal).ToList(),
            categorizedRemoved.Where(e => StringComparer.Ordinal.Equals(e.Category, category)).Select(e => e.Item).OrderBy(x => x, StringComparer.Ordinal).ToList());
    }

    // Handle unrecognized differences (MVID-only changes with no tracked member/metadata diffs)
    var sameMvid = StringComparer.Ordinal.Equals(before.ModuleVersionId, after.ModuleVersionId);
    if (categories.Count == 0 && !sameMvid)
    {
        var unrecognizedAdded = new List<string> { $"MVID {after.ModuleVersionId}" };
        var unrecognizedRemoved = new List<string> { $"MVID {before.ModuleVersionId}" };

        if (!StringComparer.Ordinal.Equals(before.NormalizedContentHash, after.NormalizedContentHash))
        {
            unrecognizedAdded.Add($"RAWFILE sha256={after.NormalizedContentHash}");
            unrecognizedRemoved.Add($"RAWFILE sha256={before.NormalizedContentHash}");
        }

        categories["unrecognized-difference"] = new PairDiffBucket(unrecognizedAdded, unrecognizedRemoved);
    }

    var hasAnyDiffs = categories.Values.Any(b => b.Added.Count > 0 || b.Removed.Count > 0);
    var classification = hasAnyDiffs ? "valid-pair" : "same-mvid";

    return new PairAnalysisResult(
        pair.PairId,
        pair.BeforePath,
        pair.AfterPath,
        before.AssemblyIdentity,
        after.AssemblyIdentity,
        before.ModuleVersionId,
        after.ModuleVersionId,
        classification,
        categories);
}

static Summary BuildSummary(List<PairAnalysisResult> results, int partialPairCount, int badDllPairCount)
{
    var byClassification = results
        .GroupBy(static result => result.Classification, StringComparer.Ordinal)
        .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);

    if (partialPairCount > 0)
    {
        byClassification["partial-pair"] = partialPairCount;
    }

    if (badDllPairCount > 0)
    {
        byClassification["bad-dll"] = badDllPairCount;
    }

    byClassification = byClassification
        .OrderByDescending(static entry => entry.Value)
        .ThenBy(static entry => entry.Key, StringComparer.Ordinal)
        .ToDictionary(static entry => entry.Key, static entry => entry.Value, StringComparer.Ordinal);

    var categoryDescriptions = BuildCategoryDescriptions();

    var byCategory = results
        .SelectMany(static result => result.Categories.Select(c => (c.Key, Added: c.Value.Added.Count, Removed: c.Value.Removed.Count)))
        .GroupBy(static entry => entry.Key, StringComparer.Ordinal)
        .OrderByDescending(static group => group.Count())
        .ThenBy(static group => group.Key, StringComparer.Ordinal)
        .Select(static group => new CategorySummary(
            group.Key,
            group.Sum(static entry => entry.Added),
            group.Sum(static entry => entry.Removed),
            group.Count()))
        .ToList();

    var hypotheticalImprovements = new List<HypotheticalImprovement>
    {
        BuildIgnoreCategoryImprovement(
            results,
            ["assembly-file-version"],
            "Ignore assembly-file-version"),
        BuildIgnoreCategoryImprovement(
            results,
            ["assembly-informational-version"],
            "Ignore assembly-informational-version"),
        BuildIgnoreCategoryImprovement(
            results,
            ["assembly-metadata"],
            "Ignore assembly-metadata"),
        BuildIgnoreCategoryImprovement(
            results,
            ["assembly-file-version", "assembly-informational-version"],
            "Ignore assembly-file-version, assembly-informational-version"),
        BuildIgnoreCategoryImprovement(
            results,
            ["assembly-file-version", "assembly-metadata"],
            "Ignore assembly-file-version, assembly-metadata"),
        BuildIgnoreCategoryImprovement(
            results,
            ["assembly-informational-version", "assembly-metadata"],
            "Ignore assembly-informational-version, assembly-metadata"),
        BuildIgnoreCategoryImprovement(
            results,
            ["assembly-file-version", "assembly-informational-version", "assembly-metadata"],
            "Ignore assembly-file-version, assembly-informational-version, assembly-metadata"),
        BuildIgnoreCategoryImprovement(
            results,
            ["state-machine", "public-async-state-machine-attribute", "awaiter-field", "display-class", "lambda-method", "local-function", "lambda-or-dynamic-cache"],
            "Ignore state-machine, public-async-state-machine-attribute, awaiter-field, display-class, lambda-method, local-function, lambda-or-dynamic-cache")
    };

    return new Summary(
        results.Count,
        byClassification,
        categoryDescriptions,
        byCategory,
        hypotheticalImprovements);
}

static string BuildSummaryText(Summary summary)
{
    var builder = new StringBuilder();
    builder.AppendLine($"Pairs analyzed: {summary.TotalPairs}");
    builder.AppendLine("Classification counts:");
    foreach (var entry in summary.ByClassification)
    {
        var percentage = summary.TotalPairs == 0 ? 0 : entry.Value * 100.0 / summary.TotalPairs;
        builder.AppendLine($"  {entry.Key}: {entry.Value} ({percentage:F1}%) - {GetClassificationDescription(entry.Key)}");
    }

    builder.AppendLine();
    var validPairCount = summary.ByClassification.GetValueOrDefault("valid-pair", 0);
    builder.AppendLine("Diff categories:");
    foreach (var categoryName in GetOrderedSummaryCategoryNames(summary))
    {
        var summaryEntry = summary.ByCategory.FirstOrDefault(category => StringComparer.Ordinal.Equals(category.Category, categoryName));
        var pairCount = summaryEntry?.PairCount ?? 0;
        var percentage = validPairCount == 0 ? 0 : pairCount * 100.0 / validPairCount;
        builder.AppendLine($"  {categoryName}: {pairCount} pairs ({percentage:F1}%) - {summary.CategoryDescriptions[categoryName]}");
    }

    if (summary.HypotheticalImprovements.Count > 0)
    {
        builder.AppendLine();
        builder.AppendLine("Hypothetical improvements:");
        for (var i = 0; i < summary.HypotheticalImprovements.Count; i++)
        {
            var improvement = summary.HypotheticalImprovements[i];
            builder.AppendLine($"  {i + 1}. {improvement.Name}: results in {improvement.IdenticalPairs} fewer pairs ({improvement.ImprovementPercentage:F1}% cache miss reduction)");
        }
    }

    return builder.ToString().TrimEnd();
}

static List<string> GetOrderedSummaryCategoryNames(Summary summary)
{
    var countsByCategory = summary.ByCategory.ToDictionary(
        static category => category.Category,
        static category => category.PairCount,
        StringComparer.Ordinal);

    return summary.CategoryDescriptions.Keys
        .OrderByDescending(categoryName => countsByCategory.GetValueOrDefault(categoryName, 0))
        .ThenBy(static categoryName => categoryName, StringComparer.Ordinal)
        .ToList();
}

static string GetClassificationDescription(string classification)
    => classification switch
    {
        "valid-pair" => "The pair had differences between before and after.",
        "bad-dll" => "A before/after pair could not be analyzed because one of the DLLs was unreadable or had invalid metadata.",
        "partial-pair" => "Only one side of the before/after pair was present, so no pairwise comparison could be performed.",
        "same-mvid" => "No tracked differences were observed, and the compared ref assemblies share the same MVID.",
        _ => "Unrecognized classification."
    };

static HypotheticalImprovement BuildIgnoreCategoryImprovement(List<PairAnalysisResult> results, IEnumerable<string> ignoredCategories, string name)
{
    var ignoredCategorySet = ignoredCategories.ToHashSet(StringComparer.Ordinal);
    var identicalPairs = results.Count(result => WouldBecomeIdenticalIfIgnoringCategories(result, ignoredCategorySet));
    var improvementPercentage = results.Count == 0 ? 0 : identicalPairs * 100.0 / results.Count;
    return new HypotheticalImprovement(name, string.Join(", ", ignoredCategorySet.OrderBy(static x => x, StringComparer.Ordinal)), identicalPairs, improvementPercentage);
}

static bool WouldBecomeIdenticalIfIgnoringCategories(PairAnalysisResult result, HashSet<string> ignoredCategories)
    => result.Categories.Keys.All(category => ignoredCategories.Contains(category));

static void WriteVisualDiffFiles(string outputDirectory, List<PairAnalysisResult> results)
{
    // Clean up legacy output structures
    foreach (var legacy in new[] { "pair-diff-publicapi-userauthored", "before", "after" })
    {
        var legacyPath = Path.Combine(outputDirectory, legacy);
        if (Directory.Exists(legacyPath)) Directory.Delete(legacyPath, recursive: true);
    }

    foreach (var legacy in new[] { "pair-diff-publicapi-userauthored.before.txt", "pair-diff-publicapi-userauthored.after.txt" })
    {
        var legacyPath = Path.Combine(outputDirectory, legacy);
        if (File.Exists(legacyPath)) File.Delete(legacyPath);
    }

    // Collect all category names that have items across any result
    var allCategories = results
        .SelectMany(static r => r.Categories.Keys)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(static c => c, StringComparer.Ordinal);

    // Clean up any previous category folders inside before/after
    var beforeRoot = Path.Combine(outputDirectory, "before");
    var afterRoot = Path.Combine(outputDirectory, "after");
    foreach (var category in allCategories)
    {
        // Also clean up old category-at-top-level layout
        var oldCategoryDir = Path.Combine(outputDirectory, category);
        if (Directory.Exists(oldCategoryDir)) Directory.Delete(oldCategoryDir, recursive: true);
    }

    foreach (var side in Enum.GetValues<DiffSide>())
    {
        var sideRoot = side == DiffSide.Before ? beforeRoot : afterRoot;

        foreach (var category in allCategories)
        {
            var categoryDirectory = Path.Combine(sideRoot, category);
            if (Directory.Exists(categoryDirectory))
            {
                Directory.Delete(categoryDirectory, recursive: true);
            }

            Directory.CreateDirectory(categoryDirectory);

            foreach (var result in results.OrderBy(static result => result.PairId, StringComparer.OrdinalIgnoreCase))
            {
                if (!result.Categories.TryGetValue(category, out var bucket))
                {
                    continue;
                }

                var items = side == DiffSide.Before ? bucket.Removed : bucket.Added;
                if (items.Count == 0)
                {
                    continue;
                }

                var relativePath = FlattenPairIdToPath(result.PairId);
                var filePath = Path.Combine(categoryDirectory, relativePath);
                var parentDirectory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                File.WriteAllText(filePath, BuildVisualDiffText(result, category, side));
            }
        }
    }
}

// Keep the first path segment of PairId as a directory; flatten the rest into the filename.
// Commit hashes (40 hex chars) are truncated to 8 and placed first in the filename.
// e.g. "2026-02-26/RepoName/abc123def.../private_Tools/Foo.net8.0" → "2026-02-26/abc123de_RepoName_private_Tools_Foo.net8.0.txt"
static string FlattenPairIdToPath(string pairId)
{
    var segments = pairId.Split('/', '\\');
    if (segments.Length <= 2)
    {
        return Path.Combine(segments) + ".txt";
    }

    var directory = segments[0];
    var rest = new List<string>();
    string? commitPrefix = null;
    for (int i = 1; i < segments.Length; i++)
    {
        if (IsCommitHash(segments[i]))
        {
            commitPrefix ??= segments[i][..8];
        }
        else
        {
            rest.Add(segments[i]);
        }
    }

    var parts = new List<string>();
    if (commitPrefix is not null)
    {
        parts.Add(commitPrefix);
    }

    parts.AddRange(rest);
    var fileName = string.Join("_", parts) + ".txt";
    return Path.Combine(directory, fileName);

    static bool IsCommitHash(string s) =>
        s.Length == 40 && s.All(c => char.IsAsciiHexDigit(c));
}

static string BuildVisualDiffText(PairAnalysisResult result, string category, DiffSide side)
{
    var builder = new StringBuilder();
    builder.AppendLine(side == DiffSide.Before
        ? "# Diff view (before)"
        : "# Diff view (after)");
    builder.AppendLine();
    builder.AppendLine($"PairId: {result.PairId}");
    builder.AppendLine();

    if (result.Categories.TryGetValue(category, out var bucket))
    {
        var items = side == DiffSide.Before ? bucket.Removed : bucket.Added;
        if (items.Count > 0)
        {
            foreach (var item in items)
            {
                builder.AppendLine(item);
            }

            return builder.ToString().TrimEnd();
        }
    }

    builder.AppendLine("(no items)");
    return builder.ToString().TrimEnd();
}

static Dictionary<string, string> BuildCategoryDescriptions()
    => new(StringComparer.Ordinal)
    {
        ["other-public"] = "Public API surface changes not covered by a more specific category.",
        ["assembly-identity"] = "Assembly identity changes such as version, name, culture, or public key token differences.",
        ["assembly-file-version"] = "Version-only AssemblyFileVersionAttribute changes whose before/after entries match after stripping the version text.",
        ["assembly-informational-version"] = "Version-only AssemblyInformationalVersionAttribute changes whose before/after entries match after stripping the version text.",
        ["assembly-metadata"] = "Version-only AssemblyMetadataAttribute changes whose before/after entries match after stripping the version text.",
        ["references"] = "Assembly reference changes from the AssemblyRef table.",
        ["state-machine"] = "Compiler-generated async/iterator machinery such as <Method>d__N types, state fields, builder fields, current fields, and parameter/this proxy fields.",
        ["public-async-state-machine-attribute"] = "Visible method entries whose before/after API signatures become identical after normalizing AsyncStateMachineAttribute by blanking its argument list.",
        ["awaiter-field"] = "Compiler-generated awaiter slots such as <>u__N fields inside async/iterator state machines.",
        ["iterator-finally"] = "Compiler-generated iterator cleanup helpers such as <>m__Finally methods.",
        ["display-class"] = "Closure/display-class artifacts such as <>c, <>c__DisplayClass*, and <>8__locals* that capture locals or hold lambda state.",
        ["lambda-method"] = "Synthesized lambda bodies such as <Method>b__N methods.",
        ["local-function"] = "Synthesized local-function implementations, typically with Roslyn-generated <Method>g__... names.",
        ["lambda-or-dynamic-cache"] = "Synthesized delegate or dynamic call-site caches such as <>9, <>9__N, <>o__, or <>p__ fields/types.",
        ["hoisted-local"] = "Hoisted user or synthesized locals that were lifted into generated frames or state machines.",
        ["backing-field"] = "Compiler-generated backing fields such as auto-property or anonymous-type backing fields.",
        ["anonymous-type-or-delegate"] = "Synthesized anonymous type or anonymous delegate artifacts.",
        ["inline-array-or-readonly-list"] = "Synthesized helper types for inline arrays or compiler-generated read-only list wrappers.",
        ["private-implementation-details"] = "Compiler-generated implementation storage under <PrivateImplementationDetails>.",
        ["compiler-generated-other"] = "Other compiler-generated artifacts that match generated-name conventions but do not fit a more specific bucket.",
        ["user-authored-ivt"] = "Non-public user-authored members in assemblies that have InternalsVisibleToAttribute.",
        ["user-authored-other"] = "Non-public user-authored members in assemblies without InternalsVisibleToAttribute.",
        ["other-metadata"] = "Metadata changes outside the modeled member/type signatures, such as module, resource, forwarder, file, or other assembly-level metadata.",
        ["unrecognized-difference"] = "Differences outside the recognized buckets, such as MVID-only changes or raw content differences that remain after tracked comparisons."
    };

static Dictionary<string, int> BuildCategoryTotals(List<PairAnalysisResult> results, Dictionary<string, string> categoryDescriptions)
{
    var totals = categoryDescriptions.Keys.ToDictionary(static k => k, static _ => 0, StringComparer.Ordinal);
    foreach (var result in results)
    {
        foreach (var category in result.Categories.Keys)
        {
            if (totals.ContainsKey(category))
                totals[category]++;
            else
                totals[category] = 1;
        }
    }

    return totals
        .OrderByDescending(static entry => entry.Value)
        .ThenBy(static entry => entry.Key, StringComparer.Ordinal)
        .ToDictionary(static entry => entry.Key, static entry => entry.Value, StringComparer.Ordinal);
}

static Dictionary<string, string> BuildCategoryTotalDescriptions(Dictionary<string, string> categoryDescriptions, Dictionary<string, int> totals)
{
    var descriptions = new Dictionary<string, string>(categoryDescriptions, StringComparer.Ordinal);
    // Add any categories present in totals but not in descriptions
    foreach (var key in totals.Keys)
    {
        descriptions.TryAdd(key, "");
    }

    return descriptions
        .OrderByDescending(entry => totals.GetValueOrDefault(entry.Key, 0))
        .ThenBy(static entry => entry.Key, StringComparer.Ordinal)
        .ToDictionary(static entry => entry.Key, static entry => entry.Value, StringComparer.Ordinal);
}

static string ClassifyMemberDiffItem(MemberDiffItem item, bool hasIvt)
    => item.IsVisibleApi ? ClassifyVisibleItem(item.Item) : ClassifyNonVisibleItem(item.Item, hasIvt);

static string ClassifyVisibleItem(string item)
    => "other-public";

static string ClassifyNonVisibleItem(string item, bool hasIvt)
{
    if (item.Contains("<>u__", StringComparison.Ordinal))
    {
        return "awaiter-field";
    }

    if (item.Contains("<>m__Finally", StringComparison.Ordinal))
    {
        return "iterator-finally";
    }

    if (item.Contains(">d__", StringComparison.Ordinal)
        || item.Contains("<>1__state", StringComparison.Ordinal)
        || item.Contains("<>2__current", StringComparison.Ordinal)
        || item.Contains("<>l__initialThreadId", StringComparison.Ordinal)
        || item.Contains("<>t__builder", StringComparison.Ordinal)
        || item.Contains("<>v__promiseOfValueOrEnd", StringComparison.Ordinal)
        || item.Contains("<>x__combinedTokens", StringComparison.Ordinal)
        || item.Contains("<>w__disposeMode", StringComparison.Ordinal)
        || item.Contains("<>3__", StringComparison.Ordinal)
        || item.Contains("<>4__this", StringComparison.Ordinal))
    {
        return "state-machine";
    }

    if (item.Contains("<>c__DisplayClass", StringComparison.Ordinal)
        || item.Contains("<>8__locals", StringComparison.Ordinal))
    {
        return "display-class";
    }

    if (item.Contains(">b__", StringComparison.Ordinal))
    {
        return "lambda-method";
    }

    if (item.Contains(">g__", StringComparison.Ordinal))
    {
        return "local-function";
    }

    if (item.Contains("<>9__", StringComparison.Ordinal)
        || item.Contains("::<>9", StringComparison.Ordinal)
        || item.Contains("<>o__", StringComparison.Ordinal)
        || item.Contains("<>p__", StringComparison.Ordinal))
    {
        return "lambda-or-dynamic-cache";
    }

    if (item.Contains(">5__", StringComparison.Ordinal)
        || item.Contains("<>7__", StringComparison.Ordinal))
    {
        return "hoisted-local";
    }

    if (item.Contains(">k__BackingField", StringComparison.Ordinal)
        || item.Contains(">i__Field", StringComparison.Ordinal)
        || item.Contains(">P", StringComparison.Ordinal))
    {
        return "backing-field";
    }

    if (item.Contains("<>f__AnonymousType", StringComparison.Ordinal)
        || item.Contains("<>f__AnonymousDelegate", StringComparison.Ordinal))
    {
        return "anonymous-type-or-delegate";
    }

    if (item.Contains("<>y__", StringComparison.Ordinal)
        || item.Contains("<>z__ReadOnly", StringComparison.Ordinal))
    {
        return "inline-array-or-readonly-list";
    }

    if (item.Contains("<PrivateImplementationDetails>", StringComparison.Ordinal))
    {
        return "private-implementation-details";
    }

    if (item.Contains("<>", StringComparison.Ordinal)
        || item.Contains(">d__", StringComparison.Ordinal)
        || item.Contains(">b__", StringComparison.Ordinal)
        || item.Contains(">g__", StringComparison.Ordinal))
    {
        return "compiler-generated-other";
    }

    return hasIvt && IsInternalMember(item) ? "user-authored-ivt" : "user-authored-other";
}

static bool IsInternalMember(string item)
    => item.Contains("::internal ", StringComparison.Ordinal)
        || item.Contains("internal ", StringComparison.Ordinal) && item.StartsWith("TYPE ", StringComparison.Ordinal);

static void RebucketStateMachineAttributeOnlyDifferences(
    List<MemberDiffItem> memberAdded,
    List<MemberDiffItem> memberRemoved,
    List<string> stateMachineAttributeAdded,
    List<string> stateMachineAttributeRemoved)
{
    var removedByNormalizedSignature = memberRemoved
        .Where(static item => item.IsVisibleApi)
        .Select(static item => (Item: item, Normalized: NormalizeStateMachineAttributeMethodSignature(item.Item)))
        .Where(static entry => entry.Normalized is not null)
        .GroupBy(static entry => entry.Normalized!, StringComparer.Ordinal)
        .ToDictionary(
            static group => group.Key,
            static group => new Queue<MemberDiffItem>(group.Select(static entry => entry.Item)),
            StringComparer.Ordinal);

    var matchedAdded = new List<MemberDiffItem>();
    var matchedRemoved = new List<MemberDiffItem>();
    foreach (var addedItem in memberAdded.Where(static item => item.IsVisibleApi))
    {
        var normalized = NormalizeStateMachineAttributeMethodSignature(addedItem.Item);
        if (normalized is null
            || !removedByNormalizedSignature.TryGetValue(normalized, out var removedMatches)
            || removedMatches.Count == 0)
        {
            continue;
        }

        matchedAdded.Add(addedItem);
        matchedRemoved.Add(removedMatches.Dequeue());
    }

    foreach (var addedItem in matchedAdded)
    {
        memberAdded.Remove(addedItem);
        stateMachineAttributeAdded.Add(addedItem.Item);
    }

    foreach (var removedItem in matchedRemoved)
    {
        memberRemoved.Remove(removedItem);
        stateMachineAttributeRemoved.Add(removedItem.Item);
    }

    stateMachineAttributeAdded.Sort(StringComparer.Ordinal);
    stateMachineAttributeRemoved.Sort(StringComparer.Ordinal);
}

static void RebucketAssemblyAttributeVersionOnlyDifferences(
    List<MemberDiffItem> memberAdded,
    List<MemberDiffItem> memberRemoved,
    List<(string Item, string Category)> assemblyAttributeAdded,
    List<(string Item, string Category)> assemblyAttributeRemoved)
{
    var removedByNormalizedSignature = new Dictionary<(string Category, string Normalized), Queue<MemberDiffItem>>();
    foreach (var removedItem in memberRemoved.Where(static item => item.IsVisibleApi))
    {
        if (!TryNormalizeAssemblyAttributeVersionOnlyDiff(removedItem.Item, out var category, out var normalized))
        {
            continue;
        }

        var key = (Category: category, Normalized: normalized);
        if (!removedByNormalizedSignature.TryGetValue(key, out var matches))
        {
            matches = new Queue<MemberDiffItem>();
            removedByNormalizedSignature[key] = matches;
        }

        matches.Enqueue(removedItem);
    }

    var matchedAdded = new List<(MemberDiffItem Item, string Category)>();
    var matchedRemoved = new List<(MemberDiffItem Item, string Category)>();
    foreach (var addedItem in memberAdded.Where(static item => item.IsVisibleApi))
    {
        if (!TryNormalizeAssemblyAttributeVersionOnlyDiff(addedItem.Item, out var category, out var normalized))
        {
            continue;
        }

        var key = (Category: category, Normalized: normalized);
        if (!removedByNormalizedSignature.TryGetValue(key, out var removedMatches) || removedMatches.Count == 0)
        {
            continue;
        }

        matchedAdded.Add((addedItem, category));
        matchedRemoved.Add((removedMatches.Dequeue(), category));
    }

    foreach (var (item, category) in matchedAdded)
    {
        memberAdded.Remove(item);
        assemblyAttributeAdded.Add((item.Item, category));
    }

    foreach (var (item, category) in matchedRemoved)
    {
        memberRemoved.Remove(item);
        assemblyAttributeRemoved.Add((item.Item, category));
    }

    assemblyAttributeAdded.Sort(static (left, right) =>
    {
        var categoryComparison = StringComparer.Ordinal.Compare(left.Category, right.Category);
        return categoryComparison != 0 ? categoryComparison : StringComparer.Ordinal.Compare(left.Item, right.Item);
    });
    assemblyAttributeRemoved.Sort(static (left, right) =>
    {
        var categoryComparison = StringComparer.Ordinal.Compare(left.Category, right.Category);
        return categoryComparison != 0 ? categoryComparison : StringComparer.Ordinal.Compare(left.Item, right.Item);
    });
}

static string? NormalizeStateMachineAttributeMethodSignature(string item)
{
    if (!item.StartsWith("METHOD ", StringComparison.Ordinal))
    {
        return null;
    }

    var normalized = Regex.Replace(
        item,
        @"\[System\.Runtime\.CompilerServices\.AsyncStateMachineAttribute\(typeof\(.*?\)\)\]\s*",
        "[System.Runtime.CompilerServices.AsyncStateMachineAttribute(typeof())] ",
        RegexOptions.CultureInvariant);

    return Regex.Replace(normalized, @"\s{2,}", " ", RegexOptions.CultureInvariant).Trim();
}

static bool TryNormalizeAssemblyAttributeVersionOnlyDiff(string item, out string category, out string normalized)
{
    if (item.StartsWith("ASSEMBLY [System.Reflection.AssemblyFileVersionAttribute(", StringComparison.Ordinal))
    {
        category = "assembly-file-version";
        normalized = NormalizeAssemblyAttributeStringValue(
            item,
            @"(?<=AssemblyFileVersionAttribute\("")(?:\\.|[^""])*(?=""\)\])");
        return true;
    }

    if (item.StartsWith("ASSEMBLY [System.Reflection.AssemblyInformationalVersionAttribute(", StringComparison.Ordinal))
    {
        category = "assembly-informational-version";
        normalized = NormalizeAssemblyAttributeStringValue(
            item,
            @"(?<=AssemblyInformationalVersionAttribute\("")(?:\\.|[^""])*(?=""\)\])");
        return true;
    }

    if (item.StartsWith("ASSEMBLY [System.Reflection.AssemblyMetadataAttribute(", StringComparison.Ordinal))
    {
        category = "assembly-metadata";
        normalized = NormalizeAssemblyAttributeStringValue(
            item,
            @"(?<=AssemblyMetadataAttribute\(""[^""]*"",\s*"")(?:\\.|[^""])*(?=""\)\])");
        return true;
    }

    category = string.Empty;
    normalized = string.Empty;
    return false;
}

static string NormalizeAssemblyAttributeStringValue(string item, string valuePattern)
{
    var normalized = Regex.Replace(
        item,
        valuePattern,
        static match => RemoveVersionText(match.Value),
        RegexOptions.CultureInvariant);

    return Regex.Replace(normalized, @"\s{2,}", " ", RegexOptions.CultureInvariant).Trim();
}

static string RemoveVersionText(string value)
{
    var normalized = Regex.Replace(
        value,
        @"(?<![A-Za-z0-9_])(?:Version=)?v?\d+(?:[.-]\d+)+(?:[-+][A-Za-z0-9.-]+)*",
        string.Empty,
        RegexOptions.CultureInvariant);

    return Regex.Replace(normalized, @"\s{2,}", " ", RegexOptions.CultureInvariant).Trim();
}

internal sealed record AssemblyPair(string PairId, string BeforePath, string AfterPath);

internal sealed record DiscoveryResult(List<AssemblyPair> Pairs, int PartialPairCount);

internal sealed record MemberDiffItem(string Item, bool IsVisibleApi);

internal sealed record PairAnalysisResult(
    string PairId,
    string BeforePath,
    string AfterPath,
    string BeforeIdentity,
    string AfterIdentity,
    string BeforeMvid,
    string AfterMvid,
    string Classification,
    Dictionary<string, PairDiffBucket> Categories);

internal sealed record Summary(
    int TotalPairs,
    Dictionary<string, int> ByClassification,
    Dictionary<string, string> CategoryDescriptions,
    List<CategorySummary> ByCategory,
    List<HypotheticalImprovement> HypotheticalImprovements);

internal sealed record PairResultsDocument(
    Dictionary<string, string> CategoryDescriptions,
    Dictionary<string, int> CategoryTotals,
    List<PairResultOutput> Results);

internal sealed record PairResultOutput(
    string PairId,
    string BeforePath,
    string AfterPath,
    string BeforeIdentity,
    string AfterIdentity,
    string BeforeMvid,
    string AfterMvid,
    string Classification,
    Dictionary<string, PairDiffBucket> Categories);

internal sealed record PairDiffBucket(List<string> Added, List<string> Removed);

internal enum DiffSide
{
    Before,
    After
}

internal sealed record CategorySummary(string Category, int AddedCount, int RemovedCount, int PairCount)
{
    public int TotalCount => AddedCount + RemovedCount;
}

internal sealed record HypotheticalImprovement(
    string Name,
    string IgnoredCategory,
    int IdenticalPairs,
    double ImprovementPercentage);

internal sealed class AssemblySnapshot
{
    public required string AssemblyIdentity { get; init; }
    public required string ModuleVersionId { get; init; }
    public required string NormalizedContentHash { get; init; }
    public required bool HasInternalsVisibleTo { get; init; }
    public required HashSet<MemberDiffItem> MemberItems { get; init; }
    public required HashSet<string> OtherMetadataItems { get; init; }
    public required HashSet<string> AssemblyReferences { get; init; }

    public static AssemblySnapshot Load(string path)
    {
        using var stream = File.OpenRead(path);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata)
        {
            throw new InvalidOperationException($"Assembly has no metadata: {path}");
        }

        var reader = peReader.GetMetadataReader();
        var assembly = reader.GetAssemblyDefinition();
        var module = reader.GetModuleDefinition();
        var provider = new DisplayNameProvider(reader);
        var accessorMethods = CollectAccessorMethods(reader);
        var memberItems = new HashSet<MemberDiffItem>();
        var otherMetadataItems = new HashSet<string>(StringComparer.Ordinal)
        {
            BuildAssemblyDefinitionSignature(reader, provider, assembly),
            BuildModuleDefinitionSignature(reader, provider, module)
        };

        var hasInternalsVisibleTo = false;
        foreach (var customAttributeHandle in assembly.GetCustomAttributes())
        {
            var formatted = FormatCustomAttribute(reader, provider, customAttributeHandle);
            memberItems.Add(new MemberDiffItem($"ASSEMBLY {formatted}", IsVisibleApi: true));
            if (formatted.StartsWith("[System.Runtime.CompilerServices.InternalsVisibleToAttribute(", StringComparison.Ordinal))
            {
                hasInternalsVisibleTo = true;
            }
        }

        foreach (var exportedTypeHandle in reader.ExportedTypes)
        {
            var exportedType = reader.GetExportedType(exportedTypeHandle);
            AddByVisibility(memberItems, BuildExportedTypeSignature(reader, provider, exportedTypeHandle), IsVisibleExportedType(exportedType.Attributes));
        }

        foreach (var handle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(handle);
            var typeName = GetTypeName(reader, provider, handle);
            if (typeName == "<Module>")
            {
                continue;
            }

            var isVisibleType = IsVisibleType(reader, handle);
            AddByVisibility(memberItems, BuildTypeSignature(reader, provider, handle), isVisibleType);

            foreach (var fieldHandle in type.GetFields())
            {
                var field = reader.GetFieldDefinition(fieldHandle);
                AddByVisibility(memberItems, BuildFieldSignature(reader, provider, handle, fieldHandle), isVisibleType && IsVisibleField(field.Attributes));
            }

            foreach (var methodHandle in type.GetMethods())
            {
                if (accessorMethods.Contains(methodHandle))
                {
                    continue;
                }

                var method = reader.GetMethodDefinition(methodHandle);
                AddByVisibility(memberItems, BuildMethodSignature(reader, provider, handle, methodHandle), isVisibleType && IsVisibleMethod(method.Attributes));
            }

            foreach (var methodImplementationHandle in type.GetMethodImplementations())
            {
                AddByVisibility(memberItems, BuildMethodImplementationSignature(reader, provider, handle, methodImplementationHandle), isVisibleType);
            }

            foreach (var propertyHandle in type.GetProperties())
            {
                if (isVisibleType && IsVisibleProperty(reader, propertyHandle))
                {
                    memberItems.Add(new MemberDiffItem(BuildPropertySignature(reader, provider, handle, propertyHandle, apiSurface: true), IsVisibleApi: true));
                }
                else
                {
                    memberItems.Add(new MemberDiffItem(BuildPropertySignature(reader, provider, handle, propertyHandle, apiSurface: false), IsVisibleApi: false));
                }
            }

            foreach (var eventHandle in type.GetEvents())
            {
                if (isVisibleType && IsVisibleEvent(reader, eventHandle))
                {
                    memberItems.Add(new MemberDiffItem(BuildEventSignature(reader, provider, handle, eventHandle, apiSurface: true), IsVisibleApi: true));
                }
                else
                {
                    memberItems.Add(new MemberDiffItem(BuildEventSignature(reader, provider, handle, eventHandle, apiSurface: false), IsVisibleApi: false));
                }
            }
        }

        var references = new HashSet<string>(StringComparer.Ordinal);
        foreach (var referenceHandle in reader.AssemblyReferences)
        {
            var reference = reader.GetAssemblyReference(referenceHandle);
            references.Add(BuildAssemblyReferenceSignature(reader, provider, referenceHandle));
        }

        foreach (var resourceHandle in reader.ManifestResources)
        {
            otherMetadataItems.Add(BuildManifestResourceSignature(reader, provider, resourceHandle));
        }

        foreach (var fileHandle in reader.AssemblyFiles)
        {
            otherMetadataItems.Add(BuildAssemblyFileSignature(reader, provider, fileHandle));
        }

        return new AssemblySnapshot
        {
            AssemblyIdentity = $"{reader.GetString(assembly.Name)}, Version={assembly.Version}, PKT={FormatPublicKeyToken(reader, assembly.PublicKey)}",
            ModuleVersionId = reader.GetGuid(module.Mvid).ToString(),
            NormalizedContentHash = ComputeNormalizedContentHash(path, reader.GetGuid(module.Mvid)),
            HasInternalsVisibleTo = hasInternalsVisibleTo,
            MemberItems = memberItems,
            OtherMetadataItems = otherMetadataItems,
            AssemblyReferences = references
        };
    }

    private static void AddByVisibility(HashSet<MemberDiffItem> items, string item, bool isVisible)
        => items.Add(new MemberDiffItem(item, isVisible));

    private static string BuildAssemblyDefinitionSignature(MetadataReader reader, DisplayNameProvider provider, AssemblyDefinition assembly)
    {
        var parts = new List<string>
        {
            $"ASSEMBLYDEF {reader.GetString(assembly.Name)}",
            $"version={assembly.Version}",
            $"culture={FormatStringHandle(reader, assembly.Culture)}",
            $"flags={assembly.Flags}",
            $"hash={assembly.HashAlgorithm}",
            $"publickey={FormatBlob(reader, assembly.PublicKey)}"
        };

        var security = FormatDeclarativeSecurityAttributes(reader, assembly.GetDeclarativeSecurityAttributes());
        if (!string.IsNullOrEmpty(security))
        {
            parts.Add(security.Trim());
        }

        return string.Join(" ", parts);
    }

    private static string BuildModuleDefinitionSignature(MetadataReader reader, DisplayNameProvider provider, ModuleDefinition module)
    {
        var parts = new List<string>
        {
            $"MODULE {reader.GetString(module.Name)}",
            $"generation={module.Generation}",
            $"generationId={FormatGuidHandle(reader, module.GenerationId)}",
            $"baseGenerationId={FormatGuidHandle(reader, module.BaseGenerationId)}"
        };

        var attributes = FormatCustomAttributes(reader, provider, module.GetCustomAttributes()).Trim();
        if (!string.IsNullOrEmpty(attributes))
        {
            parts.Add(attributes);
        }

        return string.Join(" ", parts);
    }

    private static string BuildAssemblyReferenceSignature(MetadataReader reader, DisplayNameProvider provider, AssemblyReferenceHandle handle)
    {
        var reference = reader.GetAssemblyReference(handle);
        var parts = new List<string>
        {
            reader.GetString(reference.Name),
            $"Version={reference.Version}",
            $"Culture={FormatStringHandle(reader, reference.Culture)}",
            $"Flags={reference.Flags}",
            $"PKT={FormatPublicKeyToken(reader, reference.PublicKeyOrToken)}",
            $"Hash={FormatBlob(reader, reference.HashValue)}"
        };

        var attributes = FormatCustomAttributes(reader, provider, reference.GetCustomAttributes()).Trim();
        if (!string.IsNullOrEmpty(attributes))
        {
            parts.Add(attributes);
        }

        return string.Join(", ", parts);
    }

    private static bool IsVisibleExportedType(TypeAttributes attributes)
        => (attributes & TypeAttributes.VisibilityMask) is TypeAttributes.Public or TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem;

    private static string BuildExportedTypeSignature(MetadataReader reader, DisplayNameProvider provider, ExportedTypeHandle handle)
    {
        var exportedType = reader.GetExportedType(handle);
        var namespaceName = reader.GetString(exportedType.Namespace);
        var typeName = reader.GetString(exportedType.Name);
        var fullName = string.IsNullOrEmpty(namespaceName) ? typeName : $"{namespaceName}.{typeName}";
        var attributes = FormatTypeMetadata(exportedType.Attributes, default, null);
        var customAttributes = FormatCustomAttributes(reader, provider, exportedType.GetCustomAttributes());
        var implementation = FormatImplementation(reader, provider, exportedType.Implementation);
        var forwarder = exportedType.IsForwarder ? " forwarder" : string.Empty;
        return $"EXPORTEDTYPE {GetTypeVisibility(exportedType.Attributes)} {customAttributes}{attributes}{fullName}{forwarder} -> {implementation}"
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static string BuildManifestResourceSignature(MetadataReader reader, DisplayNameProvider provider, ManifestResourceHandle handle)
    {
        var resource = reader.GetManifestResource(handle);
        var attributes = FormatCustomAttributes(reader, provider, resource.GetCustomAttributes()).Trim();
        var attributesPart = string.IsNullOrEmpty(attributes) ? string.Empty : $" {attributes}";
        return $"RESOURCE {reader.GetString(resource.Name)} attrs={resource.Attributes} offset={resource.Offset} impl={FormatImplementation(reader, provider, resource.Implementation)}{attributesPart}";
    }

    private static string BuildAssemblyFileSignature(MetadataReader reader, DisplayNameProvider provider, AssemblyFileHandle handle)
    {
        var file = reader.GetAssemblyFile(handle);
        var attributes = FormatCustomAttributes(reader, provider, file.GetCustomAttributes()).Trim();
        var attributesPart = string.IsNullOrEmpty(attributes) ? string.Empty : $" {attributes}";
        return $"FILE {reader.GetString(file.Name)} containsMetadata={file.ContainsMetadata} hash={FormatBlob(reader, file.HashValue)}{attributesPart}";
    }

    private static string BuildMethodImplementationSignature(MetadataReader reader, DisplayNameProvider provider, TypeDefinitionHandle declaringTypeHandle, MethodImplementationHandle handle)
    {
        var implementation = reader.GetMethodImplementation(handle);
        var typeName = GetTypeName(reader, provider, declaringTypeHandle);
        return $"METHODIMPL {typeName}::{FormatMethodImplementationTarget(reader, provider, implementation.MethodBody)} => {FormatMethodImplementationTarget(reader, provider, implementation.MethodDeclaration)}";
    }

    private static string ComputeNormalizedContentHash(string path, Guid moduleVersionId)
    {
        var bytes = File.ReadAllBytes(path);
        ZeroPatternOccurrences(bytes, moduleVersionId.ToByteArray());
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static void ZeroPatternOccurrences(byte[] data, byte[] pattern)
    {
        if (pattern.Length == 0 || data.Length < pattern.Length)
        {
            return;
        }

        for (var index = 0; index <= data.Length - pattern.Length; index++)
        {
            var match = true;
            for (var offset = 0; offset < pattern.Length; offset++)
            {
                if (data[index + offset] != pattern[offset])
                {
                    match = false;
                    break;
                }
            }

            if (!match)
            {
                continue;
            }

            Array.Clear(data, index, pattern.Length);
            index += pattern.Length - 1;
        }
    }

    private static HashSet<MethodDefinitionHandle> CollectAccessorMethods(MetadataReader reader)
    {
        var methods = new HashSet<MethodDefinitionHandle>();

        foreach (var typeHandle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(typeHandle);
            foreach (var propertyHandle in type.GetProperties())
            {
                var accessors = reader.GetPropertyDefinition(propertyHandle).GetAccessors();
                if (!accessors.Getter.IsNil)
                {
                    methods.Add(accessors.Getter);
                }

                if (!accessors.Setter.IsNil)
                {
                    methods.Add(accessors.Setter);
                }
            }

            foreach (var eventHandle in type.GetEvents())
            {
                var accessors = reader.GetEventDefinition(eventHandle).GetAccessors();
                if (!accessors.Adder.IsNil)
                {
                    methods.Add(accessors.Adder);
                }

                if (!accessors.Remover.IsNil)
                {
                    methods.Add(accessors.Remover);
                }

                if (!accessors.Raiser.IsNil)
                {
                    methods.Add(accessors.Raiser);
                }
            }
        }

        return methods;
    }

    private static bool IsVisibleType(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var currentHandle = handle;
        while (!currentHandle.IsNil)
        {
            var definition = reader.GetTypeDefinition(currentHandle);
            var attributes = definition.Attributes & TypeAttributes.VisibilityMask;
            var isVisible = attributes switch
            {
                TypeAttributes.Public => true,
                TypeAttributes.NestedPublic => true,
                TypeAttributes.NestedFamily => true,
                TypeAttributes.NestedFamORAssem => true,
                _ => false
            };

            if (!isVisible)
            {
                return false;
            }

            currentHandle = definition.GetDeclaringType();
        }

        return true;
    }

    private static bool IsVisibleField(FieldAttributes attributes)
        => (attributes & FieldAttributes.FieldAccessMask) is FieldAttributes.Public or FieldAttributes.Family or FieldAttributes.FamORAssem;

    private static bool IsVisibleMethod(MethodAttributes attributes)
        => (attributes & MethodAttributes.MemberAccessMask) is MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem;

    private static bool IsVisibleProperty(MetadataReader reader, PropertyDefinitionHandle handle)
    {
        var accessors = reader.GetPropertyDefinition(handle).GetAccessors();
        return (!accessors.Getter.IsNil && IsVisibleMethod(reader.GetMethodDefinition(accessors.Getter).Attributes))
            || (!accessors.Setter.IsNil && IsVisibleMethod(reader.GetMethodDefinition(accessors.Setter).Attributes));
    }

    private static bool IsVisibleEvent(MetadataReader reader, EventDefinitionHandle handle)
    {
        var accessors = reader.GetEventDefinition(handle).GetAccessors();
        return (!accessors.Adder.IsNil && IsVisibleMethod(reader.GetMethodDefinition(accessors.Adder).Attributes))
            || (!accessors.Remover.IsNil && IsVisibleMethod(reader.GetMethodDefinition(accessors.Remover).Attributes))
            || (!accessors.Raiser.IsNil && IsVisibleMethod(reader.GetMethodDefinition(accessors.Raiser).Attributes));
    }

    private static string BuildTypeSignature(MetadataReader reader, DisplayNameProvider provider, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        var typeName = GetTypeName(reader, provider, handle);
        var visibility = GetTypeVisibility(type.Attributes);
        var attributes = FormatCustomAttributes(reader, provider, type.GetCustomAttributes());
        var metadata = FormatTypeMetadata(type.Attributes, type.GetLayout(), FormatDeclarativeSecurityAttributes(reader, type.GetDeclarativeSecurityAttributes()));
        var kind = GetTypeKind(reader, provider, handle);
        var genericPart = GetGenericParameters(reader, provider, type.GetGenericParameters(), "!");
        var baseType = GetBaseTypePart(reader, provider, type, kind);
        var interfaces = type.GetInterfaceImplementations()
            .Select(interfaceHandle => GetTypeName(reader, provider, reader.GetInterfaceImplementation(interfaceHandle).Interface))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToList();

        var builder = new StringBuilder();
        builder.Append("TYPE ");
        builder.Append(visibility);
        builder.Append(' ');
        builder.Append(attributes);
        builder.Append(metadata);
        builder.Append(kind);
        builder.Append(' ');
        builder.Append(typeName);
        builder.Append(genericPart);

        if (!string.IsNullOrEmpty(baseType))
        {
            builder.Append(" : ");
            builder.Append(baseType);
        }

        if (interfaces.Count > 0)
        {
            builder.Append(" implements ");
            builder.Append(string.Join(", ", interfaces));
        }

        return builder.ToString();
    }

    private static string BuildFieldSignature(MetadataReader reader, DisplayNameProvider provider, TypeDefinitionHandle declaringTypeHandle, FieldDefinitionHandle fieldHandle)
    {
        var field = reader.GetFieldDefinition(fieldHandle);
        var fieldType = field.DecodeSignature(provider, genericContext: null);
        var typeName = GetTypeName(reader, provider, declaringTypeHandle);
        var fieldName = reader.GetString(field.Name);
        var visibility = GetFieldVisibility(field.Attributes);
        var attributes = FormatCustomAttributes(reader, provider, field.GetCustomAttributes());
        var metadata = FormatFieldMetadata(reader, field);

        return $"FIELD {typeName}::{visibility} {attributes}{metadata}{fieldType} {fieldName}".Replace("  ", " ", StringComparison.Ordinal).Trim();
    }

    private static string BuildMethodSignature(MetadataReader reader, DisplayNameProvider provider, TypeDefinitionHandle declaringTypeHandle, MethodDefinitionHandle methodHandle)
    {
        var method = reader.GetMethodDefinition(methodHandle);
        var signature = method.DecodeSignature(provider, genericContext: null);
        var parameterHandles = method.GetParameters()
            .Select(handle => (Handle: handle, Parameter: reader.GetParameter(handle)))
            .OrderBy(static entry => entry.Parameter.SequenceNumber)
            .ToArray();
        var returnParameterHandle = parameterHandles
            .Where(static entry => entry.Parameter.SequenceNumber == 0)
            .Select(static entry => (ParameterHandle?)entry.Handle)
            .FirstOrDefault();
        var methodParameters = parameterHandles
            .Where(static entry => entry.Parameter.SequenceNumber > 0)
            .Select(static entry => entry.Parameter)
            .ToArray();
        var typeName = GetTypeName(reader, provider, declaringTypeHandle);
        var methodName = reader.GetString(method.Name);
        var visibility = GetMethodVisibility(method.Attributes);
        var attributes = FormatCustomAttributes(reader, provider, method.GetCustomAttributes());
        var returnAttributes = returnParameterHandle is { } parameterHandle
            ? FormatCustomAttributes(reader, provider, reader.GetParameter(parameterHandle).GetCustomAttributes(), "return")
            : string.Empty;
        var metadata = FormatMethodMetadata(reader, provider, method, signature);

        var genericPart = GetGenericParameters(reader, provider, method.GetGenericParameters(), "!!");

        var formattedParameters = new List<string>(signature.ParameterTypes.Length);
        for (var index = 0; index < signature.ParameterTypes.Length; index++)
        {
            var parameter = index < methodParameters.Length ? methodParameters[index] : (Parameter?)null;
            formattedParameters.Add(FormatParameter(reader, provider, signature.ParameterTypes[index], parameter));
        }

        return $"METHOD {typeName}::{visibility} {attributes}{metadata}{returnAttributes}{signature.ReturnType} {methodName}{genericPart}({string.Join(", ", formattedParameters)})"
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static string BuildPropertySignature(MetadataReader reader, DisplayNameProvider provider, TypeDefinitionHandle declaringTypeHandle, PropertyDefinitionHandle propertyHandle, bool apiSurface)
    {
        var property = reader.GetPropertyDefinition(propertyHandle);
        var accessors = property.GetAccessors();
        var getter = !accessors.Getter.IsNil ? reader.GetMethodDefinition(accessors.Getter) : default;
        var setter = !accessors.Setter.IsNil ? reader.GetMethodDefinition(accessors.Setter) : default;
        var getterVisible = !accessors.Getter.IsNil && IsVisibleMethod(getter.Attributes);
        var setterVisible = !accessors.Setter.IsNil && IsVisibleMethod(setter.Attributes);
        var signature = property.DecodeSignature(provider, genericContext: null);
        var typeName = GetTypeName(reader, provider, declaringTypeHandle);
        var propertyName = reader.GetString(property.Name);
        var attributes = FormatCustomAttributes(reader, provider, property.GetCustomAttributes());
        var metadata = FormatPropertyMetadata(reader, property);
        var visibility = GetMostVisibleMethodVisibility(
            !accessors.Getter.IsNil ? getter.Attributes : null,
            !accessors.Setter.IsNil ? setter.Attributes : null);
        var parameterTypes = signature.ParameterTypes.Select(static type => type).ToArray();
        var indexerPart = parameterTypes.Length == 0 ? string.Empty : $"[{string.Join(", ", parameterTypes)}]";
        var accessorParts = new List<string>();

        if (!accessors.Getter.IsNil && (getterVisible || !apiSurface))
        {
            accessorParts.Add($"get:{GetMethodVisibility(getter.Attributes)}");
        }

        if (!accessors.Setter.IsNil && (setterVisible || !apiSurface))
        {
            accessorParts.Add($"set:{GetMethodVisibility(setter.Attributes)}");
        }

        foreach (var otherHandle in accessors.Others)
        {
            var other = reader.GetMethodDefinition(otherHandle);
            if (IsVisibleMethod(other.Attributes) || !apiSurface)
            {
                accessorParts.Add($"other:{GetMethodVisibility(other.Attributes)}:{reader.GetString(other.Name)}");
            }
        }

        return $"PROPERTY {typeName}::{visibility} {attributes}{metadata}{signature.ReturnType} {propertyName}{indexerPart} {{{string.Join("; ", accessorParts)}}}";
    }

    private static string BuildEventSignature(MetadataReader reader, DisplayNameProvider provider, TypeDefinitionHandle declaringTypeHandle, EventDefinitionHandle eventHandle, bool apiSurface)
    {
        var eventDefinition = reader.GetEventDefinition(eventHandle);
        var accessors = eventDefinition.GetAccessors();
        var adder = !accessors.Adder.IsNil ? reader.GetMethodDefinition(accessors.Adder) : default;
        var remover = !accessors.Remover.IsNil ? reader.GetMethodDefinition(accessors.Remover) : default;
        var raiser = !accessors.Raiser.IsNil ? reader.GetMethodDefinition(accessors.Raiser) : default;
        var addVisible = !accessors.Adder.IsNil && IsVisibleMethod(adder.Attributes);
        var removeVisible = !accessors.Remover.IsNil && IsVisibleMethod(remover.Attributes);
        var raiseVisible = !accessors.Raiser.IsNil && IsVisibleMethod(raiser.Attributes);
        var typeName = GetTypeName(reader, provider, declaringTypeHandle);
        var eventName = reader.GetString(eventDefinition.Name);
        var eventType = GetTypeName(reader, provider, eventDefinition.Type);
        var attributes = FormatCustomAttributes(reader, provider, eventDefinition.GetCustomAttributes());
        var metadata = FormatEventMetadata(eventDefinition);
        var visibility = GetMostVisibleMethodVisibility(
            !accessors.Adder.IsNil ? adder.Attributes : null,
            !accessors.Remover.IsNil ? remover.Attributes : null,
            !accessors.Raiser.IsNil ? raiser.Attributes : null);
        var accessorParts = new List<string>();

        if (!accessors.Adder.IsNil && (addVisible || !apiSurface))
        {
            accessorParts.Add($"add:{GetMethodVisibility(adder.Attributes)}");
        }

        if (!accessors.Remover.IsNil && (removeVisible || !apiSurface))
        {
            accessorParts.Add($"remove:{GetMethodVisibility(remover.Attributes)}");
        }

        if (!accessors.Raiser.IsNil && (raiseVisible || !apiSurface))
        {
            accessorParts.Add($"raise:{GetMethodVisibility(raiser.Attributes)}");
        }

        foreach (var otherHandle in accessors.Others)
        {
            var other = reader.GetMethodDefinition(otherHandle);
            if (IsVisibleMethod(other.Attributes) || !apiSurface)
            {
                accessorParts.Add($"other:{GetMethodVisibility(other.Attributes)}:{reader.GetString(other.Name)}");
            }
        }

        return $"EVENT {typeName}::{visibility} {attributes}{metadata}{eventType} {eventName} {{{string.Join("; ", accessorParts)}}}";
    }

    private static string FormatTypeMetadata(TypeAttributes attributes, TypeLayout layout, string? security)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(security))
        {
            parts.Add(security.Trim());
        }

        if (!layout.IsDefault)
        {
            parts.Add($"[layout: pack={layout.PackingSize}, size={layout.Size}]");
        }

        var flags = new List<string>();
        switch (attributes & TypeAttributes.LayoutMask)
        {
            case TypeAttributes.SequentialLayout:
                flags.Add("sequentiallayout");
                break;
            case TypeAttributes.ExplicitLayout:
                flags.Add("explicitlayout");
                break;
        }

        switch (attributes & TypeAttributes.StringFormatMask)
        {
            case TypeAttributes.AnsiClass:
                flags.Add("ansi");
                break;
            case TypeAttributes.UnicodeClass:
                flags.Add("unicode");
                break;
            case TypeAttributes.AutoClass:
                flags.Add("autochar");
                break;
            case TypeAttributes.CustomFormatClass:
                flags.Add("customformat");
                break;
        }

        if (attributes.HasFlag(TypeAttributes.Abstract))
        {
            flags.Add("abstract");
        }

        if (attributes.HasFlag(TypeAttributes.Sealed))
        {
            flags.Add("sealed");
        }

        if (attributes.HasFlag(TypeAttributes.SpecialName))
        {
            flags.Add("specialname");
        }

        if (attributes.HasFlag(TypeAttributes.RTSpecialName))
        {
            flags.Add("rtspecialname");
        }

        if (attributes.HasFlag(TypeAttributes.Import))
        {
            flags.Add("import");
        }

        if ((attributes & (TypeAttributes)8192) != 0)
        {
            flags.Add("serializable");
        }

        if (attributes.HasFlag(TypeAttributes.WindowsRuntime))
        {
            flags.Add("windowsruntime");
        }

        if (attributes.HasFlag(TypeAttributes.BeforeFieldInit))
        {
            flags.Add("beforefieldinit");
        }

        if (flags.Count > 0)
        {
            parts.Add($"[flags: {string.Join(", ", flags)}]");
        }

        return parts.Count == 0 ? string.Empty : string.Join(" ", parts) + " ";
    }

    private static string FormatFieldMetadata(MetadataReader reader, FieldDefinition field)
    {
        var parts = new List<string>();
        var flags = new List<string>();

        if (field.Attributes.HasFlag(FieldAttributes.Static))
        {
            flags.Add("static");
        }

        if (field.Attributes.HasFlag(FieldAttributes.InitOnly))
        {
            flags.Add("readonly");
        }

        if (field.Attributes.HasFlag(FieldAttributes.Literal))
        {
            flags.Add("const");
        }

        if ((field.Attributes & (FieldAttributes)128) != 0)
        {
            flags.Add("notserialized");
        }

        if (field.Attributes.HasFlag(FieldAttributes.SpecialName))
        {
            flags.Add("specialname");
        }

        if (field.Attributes.HasFlag(FieldAttributes.RTSpecialName))
        {
            flags.Add("rtspecialname");
        }

        if (flags.Count > 0)
        {
            parts.Add($"[flags: {string.Join(", ", flags)}]");
        }

        var defaultValue = field.GetDefaultValue();
        if (!defaultValue.IsNil)
        {
            parts.Add($"[default: {FormatConstant(reader, defaultValue)}]");
        }

        var marshallingDescriptor = field.GetMarshallingDescriptor();
        if (!marshallingDescriptor.IsNil)
        {
            parts.Add($"[marshal: {FormatBlob(reader, marshallingDescriptor)}]");
        }

        var offset = field.GetOffset();
        if (offset >= 0)
        {
            parts.Add($"[offset: {offset}]");
        }

        var rva = field.GetRelativeVirtualAddress();
        if (rva != 0)
        {
            parts.Add($"[rva: {rva}]");
        }

        return parts.Count == 0 ? string.Empty : string.Join(" ", parts) + " ";
    }

    private static string FormatMethodMetadata(MetadataReader reader, DisplayNameProvider provider, MethodDefinition method, MethodSignature<string> signature)
    {
        var parts = new List<string>();
        var flags = new List<string>();

        if (method.Attributes.HasFlag(MethodAttributes.Static))
        {
            flags.Add("static");
        }

        if (method.Attributes.HasFlag(MethodAttributes.Abstract))
        {
            flags.Add("abstract");
        }
        else if (method.Attributes.HasFlag(MethodAttributes.Virtual))
        {
            flags.Add("virtual");
        }

        if (method.Attributes.HasFlag(MethodAttributes.Final))
        {
            flags.Add("final");
        }

        if (method.Attributes.HasFlag(MethodAttributes.HideBySig))
        {
            flags.Add("hidebysig");
        }

        if ((method.Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot)
        {
            flags.Add("newslot");
        }

        if (method.Attributes.HasFlag(MethodAttributes.CheckAccessOnOverride))
        {
            flags.Add("strict");
        }

        if (method.Attributes.HasFlag(MethodAttributes.SpecialName))
        {
            flags.Add("specialname");
        }

        if (method.Attributes.HasFlag(MethodAttributes.RTSpecialName))
        {
            flags.Add("rtspecialname");
        }

        if (method.Attributes.HasFlag(MethodAttributes.RequireSecObject))
        {
            flags.Add("requiresecobj");
        }

        if (flags.Count > 0)
        {
            parts.Add($"[flags: {string.Join(", ", flags)}]");
        }

        parts.Add($"[sig: kind={signature.Header.Kind}, callconv={signature.Header.CallingConvention}, instance={signature.Header.IsInstance}, explicitthis={signature.Header.HasExplicitThis}, required={signature.RequiredParameterCount}]");
        parts.Add($"[impl: {FormatMethodImplAttributes(method.ImplAttributes)}]");

        if (method.Attributes.HasFlag(MethodAttributes.PinvokeImpl))
        {
            var import = method.GetImport();
            parts.Add($"[import: {reader.GetString(reader.GetModuleReference(import.Module).Name)}!{FormatStringHandle(reader, import.Name)} {import.Attributes}]");
        }

        var security = FormatDeclarativeSecurityAttributes(reader, method.GetDeclarativeSecurityAttributes());
        if (!string.IsNullOrEmpty(security))
        {
            parts.Add(security.Trim());
        }

        if (method.RelativeVirtualAddress != 0)
        {
            parts.Add($"[rva: {method.RelativeVirtualAddress}]");
        }

        return string.Join(" ", parts.Where(static part => !string.IsNullOrWhiteSpace(part))) + (parts.Count == 0 ? string.Empty : " ");
    }

    private static string FormatPropertyMetadata(MetadataReader reader, PropertyDefinition property)
    {
        var parts = new List<string>();
        var flags = new List<string>();
        if (property.Attributes.HasFlag(PropertyAttributes.SpecialName))
        {
            flags.Add("specialname");
        }

        if (property.Attributes.HasFlag(PropertyAttributes.RTSpecialName))
        {
            flags.Add("rtspecialname");
        }

        if (flags.Count > 0)
        {
            parts.Add($"[flags: {string.Join(", ", flags)}]");
        }

        var defaultValue = property.GetDefaultValue();
        if (!defaultValue.IsNil)
        {
            parts.Add($"[default: {FormatConstant(reader, defaultValue)}]");
        }

        return parts.Count == 0 ? string.Empty : string.Join(" ", parts) + " ";
    }

    private static string FormatEventMetadata(EventDefinition eventDefinition)
    {
        var flags = new List<string>();
        if (eventDefinition.Attributes.HasFlag(EventAttributes.SpecialName))
        {
            flags.Add("specialname");
        }

        if (eventDefinition.Attributes.HasFlag(EventAttributes.RTSpecialName))
        {
            flags.Add("rtspecialname");
        }

        return flags.Count == 0 ? string.Empty : $"[flags: {string.Join(", ", flags)}] ";
    }

    private static string FormatCustomAttributes(
        MetadataReader reader,
        DisplayNameProvider provider,
        CustomAttributeHandleCollection customAttributes,
        string? target = null)
    {
        var attributes = customAttributes
            .Select(handle => FormatCustomAttribute(reader, provider, handle, target))
            .OrderBy(static attribute => attribute, StringComparer.Ordinal)
            .ToList();

        return attributes.Count == 0 ? string.Empty : string.Join(" ", attributes) + " ";
    }

    private static string FormatCustomAttribute(
        MetadataReader reader,
        DisplayNameProvider provider,
        CustomAttributeHandle handle,
        string? target = null)
    {
        var attribute = reader.GetCustomAttribute(handle);
        var attributeType = GetCustomAttributeTypeName(reader, provider, attribute);
        var decoded = attribute.DecodeValue(provider);

        var arguments = decoded.FixedArguments
            .Select(FormatCustomAttributeArgument)
            .Concat(decoded.NamedArguments
                .OrderBy(static argument => argument.Name, StringComparer.Ordinal)
                .Select(FormatCustomAttributeNamedArgument))
            .ToList();

        var value = arguments.Count == 0
            ? attributeType
            : $"{attributeType}({string.Join(", ", arguments)})";

        return target is null ? $"[{value}]" : $"[{target}: {value}]";
    }

    private static string GetCustomAttributeTypeName(MetadataReader reader, DisplayNameProvider provider, CustomAttribute attribute)
        => attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => GetTypeName(reader, provider, reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent),
            HandleKind.MethodDefinition => GetTypeName(reader, provider, FindDeclaringType(reader, (MethodDefinitionHandle)attribute.Constructor)),
            _ => $"<{attribute.Constructor.Kind}>"
        };

    private static string FormatCustomAttributeNamedArgument(CustomAttributeNamedArgument<string> argument)
        => $"{argument.Name} = {FormatCustomAttributeValue(argument.Type, argument.Value)}";

    private static string FormatCustomAttributeArgument(CustomAttributeTypedArgument<string> argument)
        => FormatCustomAttributeValue(argument.Type, argument.Value);

    private static string FormatCustomAttributeValue(string type, object? value)
    {
        return value switch
        {
            null => "null",
            string stringValue when type == "System.Type" => $"typeof({stringValue})",
            string stringValue => $"\"{stringValue.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            char charValue => $"'{charValue}'",
            bool boolValue => boolValue ? "true" : "false",
            ImmutableArray<CustomAttributeTypedArgument<string>> values => $"[{string.Join(", ", values.Select(FormatCustomAttributeArgument))}]",
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? value.ToString() ?? string.Empty
        };
    }

    private static string FormatDeclarativeSecurityAttributes(
        MetadataReader reader,
        DeclarativeSecurityAttributeHandleCollection securityAttributes)
    {
        var attributes = securityAttributes
            .Select(handle =>
            {
                var attribute = reader.GetDeclarativeSecurityAttribute(handle);
                return $"[security: {attribute.Action}({FormatBlob(reader, attribute.PermissionSet)})]";
            })
            .OrderBy(static attribute => attribute, StringComparer.Ordinal)
            .ToList();

        return attributes.Count == 0 ? string.Empty : string.Join(" ", attributes) + " ";
    }

    private static string FormatMethodImplementationTarget(MetadataReader reader, DisplayNameProvider provider, EntityHandle handle)
        => handle.Kind switch
        {
            HandleKind.MethodDefinition => FormatMethodDefinitionTarget(reader, provider, (MethodDefinitionHandle)handle),
            HandleKind.MemberReference => FormatMemberReferenceTarget(reader, provider, (MemberReferenceHandle)handle),
            _ => $"<{handle.Kind}>"
        };

    private static string FormatMethodDefinitionTarget(MetadataReader reader, DisplayNameProvider provider, MethodDefinitionHandle handle)
    {
        var method = reader.GetMethodDefinition(handle);
        var signature = method.DecodeSignature(provider, genericContext: null);
        var typeName = GetTypeName(reader, provider, method.GetDeclaringType());
        return $"{typeName}::{reader.GetString(method.Name)}({string.Join(", ", signature.ParameterTypes)})";
    }

    private static string FormatMemberReferenceTarget(MetadataReader reader, DisplayNameProvider provider, MemberReferenceHandle handle)
    {
        var memberReference = reader.GetMemberReference(handle);
        var signature = memberReference.DecodeMethodSignature(provider, genericContext: null);
        return $"{FormatMemberReferenceParent(reader, provider, memberReference.Parent)}::{reader.GetString(memberReference.Name)}({string.Join(", ", signature.ParameterTypes)})";
    }

    private static string FormatMemberReferenceParent(MetadataReader reader, DisplayNameProvider provider, EntityHandle handle)
        => handle.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeName(reader, provider, handle),
            HandleKind.TypeReference => GetTypeName(reader, provider, handle),
            HandleKind.TypeSpecification => GetTypeName(reader, provider, handle),
            HandleKind.ModuleReference => $"moduleref:{reader.GetString(reader.GetModuleReference((ModuleReferenceHandle)handle).Name)}",
            HandleKind.MethodDefinition => FormatMethodDefinitionTarget(reader, provider, (MethodDefinitionHandle)handle),
            _ => $"<{handle.Kind}>"
        };

    private static string FormatImplementation(MetadataReader reader, DisplayNameProvider provider, EntityHandle handle)
    {
        if (handle.IsNil)
        {
            return "<none>";
        }

        return handle.Kind switch
        {
            HandleKind.AssemblyFile => $"file:{reader.GetString(reader.GetAssemblyFile((AssemblyFileHandle)handle).Name)}",
            HandleKind.AssemblyReference => $"assembly:{BuildAssemblyReferenceSignature(reader, provider, (AssemblyReferenceHandle)handle)}",
            HandleKind.ExportedType => $"exported:{BuildExportedTypeSignature(reader, provider, (ExportedTypeHandle)handle)}",
            _ => $"<{handle.Kind}>"
        };
    }

    private static string FormatMethodImplAttributes(MethodImplAttributes attributes)
    {
        var parts = new List<string>();
        parts.Add((attributes & MethodImplAttributes.CodeTypeMask) switch
        {
            MethodImplAttributes.Native => "native",
            MethodImplAttributes.OPTIL => "optil",
            MethodImplAttributes.Runtime => "runtime",
            _ => "il"
        });

        parts.Add((attributes & MethodImplAttributes.ManagedMask) == MethodImplAttributes.Unmanaged ? "unmanaged" : "managed");

        if (attributes.HasFlag(MethodImplAttributes.ForwardRef))
        {
            parts.Add("forwardref");
        }

        if (attributes.HasFlag(MethodImplAttributes.PreserveSig))
        {
            parts.Add("preservesig");
        }

        if (attributes.HasFlag(MethodImplAttributes.InternalCall))
        {
            parts.Add("internalcall");
        }

        if (attributes.HasFlag(MethodImplAttributes.Synchronized))
        {
            parts.Add("synchronized");
        }

        if (attributes.HasFlag(MethodImplAttributes.NoInlining))
        {
            parts.Add("noinlining");
        }

        if (attributes.HasFlag(MethodImplAttributes.AggressiveInlining))
        {
            parts.Add("aggressiveinlining");
        }

        if (attributes.HasFlag(MethodImplAttributes.NoOptimization))
        {
            parts.Add("nooptimization");
        }

        if (attributes.HasFlag((MethodImplAttributes)512))
        {
            parts.Add("aggressiveoptimization");
        }

        if (attributes.HasFlag((MethodImplAttributes)1024))
        {
            parts.Add("securitymitigations");
        }

        return string.Join(", ", parts);
    }

    private static string FormatConstant(MetadataReader reader, ConstantHandle handle)
    {
        var constant = reader.GetConstant(handle);
        var bytes = reader.GetBlobBytes(constant.Value);
        return constant.TypeCode switch
        {
            ConstantTypeCode.Boolean => bytes[0] == 0 ? "false" : "true",
            ConstantTypeCode.Char => $"'{(char)BitConverter.ToUInt16(bytes, 0)}'",
            ConstantTypeCode.SByte => ((sbyte)bytes[0]).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Byte => bytes[0].ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Int16 => BitConverter.ToInt16(bytes, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt16 => BitConverter.ToUInt16(bytes, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Int32 => BitConverter.ToInt32(bytes, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt32 => BitConverter.ToUInt32(bytes, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Int64 => BitConverter.ToInt64(bytes, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt64 => BitConverter.ToUInt64(bytes, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Single => BitConverter.ToSingle(bytes, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Double => BitConverter.ToDouble(bytes, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.String => $"\"{Encoding.Unicode.GetString(bytes).TrimEnd('\0').Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            ConstantTypeCode.NullReference => "null",
            _ => $"0x{Convert.ToHexString(bytes)}"
        };
    }

    private static string FormatBlob(MetadataReader reader, BlobHandle handle)
        => handle.IsNil ? "null" : Convert.ToHexString(reader.GetBlobBytes(handle)).ToLowerInvariant();

    private static string FormatGuidHandle(MetadataReader reader, GuidHandle handle)
        => handle.IsNil ? "null" : reader.GetGuid(handle).ToString();

    private static string FormatStringHandle(MetadataReader reader, StringHandle handle)
        => handle.IsNil ? "null" : reader.GetString(handle);

    private static TypeDefinitionHandle FindDeclaringType(MetadataReader reader, MethodDefinitionHandle methodHandle)
    {
        foreach (var typeHandle in reader.TypeDefinitions)
        {
            if (reader.GetTypeDefinition(typeHandle).GetMethods().Contains(methodHandle))
            {
                return typeHandle;
            }
        }

        throw new InvalidOperationException($"Could not find declaring type for method {methodHandle}.");
    }

    private static string GetTypeName(MetadataReader reader, DisplayNameProvider provider, EntityHandle handle)
        => handle.Kind switch
        {
            HandleKind.TypeDefinition => provider.GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle, rawTypeKind: (byte)SignatureTypeCode.TypeHandle),
            HandleKind.TypeReference => provider.GetTypeFromReference(reader, (TypeReferenceHandle)handle, rawTypeKind: (byte)SignatureTypeCode.TypeHandle),
            HandleKind.TypeSpecification => provider.GetTypeFromSpecification(reader, null, (TypeSpecificationHandle)handle, rawTypeKind: (byte)SignatureTypeCode.TypeHandle),
            _ => $"<{handle.Kind}>"
        };

    private static string GetTypeKind(MetadataReader reader, DisplayNameProvider provider, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        if (type.Attributes.HasFlag(TypeAttributes.Interface))
        {
            return "interface";
        }

        var baseType = type.BaseType.IsNil ? string.Empty : GetTypeName(reader, provider, type.BaseType);
        return baseType switch
        {
            "System.Enum" => "enum",
            "System.MulticastDelegate" => "delegate",
            "System.ValueType" => "struct",
            _ => "class"
        };
    }

    private static string GetBaseTypePart(MetadataReader reader, DisplayNameProvider provider, TypeDefinition type, string typeKind)
    {
        if (type.BaseType.IsNil)
        {
            return string.Empty;
        }

        var baseType = GetTypeName(reader, provider, type.BaseType);
        return typeKind switch
        {
            "class" when baseType != "System.Object" => baseType,
            "struct" when baseType != "System.ValueType" => baseType,
            "enum" => baseType,
            "delegate" => baseType,
            _ => string.Empty
        };
    }

    private static string GetGenericParameters(MetadataReader reader, DisplayNameProvider provider, GenericParameterHandleCollection handles, string prefix)
    {
        var items = new List<string>();
        foreach (var handle in handles)
        {
            var parameter = reader.GetGenericParameter(handle);
            var partBuilder = new StringBuilder();
            partBuilder.Append(FormatCustomAttributes(reader, provider, parameter.GetCustomAttributes()));

            var variance = parameter.Attributes & GenericParameterAttributes.VarianceMask;
            if (variance == GenericParameterAttributes.Covariant)
            {
                partBuilder.Append("out ");
            }
            else if (variance == GenericParameterAttributes.Contravariant)
            {
                partBuilder.Append("in ");
            }

            partBuilder.Append(prefix);
            partBuilder.Append(parameter.Index);

            var constraints = new List<string>();
            var special = parameter.Attributes & GenericParameterAttributes.SpecialConstraintMask;
            if (special.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
            {
                constraints.Add("class");
            }

            if (special.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            {
                constraints.Add("struct");
            }

            foreach (var constraintHandle in parameter.GetConstraints())
            {
                var constraint = reader.GetGenericParameterConstraint(constraintHandle);
                constraints.Add(GetTypeName(reader, provider, constraint.Type));
            }

            if (special.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
            {
                constraints.Add("new()");
            }

            if (constraints.Count > 0)
            {
                partBuilder.Append(" where ");
                partBuilder.Append(string.Join(" & ", constraints.OrderBy(static item => item, StringComparer.Ordinal)));
            }

            items.Add(partBuilder.ToString());
        }

        return items.Count == 0 ? string.Empty : $"<{string.Join(", ", items)}>";
    }

    private static string GetTypeVisibility(TypeAttributes attributes)
        => (attributes & TypeAttributes.VisibilityMask) switch
        {
            TypeAttributes.Public => "public",
            TypeAttributes.NestedPublic => "public",
            TypeAttributes.NestedFamily => "protected",
            TypeAttributes.NestedFamORAssem => "protected internal",
            TypeAttributes.NestedAssembly => "internal",
            TypeAttributes.NotPublic => "internal",
            _ => "private"
        };

    private static string GetFieldVisibility(FieldAttributes attributes)
        => (attributes & FieldAttributes.FieldAccessMask) switch
        {
            FieldAttributes.Public => "public",
            FieldAttributes.Family => "protected",
            FieldAttributes.FamORAssem => "protected internal",
            FieldAttributes.Assembly => "internal",
            _ => "private"
        };

    private static string GetMethodVisibility(MethodAttributes attributes)
        => (attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public => "public",
            MethodAttributes.Family => "protected",
            MethodAttributes.FamORAssem => "protected internal",
            MethodAttributes.Assembly => "internal",
            _ => "private"
        };

    private static string GetEffectiveTypeVisibility(MetadataReader reader, TypeDefinitionHandle handle)
        => IsVisibleType(reader, handle)
            ? GetTypeVisibility(reader.GetTypeDefinition(handle).Attributes)
            : GetTypeVisibility(reader.GetTypeDefinition(handle).Attributes) is "public" or "protected" or "protected internal"
                ? "internal"
                : GetTypeVisibility(reader.GetTypeDefinition(handle).Attributes);

    private static string GetEffectiveFieldVisibility(MetadataReader reader, TypeDefinitionHandle declaringTypeHandle, FieldAttributes attributes)
        => IsVisibleType(reader, declaringTypeHandle)
            ? GetFieldVisibility(attributes)
            : GetFieldVisibility(attributes) is "public" or "protected" or "protected internal"
                ? "internal"
                : GetFieldVisibility(attributes);

    private static string GetEffectiveMethodVisibility(MetadataReader reader, TypeDefinitionHandle declaringTypeHandle, MethodAttributes attributes)
        => GetMethodVisibility(GetEffectiveMethodAttributes(reader, declaringTypeHandle, attributes));

    private static MethodAttributes GetEffectiveMethodAttributes(MetadataReader reader, TypeDefinitionHandle declaringTypeHandle, MethodAttributes attributes)
        => IsVisibleType(reader, declaringTypeHandle)
            ? attributes
            : (attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Private;

    private static string GetMostVisibleMethodVisibility(params MethodAttributes?[] attributes)
        => attributes
            .Where(static attributes => attributes is not null)
            .Select(static attributes => GetMethodVisibility(attributes!.Value))
            .OrderByDescending(GetVisibilityRank)
            .DefaultIfEmpty("nonpublic")
            .First();

    private static int GetVisibilityRank(string visibility)
        => visibility switch
        {
            "public" => 3,
            "protected internal" => 2,
            "protected" => 1,
            _ => 0
        };

    private static string FormatParameter(MetadataReader reader, DisplayNameProvider provider, string parameterType, Parameter? parameter)
    {
        var attributes = parameter is { } parameterValue0
            ? FormatCustomAttributes(reader, provider, parameterValue0.GetCustomAttributes())
            : string.Empty;
        var flags = new List<string>();
        var typePart = parameterType;

        if (parameterType.StartsWith("ref ", StringComparison.Ordinal))
        {
            var suffix = parameterType[4..];
            if (parameter is { } parameterValue && (parameterValue.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out)
            {
                typePart = $"out {suffix}";
            }
            else if (parameter is { } parameterValue2 && (parameterValue2.Attributes & ParameterAttributes.In) == ParameterAttributes.In)
            {
                typePart = $"in {suffix}";
            }
        }

        if (parameter is { } parameterValue3)
        {
            if ((parameterValue3.Attributes & ParameterAttributes.Optional) == ParameterAttributes.Optional)
            {
                flags.Add("optional");
            }

            if ((parameterValue3.Attributes & ParameterAttributes.Lcid) == ParameterAttributes.Lcid)
            {
                flags.Add("lcid");
            }

            if ((parameterValue3.Attributes & ParameterAttributes.Retval) == ParameterAttributes.Retval)
            {
                flags.Add("retval");
            }
        }

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(attributes))
        {
            parts.Add(attributes.Trim());
        }

        parts.Add(typePart);

        if (parameter is { } parameterValue4 && !parameterValue4.Name.IsNil)
        {
            parts.Add(reader.GetString(parameterValue4.Name));
        }

        if (parameter is { } parameterValue5)
        {
            var defaultValue = parameterValue5.GetDefaultValue();
            if (!defaultValue.IsNil)
            {
                parts.Add($"= {FormatConstant(reader, defaultValue)}");
            }

            var marshallingDescriptor = parameterValue5.GetMarshallingDescriptor();
            if (!marshallingDescriptor.IsNil)
            {
                parts.Add($"[marshal: {FormatBlob(reader, marshallingDescriptor)}]");
            }
        }

        if (flags.Count > 0)
        {
            parts.Add($"[flags: {string.Join(", ", flags)}]");
        }

        return string.Join(" ", parts);
    }

    private static string FormatPublicKeyToken(MetadataReader reader, BlobHandle handle)
    {
        if (handle.IsNil)
        {
            return "null";
        }

        return Convert.ToHexString(reader.GetBlobBytes(handle)).ToLowerInvariant();
    }
}

internal sealed class DisplayNameProvider : ISignatureTypeProvider<string, object?>, ICustomAttributeTypeProvider<string>
{
    private readonly MetadataReader _reader;

    public DisplayNameProvider(MetadataReader reader)
    {
        _reader = reader;
    }

    public string GetArrayType(string elementType, ArrayShape shape)
        => $"{elementType}[{new string(',', Math.Max(shape.Rank - 1, 0))}]";

    public string GetByReferenceType(string elementType)
        => $"ref {elementType}";

    public string GetFunctionPointerType(MethodSignature<string> signature)
        => $"delegate*<{string.Join(", ", signature.ParameterTypes.Concat([signature.ReturnType]))}>";

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        => $"{genericType}<{string.Join(", ", typeArguments)}>";

    public string GetGenericMethodParameter(object? genericContext, int index)
        => $"!!{index}";

    public string GetGenericTypeParameter(object? genericContext, int index)
        => $"!{index}";

    public string GetModifiedType(string modifierType, string unmodifiedType, bool isRequired)
        => $"{(isRequired ? "modreq" : "modopt")}({modifierType}) {unmodifiedType}";

    public string GetPinnedType(string elementType)
        => $"pinned {elementType}";

    public string GetPointerType(string elementType)
        => $"{elementType}*";

    public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        => typeCode switch
        {
            PrimitiveTypeCode.Boolean => "bool",
            PrimitiveTypeCode.Byte => "byte",
            PrimitiveTypeCode.Char => "char",
            PrimitiveTypeCode.Double => "double",
            PrimitiveTypeCode.Int16 => "short",
            PrimitiveTypeCode.Int32 => "int",
            PrimitiveTypeCode.Int64 => "long",
            PrimitiveTypeCode.IntPtr => "nint",
            PrimitiveTypeCode.Object => "object",
            PrimitiveTypeCode.SByte => "sbyte",
            PrimitiveTypeCode.Single => "float",
            PrimitiveTypeCode.String => "string",
            PrimitiveTypeCode.TypedReference => "typedref",
            PrimitiveTypeCode.UInt16 => "ushort",
            PrimitiveTypeCode.UInt32 => "uint",
            PrimitiveTypeCode.UInt64 => "ulong",
            PrimitiveTypeCode.UIntPtr => "nuint",
            PrimitiveTypeCode.Void => "void",
            _ => typeCode.ToString()
        };

    public string GetSZArrayType(string elementType)
        => $"{elementType}[]";

    public string GetSystemType()
        => "System.Type";

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var definition = reader.GetTypeDefinition(handle);
        var namespaceName = reader.GetString(definition.Namespace);
        var typeName = reader.GetString(definition.Name);
        var declaringType = definition.GetDeclaringType();

        if (!declaringType.IsNil)
        {
            return $"{GetTypeFromDefinition(reader, declaringType, rawTypeKind)}+{typeName}";
        }

        return string.IsNullOrEmpty(namespaceName) ? typeName : $"{namespaceName}.{typeName}";
    }

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var reference = reader.GetTypeReference(handle);
        var namespaceName = reader.GetString(reference.Namespace);
        var typeName = reader.GetString(reference.Name);

        return string.IsNullOrEmpty(namespaceName) ? typeName : $"{namespaceName}.{typeName}";
    }

    public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
    {
        var specification = reader.GetTypeSpecification(handle);
        var decoder = new SignatureDecoder<string, object?>(this, reader, genericContext);
        var blobReader = reader.GetBlobReader(specification.Signature);
        return decoder.DecodeType(ref blobReader);
    }

    public string GetTypeFromSerializedName(string name)
        => name;

    public PrimitiveTypeCode GetUnderlyingEnumType(string type)
        => PrimitiveTypeCode.Int32;

    public bool IsSystemType(string type)
        => string.Equals(type, "System.Type", StringComparison.Ordinal);
}
