// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PatternMatching;
using Roslyn.Utilities;

namespace IdeCoreBenchmarks;

/// <summary>
/// Measures NavigateTo pre-filter performance across different index sizes and matching algorithms.
/// For each index, benchmarks test both hit (filter says "probably matches") and miss (filter rejects).
/// The full-scan baseline shows the cost we avoid by rejecting early.
/// </summary>
[MemoryDiagnoser]
public class NavigateToPreFilterBenchmarks
{
    private NavigateToSearchIndex _realistic = null!;
    private NavigateToSearchIndex _stressAll = null!;
    private NavigateToSearchIndex _stressHump = null!;
    private NavigateToSearchIndex _stressPrefix = null!;
    private NavigateToSearchIndex _stressTrigram = null!;
    private NavigateToSearchIndex _stressContainer = null!;
    private string[] _realisticSymbolNames = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _realistic = CreateRealisticIndex(out _realisticSymbolNames);
        _stressAll = CreateStressAllIndex();
        _stressHump = CreateStressHumpSetIndex();
        _stressPrefix = CreateStressHumpPrefixIndex();
        _stressTrigram = CreateStressTrigramIndex();
        _stressContainer = CreateStressContainerIndex();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Baseline: full PatternMatcher scan (the cost we're avoiding)
    // ═══════════════════════════════════════════════════════════════════════════

    [Benchmark(Baseline = true, Description = "FullScan (10k symbols, no pre-filter)")]
    public int FullScan()
    {
        using var matcher = PatternMatcher.CreatePatternMatcher("FoNa", includeMatchedSpans: false, allowFuzzyMatching: true);
        var count = 0;
        foreach (var name in _realisticSymbolNames)
        {
            using var matches = TemporaryArray<PatternMatch>.Empty;
            if (matcher.AddMatches(name, ref matches.AsRef()))
                count++;
        }

        return count;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Realistic index: 10k symbols from hard-coded CamelCase names + 5 containers
    // ═══════════════════════════════════════════════════════════════════════════

    private static NavigateToSearchIndex CreateRealisticIndex(out string[] symbolNames)
    {
        var stringTable = new StringTable();
        const int count = 10_000;
        var infos = new DeclaredSymbolInfo[count];
        symbolNames = new string[count];

        // Names like "GetApplicationContext0", "SetDatabaseConnection1", etc.
        // Humps: G,A,C / S,D,C / C,S,P / P,X,D / V,U,I / T,D,R / C,H,C / I,C / D,R,M / S,O,G
        // Containers: System.Collections.Generic, Microsoft.CodeAnalysis.CSharp, etc.
        var namePrefixes = new[]
        {
            "GetApplicationContext",
            "SetDatabaseConnection",
            "CreateServiceProvider",
            "ParseXmlDocument",
            "ValidateUserInput",
            "TransformDataRecord",
            "CalculateHashCode",
            "InitializeComponent",
            "DisposeResourceManager",
            "SerializeObjectGraph",
        };

        var containers = new[]
        {
            "System.Collections.Generic",
            "Microsoft.CodeAnalysis.CSharp",
            "Azure.Storage.Blobs",
            "Newtonsoft.Json.Linq",
            "System.Threading.Tasks",
        };

        for (var i = 0; i < count; i++)
        {
            var name = $"{namePrefixes[i % namePrefixes.Length]}{i}";
            symbolNames[i] = name;
            infos[i] = MakeInfo(stringTable, name, containers[i % containers.Length]);
        }

        return NavigateToSearchIndex.TestAccessor.CreateIndex(infos.ToImmutableArray());
    }

    // "FoNa" → humps F,N → bigram "FN" not in hump set (no name starts with F+N humps) → miss.
    [Benchmark(Description = "Realistic: CamelCase miss")]
    public bool Realistic_CamelCase_Miss()
        => _realistic.CouldContainNavigateToMatch("FoNa", null, out _);

    // "GetApp" → humps G,A → bigram "GA" is stored from "GetApplicationContext" → hit.
    [Benchmark(Description = "Realistic: CamelCase hit")]
    public bool Realistic_CamelCase_Hit()
        => _realistic.CouldContainNavigateToMatch("GetApp", null, out _);

    // "getapp" → all-lowercase → DP splits "get"+"app", both are hump prefixes of
    // "Get" and "Application" → hit.
    [Benchmark(Description = "Realistic: lowercase hit (hump-prefix DP)")]
    public bool Realistic_Lowercase_Hit()
        => _realistic.CouldContainNavigateToMatch("getapp", null, out _);

    // "foozy" → all-lowercase → DP can't split into any stored hump prefixes (no name starts
    // with "foo...") → hump miss. Trigrams "foo","ooz","ozy" not stored either → miss.
    [Benchmark(Description = "Realistic: lowercase miss (hump-prefix DP)")]
    public bool Realistic_Lowercase_Miss()
        => _realistic.CouldContainNavigateToMatch("foozy", null, out _);

    // "context" → all-lowercase, len 7 → trigrams "con","ont","nte","tex","ext" all stored
    // from "GetApplicationContext" → hit.
    [Benchmark(Description = "Realistic: lowercase trigram hit")]
    public bool Realistic_Lowercase_Trigram_Hit()
        => _realistic.CouldContainNavigateToMatch("context", null, out _);

    // "Context" → mixed-case → MixedCaseHumpCheckPasses → single hump 'C' in hump set → hit.
    [Benchmark(Description = "Realistic: CamelCase trigram hit")]
    public bool Realistic_CamelCase_Trigram_Hit()
        => _realistic.CouldContainNavigateToMatch("Context", null, out _);

    // "zqjxw" → all-lowercase, len 5 → trigrams "zqj","qjx","jxw" none stored → miss.
    [Benchmark(Description = "Realistic: lowercase trigram miss")]
    public bool Realistic_Lowercase_Trigram_Miss()
        => _realistic.CouldContainNavigateToMatch("zqjxw", null, out _);

    // "Zqjxw" → mixed-case → MixedCaseHumpCheckPasses → single hump 'Z' not in hump set → miss.
    [Benchmark(Description = "Realistic: CamelCase trigram miss")]
    public bool Realistic_CamelCase_Trigram_Miss()
        => _realistic.CouldContainNavigateToMatch("Zqjxw", null, out _);

    // "GetApp" hits name check. "System.Collections" → humps S,C match container chars
    // from "System.Collections.Generic" → hit.
    [Benchmark(Description = "Realistic: container hit")]
    public bool Realistic_Container_Hit()
        => _realistic.CouldContainNavigateToMatch("GetApp", "System.Collections", out _);

    // "GetApp" hits name check. "Zebra.Unknown" → hump Z not in any container → miss.
    [Benchmark(Description = "Realistic: container miss")]
    public bool Realistic_Container_Miss()
        => _realistic.CouldContainNavigateToMatch("GetApp", "Zebra.Unknown", out _);

    // "@getapp" → strip leading '@' → "getapp" → all-lowercase → hump-prefix DP hit.
    [Benchmark(Description = "Realistic: lowercase verbatim hit")]
    public bool Realistic_Lowercase_Verbatim_Hit()
        => _realistic.CouldContainNavigateToMatch("@getapp", null, out _);

    // "@GetApp" → strip leading '@' → "GetApp" → CamelCase → bigram "GA" hit.
    [Benchmark(Description = "Realistic: CamelCase verbatim hit")]
    public bool Realistic_CamelCase_Verbatim_Hit()
        => _realistic.CouldContainNavigateToMatch("@GetApp", null, out _);

    // "get context" → split at space → both all-lowercase words checked via hump-prefix DP.
    [Benchmark(Description = "Realistic: lowercase multi-word hit")]
    public bool Realistic_Lowercase_MultiWord_Hit()
        => _realistic.CouldContainNavigateToMatch("get context", null, out _);

    // "Get Context" → split at space → both CamelCase words checked via hump set.
    [Benchmark(Description = "Realistic: CamelCase multi-word hit")]
    public bool Realistic_CamelCase_MultiWord_Hit()
        => _realistic.CouldContainNavigateToMatch("Get Context", null, out _);

    // ═══════════════════════════════════════════════════════════════════════════
    //  Stress-all index: every structure saturated simultaneously
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Saturates every structure at once: all 676 CamelCase bigrams, 500 long-hump names for prefix
    /// bloom, 500 long-word names for trigram bloom, and containers spanning all 26 uppercase initials.
    /// </summary>
    private static NavigateToSearchIndex CreateStressAllIndex()
    {
        var stringTable = new StringTable();
        var infos = new List<DeclaredSymbolInfo>();
        var sb = new StringBuilder();

        // 676 two-hump names covering every (A..Z, A..Z) bigram pair.
        for (var c1 = 'A'; c1 <= 'Z'; c1++)
        {
            for (var c2 = 'A'; c2 <= 'Z'; c2++)
            {
                sb.Clear().Append(c1).Append("aa").Append(c2).Append("bb");
                infos.Add(MakeInfo(stringTable, sb.ToString(), "All.Stress"));
            }
        }

        // 500 single-hump names with long unique lowercase tails → many hump prefixes.
        for (var i = 0; i < 500; i++)
        {
            sb.Clear().Append((char)('A' + (i % 26)));
            for (var j = 0; j < 20; j++)
                sb.Append((char)('a' + ((i * 7 + j * 13) % 26)));
            infos.Add(MakeInfo(stringTable, sb.ToString(), "All.Stress"));
        }

        // 500 single-word names with long varied sequences → many trigrams.
        for (var i = 0; i < 500; i++)
        {
            sb.Clear().Append((char)('A' + (i % 26)));
            for (var j = 0; j < 25; j++)
                sb.Append((char)('a' + ((i * 11 + j * 17) % 26)));
            infos.Add(MakeInfo(stringTable, sb.ToString(), "All.Stress"));
        }

        // Containers spanning all 26 uppercase initials.
        for (var c = 'A'; c <= 'Z'; c++)
            infos.Add(MakeInfo(stringTable, "Method" + c, $"{c}lpha.{(char)('A' + (c - 'A' + 13) % 26)}eta"));

        return NavigateToSearchIndex.TestAccessor.CreateIndex(infos.ToImmutableArray());
    }

    // "AaBb" → humps A,B → bigram "AB" is stored (all 676 bigrams present) → hit.
    [Benchmark(Description = "StressAll: hit")]
    public bool StressAll_Hit()
        => _stressAll.CouldContainNavigateToMatch("AaBb", null, out _);

    // "FoNa" → humps F,N → bigram "FN" is stored (all bigrams present), so hump check hits.
    // However the name "FoNa" doesn't actually exist — this tests the cost of a false positive
    // from the hump set (it passes but the actual PatternMatcher would reject later).
    [Benchmark(Description = "StressAll: miss")]
    public bool StressAll_Miss()
        => _stressAll.CouldContainNavigateToMatch("FoNa", null, out _);

    // ═══════════════════════════════════════════════════════════════════════════
    //  Stress _humpSet: large frozen set of bigrams/chars
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 5,000 names with 3 humps each, systematically varying initials across A-Z.
    /// Names like "AabAcdAef", "BabAcdAef", ..., covering many unique bigram pairs.
    /// Word-parts kept short (3 chars each) to minimize trigram/prefix noise.
    /// </summary>
    private static NavigateToSearchIndex CreateStressHumpSetIndex()
    {
        var stringTable = new StringTable();
        var infos = new DeclaredSymbolInfo[5000];
        var sb = new StringBuilder();

        for (var i = 0; i < 5000; i++)
        {
            sb.Clear();
            var h1 = (char)('A' + (i % 26));
            var h2 = (char)('A' + ((i / 26) % 26));
            var h3 = (char)('A' + ((i / 676) % 26));
            sb.Append(h1).Append("ab");
            sb.Append(h2).Append("cd");
            sb.Append(h3).Append("ef");
            infos[i] = MakeInfo(stringTable, sb.ToString(), "X.Y");
        }

        return NavigateToSearchIndex.TestAccessor.CreateIndex(infos.ToImmutableArray());
    }

    // "AaCd" → humps A,C → bigram "AC" is stored (name "AabCcdXef" exists) → hit.
    [Benchmark(Description = "StressHumpSet: CamelCase hit")]
    public bool StressHumpSet_Hit()
        => _stressHump.CouldContainNavigateToMatch("AaCd", null, out _);

    // "FoNa" → humps F,N → bigram "FN" not stored (h2 varies with h1, not all pairs covered).
    // No names have both F and N as adjacent hump initials → miss.
    [Benchmark(Description = "StressHumpSet: CamelCase miss")]
    public bool StressHumpSet_Miss()
        => _stressHump.CouldContainNavigateToMatch("FoNa", null, out _);

    // ═══════════════════════════════════════════════════════════════════════════
    //  Stress _humpPrefixFilter: dense bloom of lowercased hump prefixes
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 2,000 names each with a single long hump (20 lowercase chars after the initial).
    /// Each name produces ~20 unique prefixes, flooding the bloom filter with ~40,000
    /// distinct prefix strings. Character sequences use (i*7 + j*13) % 26 for variety.
    /// </summary>
    private static NavigateToSearchIndex CreateStressHumpPrefixIndex()
    {
        var stringTable = new StringTable();
        var infos = new DeclaredSymbolInfo[2000];
        var sb = new StringBuilder();

        for (var i = 0; i < 2000; i++)
        {
            sb.Clear().Append((char)('A' + (i % 26)));
            for (var j = 0; j < 20; j++)
                sb.Append((char)('a' + ((i * 7 + j * 13) % 26)));
            infos[i] = MakeInfo(stringTable, sb.ToString(), "X.Y");
        }

        return NavigateToSearchIndex.TestAccessor.CreateIndex(infos.ToImmutableArray());
    }

    // "aahub" → all-lowercase → DP tries to split into hump prefixes. Names starting with 'A'
    // have varied prefixes; "a","aa","aah",... likely stored in the dense bloom → hit.
    [Benchmark(Description = "StressHumpPrefix: lowercase hit")]
    public bool StressHumpPrefix_Hit()
        => _stressPrefix.CouldContainNavigateToMatch("aahub", null, out _);

    // "zzzzqqqq" → all-lowercase → DP tries prefixes "z","zz","zzz","zzzz",...
    // No name has these as hump prefixes (no repeated-z sequences stored) → miss.
    [Benchmark(Description = "StressHumpPrefix: lowercase miss")]
    public bool StressHumpPrefix_Miss()
        => _stressPrefix.CouldContainNavigateToMatch("zzzzqqqq", null, out _);

    // ═══════════════════════════════════════════════════════════════════════════
    //  Stress _trigramFilter: dense bloom of 3-char sliding windows
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 2,000 names each with a long single word-part (25 lowercase chars after the initial).
    /// Each produces ~23 trigrams, flooding the bloom filter with ~46,000 distinct trigram
    /// strings. Character sequences use (i*11 + j*17) % 26 for variety.
    /// </summary>
    private static NavigateToSearchIndex CreateStressTrigramIndex()
    {
        var stringTable = new StringTable();
        var infos = new DeclaredSymbolInfo[2000];
        var sb = new StringBuilder();

        for (var i = 0; i < 2000; i++)
        {
            sb.Clear().Append((char)('A' + (i % 26)));
            for (var j = 0; j < 25; j++)
                sb.Append((char)('a' + ((i * 11 + j * 17) % 26)));
            infos[i] = MakeInfo(stringTable, sb.ToString(), "X.Y");
        }

        return NavigateToSearchIndex.TestAccessor.CreateIndex(infos.ToImmutableArray());
    }

    // "aahub" → all-lowercase, len 5 → trigrams "aah","ahu","hub" checked against dense bloom.
    // With 46k trigrams stored, common 3-char sequences are likely present → hit.
    [Benchmark(Description = "StressTrigram: lowercase hit")]
    public bool StressTrigram_Hit()
        => _stressTrigram.CouldContainNavigateToMatch("aahub", null, out _);

    // "zqxjw" → all-lowercase, len 5 → trigrams "zqx","qxj","xjw" are rare 3-char sequences
    // unlikely to appear even in a dense bloom → miss.
    [Benchmark(Description = "StressTrigram: lowercase miss")]
    public bool StressTrigram_Miss()
        => _stressTrigram.CouldContainNavigateToMatch("zqxjw", null, out _);

    // ═══════════════════════════════════════════════════════════════════════════
    //  Stress _containerCharSet: all 26 A-Z initials in frozen char set
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 26 symbols with containers whose hump initials span all of A-Z (e.g.,
    /// "Aoo.Far.Naz", "Boo.Gar.Oaz", ...), making the frozen char set as full as possible.
    /// All symbols share name "SimpleMethod".
    /// </summary>
    private static NavigateToSearchIndex CreateStressContainerIndex()
    {
        var stringTable = new StringTable();
        var infos = new DeclaredSymbolInfo[26];

        for (var i = 0; i < 26; i++)
        {
            var c = (char)('A' + i);
            infos[i] = MakeInfo(stringTable, "SimpleMethod",
                $"{c}oo.{(char)('A' + (i + 5) % 26)}ar.{(char)('A' + (i + 13) % 26)}az");
        }

        return NavigateToSearchIndex.TestAccessor.CreateIndex(infos.ToImmutableArray());
    }

    // "Simple" hits name check (hump 'S' matches "SimpleMethod"). "Foo.Bar" → humps F,B →
    // both present in full A-Z container char set → hit.
    [Benchmark(Description = "StressContainer: container hit")]
    public bool StressContainer_Hit()
        => _stressContainer.CouldContainNavigateToMatch("Simple", "Foo.Bar", out _);

    // "Simple" hits name check. "0Invalid.1Bad" → hump initials '0','1' are digits,
    // not in the A-Z container char set → miss.
    [Benchmark(Description = "StressContainer: container miss")]
    public bool StressContainer_Miss()
        => _stressContainer.CouldContainNavigateToMatch("Simple", "0Invalid.1Bad", out _);

    // ═══════════════════════════════════════════════════════════════════════════

    private static DeclaredSymbolInfo MakeInfo(StringTable stringTable, string name, string container)
        => DeclaredSymbolInfo.Create(
            stringTable, name, nameSuffix: null, containerDisplayName: null,
            fullyQualifiedContainerName: container,
            isPartial: false, hasAttributes: false,
            DeclaredSymbolInfoKind.Method, Accessibility.Public,
            default, ImmutableArray<string>.Empty);
}
