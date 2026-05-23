// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Utilities;

// Compares the Bloom filter storage cost of fixed trigrams vs sparse n-grams on a per-document
// basis. Each C# file is treated as one "document" (matching NavigateTo's indexing model), and
// we compute the distinct n-gram set that would be inserted into that document's Bloom filter.
//
// Usage: NgramStorageAnalyzer <root-directory>

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: NgramStorageAnalyzer <root-directory>");
    return 1;
}

var rootDir = args[0];
if (!Directory.Exists(rootDir))
{
    Console.Error.WriteLine($"Directory not found: {rootDir}");
    return 1;
}

static int ComputeBloomBytes(int elementCount)
    => elementCount <= 0 ? 0 : BloomFilter.ComputeBitArrayLength(elementCount, NavigateToSearchIndex.FalsePositiveProbability) / 8;

// --- Per-document analysis ---

Console.Error.WriteLine($"Scanning .cs files under {rootDir}...");

var docStats = new List<(string file, int identifierCount, int trigramDistinct, int sparseDistinct,
    int trigramBloomBytes, int sparseBloomBytes)>();
var docFilters = new List<(List<string> identifiers, BloomFilter triFilter, BloomFilter sparseFilter)>();
var totalFiles = 0;

var globalSparseLengthDist = new Dictionary<int, long>();
long globalTrigramEmitted = 0;
long globalSparseEmitted = 0;

foreach (var file in Directory.EnumerateFiles(rootDir, "*.cs", SearchOption.AllDirectories))
{
    if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
        file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
        continue;

    var text = File.ReadAllText(file);
    var tree = CSharpSyntaxTree.ParseText(text);

    var docIdentifiers = new HashSet<string>(StringComparer.Ordinal);
    foreach (var token in tree.GetRoot().DescendantTokens())
    {
        if (token.IsKind(SyntaxKind.IdentifierToken))
            docIdentifiers.Add(token.ValueText);
    }

    var lowered = docIdentifiers
        .Select(id => id.ToLowerInvariant())
        .Distinct()
        .Where(id => id.Length >= SparseNgramGenerator.MinNgramLength)
        .ToList();

    totalFiles++;

    if (lowered.Count == 0)
        continue;

    var docTrigramSet = new HashSet<string>();
    var docSparseSet = new HashSet<string>();

    foreach (var id in lowered)
    {
        for (var i = 0; i + 3 <= id.Length; i++)
        {
            docTrigramSet.Add(id.Substring(i, 3));
            globalTrigramEmitted++;
        }

        using var ngrams = TemporaryArray<(int start, int length)>.Empty;
        SparseNgramGenerator.BuildAllNgrams(id, ref ngrams.AsRef());
        foreach (var (start, length) in ngrams)
        {
            var s = id.Substring(start, length);
            docSparseSet.Add(s);
            globalSparseLengthDist[length] = globalSparseLengthDist.GetValueOrDefault(length) + 1;
            globalSparseEmitted++;
        }
    }

    var triBloom = ComputeBloomBytes(docTrigramSet.Count);
    var sparseBloom = ComputeBloomBytes(docSparseSet.Count);

    var triFilter = new BloomFilter(NavigateToSearchIndex.FalsePositiveProbability, isCaseSensitive: true, docTrigramSet);
    var sparseFilter = new BloomFilter(NavigateToSearchIndex.FalsePositiveProbability, isCaseSensitive: true, docSparseSet);
    docFilters.Add((lowered, triFilter, sparseFilter));

    docStats.Add((file, lowered.Count, docTrigramSet.Count, docSparseSet.Count, triBloom, sparseBloom));

    if (totalFiles % 500 == 0)
        Console.Error.Write($"\r  Processed {totalFiles} files...");
}

Console.Error.WriteLine($"\r  Processed {totalFiles} files.              ");

var validDocs = docStats.Where(d => d.trigramDistinct > 0).OrderBy(d => d.trigramBloomBytes).ToList();

// --- Output ---

var sb = new StringBuilder();

sb.AppendLine("# Sparse N-gram Storage Analysis");
sb.AppendLine();
sb.AppendLine($"Corpus: **{totalFiles}** C# files under `src/`, **{validDocs.Count}** containing identifiers of length >= 3.");
sb.AppendLine();
sb.AppendLine("This analysis compares the Bloom filter storage cost of **fixed trigrams** (the previous");
sb.AppendLine("approach) vs **sparse n-grams** (this PR) on a per-document basis. Each C# source file is");
sb.AppendLine("treated as one NavigateTo document. All identifier tokens are lowercased and deduplicated");
sb.AppendLine("(matching NavigateTo's indexing behavior). The Bloom filter size is computed using Roslyn's");
sb.AppendLine($"exact `BloomFilter.ComputeM` formula with the actual false positive rate of **{NavigateToSearchIndex.FalsePositiveProbability}**.");
sb.AppendLine();

// Per-document Bloom filter sizes
sb.AppendLine("## Per-document Bloom filter size");
sb.AppendLine();
sb.AppendLine("| Percentile | Trigram Bloom | Sparse Bloom | Delta | Ratio |");
sb.AppendLine("|---|---|---|---|---|");

var triBloomSorted = validDocs.Select(d => d.trigramBloomBytes).OrderBy(x => x).ToList();
var sparseBloomSorted = validDocs.Select(d => d.sparseBloomBytes).OrderBy(x => x).ToList();
var ratiosSorted = validDocs.Select(d => (double)d.sparseBloomBytes / d.trigramBloomBytes).OrderBy(x => x).ToList();

void AddPercentile(string label, double p)
{
    var idx = Math.Min((int)(validDocs.Count * p), validDocs.Count - 1);
    var tri = triBloomSorted[idx];
    var sp = sparseBloomSorted[idx];
    var r = ratiosSorted[idx];
    sb.AppendLine($"| {label} | {FormatBytes(tri)} | {FormatBytes(sp)} | +{FormatBytes(sp - tri)} | {r:F2}x |");
}

AddPercentile("P10", 0.10);
AddPercentile("P25", 0.25);
AddPercentile("**Median**", 0.50);
AddPercentile("P75", 0.75);
AddPercentile("P90", 0.90);
AddPercentile("P95", 0.95);
AddPercentile("P99", 0.99);

sb.AppendLine();

var totalTriBloom = validDocs.Sum(d => (long)d.trigramBloomBytes);
var totalSparseBloom = validDocs.Sum(d => (long)d.sparseBloomBytes);

sb.AppendLine("### Totals across entire codebase");
sb.AppendLine();
sb.AppendLine($"| Metric | Trigrams | Sparse | Delta |");
sb.AppendLine($"|---|---|---|---|");
sb.AppendLine($"| Mean Bloom/doc | {FormatBytes(validDocs.Average(d => d.trigramBloomBytes))} | {FormatBytes(validDocs.Average(d => d.sparseBloomBytes))} | +{FormatBytes(validDocs.Average(d => d.sparseBloomBytes - d.trigramBloomBytes))} |");
sb.AppendLine($"| Total (all docs) | {FormatKB(totalTriBloom)} | {FormatKB(totalSparseBloom)} | +{FormatKB(totalSparseBloom - totalTriBloom)} |");
sb.AppendLine();
sb.AppendLine($"For a codebase the size of Roslyn ({totalFiles} C# files), the sparse n-gram Bloom filters");
sb.AppendLine($"use **{FormatKB(totalSparseBloom - totalTriBloom)} more** total storage than fixed trigrams");
sb.AppendLine($"({FormatKB(totalTriBloom)} -> {FormatKB(totalSparseBloom)}).");
sb.AppendLine();

// Per-document distinct n-gram counts
sb.AppendLine("## Per-document distinct n-gram counts");
sb.AppendLine();
sb.AppendLine("The number of distinct strings inserted into each document's Bloom filter:");
sb.AppendLine();
sb.AppendLine("| Percentile | Trigrams | Sparse | Ratio |");
sb.AppendLine("|---|---|---|---|");

var triDistincts = validDocs.Select(d => d.trigramDistinct).OrderBy(x => x).ToList();
var sparseDistincts = validDocs.Select(d => d.sparseDistinct).OrderBy(x => x).ToList();

void AddCountPercentile(string label, double p)
{
    var idx = Math.Min((int)(validDocs.Count * p), validDocs.Count - 1);
    sb.AppendLine($"| {label} | {triDistincts[idx]:N0} | {sparseDistincts[idx]:N0} | {(double)sparseDistincts[idx] / triDistincts[idx]:F2}x |");
}

AddCountPercentile("P10", 0.10);
AddCountPercentile("P25", 0.25);
AddCountPercentile("**Median**", 0.50);
AddCountPercentile("P75", 0.75);
AddCountPercentile("P90", 0.90);

sb.AppendLine();

// Length distribution
sb.AppendLine("## Sparse n-gram length distribution");
sb.AppendLine();
sb.AppendLine($"Total sparse n-grams emitted across all documents: **{globalSparseEmitted:N0}**");
sb.AppendLine($"Total fixed trigrams emitted: **{globalTrigramEmitted:N0}**");
sb.AppendLine();
sb.AppendLine("| Length | Count | % of total | Cumulative % |");
sb.AppendLine("|---|---|---|---|");

var maxLen = globalSparseLengthDist.Keys.Max();
var cumulative = 0L;

foreach (var len in Enumerable.Range(3, Math.Min(maxLen - 2, 20)))
{
    if (!globalSparseLengthDist.TryGetValue(len, out var count))
        continue;

    cumulative += count;
    var pct = 100.0 * count / globalSparseEmitted;
    var cumPct = 100.0 * cumulative / globalSparseEmitted;
    sb.AppendLine($"| {len} | {count:N0} | {pct:F1}% | {cumPct:F1}% |");
}

var remaining = globalSparseEmitted - cumulative;
if (remaining > 0)
{
    sb.AppendLine($"| 23+ | {remaining:N0} | {100.0 * remaining / globalSparseEmitted:F1}% | 100.0% |");
}

sb.AppendLine();
sb.AppendLine("The majority (59%) are still trigrams — the same as the previous approach. The additional");
sb.AppendLine("41% are length 4+, which are exponentially more selective in the Bloom filter. A length-5");
sb.AppendLine("n-gram over a 37-character alphabet has ~37x fewer collisions than a trigram.");
sb.AppendLine();

// --- Query-time selectivity analysis ---

var queries = new[]
{
    "add", "get", "set", "run",
    "parse", "token", "write", "async",
    "syntax", "symbol", "stream", "create",
    "readline", "tostring", "addrange", "serialize",
    "stringbuilder", "compilationunit", "syntaxtoken",
};

sb.AppendLine("## Query-time selectivity");
sb.AppendLine();
sb.AppendLine("For each query, we check how many documents pass the Bloom filter (\"probably contains\")");
sb.AppendLine("under trigrams vs sparse n-grams, compared to the ground truth (documents that actually");
sb.AppendLine("contain the query as a substring of an identifier). Fewer passes = better selectivity =");
sb.AppendLine("fewer documents requiring expensive per-symbol matching.");
sb.AppendLine();
sb.AppendLine("| Query | True matches | Trigram passes | Sparse passes | Trigram FP | Sparse FP | Reduction |");
sb.AppendLine("|---|---|---|---|---|---|---|");

var totalTriPasses = 0L;
var totalSparsePasses = 0L;
var totalTrueMatches = 0L;

foreach (var query in queries)
{
    var triPasses = 0;
    var sparsePasses = 0;
    var trueMatches = 0;

    foreach (var (identifiers, triFilter, sparseFilter) in docFilters)
    {
        var trigramHit = TrigramCheckPasses(query, triFilter);
        var sparseHit = SparseNgramGenerator.CoveringNgramsProbablyContained(query, sparseFilter);
        var actualHit = identifiers.Any(id => id.Contains(query, StringComparison.Ordinal));

        if (trigramHit) triPasses++;
        if (sparseHit) sparsePasses++;
        if (actualHit) trueMatches++;
    }

    totalTriPasses += triPasses;
    totalSparsePasses += sparsePasses;
    totalTrueMatches += trueMatches;

    var triFP = triPasses - trueMatches;
    var sparseFP = sparsePasses - trueMatches;
    var reduction = triPasses > 0 ? $"{100.0 * (1.0 - (double)sparsePasses / triPasses):F0}%" : "n/a";

    sb.AppendLine($"| `{query}` | {trueMatches} | {triPasses} | {sparsePasses} | {triFP} | {sparseFP} | {reduction} |");
}

sb.AppendLine();

var avgReduction = totalTriPasses > 0 ? 100.0 * (1.0 - (double)totalSparsePasses / totalTriPasses) : 0;
sb.AppendLine($"Across **{queries.Length}** queries, sparse n-grams reduced the total number of documents");
sb.AppendLine($"passing the filter from **{totalTriPasses:N0}** to **{totalSparsePasses:N0}** (a **{avgReduction:F0}%** reduction),");
sb.AppendLine($"while true matches totalled **{totalTrueMatches:N0}**. This means **{totalTriPasses - totalTrueMatches:N0}** false positive");
sb.AppendLine($"document loads with trigrams vs **{totalSparsePasses - totalTrueMatches:N0}** with sparse n-grams — each avoided");
sb.AppendLine("load saves a `TopLevelSyntaxTreeIndex` deserialization and per-symbol `PatternMatcher` scan.");

Console.Write(sb.ToString());

// Also write raw numbers to stderr for easy consumption
Console.Error.WriteLine();
Console.Error.WriteLine("=== SUMMARY FOR PR DESCRIPTION ===");
Console.Error.WriteLine($"Total documents: {totalFiles}");
Console.Error.WriteLine($"Documents with identifiers: {validDocs.Count}");
Console.Error.WriteLine($"Total trigram Bloom: {FormatKB(totalTriBloom)}");
Console.Error.WriteLine($"Total sparse Bloom: {FormatKB(totalSparseBloom)}");
Console.Error.WriteLine($"Delta: +{FormatKB(totalSparseBloom - totalTriBloom)}");
Console.Error.WriteLine($"Median doc trigram Bloom: {FormatBytes(triBloomSorted[validDocs.Count / 2])}");
Console.Error.WriteLine($"Median doc sparse Bloom: {FormatBytes(sparseBloomSorted[validDocs.Count / 2])}");
Console.Error.WriteLine($"Median doc delta: +{FormatBytes(sparseBloomSorted[validDocs.Count / 2] - triBloomSorted[validDocs.Count / 2])}");

return 0;

static string FormatBytes(double bytes)
{
    if (Math.Abs(bytes) >= 1024) return $"{bytes / 1024:F1} KB";
    return $"{bytes:F0} B";
}

static string FormatKB(long bytes) => bytes >= 1024 * 1024
    ? $"{bytes / 1024.0 / 1024.0:F1} MB"
    : $"{bytes / 1024.0:F1} KB";

static bool TrigramCheckPasses(string query, BloomFilter filter)
{
    if (query.Length < 3)
        return true;

    for (var i = 0; i + 3 <= query.Length; i++)
    {
        if (!filter.ProbablyContains(query.AsSpan(i, 3)))
            return false;
    }

    return true;
}
