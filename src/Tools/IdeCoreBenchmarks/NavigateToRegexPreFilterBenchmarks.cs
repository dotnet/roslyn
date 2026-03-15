// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.PatternMatching;
using Roslyn.Utilities;

namespace IdeCoreBenchmarks;

/// <summary>
/// Benchmarks for regex-based NavigateTo pre-filtering: query compilation from a regex pattern,
/// pre-filter evaluation against a document's bigram index, and full regex matching.
/// </summary>
[MemoryDiagnoser]
public class NavigateToRegexPreFilterBenchmarks
{
    private NavigateToSearchIndex _index = null!;
    private RegexQuery _simpleQuery = null!;
    private RegexQuery _alternationQuery = null!;
    private RegexQuery _complexQuery = null!;
    private RegexQuery _noMatchQuery = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _index = CreateIndex();
        _simpleQuery = RegexQueryCompiler.Compile("ReadLine")!;
        _alternationQuery = RegexQueryCompiler.Compile("(Read|Write)Line")!;
        _complexQuery = RegexQueryCompiler.Compile("(Get|Set)(Value|Item)s?")!;
        _noMatchQuery = RegexQueryCompiler.Compile("Xyz.*Wvq")!;
    }

    private static NavigateToSearchIndex CreateIndex()
    {
        var stringTable = new StringTable();
        var infos = new DeclaredSymbolInfo[1000];
        var names = new[]
        {
            "ReadLine", "WriteLine", "ReadKey", "WriteBuffer", "StreamReader",
            "StreamWriter", "GetValue", "SetValue", "GetItem", "SetItem",
            "ToString", "GetHashCode", "Equals", "CompareTo", "Dispose",
            "Initialize", "Configure", "Execute", "Validate", "Transform",
        };

        for (var i = 0; i < 1000; i++)
        {
            var name = names[i % names.Length] + (i / names.Length);
            infos[i] = DeclaredSymbolInfo.Create(
                stringTable, name, nameSuffix: null, containerDisplayName: null,
                fullyQualifiedContainerName: "", isPartial: false, hasAttributes: false,
                DeclaredSymbolInfoKind.Method, Accessibility.Public,
                default, ImmutableArray<string>.Empty);
        }

        return NavigateToSearchIndex.TestAccessor.CreateIndex(infos.ToImmutableArray());
    }

    [Benchmark(Description = "Compile: simple literal")]
    public object? CompileSimple() => RegexQueryCompiler.Compile("ReadLine");

    [Benchmark(Description = "Compile: alternation")]
    public object? CompileAlternation() => RegexQueryCompiler.Compile("(Read|Write)Line");

    [Benchmark(Description = "Compile: complex")]
    public object? CompileComplex() => RegexQueryCompiler.Compile("(Get|Set)(Value|Item)s?");

    [Benchmark(Description = "PreFilter: simple literal (match)")]
    public bool PreFilterSimple() => _index.GetTestAccessor().RegexQueryCheckPasses(_simpleQuery);

    [Benchmark(Description = "PreFilter: alternation (match)")]
    public bool PreFilterAlternation() => _index.GetTestAccessor().RegexQueryCheckPasses(_alternationQuery);

    [Benchmark(Description = "PreFilter: complex (match)")]
    public bool PreFilterComplex() => _index.GetTestAccessor().RegexQueryCheckPasses(_complexQuery);

    [Benchmark(Description = "PreFilter: no match")]
    public bool PreFilterNoMatch() => _index.GetTestAccessor().RegexQueryCheckPasses(_noMatchQuery);

    [Benchmark(Description = "IsRegexPattern: plain text")]
    public bool DetectPlainText() => RegexPatternDetector.IsRegexPattern("ReadLine");

    [Benchmark(Description = "IsRegexPattern: regex")]
    public bool DetectRegex() => RegexPatternDetector.IsRegexPattern("(Read|Write)Line");
}
