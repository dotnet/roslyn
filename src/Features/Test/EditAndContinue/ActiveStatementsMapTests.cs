// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

using static ActiveStatementTestHelpers;

[UseExportProvider]
public sealed class ActiveStatementsMapTests
{
    [Theory]
    [InlineData(/*span*/ 3, 0, 5, 2,     /*expected*/ 0, 4)]
    [InlineData(/*span*/ 2, 0, 3, 1,     /*expected*/ 0, 1)]
    [InlineData(/*span*/ 19, 1, 19, 100, /*expected*/ 0, 0)]
    [InlineData(/*span*/ 20, 1, 20, 2,   /*expected*/ 0, 0)]
    [InlineData(/*span*/ 0, 0, 100, 0,   /*expected*/ 0, 6)]
    public void GetSpansStartingInSpan1(int sl, int sc, int el, int ec, int s, int e)
    {
        var span = new LinePositionSpan(new(sl, sc), new(el, ec));
        var array = ImmutableArray.Create(
            new LinePositionSpan(new(3, 0), new(3, 1)),
            new LinePositionSpan(new(3, 5), new(3, 6)),
            new LinePositionSpan(new(4, 4), new(4, 18)),
            new LinePositionSpan(new(5, 1), new(5, 2)),
            new LinePositionSpan(new(5, 2), new(5, 8)),
            new LinePositionSpan(new(19, 0), new(19, 42)));

        Assert.Equal(new Range(s, e), ActiveStatementsMap.GetSpansStartingInSpan(span.Start, span.End, array, startPositionComparer: (x, y) => x.Start.CompareTo(y)));
    }

    [Fact]
    public void GetSpansStartingInSpan2()
    {
        var span = TextSpan.FromBounds(8, 11);

        var array = ImmutableArray.Create(
            TextSpan.FromBounds(1, 6), // does not overlap
            TextSpan.FromBounds(3, 9), // overlaps
            TextSpan.FromBounds(4, 5), // does not overlap
            TextSpan.FromBounds(6, 7), // does not overlap
            TextSpan.FromBounds(7, 9), // overlaps
            TextSpan.FromBounds(10, 12), // overlaps
            TextSpan.FromBounds(13, 15)); // does not overlap

        // only one span has start position within the span:
        Assert.Equal(new Range(5, 6), ActiveStatementsMap.GetSpansStartingInSpan(span.Start, span.End, array, startPositionComparer: (x, y) => x.Start.CompareTo(y)));
    }

    [Theory]
    [InlineData(/*span*/ 5, 1, 5, 2,     /*expected*/ 0, 2)]
    [InlineData(/*span*/ 5, 1, 5, 8,     /*expected*/ 0, 2)]
    public void GetSpansStartingInSpan_MultipleSameStart1(int sl, int sc, int el, int ec, int s, int e)
    {
        var span = new LinePositionSpan(new(sl, sc), new(el, ec));
        var array = ImmutableArray.Create(
            new LinePositionSpan(new(5, 1), new(5, 2)),
            new LinePositionSpan(new(5, 1), new(5, 8)),
            new LinePositionSpan(new(6, 4), new(6, 18)));

        Assert.Equal(new Range(s, e), ActiveStatementsMap.GetSpansStartingInSpan(span.Start, span.End, array, startPositionComparer: (x, y) => x.Start.CompareTo(y)));
    }

    [Theory]
    [InlineData(/*span*/ 5, 1, 5, 2,     /*expected*/ 0, 3)]
    public void GetSpansStartingInSpan_MultipleSameStart2(int sl, int sc, int el, int ec, int s, int e)
    {
        var span = new LinePositionSpan(new(sl, sc), new(el, ec));
        var array = ImmutableArray.Create(
            new LinePositionSpan(new(5, 1), new(5, 2)),
            new LinePositionSpan(new(5, 1), new(5, 3)),
            new LinePositionSpan(new(5, 1), new(5, 8)));

        Assert.Equal(new Range(s, e), ActiveStatementsMap.GetSpansStartingInSpan(span.Start, span.End, array, startPositionComparer: (x, y) => x.Start.CompareTo(y)));
    }

    [Fact]
    public async Task Ordering()
    {
        using var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features);

        var source = """

            class C
            {
                void F()
                {
            #line 2 "x"
            S1();
            S2();
            S3();
            #line 1 "x"    
            S0();
            S1();
            S2();
            #line 5 "x"
            S4();
            S5();
            S5();
            #line default
                }
            }
            """;

        var solution = workspace.CurrentSolution
            .AddProject("proj", "proj", LanguageNames.CSharp)
            .AddDocument("doc", SourceText.From(source, Encoding.UTF8), filePath: "a.cs").Project.Solution;

        var project = solution.Projects.Single();
        var document = project.Documents.Single();
        var analyzer = project.Services.GetRequiredService<IEditAndContinueAnalyzer>();

        var documentPathMap = new Dictionary<string, ImmutableArray<ActiveStatement>>();

        var moduleId = Guid.NewGuid();
        var token = 0x06000001;
        ManagedActiveStatementDebugInfo CreateInfo(int startLine, int startColumn, int endLine, int endColumn, string fileName)
            => new(new(new(moduleId, token++, version: 1), ilOffset: 0), fileName, new SourceSpan(startLine, startColumn, endLine, endColumn), ActiveStatementFlags.MethodUpToDate);

        var debugInfos = ImmutableArray.Create(
            CreateInfo(3, 0, 3, 4, "x"),
            CreateInfo(6, 0, 6, 4, "x"),
            CreateInfo(4, 0, 4, 4, "x"),
            CreateInfo(2, 0, 2, 4, "x"),
            CreateInfo(5, 0, 5, 4, "x"),
            CreateInfo(0, 0, 0, 4, "x"),
            CreateInfo(1, 0, 1, 4, "x")
        );

        var map = ActiveStatementsMap.Create(debugInfos, remapping: ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>.Empty);

        var oldSpans = await map.GetOldActiveStatementsAsync(analyzer, document, CancellationToken.None);

        AssertEx.Equal(
        [
            "[48..52) -> (1,0)-(1,4) #6",
            "[55..59) -> (2,0)-(2,4) #3",
            "[62..66) -> (3,0)-(3,4) #0",
            "[86..90) -> (0,0)-(0,4) #5",
            "[120..124) -> (4,0)-(4,4) #2",
            "[127..131) -> (5,0)-(5,4) #4",
            "[134..138) -> (6,0)-(6,4) #1"
        ], oldSpans.Select(s => $"{s.UnmappedSpan} -> {s.Statement.Span} #{s.Statement.Id.Ordinal}"));
    }

    [Fact]
    public async Task InvalidActiveStatements()
    {
        using var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features);

        var source = """

            class C
            {
                void F()
                {
            S1();
                }
            }
            """;

        var solution = workspace.CurrentSolution
            .AddProject("proj", "proj", LanguageNames.CSharp)
            .AddDocument("doc", SourceText.From(source, Encoding.UTF8), filePath: "a.cs").Project.Solution;

        var project = solution.Projects.Single();
        var document = project.Documents.Single();
        var analyzer = project.Services.GetRequiredService<IEditAndContinueAnalyzer>();

        var documentPathMap = new Dictionary<string, ImmutableArray<ActiveStatement>>();

        var moduleId = Guid.NewGuid();
        var token = 0x06000001;
        ManagedActiveStatementDebugInfo CreateInfo(int startLine, int startColumn, int endLine, int endColumn, string fileName)
            => new(new(new(moduleId, token++, version: 1), ilOffset: 0), fileName, new SourceSpan(startLine, startColumn, endLine, endColumn), ActiveStatementFlags.MethodUpToDate);

        // Create a bad active span that is outside the document, but passes the `TryGetTextSpan` check in ActiveStatementMap
        var debugInfos = ImmutableArray.Create(
            CreateInfo(7, 9, 7, 10, "a.cs")
        );

        var map = ActiveStatementsMap.Create(debugInfos, remapping: ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>.Empty);

        var oldSpans = await map.GetOldActiveStatementsAsync(analyzer, document, CancellationToken.None);

        AssertEx.Empty(oldSpans);
    }

    [Fact]
    public void ExpandMultiLineSpan()
    {
        using var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features);

        var source = """

            using System;

            class C
            {
                void F()
                {
                    G(x => x switch
                    {
                        _ => 0,
                    });
                }

                static void G(Func<int, int> a)
                {
                    a(1);
                }
            }
            """;

        var solution = workspace.CurrentSolution
            .AddProject("proj", "proj", LanguageNames.CSharp)
            .AddDocument("doc", SourceText.From(source, Encoding.UTF8), filePath: "a.cs").Project.Solution;

        var project = solution.Projects.Single();
        var document = project.Documents.Single();
        var analyzer = project.Services.GetRequiredService<IEditAndContinueAnalyzer>();

        var documentPathMap = new Dictionary<string, ImmutableArray<ActiveStatement>>();

        var moduleId = Guid.NewGuid();
        var token = 0x06000001;
        ManagedActiveStatementDebugInfo CreateInfo(int startLine, int startColumn, int endLine, int endColumn)
            => new(new(new(moduleId, token++, version: 1), ilOffset: 0), "a.cs", new SourceSpan(startLine, startColumn, endLine, endColumn), ActiveStatementFlags.NonLeafFrame);

        var debugInfos = ImmutableArray.Create(
            CreateInfo(9, 17, 9, 18),                              // 0
            CreateInfo(7, 16, 10, 9),                              // x switch { ... }
            CreateInfo(15, 8, 15, 13)                              // a()
        );

        var remapping = ImmutableDictionary.CreateBuilder<ManagedMethodId, ImmutableArray<NonRemappableRegion>>();

        CreateRegion(0, Span(9, 17, 9, 18), Span(9, 17, 9, 18));    // Current active statement doesn't move
        CreateRegion(1, Span(7, 16, 10, 9), Span(7, 16, 15, 9));    // Insert 5 lines inside the switch
        CreateRegion(2, Span(15, 8, 15, 13), Span(20, 8, 20, 13));  // a() call moves down 5 lines

        var map = ActiveStatementsMap.Create(debugInfos, remapping.ToImmutable());

        AssertEx.Equal(
        [
            "(7,16)-(15,9)",
            "(9,17)-(9,18)",
            "(20,8)-(20,13)"
        ], map.DocumentPathMap["a.cs"].OrderBy(s => s.Span.Start.Line).Select(s => $"{s.Span}"));

        void CreateRegion(int ordinal, SourceFileSpan oldSpan, SourceFileSpan newSpan)
            => remapping.Add(debugInfos[ordinal].ActiveInstruction.Method, [new NonRemappableRegion(oldSpan, newSpan, isExceptionRegion: false)]);

        SourceFileSpan Span(int startLine, int startColumn, int endLine, int endColumn)
            => new("a.cs", new(new(startLine, startColumn), new(endLine, endColumn)));
    }

    [Theory, CombinatorialData]
    public void NonRemappableRegionOrdering(bool reverse)
    {
        var source1 =
            """
            class C
            {
                static void M()
                {
                    try 
                    {
                    }
                    <ER:0.0>catch
                    {



                        <AS:0>M();</AS:0>
                    }</ER:0.0>
                }
            }
            """;
        var unmappedActiveStatements = GetUnmappedActiveStatementsCSharp(
            [source1],
            flags: [ActiveStatementFlags.LeafFrame]);

        var debugInfos = ActiveStatementsDescription.GetActiveStatementDebugInfos(
            unmappedActiveStatements,
            methodRowIds: [1],
            methodVersions: [1],
            ilOffsets: [1]);

        var exceptionRegions = unmappedActiveStatements[0].ExceptionRegions;

        // Emulate move of the catch block by +3 lines while the active statement remains in the same line.
        var mapping1 = new NonRemappableRegion(oldSpan: exceptionRegions.Spans[0], newSpan: exceptionRegions.Spans[0].AddLineDelta(+3), isExceptionRegion: true);
        var mapping2 = new NonRemappableRegion(oldSpan: unmappedActiveStatements[0].Statement.FileSpan, newSpan: unmappedActiveStatements[0].Statement.FileSpan, isExceptionRegion: false);

        // The order should not matter.
        var remapping = ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>.Empty.Add(
            debugInfos[0].ActiveInstruction.Method,
            reverse ? [mapping1, mapping2] : [mapping2, mapping1]);

        var map = ActiveStatementsMap.Create(debugInfos, remapping);

        var activeStatement = map.DocumentPathMap[unmappedActiveStatements[0].Statement.FilePath][0];

        // Span shouldn't be mapped because the only non-remappable region mapping is an exception region, not an active statement:
        Assert.Equal(unmappedActiveStatements[0].Statement.Span, activeStatement.FileSpan.Span);
    }

    [Theory, CombinatorialData]
    public void SubSpan(bool reverse)
    {
        var source1 =
            """
            class C
            {
                static void M()
                {
                    <AS:0>var x = y switch { 1 => 0, _ => <AS:1>1</AS:1> };</AS:0>
                }
            }
            """;
        var unmappedActiveStatements = GetUnmappedActiveStatementsCSharp(
            [source1],
            flags: [ActiveStatementFlags.LeafFrame, ActiveStatementFlags.LeafFrame]);

        var debugInfos = ActiveStatementsDescription.GetActiveStatementDebugInfos(
            unmappedActiveStatements,
            methodRowIds: [1, 1],
            methodVersions: [1, 1],
            ilOffsets: [1, 10]);

        // Emulate move of both active statements by +1 line.
        var mapping1 = new NonRemappableRegion(oldSpan: unmappedActiveStatements[0].Statement.FileSpan, newSpan: unmappedActiveStatements[0].Statement.FileSpan.AddLineDelta(+1), isExceptionRegion: false);
        var mapping2 = new NonRemappableRegion(oldSpan: unmappedActiveStatements[1].Statement.FileSpan, newSpan: unmappedActiveStatements[1].Statement.FileSpan.AddLineDelta(+1), isExceptionRegion: false);

        // The order should not matter.
        var remapping = ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>.Empty.Add(
            debugInfos[0].ActiveInstruction.Method,
            reverse ? [mapping1, mapping2] : [mapping2, mapping1]);

        var map = ActiveStatementsMap.Create(debugInfos, remapping);

        var newActiveStatements = map.DocumentPathMap[unmappedActiveStatements[0].Statement.FilePath];

        // Span shouldn't be mapped because the only non-remappable region mapping is an exception region, not an active statement:
        Assert.Equal(unmappedActiveStatements[0].Statement.Span.AddLineDelta(+1), newActiveStatements[0].FileSpan.Span);
        Assert.Equal(unmappedActiveStatements[1].Statement.Span.AddLineDelta(+1), newActiveStatements[1].FileSpan.Span);
    }
}
