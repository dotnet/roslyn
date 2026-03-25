// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Analyzes bigram frequencies across all C# identifiers in a codebase and generates a weight
// table for the sparse n-gram algorithm. Rare bigrams get high weights so they become n-gram
// boundaries, producing longer (more selective) n-grams through common character sequences.
//
// Usage: BigramFrequencyAnalyzer <root-directory>
//
// See: https://cursor.com/blog/fast-regex-search ("Sparse N-grams: Smarter Trigram Selection")

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: BigramFrequencyAnalyzer <root-directory>");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Scans all .cs files under <root-directory>, extracts identifier tokens,");
    Console.Error.WriteLine("computes lowercased bigram frequencies, and emits a weight table suitable");
    Console.Error.WriteLine("for use in SparseNgramGenerator.HashBigram.");
    return 1;
}

var rootDir = args[0];
if (!Directory.Exists(rootDir))
{
    Console.Error.WriteLine($"Directory not found: {rootDir}");
    return 1;
}

// Alphabet: a-z (26) + 0-9 (10) + underscore (1) = 37 characters.
// This covers all characters that appear in C#/VB identifiers after lowercasing.
const int AlphabetSize = 37;
const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789_";

static int CharIndex(char c)
{
    if (c is >= 'a' and <= 'z') return c - 'a';
    if (c is >= '0' and <= '9') return 26 + (c - '0');
    if (c == '_') return 36;
    return -1;
}

var bigramCounts = new long[AlphabetSize * AlphabetSize];
var totalBigrams = 0L;
var totalIdentifiers = 0L;
var totalFiles = 0;

// Collect unique identifiers to avoid over-weighting identifiers that appear many times.
// A method called "ToString" in 500 files shouldn't count 500x more than one called "FrobnicateWidget".
var uniqueIdentifiers = new HashSet<string>(StringComparer.Ordinal);

Console.Error.WriteLine($"Scanning .cs files under {rootDir}...");

foreach (var file in Directory.EnumerateFiles(rootDir, "*.cs", SearchOption.AllDirectories))
{
    // Skip generated/obj/bin directories
    if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
        file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
    {
        continue;
    }

    var text = File.ReadAllText(file);
    var tree = CSharpSyntaxTree.ParseText(text);
    var root = tree.GetRoot();

    foreach (var token in root.DescendantTokens())
    {
        if (token.IsKind(SyntaxKind.IdentifierToken))
            uniqueIdentifiers.Add(token.ValueText);
    }

    totalFiles++;
    if (totalFiles % 500 == 0)
        Console.Error.Write($"\r  Parsed {totalFiles} files, {uniqueIdentifiers.Count} unique identifiers...");
}

Console.Error.WriteLine($"\r  Parsed {totalFiles} files, {uniqueIdentifiers.Count} unique identifiers.     ");
Console.Error.WriteLine("Computing bigram frequencies...");

foreach (var identifier in uniqueIdentifiers)
{
    var lowered = identifier.ToLowerInvariant();
    totalIdentifiers++;

    for (var i = 0; i < lowered.Length - 1; i++)
    {
        var idx1 = CharIndex(lowered[i]);
        var idx2 = CharIndex(lowered[i + 1]);
        if (idx1 >= 0 && idx2 >= 0)
        {
            bigramCounts[idx1 * AlphabetSize + idx2]++;
            totalBigrams++;
        }
    }
}

Console.Error.WriteLine($"Total unique identifiers: {totalIdentifiers}");
Console.Error.WriteLine($"Total bigrams counted: {totalBigrams}");

// Count how many bigrams were observed at all.
var observedBigrams = 0;
for (var i = 0; i < bigramCounts.Length; i++)
{
    if (bigramCounts[i] > 0)
        observedBigrams++;
}

Console.Error.WriteLine($"Observed bigrams: {observedBigrams} / {AlphabetSize * AlphabetSize} ({100.0 * observedBigrams / (AlphabetSize * AlphabetSize):F1}%)");

// Compute weights: inverse frequency. The rarest bigrams get the highest weights.
// We use rank-based weighting: sort bigrams by frequency, assign weight = rank.
// This avoids issues with zero-frequency bigrams (they get the highest ranks)
// and produces a clean distribution that the monotonic stack works well with.
var ranked = Enumerable.Range(0, AlphabetSize * AlphabetSize)
    .OrderByDescending(i => bigramCounts[i])
    .ThenBy(i => i)
    .ToArray();

var weights = new uint[AlphabetSize * AlphabetSize];
for (var rank = 0; rank < ranked.Length; rank++)
    weights[ranked[rank]] = (uint)rank;

// Print the top-20 most common and top-20 rarest observed bigrams for analysis.
Console.Error.WriteLine();
Console.Error.WriteLine("Top 20 most common bigrams (low weight = interior of n-grams):");
for (var i = 0; i < 20 && i < ranked.Length; i++)
{
    var idx = ranked[i];
    var c1 = Alphabet[idx / AlphabetSize];
    var c2 = Alphabet[idx % AlphabetSize];
    Console.Error.WriteLine($"  '{c1}{c2}' = {bigramCounts[idx],8} occurrences, weight = {weights[idx],4}");
}

Console.Error.WriteLine();
Console.Error.WriteLine("Top 20 rarest observed bigrams (high weight = n-gram boundaries):");
var rarestObserved = Enumerable.Range(0, AlphabetSize * AlphabetSize)
    .Where(i => bigramCounts[i] > 0)
    .OrderBy(i => bigramCounts[i])
    .ThenByDescending(i => i)
    .Take(20)
    .ToArray();

foreach (var idx in rarestObserved)
{
    var c1 = Alphabet[idx / AlphabetSize];
    var c2 = Alphabet[idx % AlphabetSize];
    Console.Error.WriteLine($"  '{c1}{c2}' = {bigramCounts[idx],8} occurrences, weight = {weights[idx],4}");
}

// Also show some sample identifiers and how n-gram boundaries would differ.
Console.Error.WriteLine();
Console.Error.WriteLine("Sample identifiers — bigram weights (higher = more likely boundary):");
var samples = new[] { "GetValue", "ToString", "ReadLine", "StringBuilder", "IAsyncEnumerable", "CancellationToken" };
foreach (var sample in samples)
{
    var lowered = sample.ToLowerInvariant();
    var sb = new StringBuilder();
    sb.Append($"  {sample,-22} →  ");
    for (var i = 0; i < lowered.Length - 1; i++)
    {
        var idx1 = CharIndex(lowered[i]);
        var idx2 = CharIndex(lowered[i + 1]);
        if (idx1 >= 0 && idx2 >= 0)
        {
            var w = weights[idx1 * AlphabetSize + idx2];
            sb.Append($"{lowered[i]}{lowered[i + 1]}:{w,4}  ");
        }
    }

    Console.Error.WriteLine(sb.ToString());
}

// Generate C# source for the weight table.
Console.Error.WriteLine();
Console.Error.WriteLine("Generating C# weight table...");

var output = new StringBuilder();
output.AppendLine("// Licensed to the .NET Foundation under one or more agreements.");
output.AppendLine("// The .NET Foundation licenses this file to you under the MIT license.");
output.AppendLine("// See the LICENSE file in the project root for more information.");
output.AppendLine();
output.AppendLine("// Auto-generated by BigramFrequencyAnalyzer.");
output.AppendLine("// Source corpus: Roslyn codebase C# identifiers.");
output.AppendLine($"// {totalFiles} files, {totalIdentifiers} unique identifiers, {totalBigrams} bigrams.");
output.AppendLine("//");
output.AppendLine("// Rare bigrams get high weights so they become n-gram boundaries in the");
output.AppendLine("// sparse n-gram algorithm. This produces longer n-grams through common");
output.AppendLine("// character sequences (like \"tion\" or \"ing\"), making them more selective");
output.AppendLine("// in the Bloom filter and reducing false positives at query time.");
output.AppendLine("//");
output.AppendLine("// See: https://cursor.com/blog/fast-regex-search");
output.AppendLine();
output.AppendLine("namespace Microsoft.CodeAnalysis.FindSymbols;");
output.AppendLine();
output.AppendLine("internal static partial class SparseNgramGenerator");
output.AppendLine("{");
output.AppendLine("    // Alphabet: a-z (0-25), 0-9 (26-35), underscore (36) = 37 chars.");
output.AppendLine("    // Table is indexed by (CharIndex(c1) * 37 + CharIndex(c2)).");
output.AppendLine("    // Range: 0 (most common bigram) to 1368 (rarest / unobserved).");
output.AppendLine($"    private const int FrequencyAlphabetSize = {AlphabetSize};");
output.AppendLine();
output.AppendLine("    private static int FrequencyCharIndex(char c)");
output.AppendLine("    {");
output.AppendLine("        if (c is >= 'a' and <= 'z') return c - 'a';");
output.AppendLine("        if (c is >= '0' and <= '9') return 26 + (c - '0');");
output.AppendLine("        if (c == '_') return 36;");
output.AppendLine("        return -1;");
output.AppendLine("    }");
output.AppendLine();
output.AppendLine("    /// <summary>");
output.AppendLine("    /// Frequency-based bigram weight for the sparse n-gram algorithm.");
output.AppendLine("    /// Returns a high weight for rare bigrams (they become n-gram boundaries)");
output.AppendLine("    /// and a low weight for common ones (they stay interior to long n-grams).");
output.AppendLine("    /// Falls back to the CRC-style hash for characters outside the identifier alphabet.");
output.AppendLine("    /// </summary>");
output.AppendLine("    private static uint FrequencyHashBigram(ReadOnlySpan<char> text, int pos)");
output.AppendLine("    {");
output.AppendLine("        var idx1 = FrequencyCharIndex(text[pos]);");
output.AppendLine("        var idx2 = FrequencyCharIndex(text[pos + 1]);");
output.AppendLine("        if (idx1 >= 0 && idx2 >= 0)");
output.AppendLine("            return s_frequencyWeights[idx1 * FrequencyAlphabetSize + idx2];");
output.AppendLine();
output.AppendLine("        // Fallback for non-identifier characters.");
output.AppendLine("        var a = text[pos] * Mul1 + text[pos + 1] * Mul2;");
output.AppendLine("        return (uint)(a + (~a >> 47));");
output.AppendLine("    }");
output.AppendLine();
output.AppendLine("    // Rank-based weights: 0 = most common bigram, 1368 = rarest/unobserved.");
output.AppendLine($"    private static readonly uint[] s_frequencyWeights =");
output.AppendLine("    [");

// Write the table in rows of AlphabetSize (one row per first character).
for (var row = 0; row < AlphabetSize; row++)
{
    var c1 = Alphabet[row];
    output.Append($"        /* {c1}_ */ ");
    for (var col = 0; col < AlphabetSize; col++)
    {
        var idx = row * AlphabetSize + col;
        output.Append($"{weights[idx],4}");
        if (idx < AlphabetSize * AlphabetSize - 1)
            output.Append(", ");
    }

    output.AppendLine();
}

output.AppendLine("    ];");
output.AppendLine("}");

Console.Write(output.ToString());

Console.Error.WriteLine("Done. Table written to stdout.");
return 0;
