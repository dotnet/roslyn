// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PatternMatching;
using Roslyn.Utilities;

namespace IdeCoreBenchmarks;

/// <summary>
/// Benchmarks specifically for the fuzzy pre-filter improvements: the corrected length check
/// and the q-gram bigram count check. Demonstrates the scenario where many symbols share the
/// same length as the pattern, making the length-only check produce false positives that the
/// bigram check rejects.
/// </summary>
[MemoryDiagnoser]
public class NavigateToFuzzyPreFilterBenchmarks
{
    private NavigateToSearchIndex _sameLengthIndex = null!;
    private NavigateToSearchIndex _variedLengthIndex = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _sameLengthIndex = CreateSameLengthIndex();
        _variedLengthIndex = CreateVariedLengthIndex();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Same-length index: 1000 6-character symbols, all different from "XyzWvq"
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 1000 symbols each with length 6, using varied CamelCase names. The pattern "XyzWvq" also
    /// has length 6 — so the length check always passes. But the bigram check can discriminate:
    /// none of these symbols share bigrams "xy","yz","zw","wv","vq".
    /// </summary>
    private static NavigateToSearchIndex CreateSameLengthIndex()
    {
        var stringTable = new StringTable();
        var infos = new DeclaredSymbolInfo[1000];
        var sb = new StringBuilder();

        for (var i = 0; i < 1000; i++)
        {
            sb.Clear();
            var c1 = (char)('A' + (i % 26));
            var c2 = (char)('A' + ((i / 26) % 26));
            sb.Append(c1).Append("ab").Append(c2).Append("cd");
            infos[i] = MakeInfo(stringTable, sb.ToString(), "Test.Ns");
        }

        return NavigateToSearchIndex.TestAccessor.CreateIndex(infos.ToImmutableArray());
    }

    /// <summary>
    /// Same 1000 symbols but with lengths ranging from 4 to 12, so the length check itself
    /// provides meaningful filtering for some patterns.
    /// </summary>
    private static NavigateToSearchIndex CreateVariedLengthIndex()
    {
        var stringTable = new StringTable();
        var infos = new DeclaredSymbolInfo[1000];
        var sb = new StringBuilder();

        for (var i = 0; i < 1000; i++)
        {
            sb.Clear();
            var len = 4 + (i % 9);
            sb.Append((char)('A' + (i % 26)));
            for (var j = 1; j < len; j++)
                sb.Append((char)('a' + ((i * 3 + j * 7) % 26)));
            infos[i] = MakeInfo(stringTable, sb.ToString(), "Test.Ns");
        }

        return NavigateToSearchIndex.TestAccessor.CreateIndex(infos.ToImmutableArray());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Length check: passes for same-length index, provides filtering for varied
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// "XyzWvq" (length 6) against 1000 length-6 symbols → length check always passes.
    /// This is the false-positive scenario that motivated the bigram check.
    /// </summary>
    [Benchmark(Description = "SameLen: LengthCheck pass (false positive)")]
    public bool SameLength_LengthCheck_Pass()
        => _sameLengthIndex.GetTestAccessor().LengthCheckPasses("XyzWvq");

    /// <summary>
    /// "XyzWvq" (length 6) against varied-length symbols → length check may or may not pass
    /// depending on whether any symbol has length 4–8.
    /// </summary>
    [Benchmark(Description = "VariedLen: LengthCheck pass")]
    public bool VariedLength_LengthCheck_Pass()
        => _variedLengthIndex.GetTestAccessor().LengthCheckPasses("XyzWvq");

    // ═══════════════════════════════════════════════════════════════════════════
    //  Bigram count check: provides strong filtering even for same-length symbols
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// "XyzWvq" (length 6) against 1000 length-6 symbols → bigram check rejects because
    /// bigrams "xy","yz","zw","wv","vq" are not stored in any of the "AabBcd" style names.
    /// This is the key improvement: where length check produces a false positive, the bigram
    /// check correctly rejects.
    /// </summary>
    [Benchmark(Description = "SameLen: BigramCheck reject (true negative!)")]
    public bool SameLength_BigramCheck_Reject()
        => _sameLengthIndex.GetTestAccessor().BigramCountCheckPasses("XyzWvq");

    /// <summary>
    /// "AabBcd" (length 6) against same-length index → bigram check passes because these
    /// exact bigrams are stored.
    /// </summary>
    [Benchmark(Description = "SameLen: BigramCheck pass (true positive)")]
    public bool SameLength_BigramCheck_Pass()
        => _sameLengthIndex.GetTestAccessor().BigramCountCheckPasses("AabBcd");

    // ═══════════════════════════════════════════════════════════════════════════
    //  Combined: CouldContainNavigateToMatch with fuzzy result
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Pattern "XyzWvq": hump check fails (X not stored), not all-lowercase so trigram fails,
    /// length check passes but bigram check fails → result is false. The bigram check saved
    /// us from a futile fuzzy matching scan.
    /// </summary>
    [Benchmark(Description = "SameLen: Combined reject (bigram saves)")]
    public bool SameLength_Combined_Reject()
        => _sameLengthIndex.CouldContainNavigateToMatch("XyzWvq", null) != PatternMatcherKind.None;

    /// <summary>
    /// Pattern "AabBxx": hump check passes (A,B stored), length check passes, bigram check
    /// passes → result is true with fuzzy enabled.
    /// </summary>
    [Benchmark(Description = "SameLen: Combined pass")]
    public bool SameLength_Combined_Pass()
        => _sameLengthIndex.CouldContainNavigateToMatch("AabBxx", null) != PatternMatcherKind.None;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Longer patterns: bigram filtering gets stronger
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Length 8 pattern (k=2, min_shared=3): needs ≥ 3 of 7 bigrams. More selective.
    /// </summary>
    [Benchmark(Description = "SameLen: BigramCheck reject len=8")]
    public bool SameLength_BigramCheck_Reject_Len8()
        => _sameLengthIndex.GetTestAccessor().BigramCountCheckPasses("XyzWvqRs");

    /// <summary>
    /// Length 10 pattern (k=2, min_shared=5): needs ≥ 5 of 9 bigrams. Very selective.
    /// </summary>
    [Benchmark(Description = "VariedLen: BigramCheck reject len=10")]
    public bool VariedLength_BigramCheck_Reject_Len10()
        => _variedLengthIndex.GetTestAccessor().BigramCountCheckPasses("XyzWvqRsTu");

    // ═══════════════════════════════════════════════════════════════════════════

    private static DeclaredSymbolInfo MakeInfo(StringTable stringTable, string name, string container)
        => DeclaredSymbolInfo.Create(
            stringTable, name, nameSuffix: null, containerDisplayName: null,
            fullyQualifiedContainerName: container,
            isPartial: false, hasAttributes: false,
            DeclaredSymbolInfoKind.Method, Accessibility.Public,
            default, ImmutableArray<string>.Empty);
}
